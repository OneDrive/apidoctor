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
        public bool ValidateJson(string json, out ValidationError[] errors, Dictionary<string, JsonSchema> otherSchemas, bool isCollection = false)
        {
            JContainer obj = (JContainer)JsonConvert.DeserializeObject(json);

            List<ValidationError> detectedErrors = new List<ValidationError>();
            List<string> missingProperties = new List<string>(Schema.Keys);

            if (isCollection)
            {
                // We only care about validating the members of the "value" property now
                var returnedObject = obj["value"].First();
                obj = (JContainer)returnedObject;
            }

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

        private static JsonProperty ParseProperty(string name, JToken value)
        {
            switch (value.Type)
            {
                case JTokenType.Boolean:
                    return new JsonProperty { Name = name, Type = JsonDataType.Boolean, OriginalValue = value.ToString() };

                case JTokenType.Float:
                case JTokenType.Integer:
                    return new JsonProperty { Name = name, Type = JsonDataType.Number, OriginalValue = value.ToString() };

                case JTokenType.String:
                    return new JsonProperty { Name = name, Type = JsonDataType.String, OriginalValue = value.ToString() };

                case JTokenType.Object:
                    Console.WriteLine("Ouch");
                    var objectSchema = ObjectToSchema((JObject)value);
                    if (objectSchema.ContainsKey("@odata.type"))
                    {
                        return new JsonProperty { Name = name, Type = JsonDataType.ODataType, ODataTypeName = objectSchema["@odata.type"].OriginalValue };
                    }
                    else
                    {
                        return new JsonProperty { Name = name, Type = JsonDataType.Custom, CustomMembers = ObjectToSchema((JObject)value) };
                    }
                    break;

                case JTokenType.Array:
                    // Array
                    return new JsonProperty { Name = name, Type = JsonDataType.Array, OriginalValue = value.ToString(), CustomMembers = ObjectToSchema((JObject)value.First()) };
                    break;

                default:
                    Console.WriteLine("Unhandled token type: " + value.Type);
                    break;
            }

            return null;
        }

        private static JsonProperty ParseProperty(JToken token)
        {
            JsonProperty propertyInfo = null;
            if (token.Type == JTokenType.Property)
            {
                JProperty tokenProperty = (JProperty)token;
                propertyInfo = ParseProperty(tokenProperty.Name, tokenProperty.Value);
            }
            //else if (token.Type == JTokenType.Object)
            //{
            //    JObject jsonObj = (JObject)token;
            //    foreach (var property in jsonObj)
            //    {
            //        Console.WriteLine(property.Key);
            //    }

            //    var info = ParseProperty(token.Values().First());
            //    if (info.Name == "@odata.type")
            //    {
            //        propertyInfo = new JsonProperty { Name = token.Path, Type = JsonDataType.ODataType, ODataTypeName = info.OriginalValue };
            //    }
            //    else
            //    {
            //        propertyInfo = new JsonProperty { Name = token.Path, Type = JsonDataType.Custom, CustomMembers = TokenToSchema(token) };
            //    }
            //}
            else
            {
                Console.WriteLine("Unhandled token type: " + token.Type);
            }
            return propertyInfo;
        }

        //static Dictionary<string, JsonProperty> TokenToSchema(JToken obj)
        //{
        //    Dictionary<string, JsonProperty> schema = new Dictionary<string, JsonProperty>();
        //    foreach (JToken token in obj.Values())
        //    {
        //        JsonProperty propertyInfo = ParseProperty(token);
        //        schema[propertyInfo.Name] = propertyInfo;
        //    }
        //    return schema;
        //}

        static Dictionary<string, JsonProperty> ObjectToSchema(JObject obj)
        {
            Dictionary<string, JsonProperty> schema = new Dictionary<string, JsonProperty>();
            foreach (var prop in obj)
            {
                string name = prop.Key;
                JToken value = prop.Value;
                JsonProperty propertyInfo = ParseProperty(name, value);
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
