using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace ApiDocumentationTester
{
    public class JsonSchema
    {
        public string ResourceName { get; set; }

        protected Dictionary<string, JsonProperty> Schema { get; private set; }

        public JsonSchema(string json)
        {
            BuildSchemaFromJson(json);
        }

        /// <summary>
        /// Validate the input json against the defined scehma when the instance was created.
        /// </summary>
        /// <param name="json">Input json to validate against schema</param>
        /// <param name="errors">Array of errors if the validation fails</param>
        /// <returns>True if validation was successful, otherwise false.</returns>
        public bool ValidateJson(string json, out ValidationError[] errors, Dictionary<string, JsonSchema> otherSchemas)
        {
            JContainer obj = (JContainer)JsonConvert.DeserializeObject(json);

            List<ValidationError> detectedErrors = new List<ValidationError>();
            List<string> missingProperties = new List<string>(Schema.Keys);

            foreach (JToken token in obj)
            {
                JsonProperty inputProperty = ParseProperty(token);
                missingProperties.Remove(inputProperty.Name);
                var validationResponse = ValidateProperty(inputProperty, otherSchemas, detectedErrors);
                Console.WriteLine("validating {0} = {1}", inputProperty.Name, validationResponse);
            }

            if (missingProperties.Count > 0)
            {
                detectedErrors.Add(new ValidationError { Message = string.Format("JSON was missing these properties: {0}", missingProperties.ComponentsJoinedByString(",")) });
            }

            errors = detectedErrors.ToArray();
            return detectedErrors.Count == 0;
        }

        private PropertyValidationOutcome ValidateProperty(JsonProperty inputProperty, Dictionary<string, JsonSchema> otherSchemas, List<ValidationError> detectedErrors)
        {
            if (Schema.ContainsKey(inputProperty.Name))
            {
                var schemaPropertyDef = Schema[inputProperty.Name];
                if (schemaPropertyDef.Type == inputProperty.Type)
                {
                    // This checks out.
                    return PropertyValidationOutcome.OK;
                }
                else if (schemaPropertyDef.Type == JsonDataType.ODataType && inputProperty.Type == JsonDataType.Custom)
                {
                    // Compare the ODataType schema to the custom schema
                    if (!otherSchemas.ContainsKey(schemaPropertyDef.ODataTypeName))
                    {
                        detectedErrors.Add(new ValidationError { Message = string.Format("Property {1}: Expected OData resource type '{0}' was not found", schemaPropertyDef.ODataTypeName, inputProperty.Name) });
                        return PropertyValidationOutcome.MissingResourceType;
                    }
                    else
                    {
                        var odataSchema = otherSchemas[schemaPropertyDef.ODataTypeName];
                        ValidationError[] odataErrors;
                        if (!odataSchema.ValidateCustomObject(inputProperty.CustomMembers.Values.ToArray(), out odataErrors, otherSchemas))
                        {
                            var errorStrings = from m in odataErrors select m.Message;
                            detectedErrors.Add(new ValidationError { Message = string.Format("Property '{0}' of type '{1}' has errors: {2}", inputProperty.Name, odataSchema.ResourceName, errorStrings.ComponentsJoinedByString(", ")), 
                                InnerErrors = odataErrors });
                            return PropertyValidationOutcome.InvalidType;
                        }
                    }
                }
                else
                {
                    detectedErrors.Add(new ValidationError
                    {
                        Message = string.Format("Property '{0}' of type '{1}' doesn't match schema type of '{2}'",
                        inputProperty.Name, inputProperty.Type, schemaPropertyDef.Type)
                    });
                    return PropertyValidationOutcome.InvalidType;
                }
            }
            else
            {
                detectedErrors.Add(new ValidationError { Message = string.Format("Property '{0}' of type '{1}' was not part of the schema.", inputProperty.Name, inputProperty.Type) });
                return PropertyValidationOutcome.MissingFromSchema;
            }

            return PropertyValidationOutcome.OK;
        }

        private bool ValidateCustomObject(JsonProperty[] properties, out ValidationError[] errors, Dictionary<string, JsonSchema> otherSchemas)
        {
            List<string> missingProperties = new List<string>(Schema.Keys);
            List<ValidationError> detectedErrors = new List<ValidationError>();
            foreach (var inputProperty in properties)
            {
                missingProperties.Remove(inputProperty.Name);
                ValidateProperty(inputProperty, otherSchemas, detectedErrors);
            }

            if (missingProperties.Count > 0)
            {
                detectedErrors.Add(new ValidationError { Message = string.Format("Missing properties: {0}", missingProperties.ComponentsJoinedByString(",")) });
            }

            errors = detectedErrors.ToArray();
            return detectedErrors.Count == 0;
        }

        


        private void BuildSchemaFromJson(string json)
        {
            Dictionary<string, JsonProperty> schema = new Dictionary<string, JsonProperty>();

            JContainer obj = (JContainer)JsonConvert.DeserializeObject(json);
            foreach (JToken token in obj)
            {
                JsonProperty propertyInfo = ParseProperty(token);
                schema[propertyInfo.Name] = propertyInfo;
            }
            Schema = schema;
        }


        private static JsonProperty ParseProperty(JToken token)
        {
            JsonProperty propertyInfo = null;
            if (token.Type == JTokenType.Property)
            {
                string name = ((JProperty)token).Name;
                var firstValue = token.Values().First();
                var propertyType = firstValue.Type;
                string propertyValue = null;

                switch (propertyType)
                {
                    case JTokenType.Boolean:
                        propertyInfo = new JsonProperty { Name = name, Type = JsonDataType.Boolean, OriginalValue = firstValue.ToString() };
                        break;

                    case JTokenType.Float:
                    case JTokenType.Integer:
                        propertyInfo = new JsonProperty { Name = name, Type = JsonDataType.Number, OriginalValue = firstValue.ToString() };
                        break;

                    case JTokenType.String:
                        propertyInfo = new JsonProperty { Name = name, Type = JsonDataType.String, OriginalValue = firstValue.ToString() };
                        break;

                    case JTokenType.Property:
                        // Object
                        var info = ParseProperty(firstValue);
                        if (info.Name == "@odata.type")
                        {
                            propertyInfo = new JsonProperty { Name = name, Type = JsonDataType.ODataType, ODataTypeName = info.OriginalValue };
                        }
                        else
                        {
                            propertyInfo = new JsonProperty { Name = name, Type = JsonDataType.Custom, CustomMembers = TokenToSchema(token) };
                        }
                        break;

                    //case JTokenType.Array:
                    //    // Array
                    //    break;

                    default:
                        Console.WriteLine("Unhandled token type: " + propertyType);
                        break;
                }

                Console.WriteLine(" '{0}' = '{1}' ({2})", name, propertyValue, propertyType);
            }
            else if (token.Type == JTokenType.Object)
            {
                propertyInfo = new JsonProperty { Name = token.Path.Split('.').Last(), Type = JsonDataType.Custom, CustomMembers = TokenToSchema(token) };
            }
            else
            {
                Console.WriteLine("Unhandled token type: " + token.Type);
            }
            return propertyInfo;
        }

        static Dictionary<string, JsonProperty> TokenToSchema(JToken obj)
        {
            Dictionary<string, JsonProperty> schema = new Dictionary<string, JsonProperty>();
            foreach (JToken token in obj.Values())
            {
                JsonProperty propertyInfo = ParseProperty(token);
                schema[propertyInfo.Name] = propertyInfo;
            }
            return schema;
        }

        enum PropertyValidationOutcome
        {
            OK,
            InvalidType,
            MissingFromSchema,
            MissingResourceType
        }
    }
}
