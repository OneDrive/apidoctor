namespace OneDrive.ApiDocumentation.Validation.Json
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class JsonSchema
    {
        #region Properties
        public string ResourceName { get { return Metadata.ResourceType; } }

        protected Dictionary<string, JsonProperty> Schema { get; private set; }

        protected CodeBlockAnnotation Metadata { get; private set; }

        public string[] OptionalProperties { get { return Metadata.OptionalProperties; } }

        #endregion

        #region Constructor
        public JsonSchema(string json, CodeBlockAnnotation annotation)
        {
            Metadata = annotation;
            Schema = BuildSchemaFromJson(json);
        }
        #endregion

        #region Json Validation Against Schema

        /// <summary>
        /// Validate the input json against the defined scehma when the instance was created.
        /// </summary>
        /// <param name="json">Input json to validate against schema</param>
        /// <param name="errors">Array of errors if the validation fails</param>
        /// <returns>True if validation was successful, otherwise false.</returns>
        public bool ValidateJson(string json, out ValidationError[] errors, Dictionary<string, JsonSchema> otherSchemas, CodeBlockAnnotation annotation =null, string collectionPropertyName = "value")
        {
            JContainer obj = null;
            try
            {
                obj = (JContainer)JsonConvert.DeserializeObject(json);
            }
            catch (Exception ex)
            {
                errors = new ValidationError[] { new ValidationError(null, "Failed to parse json string: {0}. Json: {1}", ex.Message, json) };
                return false;
            }

            List<ValidationError> detectedErrors = new List<ValidationError>();
            List<string> missingProperties = new List<string>(Schema.Keys);

            // Check for an error response
            dynamic errorObject = obj["error"];
            if (null != errorObject)
            {
                string code = errorObject.code;
                string message = errorObject.message;
                string odataError = errorObject["@error.details"];

                detectedErrors.Clear();
                detectedErrors.Add(new ValidationError(null, "Error response received. Code: {0}, Message: {1}", code, message));
                errors = detectedErrors.ToArray();
                return false;
            }

            if (null != annotation && annotation.IsCollection)
            {
                // TODO: also validate additional properties on the collection, like nextDataLink

                // We only care about validating the members of the "value" property now
                var collection = obj[collectionPropertyName];
                if (null == collection)
                {
                    detectedErrors.Add(new ValidationError(null, "Failed to location collection property '{0}' in response.", collectionPropertyName));
                }
                else
                {
                    var collectionMembers = obj[collectionPropertyName];
                    if (collectionMembers.Count() == 0)
                    {
                        detectedErrors.Add(new ValidationWarning(null, "Property contained an empty array that was not validated: {0}", collectionPropertyName));
                    }
                    foreach (JContainer container in collectionMembers)
                    {
                        ValidateJContainer(container, annotation, otherSchemas, detectedErrors, missingProperties);
                    }
                }
            }
            else if (null != obj)
            {
                ValidateJContainer(obj, annotation, otherSchemas, detectedErrors, missingProperties);
            }

            errors = detectedErrors.ToArray();
            return detectedErrors.Count == 0;
        }

        private void ValidateJContainer(JContainer obj, CodeBlockAnnotation annotation, Dictionary<string, JsonSchema> otherSchemas, List<ValidationError> detectedErrors, List<string> missingProperties)
        {
            foreach (JToken token in obj)
            {
                JsonProperty inputProperty = ParseProperty(token);
                missingProperties.Remove(inputProperty.Name);
                var validationResponse = ValidateProperty(inputProperty, otherSchemas, detectedErrors, annotation.TruncatedResult);
            }

            if (null != OptionalProperties)
            {
                foreach (var optionalProp in OptionalProperties)
                {
                    missingProperties.Remove(optionalProp);
                }
            }

            if ((annotation == null || !annotation.TruncatedResult) && missingProperties.Count > 0)
            {
                detectedErrors.Add(new ValidationWarning(null, "Missing properties: response was missing these required properties: {0}", missingProperties.ComponentsJoinedByString(",")));
            }
        }

        /// <summary>
        /// Verify that a property from the json-to-validate matches something in our schema
        /// </summary>
        /// <param name="inputProperty"></param>
        /// <param name="otherSchemas"></param>
        /// <param name="detectedErrors"></param>
        /// <returns></returns>
        private PropertyValidationOutcome ValidateProperty(JsonProperty inputProperty, Dictionary<string, JsonSchema> otherSchemas, List<ValidationError> detectedErrors, bool isTruncated = false)
        {
            if (Schema.ContainsKey(inputProperty.Name))
            {
                var schemaPropertyDef = Schema[inputProperty.Name];
                if (schemaPropertyDef.Type == inputProperty.Type)
                {
                    // TODO: For an Array type, we should validate the first child of the array.
                    if (inputProperty.Type == JsonDataType.Array)
                    {
                        detectedErrors.Add(new ValidationWarning(null, "Warning: Object included an array property '{0}' whose children were not validated.", inputProperty.Name));
                    }
                    // This checks out.
                    return PropertyValidationOutcome.OK;
                }
                else if (schemaPropertyDef.Type == JsonDataType.ODataType && inputProperty.Type == JsonDataType.Custom)
                {
                    // Compare the ODataType schema to the custom schema
                    if (!otherSchemas.ContainsKey(schemaPropertyDef.ODataTypeName))
                    {
                        detectedErrors.Add(new ValidationError(null, "Missing resource: resource [0] was not found (property name '{1}').", schemaPropertyDef.ODataTypeName, inputProperty.Name));
                        return PropertyValidationOutcome.MissingResourceType;
                    }
                    else
                    {
                        var odataSchema = otherSchemas[schemaPropertyDef.ODataTypeName];
                        ValidationError[] odataErrors;
                        if (!odataSchema.ValidateCustomObject(inputProperty.CustomMembers.Values.ToArray(), out odataErrors, otherSchemas, isTruncated))
                        {
                            var propertyError = ValidationError.NewConsolidatedError(odataErrors, "Schema validation failed on property '{0}' ['{1}']", inputProperty.Name, odataSchema.ResourceName);
                            detectedErrors.Add(propertyError);

                            return PropertyValidationOutcome.InvalidType;
                        }
                    }
                }
                else
                {
                    detectedErrors.Add(new ValidationError(null, "Type mismatch: property '{0}' [{1}] doesn't match expected type [{2}].", 
                        inputProperty.Name, inputProperty.Type, schemaPropertyDef.Type));
                    return PropertyValidationOutcome.InvalidType;
                }
            }
            else
            {
                detectedErrors.Add(new ValidationWarning(null, "Extra property: property '{0}' [{1}] was not expected.", inputProperty.Name, inputProperty.Type));
                return PropertyValidationOutcome.MissingFromSchema;
            }

            return PropertyValidationOutcome.OK;
        }

        private bool ValidateCustomObject(JsonProperty[] properties, out ValidationError[] errors, Dictionary<string, JsonSchema> otherSchemas, bool ignoreMissingProperties = false)
        {
            List<string> missingProperties = new List<string>(Schema.Keys);
            List<ValidationError> detectedErrors = new List<ValidationError>();
            foreach (var inputProperty in properties)
            {
                missingProperties.Remove(inputProperty.Name);
                ValidateProperty(inputProperty, otherSchemas, detectedErrors);
            }

            if (null != OptionalProperties)
            {
                foreach (var optionalProp in OptionalProperties)
                {
                    missingProperties.Remove(optionalProp);
                }
            }

            if (!ignoreMissingProperties && missingProperties.Count > 0)
            {
                detectedErrors.Add(new ValidationWarning(null, "missing properties detected: {0}", missingProperties.ComponentsJoinedByString(",")));
            }

            errors = detectedErrors.ToArray();
            return detectedErrors.Count == 0;
        }

        #endregion

        #region Schema Building
        private Dictionary<string, JsonProperty> BuildSchemaFromJson(string json)
        {
            Dictionary<string, JsonProperty> schema = new Dictionary<string, JsonProperty>();

            JContainer obj = (JContainer)JsonConvert.DeserializeObject(json);
            foreach (JToken token in obj)
            {
                JsonProperty propertyInfo = ParseProperty(token);
                schema[propertyInfo.Name] = propertyInfo;
            }
            return schema;
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

                case JTokenType.Date:
                    return new JsonProperty { Name = name, Type = JsonDataType.String, OriginalValue = value.ToString() };

                case JTokenType.Object:
                    var objectSchema = ObjectToSchema((JObject)value);
                    if (objectSchema.ContainsKey("@odata.type"))
                    {
                        return new JsonProperty { Name = name, Type = JsonDataType.ODataType, ODataTypeName = objectSchema["@odata.type"].OriginalValue };
                    }
                    else
                    {
                        return new JsonProperty { Name = name, Type = JsonDataType.Custom, CustomMembers = ObjectToSchema((JObject)value) };
                    }

                case JTokenType.Array:
                    // Array
                    return new JsonProperty { Name = name, Type = JsonDataType.Array, OriginalValue = value.ToString(), CustomMembers = ObjectToSchema((JObject)value.First()) };

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

        private static Dictionary<string, JsonProperty> ObjectToSchema(JObject obj)
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
        #endregion

        public static JsonSchema EmptyResponseSchema
        {
            get
            {
                return new JsonSchema("{ }", new CodeBlockAnnotation { BlockType = CodeBlockType.Response, IsCollection = false, MethodName = "EmptyResponse" });
            }
        }
    }
}
