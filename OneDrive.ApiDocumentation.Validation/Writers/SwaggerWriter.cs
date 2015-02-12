using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestPath = System.String;
using HttpMethod = System.String;
using ResourceName = System.String;

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
        public string BaseUrl { get; set; }

        public SwaggerWriter(DocSet docs) : base(docs)
        {
            Title = "OneDrive API";
            Description = "A modern REST API for files and folders";
            Version = "1.0.0";
            ProductionHost = "api.onedrive.com";
            BaseUrl = "/v1.0";
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
                basePath = BaseUrl,
                schemes = new object[] { "https" },
                produces = new object[] { "application/json" },
                consumes = new object[] { "application/json" },
                paths = GeneratePathsFromDocSet(),
                definitions = GenerateResourcesFromDocSet(),
                security = new object[] { new Dictionary<string, object> { { "microsoftAccount", new object[0] } } },
                securityDefinitions = BuildSecurityDefinition()

            };

            string output = Newtonsoft.Json.JsonConvert.SerializeObject(swag, Formatting.Indented);
            using (var outputFile = System.IO.File.CreateText(System.IO.Path.Combine(outputFolder, "swagger.json")))
            {
                outputFile.Write(output);
            }
        }

        private object BuildSecurityDefinition()
        {
            return new {
                microsoftAccount = new { 
                    type = "oauth2",
                    scopes = new Dictionary<string, object> {
                        { "onedrive.readonly", "Grants read-only access to all files in OneDrive" },
                        { "onedrive.readwrite", "Grants read-write access to all files in OneDrive" },
                        { "onedrive.appfolder", "Grants read-write access to files in the application's folder in OneDrive" }
                    },
                    flow = "implicit",
                    authorizationUrl = "https://login.live.com/oauth20_authorize.srf"
                }
            };

        }

        /// <summary>
        /// Generate the swagger-compatible property type based on the internal object model type
        /// </summary>
        /// <returns>The property type.</returns>
        /// <param name="type">Type.</param>
        /// <param name="odataTypeName">Odata type name.</param>

        /// <summary>
        /// Build a set of "schema" for the resources in the doc set
        /// </summary>
        /// <returns>The resources from document set.</returns>
        private object GenerateResourcesFromDocSet()
        {
            var swaggerDefinitions = new Dictionary<string, object>();
            foreach (var jsonSchema in Documents.ResourceCollection.RegisteredSchema)
            {
                var resourceName = jsonSchema.ResourceName.SwaggerResourceName();

                var propertiesForThisResource = new Dictionary<ResourceName, object>();

                foreach (var property in jsonSchema.Properties)
                {
                    propertiesForThisResource.Add(
                        property.Name.SwaggerResourceName(), 
                        property.AsSwaggerProperty()
                    );
                }
                
                var definition = new
                {
                    properties = propertiesForThisResource
                };
                            
                if (!swaggerDefinitions.ContainsKey(resourceName))
                {
                    swaggerDefinitions.Add(resourceName, definition);
                }
                else
                {
                    Console.WriteLine("Found a duplicate resource type: " + resourceName);
                }

            }
            return swaggerDefinitions;
        }



        /// <summary>
        /// Convert our method defintions into the REST paths and methods expected by swagger
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, Dictionary<string, SwaggerMethod>> GeneratePathsFromDocSet()
        {
            Http.HttpParser parser = new Http.HttpParser();

            // "/products" -> "get" -> { SwaggerMethod }
            var swaggerPathObject = new Dictionary<RestPath, Dictionary<HttpMethod, SwaggerMethod>>();

            foreach (var method in Documents.Methods)
            {
                string relativePath, queryString, httpMethod;
                method.SplitRequestUrl(out relativePath, out queryString, out httpMethod);

                // Skip things that look wrong
                if (relativePath.StartsWith("https://"))
                    continue;

                // Create a node for the REST path if it doesn't exist
                if (!swaggerPathObject.ContainsKey(relativePath))
                {
                    swaggerPathObject[relativePath] = new Dictionary<string, SwaggerMethod>();
                }
                var restPathNode = swaggerPathObject[relativePath];

                // Add the HTTP Method to the restPathNode if we need to
                httpMethod = httpMethod.ToLower();
                if (!restPathNode.ContainsKey(httpMethod))
                {
                    restPathNode.Add(httpMethod, method.ToSwaggerMethod());
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Couldn't save repeated method {0} for path {1}", httpMethod, relativePath);
                }
            }
            return swaggerPathObject;
        }

        private SwaggerMethod CreateSwaggerMethodFromRequest(MethodDefinition method, string relativePath, string queryString)
        {
            SwaggerMethod sm = new SwaggerMethod()
            {
                Summary = method.Title,
                Description = method.Description
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

       

    }
}
