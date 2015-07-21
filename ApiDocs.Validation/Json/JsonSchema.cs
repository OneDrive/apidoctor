namespace ApiDocs.Validation.Json
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

        internal CodeBlockAnnotation Metadata { get; private set; }

        public string[] OptionalProperties { get { return (null == Metadata) ? null : Metadata.OptionalProperties; } }

        public string[] NullableProperties { get { return (null == Metadata) ? null : Metadata.NullableProperties; } }

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
        /// Checks that the expected response of a method definition is valid with this resource.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="errors"></param>
        /// <param name="otherSchemas"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public bool ValidateExpectedResponse(MethodDefinition method, out ValidationError[] errors)
        {
            Http.HttpParser parser = new Http.HttpParser();
            var response = parser.ParseHttpResponse(method.ExpectedResponse);
            
            JsonExample example = new JsonExample(response.Body, method.ExpectedResponseMetadata);
            var otherSchemas = new Dictionary<string, JsonSchema>();
            return ValidateJson(example, out errors, otherSchemas, null);
        }

        /// <summary>
        /// Validate the input json against the defined scehma when the instance was created.
        /// </summary>
        /// <param name="json">Input json to validate against schema</param>
        /// <param name="errors">Array of errors if the validation fails</param>
        /// <returns>True if validation was successful, otherwise false.</returns>
        public bool ValidateJson(JsonExample jsonInput, out ValidationError[] errors, Dictionary<string, JsonSchema> otherSchemas, ValidationOptions options, JsonExample expectedJson = null)
        {
            JContainer obj = null;
            try
            {
                var settings = new JsonSerializerSettings { DateParseHandling = Newtonsoft.Json.DateParseHandling.None, NullValueHandling = NullValueHandling.Include, DefaultValueHandling = DefaultValueHandling.Include };
                obj = (JContainer)JsonConvert.DeserializeObject(jsonInput.JsonData, settings);
            }
            catch (Exception ex)
            {
                errors = new ValidationError[] { new ValidationError(ValidationErrorCode.JsonParserException, null, "Failed to parse json string: {0}. Json: {1}", ex.Message, jsonInput.JsonData) };
                return false;
            }

            var annotation = jsonInput.Annotation ?? new CodeBlockAnnotation();

            List<ValidationError> detectedErrors = new List<ValidationError>();

            bool expectErrorObject = (jsonInput.Annotation != null) ? jsonInput.Annotation.ExpectError : false;

            // Check for an error response
            dynamic errorObject = obj["error"];
            if (null != errorObject && !expectErrorObject)
            {
                string code = errorObject.code;
                string message = errorObject.message;

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
                ValidateCollectionObject(obj, annotation, otherSchemas, options.CollectionPropertyName, detectedErrors);
            }
            // otherwise verify the object matches this schema
            else if (null != obj)
            {
                JsonSchema expectedJsonSchema = null; 
                if (null != expectedJson)
                {
                    expectedJsonSchema = new JsonSchema(expectedJson.JsonData, expectedJson.Annotation);
                    options.ExpectedJsonSchema = expectedJsonSchema;
                    options.RequiredPropertyNames = expectedJsonSchema.ExpectedProperties.Keys.ToArray();
                }
                ValidateContainerObject(obj, options, otherSchemas, detectedErrors);
            }

            errors = detectedErrors.ToArray();
            return detectedErrors.Count == 0;
        }

        private void ValidateCollectionObject(JContainer obj, CodeBlockAnnotation annotation, Dictionary<string, JsonSchema> otherSchemas, string collectionPropertyName, List<ValidationError> detectedErrors)
        {
            // TODO: also validate additional properties on the collection, like nextDataLink
            var collection = obj[collectionPropertyName];
            if (null == collection)
            {
                detectedErrors.Add(new ValidationError(ValidationErrorCode.MissingCollectionProperty, null, "Failed to locate collection property '{0}' in response.", collectionPropertyName));
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
                    ValidateContainerObject(container, new ValidationOptions { AllowTruncatedResponses = annotation.TruncatedResult }, otherSchemas, detectedErrors);
                }
            }
        }

        /// <summary>
        /// Verify that a Json container (object) is valid according to it's resource name (schema).
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="resourceType"></param>
        /// <param name="allowTruncation"></param>
        /// <param name="otherSchemas"></param>
        /// <param name="detectedErrors"></param>
        private void ValidateContainerObject(JContainer obj, ValidationOptions options, Dictionary<string, JsonSchema> otherSchemas, List<ValidationError> detectedErrors)
        {
            var containerProperties = from p in obj
                                      select ParseProperty(p, this, detectedErrors);

            ValidateObjectProperties(containerProperties.Where(x => null != x), options, otherSchemas, detectedErrors);
        }

        private void ValidateObjectProperties(IEnumerable<JsonProperty> propertiesOnObject, ValidationOptions options, Dictionary<string, JsonSchema> otherSchemas, List<ValidationError> detectedErrors)
        {
            List<string> missingProperties = new List<string>();
            missingProperties.AddRange(from m in ExpectedProperties select m.Key);

            foreach(var property in propertiesOnObject)
            {
                missingProperties.Remove(property.Name);

                // This detects bad types, extra properties, etc.
                if (null != options && (property.IsArray || property.Type == JsonDataType.ODataType || property.Type == JsonDataType.Object))
                {
                    var propertyOptions = options.CreateForProperty(property.Name);
                    ValidateProperty(property, otherSchemas, detectedErrors, propertyOptions);
                }
                else
                {
                    ValidateProperty(property, otherSchemas, detectedErrors, options);
                }
            }

            CleanMissingProperties(options, missingProperties);
            if (missingProperties.Count > 0)
            {
                detectedErrors.Add(new ValidationError(ValidationErrorCode.RequiredPropertiesMissing, null, "Missing properties: response was missing these required properties: {0}", missingProperties.ComponentsJoinedByString(", ")));
            }
        }

        /// <summary>
        /// Modifies the missingProperties list to remove optional properties 
        /// and handle truncated or required properties.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="missingProperties"></param>
        private void CleanMissingProperties(ValidationOptions options, List<string> missingProperties)
        {
            if (null == options)
                return;

            // Ignore any missing properties that are defined in the schema as optional
            missingProperties.RemoveRange(OptionalProperties);

            if (ValidationConfig.ExpectedResponseAsRequiredProperties &&
                options.AllowTruncatedResponses && null != options.RequiredPropertyNames)
            {
                // Ignore any missing properties that aren't in the required properties list
                missingProperties.IntersectInPlace(options.RequiredPropertyNames);
            }
            else if (options.AllowTruncatedResponses)
            {
                // Ignore all missing properties
                missingProperties.Clear();
            }
        }

        /// <summary>
        /// Verify that a property from the json-to-validate matches something in our schema
        /// </summary>
        /// <param name="inputProperty"></param>
        /// <param name="schemas"></param>
        /// <param name="detectedErrors"></param>
        /// <returns></returns>
        private PropertyValidationOutcome ValidateProperty(JsonProperty inputProperty, Dictionary<string, JsonSchema> schemas, List<ValidationError> detectedErrors, ValidationOptions options)
        {
            if (ExpectedProperties.ContainsKey(inputProperty.Name))
            {
                // The property was expected to be found in this schema! Yay.
                var schemaPropertyDef = ExpectedProperties[inputProperty.Name];
                
                // Check for simple value types first
                if (SimpleValueTypes(schemaPropertyDef.Type, inputProperty.Type) && 
                    AllFalse(schemaPropertyDef.IsArray, inputProperty.IsArray))
                {
                    if (schemaPropertyDef.Type == inputProperty.Type && inputProperty.Type != JsonDataType.String)
                    {
                        return PropertyValidationOutcome.OK;
                    }
                    else if (schemaPropertyDef.Type == inputProperty.Type && inputProperty.Type == JsonDataType.String)
                    {
                        // Perform extra validation to see if the string is the right format (iso date, enum value, url, or just a string)
                        return ValidateStringFormat(schemaPropertyDef, inputProperty, detectedErrors);
                    }
                    else
                    {
                        // Type of the inputProperty is mismatched from the expected value.
                        detectedErrors.Add(new ValidationError(ValidationErrorCode.ExpectedTypeDifferent, null, "Expected type {0} but was instead {1}: {2}", schemaPropertyDef.Type, inputProperty.Type, inputProperty.Name));
                        return PropertyValidationOutcome.InvalidType;
                    }
                }
                else if (null == inputProperty.OriginalValue)
                {
                    if (null != NullableProperties && !NullableProperties.Contains(schemaPropertyDef.Name))
                    {
                        detectedErrors.Add(new ValidationWarning(ValidationErrorCode.NullPropertyValue, null, "Non-nullable property {0} had a null value in the response. Expected {1}.", schemaPropertyDef.Name, schemaPropertyDef.Type));
                    }
                    return PropertyValidationOutcome.OK;
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

                    return ValidateArrayProperty(inputProperty, schemas, detectedErrors, options);
                }
                else if (schemaPropertyDef.Type == JsonDataType.ODataType && (inputProperty.Type == JsonDataType.Object || inputProperty.Type == JsonDataType.ODataType))
                {
                    // Compare the ODataType schema to the custom schema
                    if (!schemas.ContainsKey(schemaPropertyDef.ODataTypeName))
                    {
                        detectedErrors.Add(new ValidationError(ValidationErrorCode.ResourceTypeNotFound, null, "Missing resource: resource {0} was not found (property name '{1}').", schemaPropertyDef.ODataTypeName, inputProperty.Name));
                        return PropertyValidationOutcome.MissingResourceType;
                    }
                    else if (inputProperty.Type == JsonDataType.Object)
                    {
                        var odataSchema = schemas[schemaPropertyDef.ODataTypeName];
                        ValidationError[] odataErrors;
                        if (null != inputProperty.CustomMembers && !odataSchema.ValidateCustomObject(inputProperty.CustomMembers.Values.ToArray(), out odataErrors, schemas, options))
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
                        var odataSchema = schemas[schemaPropertyDef.ODataTypeName];
                        if (inputProperty.CustomMembers == null)
                        {
                            detectedErrors.Add(new ValidationError(ValidationErrorCode.MissingCustomMembers, null, "Property {0} is missing custom members and cannot be validated.", inputProperty.Name));
                            return PropertyValidationOutcome.InvalidType;
                        }
                        else
                        {
                            odataSchema.ValidateObjectProperties(inputProperty.CustomMembers.Values, options, schemas, detectedErrors);
                            return PropertyValidationOutcome.OK;
                        }
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

        private static string[] ISO8601Formats = new string[] { "yyyy-MM-dd", @"HH\:mm\:ss.fff", @"HH\:mm\:ss", @"yyyy-MM-ddTHH\:mm\:ssZ", @"yyyy-MM-ddTHH\:mm\:ss.fffZ", @"yyyy-MM-ddTHH\:mm\:ss.fffffffZ" };

        private static PropertyValidationOutcome ValidateStringFormat(JsonProperty schemaProperty, JsonProperty inputProperty, List<ValidationError> detectedErrorsCollection)
        {
            switch (schemaProperty.StringFormat)
            {
                case ExpectedStringFormat.Iso8601Date:
                    {
                        DateTime output;
                        bool result = (DateTime.TryParseExact(inputProperty.OriginalValue, ISO8601Formats, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.None, out output));
                        if (!result)
                        {
                            detectedErrorsCollection.Add(new ValidationError(ValidationErrorCode.InvalidDateTimeString, null, "Invalid ISO 8601 date-time string in property: {1}: {0}", inputProperty.OriginalValue, schemaProperty.Name));
                            return PropertyValidationOutcome.BadStringValue;
                        }
                        return PropertyValidationOutcome.OK;
                    }
                case ExpectedStringFormat.AbsoluteUrl:
                    {
                        try
                        {
                            Uri parsedUri = new Uri(inputProperty.OriginalValue, UriKind.Absolute);
                            return PropertyValidationOutcome.OK;
                        }
                        catch (FormatException)
                        {
                            detectedErrorsCollection.Add(new ValidationError(ValidationErrorCode.InvalidUrlString, null, "Invalid absolute URL value in property {1}: {0}", inputProperty.OriginalValue, schemaProperty.Name));
                            return PropertyValidationOutcome.BadStringValue;
                        }
                    }
                case ExpectedStringFormat.EnumeratedValue:
                    {
                        if (!schemaProperty.IsValidEnumValue(inputProperty.OriginalValue))
                        {
                            detectedErrorsCollection.Add(new ValidationError(ValidationErrorCode.InvalidEnumeratedValueString, null, "Invalid enumerated value in property {1}: {0}", inputProperty.OriginalValue, schemaProperty.Name));
                            return PropertyValidationOutcome.BadStringValue;
                        }
                        return PropertyValidationOutcome.OK;
                    }
                case ExpectedStringFormat.Generic:
                    return PropertyValidationOutcome.OK;
                default:
                    throw new NotImplementedException();
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
        private PropertyValidationOutcome ValidateArrayProperty(JsonProperty actualProperty, Dictionary<string, JsonSchema> schemas, List<ValidationError> detectedErrors, ValidationOptions options)
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
                    memberSchema.ValidateContainerObject(member, options, schemas, memberErrors);

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

        private bool ValidateCustomObject(JsonProperty[] properties, out ValidationError[] errors, Dictionary<string, JsonSchema> otherSchemas, ValidationOptions options)
        {
            List<string> missingProperties = new List<string>(ExpectedProperties.Keys);
            List<ValidationError> detectedErrors = new List<ValidationError>();
            foreach (var inputProperty in properties)
            {
                missingProperties.Remove(inputProperty.Name);
                ValidateProperty(inputProperty, otherSchemas, detectedErrors, new ValidationOptions());
            }

            CleanMissingProperties(options, missingProperties);

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
                if (obj is JArray)
                {
                    obj = obj.First as JContainer;
                }
                if (null != obj)
                {
                    foreach (JToken token in obj)
                    {
                        JsonProperty propertyInfo = ParseProperty(token, null);
                        AddParameterDataToProperty(parameters, propertyInfo);
                        schema[propertyInfo.Name] = propertyInfo;
                    }
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
                            else
                            {
                                members = new Dictionary<string, JsonProperty>();
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
            GenericError,
            BadStringValue
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
