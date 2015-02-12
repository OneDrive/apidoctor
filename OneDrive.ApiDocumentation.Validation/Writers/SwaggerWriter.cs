using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.Validation.Writers
{
    /// <summary>
    /// Generates a swagger json output for the methods defined in the API documentation
    /// </summary>
    public class SwaggerWriter : DocumentPublisher
    {

        

        public string Title { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public string ProductionHost { get; set; }

        public SwaggerWriter(DocSet docs) : base(docs)
        {
            
        }

        public override async Task PublishToFolderAsync(string outputFolder)
        {
            var swag = new
            {
                swagger = "2.0",
                info = new
                {
                    title = Title,
                    description = Description,
                    version = Version
                },
                host = ProductionHost,
                schemes = new object[] { "https" },
                produces = new object[] { "application/json" },
                consumes = new object[] { "application/json" },
                paths = GeneratePathsFromDocSet(),
                definitions = GenerateResourcesFromDocSet(),
            };

            string output = Newtonsoft.Json.JsonConvert.SerializeObject(swag, Formatting.Indented);
            using (var outputFile = System.IO.File.CreateText(System.IO.Path.Combine(outputFolder, "swagger.json")))
            {
                outputFile.Write(output);
            }
        }

        private object GenerateResourcesFromDocSet()
        {
            var definitions = new Dictionary<string, object>();
            foreach (var jsonSchema in Documents.ResourceCollection.RegisteredSchema)
            {
                var props = new Dictionary<string, object>();
                foreach (var property in jsonSchema.Properties)
                {
                    props.Add(property.Name, new { type = property.Type.ToString().ToLower(), description = "No description available" });
                }
                
                var definition = new
                {
                    properties = props
                };

                definitions.Add(jsonSchema.ResourceName, definition);
            }
            return definitions;
        }

        /// <summary>
        /// Convert our method defintions into the REST paths and methods expected by swagger
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, Dictionary<string, SwaggerMethod>> GeneratePathsFromDocSet()
        {
            Http.HttpParser parser = new Http.HttpParser();

            // "/products" -> "get" -> { SwaggerMethod }
            var paths = new Dictionary<string, Dictionary<string, SwaggerMethod>>();

            Uri baseUri = new Uri("https://example.org");
            foreach (var method in Documents.Methods)
            {
                var request = parser.ParseHttpRequest(method.Request);
                string httpMethod = request.Method;

                int index = request.Url.IndexOf('?');
                string relativePath, queryString;
                if (index == -1)
                {
                    relativePath = request.Url;
                    queryString = null;
                }
                else
                {
                    relativePath = request.Url.Substring(0, index);
                    queryString = request.Url.Substring(index + 1);
                }
                
                if (!paths.ContainsKey(relativePath))
                {
                    paths[relativePath] = new Dictionary<string, SwaggerMethod>();
                }
                Dictionary<string, SwaggerMethod> methodPathDict = paths[relativePath];
                if (!methodPathDict.ContainsKey(httpMethod))
                {
                    methodPathDict.Add(httpMethod, CreateSwaggerMethodFromRequest(method, relativePath, queryString));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Couldn't save repeated method {0} for path {1}", httpMethod, relativePath);
                }
            }
            return paths;
        }

        private SwaggerMethod CreateSwaggerMethodFromRequest(MethodDefinition method, string relativePath, string queryString)
        {
            SwaggerMethod sm = new SwaggerMethod()
            {
                Summary = "TBD summary",
                Description = "TBD description"
            };

            // Add path variables
            var pathVariables = CapturePathVariables(relativePath);
            sm.Parameters.AddRange(from pv in pathVariables select new SwaggerParameter { Name = pv, In = "path", Required = true, Type = "string" });

            // TODO: add query string variables

            // Add responses (right now we're just using a default)
            sm.Responses.Add("default", new SwaggerResponse() { Description = "Default response", Schema = new Dictionary<string,object>{ { "$ref", "#/definitions/" + method.ExpectedResponseMetadata.ResourceType}}});

            return sm;
        }

        private System.Text.RegularExpressions.Regex PathVariableRegex = new System.Text.RegularExpressions.Regex("{(?<var>.*)}");

        private string[] CapturePathVariables(string relativePath)
        {
            var matches = PathVariableRegex.Matches(relativePath);
            List<string> variables = new List<string>();
            for(int i=0; i<matches.Count; i++)
            {
                var match = matches[i];
                var capture = match.Groups["var"].Value;
                variables.Add(capture);
            }
            return variables.ToArray();
        }

        class SwaggerResource
        {
            
        }

        class SwaggerMethod 
        {
            [JsonProperty("summary")]
            public string Summary { get; set; }
            [JsonProperty("description")]
            public string Description { get; set; }
            [JsonProperty("parameters")]
            public List<SwaggerParameter> Parameters { get; set; }
            [JsonProperty("tags")]
            public List<string> Tags { get; set; }

            /// <summary>
            /// Key is either the response status "200" or "default"
            /// </summary>
            [JsonProperty("responses")]
            public Dictionary<string, SwaggerResponse> Responses { get; set; }

            public SwaggerMethod()
            {
                Parameters = new List<SwaggerParameter>();
                Tags = new List<string>();
                Responses = new Dictionary<string, SwaggerResponse>();
            }

        }

        class SwaggerParameter
        {
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("in")]
            public string In { get; set; }
            [JsonProperty("description")]
            public string Description { get; set; }
            [JsonProperty("required")]
            public bool Required { get; set; }
            [JsonProperty("type")]
            public string Type { get; set; }
            [JsonProperty("format")]
            public string Format { get; set; }
        }

        class SwaggerResponse
        {
            [JsonProperty("description")]
            public string Description { get; set; }
            [JsonProperty("schema")]
            public Dictionary<string, object> Schema { get; set; }
        }

    }
}
