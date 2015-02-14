using System;
using ResourceName = System.String;
using System.Collections.Generic;
using System.Linq;
using OneDrive.ApiDocumentation.Validation.Json;

namespace OneDrive.ApiDocumentation.Validation
{
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

        internal static string SwaggerType(this Json.JsonDataType datatype)
        {
            switch (datatype)
            {
                case OneDrive.ApiDocumentation.Validation.Json.JsonDataType.ODataType:
                    throw new ArgumentException();
                case OneDrive.ApiDocumentation.Validation.Json.JsonDataType.Object:
                    return "object";
                default:
                    return datatype.ToString().ToLower();
            }
        }

        private static object SwaggerProperty(JsonDataType type, string odataTypeName)
        {
            object definition = null;
            if (type == JsonDataType.ODataType)
            {
                definition = new Dictionary<string, object> {
                    { "$ref", "#/definitions/" + odataTypeName.SwaggerResourceName() }
                };
            }
            else
            {
                definition = new Dictionary<string, object>
                {
                    { "type", type.SwaggerType() }
                };
            }
            return definition;
        }
            
        internal static object AsSwaggerProperty(this OneDrive.ApiDocumentation.Validation.Json.JsonProperty property)
        {
            return SwaggerProperty(property.Type, property.ODataTypeName);
        }

        internal static object ExpectedResponseAsSwaggerProperty(this OneDrive.ApiDocumentation.Validation.MethodDefinition method)
        {
            if (!string.IsNullOrEmpty(method.ExpectedResponseMetadata.ResourceType))
                return SwaggerProperty(JsonDataType.ODataType, method.ExpectedResponseMetadata.ResourceType);
            else
                return null;
        }

        internal static SwaggerParameter ToSwaggerParameter(this OneDrive.ApiDocumentation.Validation.ParameterDefinition parameter)
        {
            SwaggerParameter p = new SwaggerParameter()
            {
                    Name = parameter.Name,
                    In = parameter.Location.ToString().ToLower(),
                    Required = parameter.Required,
                    Type = parameter.Type.SwaggerType()
            };
            return p;
        }

        /// <summary>
        /// Convert a REST method into a swagger method
        /// </summary>
        /// <returns>The swagger method.</returns>
        /// <param name="method">Method.</param>
        internal static SwaggerMethod ToSwaggerMethod(this MethodDefinition method)
        {
            var output = new SwaggerMethod();
            output.Summary = method.Title;
            if (!string.IsNullOrEmpty(method.Description))
                output.Description = method.Description;


            //method.ParseParameters();

            output.Parameters.AddRange(from p in method.Parameters
                select p.ToSwaggerParameter());
            if (output.Parameters.Count == 0)
                output.Parameters = null;

            output.Tags = null;


            output.Responses.Add("200", new SwaggerResponse()
                {
                    Description = "real response tbd",
                    Schema = method.ExpectedResponseAsSwaggerProperty()
                });
//            output.Responses = null;

            return output;
        }

    }
}

