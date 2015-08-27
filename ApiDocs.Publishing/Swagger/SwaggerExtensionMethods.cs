/*
 * Markdown Scanner
 * Copyright (c) Microsoft Corporation
 * All rights reserved. 
 * 
 * MIT License
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of 
 * this software and associated documentation files (the ""Software""), to deal in 
 * the Software without restriction, including without limitation the rights to use, 
 * copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
 * Software, and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all 
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace ApiDocs.Publishing.Swagger
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ApiDocs.Validation;
    using ApiDocs.Validation.Http;
    using ApiDocs.Validation.Json;
    using ResourceName = System.String;

    public static class SwaggerExtensionMethods
    {
    
        /// <summary>
        /// Swagger doesn't like resource / type names with periods. So we replace them.
        /// </summary>
        /// <returns>The resource name.</returns>
        /// <param name="originalResourceName">Original resource name.</param>
        internal static string SwaggerResourceName(this string originalResourceName)
        {
            if (null == originalResourceName)
                return null;

            return originalResourceName.Replace('.', '_');
        }

        internal static string SwaggerType(this JsonDataType datatype)
        {
            switch (datatype)
            {
                case JsonDataType.ODataType:
                    throw new ArgumentException();
                case JsonDataType.Object:
                    return "object";
                default:
                    return datatype.ToString().ToLower();
            }
        }

        //private static object SwaggerProperty(JsonDataType type, string odataTypeName)
        private static object SwaggerProperty(JsonProperty property)
        {

            JsonDataType type = property.Type;
            string customDataType = property.ODataTypeName;
            string description = property.Description;

            return MakeSwaggerProperty(type, customDataType, description);
        }

        private static object SwaggerPropertyForResponse(MethodDefinition definition)
        {
            JsonDataType type = JsonDataType.ODataType;
            string customDataType = definition.ExpectedResponseMetadata.ResourceType;
            string description = null;
            return MakeSwaggerProperty(type, customDataType, description);
        }

        private static object MakeSwaggerProperty(JsonDataType type, string customDataType, string description)
        {
            Dictionary<string, object> definition = null;
            if (type == JsonDataType.ODataType)
            {
                definition = new Dictionary<string, object> {
                    { "$ref", "#/definitions/" + customDataType.SwaggerResourceName() }
                };
            }
            else
            {
                definition = new Dictionary<string, object>
                {
                    { "type", type.SwaggerType() }
                };
            }
            if (!string.IsNullOrEmpty(description))
            {
                definition.Add("description", description);
            }
            return definition;
        }
            
        internal static object AsSwaggerProperty(this JsonProperty property)
        {
            return SwaggerProperty(property);
        }

        internal static object ExpectedResponseAsSwaggerProperty(this MethodDefinition method)
        {

            string resourceType = null;
            if (null != method.ExpectedResponseMetadata && null != method.ExpectedResponseMetadata.ResourceType)
            {
                resourceType = method.ExpectedResponseMetadata.ResourceType;
            }
            else
            {
                return null;
            }

            if (resourceType.Equals("stream", StringComparison.OrdinalIgnoreCase))
                return null;

            return SwaggerPropertyForResponse(method);
        }

        internal static SwaggerParameter ToSwaggerParameter(this ParameterDefinition parameter)
        {
            SwaggerParameter p = new SwaggerParameter()
            {
                    Name = parameter.Name,
                    Required = parameter.Required,
                    Type = parameter.Type.SwaggerType(),
                    Description = parameter.Description
            };

            //In = parameter.Location.ToString().ToLower(),
            switch (parameter.Location)
            {
                case ParameterLocation.Header:
                    p.In = "header";
                    break;
                case ParameterLocation.Path:
                    p.In = "path";
                    break;
                case ParameterLocation.QueryString:
                    p.In = "query";
                    break;
                case ParameterLocation.JsonObject:
                    p.In = "object";
                    break;
                default:
                    throw new NotSupportedException(string.Format("Unsupported parameter location: {0}", parameter.Location));
            }

            return p;
        }

        /// <summary>
        /// Convert a REST method into a swagger method
        /// </summary>
        /// <returns>The swagger method.</returns>
        /// <param name="method">Method.</param>
        internal static SwaggerMethod ToSwaggerMethod(this MethodDefinition method)
        {
            var output = new SwaggerMethod { Summary = method.Title };

            if (!string.IsNullOrEmpty(method.Description))
                output.Description = method.Description;
            
            output.Parameters.AddRange(from p in method.Parameters select p.ToSwaggerParameter());

            // Make sure any path parameters are parsed since Swagger requires them
            var missingPathParameters = MissingRequestParameters(method);
            output.Parameters.AddRange(from p in missingPathParameters select p.ToSwaggerParameter());

            if (output.Parameters.Count == 0)
                output.Parameters = null;

            output.Tags = null;

            string statusCode;
            var response = method.ToSwaggerResponse(out statusCode);
            output.Responses.Add(statusCode, response);
            
            //output.Responses.Add("200", new SwaggerResponse()
            //    {
            //        Description = "real response tbd",
            //        Schema = method.ExpectedResponseAsSwaggerProperty()
            //    });
//            output.Responses = null;

            return output;
        }

        private static SwaggerResponse ToSwaggerResponse(this MethodDefinition method, out string httpStatusCode)
        {
            HttpParser parser = new HttpParser();
            var response = parser.ParseHttpResponse(method.ExpectedResponse);

            httpStatusCode = response.StatusCode.ToString();

            return new SwaggerResponse { Schema = method.ExpectedResponseAsSwaggerProperty() };
            
        }


   

        internal static IEnumerable<ParameterDefinition> MissingRequestParameters(this MethodDefinition method, bool queryStringOnly = false)
        {
            HttpParser parser = new HttpParser();
            var request = parser.ParseHttpRequest(method.Request);
            string urlString = request.Url;

            string path, queryString;
            urlString.SplitUrlComponents(out path, out queryString);

            List<ParameterDefinition> missingParameters = new List<ParameterDefinition>();
            if (!queryStringOnly)
            {
                missingParameters.AddRange(from id in FindVariablesInString(path)
                                           where !method.Parameters.HasMatchingParameter(id, ParameterLocation.Path)
                                           select new ParameterDefinition { Name = id, Location = ParameterLocation.Path, Required = true, Type = JsonDataType.String });
            }
            if (!string.IsNullOrEmpty(queryString))
            {
                missingParameters.AddRange(from id in FindVariablesInQueryString(queryString)
                                           where !method.Parameters.HasMatchingParameter(id, ParameterLocation.QueryString)
                                           select new ParameterDefinition { Name = id, Location = ParameterLocation.QueryString, Required = false, Type = JsonDataType.String });
            }
            return missingParameters;
        }

        public static IEnumerable<string> FindVariablesInQueryString(string querystring)
        {
            // TODO: Return they query string keys (not the variables that we're using in the URL)
            return new List<string>();
        }

        public static bool HasMatchingParameter(this List<ParameterDefinition> parameters, string name, ParameterLocation loc)
        {
            var existingParameter = (from p in parameters
                                         where p.Name == name && p.Location == loc
                                         select p);
            return existingParameter.FirstOrDefault() != null;
        }

        public static List<ResourceName> FindVariablesInString(string path)
        {
            var matches = ExtensionMethods.PathVariableRegex.Matches(path);
            List<string> variables = new List<string>();
            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var capture = match.Groups["var"].Value;
                variables.Add(capture);
            }
            return variables;
        }






    }
}

