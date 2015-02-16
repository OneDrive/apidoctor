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
        public string BaseUrl { get; set; }

        public SwaggerWriter(DocSet docs) : base(docs)
        {
            Title = "OneDrive API";
            Description = "REST API for OneDrive files and folders";
            Version = "1.0";
            ProductionHost = "api.onedrive.com";
            BaseUrl = "/v1.0";
        }

        public override async Task PublishToFolderAsync(string outputFolder)
        {
            SnapVariables();

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

            foreach (var path in swag.paths)
            {
                Console.WriteLine(path.Key);
            }

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
                if (IsDocFileInternal(jsonSchema.OriginalResource.SourceFile))
                    continue;

                var resourceName = jsonSchema.ResourceName.SwaggerResourceName();

                var propertiesForThisResource = new Dictionary<string, object>();

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

        class PathLengthSorter : IComparer<string>
        {
            public int Compare(string path1, string path2)
            {
                var separators = new char[] { '/', ':' };
                string[] parts1 = path1.FlattenVariableNames().Split(separators);
                string[] parts2 = path2.FlattenVariableNames().Split(separators);

                // URL with less path componentns come first
                int result = 0;
                // otherwise we sort alphabetically by component
                for(int i=0; i<Math.Min(parts1.Length, parts2.Length); i++)
                {
                    if (parts1[i] == "{}" && parts2[i] != "{}")
                        return 1;
                    if (parts1[i] != "{}" && parts2[i] == "{}")
                        return -1;
                    result = parts1[i].CompareTo(parts2[i]);
                    if (result != 0) return result;
                }

                result = parts1.Length.CompareTo(parts2.Length);
                if (result != 0) return result;
                
                return 0;
            }
        }

        /// <summary>
        /// IComparer implementation that orders HTTP methods according to the desired order.
        /// </summary>
        class HttpMethodComparer : IComparer<string>
        {
            public int Compare(string method1, string method2)
            {
                int value1 = ValueOfHttpMethod(method1);
                int value2 = ValueOfHttpMethod(method2);

                if (value1 == value2)
                    return method1.CompareTo(method2);
                else 
                    return value1.CompareTo(value2);
            }

            private static int ValueOfHttpMethod(string method)
            {
                KnownHttpMethods parsedValue;
                if (Enum.TryParse<KnownHttpMethods>(method, true, out parsedValue))
                    return (int)parsedValue;
                
                return (int)KnownHttpMethods.Unknown;
            }

            enum KnownHttpMethods
            {
                Get = 0,
                Put,
                Patch,
                Delete,
                Unknown = 999
            }

        }

        /// <summary>
        /// Convert our method defintions into the REST paths and methods expected by swagger
        /// </summary>
        /// <returns></returns>
        private IDictionary<string, IDictionary<string, SwaggerMethod>> GeneratePathsFromDocSet()
        {
            Http.HttpParser parser = new Http.HttpParser();

            // "/products" -> "get" -> { SwaggerMethod }
            var swaggerPathObject = new SortedDictionary<string, IDictionary<string, SwaggerMethod>>(new PathLengthSorter());
            //var swaggerPathObject = new Dictionary<string, IDictionary<string, SwaggerMethod>>();
            foreach (var method in Documents.Methods)
            {
                if (IsDocFileInternal(method.SourceFile))
                    continue;

                string relativePath, queryString, httpMethod;
                method.SplitRequestUrl(out relativePath, out queryString, out httpMethod);

                // Skip things that look wrong
                if (relativePath.StartsWith("https://"))
                    continue;

                // Create a node for the REST path if it doesn't exist
                string knownPath;
                if (!IsPathKnown(swaggerPathObject, relativePath, out knownPath))
                {
                    swaggerPathObject[relativePath] = new SortedDictionary<string, SwaggerMethod>(new HttpMethodComparer());
                    knownPath = relativePath;
                }
                var restPathNode = swaggerPathObject[knownPath];

                // Add the HTTP Method to the restPathNode if we need to
                httpMethod = httpMethod.ToLower();
                if (!restPathNode.ContainsKey(httpMethod))
                {
                    restPathNode.Add(httpMethod, method.ToSwaggerMethod());
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Couldn't save repeated method {0} for path {1} (from {2}). Copying query parameters.", httpMethod, relativePath, method.SourceFile.DisplayName);
                    var existing = restPathNode[httpMethod];
                    
                    // Make sure any query string parameters on this method are included in the existing definition
                    var missingQueryParameters = method.MissingRequestParameters(true);
                    existing.Parameters.AddRange(from qp in missingQueryParameters select qp.ToSwaggerParameter());
                }
            }
            return swaggerPathObject;
        }

        static bool IsPathKnown(IDictionary<string, IDictionary<string, SwaggerMethod>> knownPaths, string newPath, out string existingPathIfFound)
        {
            string flattenedName = newPath.FlattenVariableNames();

            foreach(var knownPath in knownPaths.Keys)
            {
                if (flattenedName.Equals(knownPath.FlattenVariableNames(), StringComparison.OrdinalIgnoreCase))
                {
                    existingPathIfFound = knownPath;
                    return true;
                }
            }
            existingPathIfFound = null;
            return false;
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
