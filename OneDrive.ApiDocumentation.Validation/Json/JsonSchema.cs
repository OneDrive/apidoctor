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

        protected Dictionary<string, JsonProperty> ExpectedProperties { get; private set; }

        protected CodeBlockAnnotation Metadata { get; private set; }

        public string[] OptionalProperties { get { return (null == Metadata) ? null : Metadata.OptionalProperties; } }

        public ResourceDefinition OriginalResource { get; set; }

        public JsonProperty[] Properties
        {
            get { return ExpectedProperties.Values.ToArray(); }
        }

        #endregion

        #region Constructor
        public JsonSchema(string json, CodeBlockAnnotation annotation)
        {
            Metadata = annotation;
            ExpectedProperties = BuildSchemaFromJson(json);
        }

        public JsonSchema(ResourceDefinition resource)
        {
            Metadata = resource.Metadata;
            ExpectedProperties = BuildSchemaFromJson(resource.JsonExample, resource.Parameters);
            OriginalResource = resource;
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
                errors = new ValidationError[] { new ValidationError(ValidationErrorCode.JsonParserException, null, "Failed to parse json string: {0}. Json: {1}", ex.Message, json) };
                return false;
            }

            List<ValidationError> detectedErrors = new List<ValidationError>();

            bool expectErrorObject = (annotation != null) ? annotation.ExpectError : false;

            // Check for an error response
            dynamic errorObject = obj["error"];
            if (null != errorObject && !expectErrorObject)
            {
                string code = errorObject.code;
                string message = errorObject.message;
//                string odataError = errorObject["@error.details"];

                detectedErrors.Clear();
                detectedErrors.Add(new ValidationError(ValidationErrorCode.JsonErrorObject, null, "Error response received. Code: {0}, Message: {1}", code, message));
                errors = detectedErrors.ToArray();
                return false;
            }
            else if (expectErrorObject && null == errorObject)
            {
                detectedErrors.Clear();
                detectedErrors.Add(new ValidationError(ValidationErrorCode.JsonErrorObjectExpected, null, "Expected an error object response, but didn't receive one."));
                errors = detectedErrors.ToArray();
                return false;

            }

            // Check to see if this is a "collection" instance
            if (null != annotation && annotation.IsCollection)
            {
                // TODO: also validate additional properties on the collection, like nextDataLink

                // We only care about validating the members of the "value" property now
                var collection = obj[collectionPropertyName];
                if (null == collection)
                {
                    detectedErrors.Add(new ValidationError(ValidationErrorCode.MissingCollectionProperty, null, "Failed to location collection property '{0}' in response.", collectionPropertyName));
                }
                else
                {
                    var collectionMembers = obj[collectionPropertyName];
                    if (!collectionMembers.Any())
                    {
                        if (!annotation.IsEmpty)
                        {
                            detectedErrors.Add(
                                new ValidationWarning(
                                    ValidationErrorCode.CollectionArrayEmpty,
                                    null,
                                    "Property contained an empty array that was not validated: {0}",
                                    collectionPropertyName));
                        }
                    }
                    else if (annotation.IsEmpty)
                    {
                        detectedErrors.Add(
                            new ValidationWarning(
                                ValidationErrorCode.CollectionArrayNotEmpty,
                                null,
                                "Property contained a non-empty array that was expected to be empty: {0}",
                                collectionPropertyName));
                    }

                    foreach (JContainer container in collectionMembers)
                    {
                        ValidateJContainer(container, annotation, otherSchemas, detectedErrors);
                    }
                }
            }
            else if (null != obj)
            {
                ValidateJContainer(obj, annotation, otherSchemas, detectedErrors);
            }

            errors = detectedErrors.ToArray();
            return detectedErrors.Count == 0;
        }

        private void ValidateJContainer(JContainer obj, CodeBlockAnnotation annotation, Dictionary<string, JsonSchema> otherSchemas, List<ValidationError> detectedErrors)
        {
            bool allowTruncatedResult = false;
            if (null != annotation)
            {
                if (annotation.ResourceType != ResourceName)
                {
                    throw new InvalidOperationException("Attempting to verify a container with a different resource type than the current schema.");
                }
                allowTruncatedResult = annotation.TruncatedResult;
            }
            ValidateJContainer(obj, allowTruncatedResult, otherSchemas, detectedErrors);
        }

        /// <summary>
        /// Verify that a Json container (object) is valid according to it's resource name (schema).
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="resourceType"></param>
        /// <param name="allowTruncation"></param>
        /// <param name="otherSchemas"></param>
        /// <param name="detectedErrors"></param>
        private void ValidateJContainer(JContainer obj, bool allowTruncation, Dictionary<string, JsonSchema> otherSchemas, List<ValidationError> detectedErrors)
        {
            List<string> missingProperties = new List<string>();
            missingProperties.AddRange(from m in ExpectedProperties select m.Key);

            foreach (JToken token in obj)
            {
                JsonProperty inputProperty = ParseProperty(token, this, detectedErrors);
                if (inputProperty != null)
                {
                    missingProperties.Remove(inputProperty.Name);
                    // This detects bad types, extra properties, etc.
                    ValidateProperty(inputProperty, otherSchemas, detectedErrors, allowTruncation);
                }
            }

            if (null != OptionalProperties)
            {
                foreach (var optionalProp in OptionalProperties)
                {
                    missingProperties.Remove(optionalProp);
                }
            }

            if (!allowTruncation && missingProperties.Count > 0)
            {
                detectedErrors.Add(new ValidationWarning(ValidationErrorCode.RequiredPropertiesMissing, null, "Missing properties: response was missing these required properties: {0}", missingProperties.ComponentsJoinedByString(", ")));
            }
        }

        /// <summary>
        /// Verify that a property from the json-to-validate matches something in our schema
        /// </summary>
        /// <param name="inputProperty"></param>
        /// <param name="schemas"></param>
        /// <param name="detectedErrors"></param>
        /// <returns></returns>
        private PropertyValidationOutcome ValidateProperty(JsonProperty inputProperty, Dictionary<string, JsonSchema> schemas, List<ValidationError> detectedErrors, bool isTruncated = false)
        {
            if (ExpectedProperties.ContainsKey(inputProperty.Name))
            {
                // The property was expected to be found in this schema! Yay.
                var schemaPropertyDef = ExpectedProperties[inputProperty.Name];
                
                // Check for simple value types first
                if (SimpleValueTypes(schemaPropertyDef.Type, inputProperty.Type) && 
                    AllFalse(schemaPropertyDef.IsArray, inputProperty.IsArray))
                {
                    if (schemaPropertyDef.Type == inputProperty.Type)
                    {
                        return PropertyValidationOutcome.OK;
                    }
                    else
                    {
                        // Type of the inputProperty is mismatched from the expected value.
                        detectedErrors.Add(new ValidationError(ValidationErrorCode.ExpectedTypeDifferent, null, "Expected type {0} but was instead {1}: {2}", schemaPropertyDef.Type, inputProperty.Type, inputProperty.Name));
                        return PropertyValidationOutcome.InvalidType;
                    }
                }
                else if (schemaPropertyDef.IsArray || inputProperty.IsArray)
                {
                    // Check for an array
                    if (schemaPropertyDef.IsArray && !inputProperty.IsArray)
                    {
                        // Expected an array, but didn't get one
                        detectedErrors.Add(new ValidationError(ValidationErrorCode.ExpectedArrayValue, null, "Expected an array but property was not an array: {0}", inputProperty.Name));
                        return PropertyValidationOutcome.InvalidType;
                    }
                    else if (!schemaPropertyDef.IsArray && inputProperty.IsArray)
                    {
                        detectedErrors.Add(new ValidationError(ValidationErrorCode.ExpectedNonArrayValue, null, "Expected a value of type {0} but property was an array: {1}", schemaPropertyDef.Type, inputProperty.Name));
                        return PropertyValidationOutcome.InvalidType;
                    }


                    return ValidateArrayProperty(inputProperty, schemas, detectedErrors, isTruncated);
                }
                else if (schemaPropertyDef.Type == JsonDataType.ODataType && (inputProperty.Type == JsonDataType.Object || inputProperty.Type == JsonDataType.ODataType))
                {
                    // Compare the ODataType schema to the custom schema
                    if (!schemas.ContainsKey(schemaPropertyDef.ODataTypeName))
                    {
                        detectedErrors.Add(new ValidationError(ValidationErrorCode.ResourceTypeNotFound, null, "Missing resource: resource [0] was not found (property name '{1}').", schemaPropertyDef.ODataTypeName, inputProperty.Name));
                        return PropertyValidationOutcome.MissingResourceType;
                    }
                    else if (inputProperty.Type == JsonDataType.Object)
                    {
                        var odataSchema = schemas[schemaPropertyDef.ODataTypeName];
                        ValidationError[] odataErrors;
                        if (null != inputProperty.CustomMembers && !odataSchema.ValidateCustomObject(inputProperty.CustomMembers.Values.ToArray(), out odataErrors, schemas, isTruncated))
                        {
                            var propertyError = ValidationError.NewConsolidatedError(ValidationErrorCode.ConsolidatedError, odataErrors, "Schema validation failed on property '{0}' ['{1}']", inputProperty.Name, odataSchema.ResourceName);
                            detectedErrors.Add(propertyError);

                            return PropertyValidationOutcome.InvalidType;
                        }
                        else if (null == inputProperty.CustomMembers)
                        {
                            detectedErrors.Add(new ValidationError(ValidationErrorCode.NoCustomMembersFound, null, "Property '{0}' is of type Custom but has no custom members.", inputProperty.Name));
                        }
                        return PropertyValidationOutcome.OK;
                    }
                    else
                    {
//                        // TODO: Verify that the property value matches the resource schema
//                        var odataSchema = schemas[schemaPropertyDef.ODataTypeName];
//                        odataSchema.ValidateJContainer(
//
//
                        return PropertyValidationOutcome.OK;
                    }
                }
                else if (schemaPropertyDef.Type == JsonDataType.Object)
                {
                    detectedErrors.Add(new ValidationWarning(ValidationErrorCode.CustomValidationNotSupported, null, "Schema type was 'Custom' which is not supported. Add a resource type to the definition of property: {0}", inputProperty.Name));
                    return PropertyValidationOutcome.MissingResourceType;
                }
                else
                {
                    detectedErrors.Add(new ValidationError(ValidationErrorCode.ExpectedTypeDifferent, null, "Type mismatch: property '{0}' [{1}] doesn't match expected type [{2}].", 
                        inputProperty.Name, inputProperty.Type, schemaPropertyDef.Type));
                    return PropertyValidationOutcome.InvalidType;
                }
            }
            else
            {
                detectedErrors.Add(new ValidationWarning(ValidationErrorCode.AdditionalPropertyDetected, null, "Extra property: property '{0}' [{1}] was not expected.", inputProperty.Name, inputProperty.Type));
                return PropertyValidationOutcome.MissingFromSchema;
            }
        }

        private bool SimpleValueTypes(params JsonDataType[] types)
        {
            return types.All(type => type != JsonDataType.ODataType && type != JsonDataType.Object);
        }

        private bool AllFalse(params bool[] input)
        {
            return input.All(value => !value);
        }

        private PropertyValidationOutcome ValidateSimpleArrayProperty(JsonProperty actualProperty, JsonProperty expectedProperty, List<ValidationError> detectedErrors)
        {
            if (actualProperty.IsArray != true || expectedProperty.IsArray != true)
            {
                throw new SchemaBuildException("Cannot use simple array valiation without array types", null);
            }

            if (actualProperty.Type == expectedProperty.Type && expectedProperty.Type != JsonDataType.Object && expectedProperty.Type != JsonDataType.ODataType)
            {
                return PropertyValidationOutcome.OK;
            }
            else if (expectedProperty.IsArray && actualProperty.IsArray && actualProperty.OriginalValue == "[]")
            {
                return PropertyValidationOutcome.OK;
            }
            else
            {
                detectedErrors.Add(new ValidationError(ValidationErrorCode.ArrayTypeMismatch, null, "Array expected members to be of type {0} but found: {1}", expectedProperty.Type, actualProperty.Type));
                return PropertyValidationOutcome.InvalidType;
            }
        }

        /// <summary>
        /// Check each member of the actualProperty's array to make sure it matches the resource type specified for the property.
        /// </summary>
        /// <param name="inputProperty"></param>
        /// <param name="schemaPropertyDef"></param>
        /// <param name="otherSchemas"></param>
        /// <param name="detectedErrors"></param>
        private PropertyValidationOutcome ValidateArrayProperty(JsonProperty actualProperty, Dictionary<string, JsonSchema> schemas, List<ValidationError> detectedErrors, bool allowTruncatedResponse)
        {
            JArray actualArray = (JArray)JsonConvert.DeserializeObject(actualProperty.OriginalValue);

            JsonSchema memberSchema;
            if (string.IsNullOrEmpty(actualProperty.ODataTypeName))
            {
                return ValidateSimpleArrayProperty(actualProperty, this.ExpectedProperties[actualProperty.Name], detectedErrors);
            }
            else if (!schemas.TryGetValue(actualProperty.ODataTypeName, out memberSchema))
            {
                detectedErrors.Add(new ValidationError(ValidationErrorCode.ResourceTypeNotFound, null, "Failed to locate resource definition for: {0}", actualProperty.ODataTypeName));
                return PropertyValidationOutcome.MissingResourceType;
            }

            bool hadErrors = false;
            for(int i=0; i<actualArray.Count; i++)
            {
                JContainer member = actualArray[i] as JContainer;
                if (member != null)
                {
                    List<ValidationError> memberErrors = new List<ValidationError>();
                    memberSchema.ValidateJContainer(member, allowTruncatedResponse, schemas, memberErrors);

                    hadErrors |= memberErrors.Count > 0;
                    foreach (var error in memberErrors)
                    {
                        error.Source = string.Format("{0} [{1}]", actualProperty.Name, i);
                        detectedErrors.Add(error);
                    }
                }
            }

            return hadErrors ? PropertyValidationOutcome.GenericError : PropertyValidationOutcome.OK;
        }

        private bool ValidateCustomObject(JsonProperty[] properties, out ValidationError[] errors, Dictionary<string, JsonSchema> otherSchemas, bool ignoreMissingProperties = false)
        {
            List<string> missingProperties = new List<string>(ExpectedProperties.Keys);
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
                detectedErrors.Add(new ValidationWarning(ValidationErrorCode.RequiredPropertiesMissing, null, "missing properties detected: {0}", missingProperties.ComponentsJoinedByString(",")));
            }

            errors = detectedErrors.ToArray();
            return detectedErrors.Count == 0;
        }

        #endregion

        #region Schema Building
        private Dictionary<string, JsonProperty> BuildSchemaFromJson(string json, IEnumerable<ParameterDefinition> parameters = null)
        {
            Dictionary<string, JsonProperty> schema = new Dictionary<string, JsonProperty>();
            try
            {
                JContainer obj = (JContainer)JsonConvert.DeserializeObject(json);
                foreach (JToken token in obj)
                {
                    JsonProperty propertyInfo = ParseProperty(token, null);
                    AddParameterDataToProperty(parameters, propertyInfo);
                    schema[propertyInfo.Name] = propertyInfo;
                }
            }
            catch (Exception ex)
            {
                throw new SchemaBuildException(ex.Message, ex);
            }
            return schema;
        }

        private static void AddParameterDataToProperty(IEnumerable<ParameterDefinition> parameters, JsonProperty propertyInfo)
        {
            if (null != propertyInfo && null != parameters)
            {
                // See if we can look up more data for this new property
                var findParameterQuery = from p in parameters
                                         where p.Name == propertyInfo.Name
                                         select p;
                var parameterData = findParameterQuery.FirstOrDefault();
                if (null != parameterData)
                {
                    propertyInfo.Description = parameterData.Description;
                }
            }
        }

        private static JsonProperty ParseProperty(string name, JToken value, JsonSchema containerSchema)
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
                    {
                        var objectSchema = ObjectToSchema((JObject)value);
                        if (objectSchema.ContainsKey("@odata.type"))
                        {
                            return new JsonProperty { Name = name, Type = JsonDataType.ODataType, ODataTypeName = objectSchema["@odata.type"].OriginalValue };
                        }
                        else
                        {
                            // See if we can infer type from the parent scehma
                            JsonProperty schemaProperty;
                            JsonDataType propertyType = JsonDataType.Object;
                            string odataTypeName = null;
                            if (null != containerSchema && containerSchema.ExpectedProperties.TryGetValue(name, out schemaProperty))
                            {
                                odataTypeName = schemaProperty.ODataTypeName;
                                propertyType = schemaProperty.Type;
                            }
                            return new JsonProperty { Name = name, Type = propertyType, ODataTypeName = odataTypeName, CustomMembers = ObjectToSchema((JObject)value) };
                        }
                    }

                case JTokenType.Array:
                    {
                        // Array
                        JsonProperty schemaProperty;
                        JsonDataType propertyType = JsonDataType.Array;
                        string odataTypeName = null;

                        // Infer type from the items in the array
                        var firstChild = value.First;
                        if (null != firstChild)
                        {
                            var objectType = ParseProperty("array[0]", firstChild, null);
                            if (null != objectType)
                            {
                                odataTypeName = objectType.ODataTypeName;
                                propertyType = objectType.Type;
                            }
                        }
                        else
                        {
                            propertyType = JsonDataType.Array;
                            odataTypeName = null;
                        }

                        // See if we can do better than just Custom
                        if (propertyType == JsonDataType.Object)
                        {
                            if (null != containerSchema && containerSchema.ExpectedProperties.TryGetValue(name, out schemaProperty))
                            {
                                // Use the parent schema's type indication
                                odataTypeName = schemaProperty.ODataTypeName;
                                propertyType = schemaProperty.Type;
                            }
                        }

                        Dictionary<string, JsonProperty> members = null;
                        if (propertyType == JsonDataType.Object || propertyType == JsonDataType.Array)
                        {
                            var firstValue = (JObject)value.First;
                            if (firstValue != null)
                            {
                                members = ObjectToSchema(firstValue);
                            }
                        }

                        return new JsonProperty { Name = name, Type = propertyType, ODataTypeName = odataTypeName, IsArray = true,
                            OriginalValue = value.ToString(), CustomMembers = members };
                    }
                case JTokenType.Null:
                    return new JsonProperty { Name = name, Type = JsonDataType.Object, IsArray = false, OriginalValue = null };
                default:
                    Console.WriteLine("Unsupported: Property {0} is of type {1} which is not currently supported.", name, value.Type);
                    throw new NotSupportedException(string.Format("Unsupported: Property {0} is of type {1} which is not currently supported.", name, value.Type));
            }

            return null;
        }

        private static JsonProperty ParseProperty(JToken token, JsonSchema containerSchema, List<ValidationError> detectedErrors = null)
        {
            JsonProperty propertyInfo = null;
            if (token.Type == JTokenType.Property)
            {
                JProperty tokenProperty = (JProperty)token;
                propertyInfo = ParseProperty(tokenProperty.Name, tokenProperty.Value, containerSchema);
            }
            else
            {
                if (detectedErrors != null)
                {
                    detectedErrors.Add(
                        new ValidationWarning(
                            ValidationErrorCode.JsonParserException,
                            token.Path,
                            "Unhandled token type: " + token.Type));
                }
                else
                {
                    Console.WriteLine("Unhandled token type: " + token.Type);
                }
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
                JsonProperty propertyInfo = ParseProperty(name, value, null);
                if (null != propertyInfo && null != propertyInfo.Name)
                    schema[propertyInfo.Name] = propertyInfo;
            }
            return schema;
        }

        enum PropertyValidationOutcome
        {
            OK,
            InvalidType,
            MissingFromSchema,
            MissingResourceType,
            GenericError
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
