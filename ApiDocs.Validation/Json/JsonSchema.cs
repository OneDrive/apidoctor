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

namespace ApiDocs.Validation.Json
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using ApiDocs.Validation.Error;
    using ApiDocs.Validation.Http;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class JsonSchema
    {
        #region Properties
        public string ResourceName { get { return this.Metadata?.ResourceType; } }

        protected Dictionary<string, ParameterDefinition> ExpectedProperties { get; private set; }

        internal CodeBlockAnnotation Metadata { get; private set; }

        public string[] OptionalProperties { get { return this.Metadata?.OptionalProperties; } }

        public string[] NullableProperties { get { return this.Metadata?.NullableProperties; } }

        public ResourceDefinition OriginalResource { get; set; }

        public ParameterDefinition[] Properties
        {
            get { return this.ExpectedProperties.Values.ToArray(); }
        }

        #endregion

        #region Constructor
        public JsonSchema(string json, CodeBlockAnnotation annotation)
        {
            this.Metadata = annotation;
            this.ExpectedProperties = this.BuildSchemaFromJson(json);
        }

        public JsonSchema(ResourceDefinition resource)
        {
            this.Metadata = resource.OriginalMetadata;
            this.ExpectedProperties = this.BuildSchemaFromJson(resource.ExampleText, resource.Parameters);
            this.OriginalResource = resource;
        }
        #endregion

        #region Json Validation Against Schemas

        /// <summary>
        /// Checks that the expected response of a method definition is valid with this resource.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public bool ValidateExpectedResponse(MethodDefinition method, out ValidationError[] errors)
        {
            HttpParser parser = new HttpParser();
            var response = parser.ParseHttpResponse(method.ExpectedResponse);
            
            JsonExample example = new JsonExample(response.Body, method.ExpectedResponseMetadata);
            var otherSchemas = new Dictionary<string, JsonSchema>();
            return this.ValidateJson(example, out errors, otherSchemas, null);
        }

        /// <summary>
        /// Validate the input json against the defined scehma when the instance was created.
        /// </summary>
        /// <param name="jsonInput">Input json to validate against schema</param>
        /// <param name="errors">Array of errors if the validation fails</param>
        /// <param name="otherSchemas"></param>
        /// <param name="options"></param>
        /// <param name="expectedJson"></param>
        /// <returns>True if validation was successful, otherwise false.</returns>
        public bool ValidateJson(JsonExample jsonInput, out ValidationError[] errors, Dictionary<string, JsonSchema> otherSchemas, ValidationOptions options, JsonExample expectedJson = null)
        {
            JContainer obj;
            try
            {
                var settings = new JsonSerializerSettings { DateParseHandling = DateParseHandling.None, NullValueHandling = NullValueHandling.Include, DefaultValueHandling = DefaultValueHandling.Include };
                obj = (JContainer)JsonConvert.DeserializeObject(jsonInput.JsonData, settings);
            }
            catch (Exception ex)
            {
                errors = new ValidationError[] { new ValidationError(ValidationErrorCode.JsonParserException, null, "Failed to parse json string: {0}. Json: {1}", ex.Message, jsonInput.JsonData) };
                return false;
            }

            var annotation = jsonInput.Annotation ?? new CodeBlockAnnotation();

            List<ValidationError> detectedErrors = new List<ValidationError>();

            bool expectErrorObject = (jsonInput.Annotation != null) && jsonInput.Annotation.ExpectError;

            // Check for an error response
            try
            {
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
            }
            catch (Exception ex)
            {
                if (annotation.ExpectError)
                {
                    detectedErrors.Add(new ValidationError(ValidationErrorCode.JsonErrorObjectExpected, null, $"Expected an error object, but it doesn't look like we got one: {ex.Message}"));
                }
            }

            // Check to see if this is a "collection" instance
            if (null != annotation && annotation.IsCollection)
            {
                this.ValidateCollectionObject(obj, annotation, otherSchemas, options.CollectionPropertyName, detectedErrors, options);
            }
            // otherwise verify the object matches this schema
            else
            {
                options = options ?? new ValidationOptions(annotation);
                if (null != expectedJson)
                {
                    var expectedJsonSchema = new JsonSchema(expectedJson.JsonData, expectedJson.Annotation);
                    options.ExpectedJsonSchema = expectedJsonSchema;
                    options.RequiredPropertyNames = expectedJsonSchema.ExpectedProperties.Keys.ToArray();
                }
                this.ValidateContainerObject(obj, options, otherSchemas, detectedErrors);
            }

            errors = detectedErrors.ToArray();
            return detectedErrors.Count == 0;
        }

        private void ValidateCollectionObject(JContainer obj, CodeBlockAnnotation annotation, Dictionary<string, JsonSchema> otherSchemas, string collectionPropertyName, List<ValidationError> detectedErrors, ValidationOptions options)
        {
            // TODO: also validate additional properties on the collection, like nextDataLink
            JToken collection = null;
            if (obj is JArray)
            {
                collection = obj;
            }
            else
            {
                try
                {
                    collection = obj[collectionPropertyName];
                }
                catch (Exception ex)
                {
                    detectedErrors.Add(new ValidationError(ValidationErrorCode.JsonErrorObjectExpected, null, $"Unable to find collection parameter or array to validate: {ex.Message}"));
                    return;
                }
            }

            if (null == collection)
            {
                detectedErrors.Add(new ValidationError(ValidationErrorCode.MissingCollectionProperty, null, "Failed to locate collection property '{0}' in response.", collectionPropertyName));
            }
            else
            {
                if (!collection.Any())
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

                foreach (var jToken in collection)
                {
                    var container = jToken as JContainer;
                    if (null != container)
                    {
                        List<ValidationError> containerErrors = new List<ValidationError>();

                        var deeperOptions = new ValidationOptions(options)
                        {
                            AllowTruncatedResponses = annotation.TruncatedResult
                        };

                        this.ValidateContainerObject(
                            container,
                            deeperOptions,
                            otherSchemas,
                            containerErrors);

                        detectedErrors.AddUniqueErrors(containerErrors);
                    }
                }
            }
        }

        /// <summary>
        /// Verify that a Json container (object) is valid according to it's resource name (schema).
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="options"></param>
        /// <param name="otherSchemas"></param>
        /// <param name="detectedErrors"></param>
        private void ValidateContainerObject(JContainer obj, ValidationOptions options, Dictionary<string, JsonSchema> otherSchemas, List<ValidationError> detectedErrors)
        {
            var containerProperties = from p in obj
                                      select ParseProperty(p, this, detectedErrors);

            this.ValidateObjectProperties(containerProperties.Where(x => null != x), options, otherSchemas, detectedErrors);
        }

        private void ValidateObjectProperties(IEnumerable<ParameterDefinition> propertiesOnObject, ValidationOptions options, Dictionary<string, JsonSchema> otherSchemas, List<ValidationError> detectedErrors)
        {
            List<string> missingProperties = new List<string>();
            missingProperties.AddRange(from m in this.ExpectedProperties select m.Key);

            foreach(var property in propertiesOnObject)
            {
                missingProperties.Remove(property.Name);

                // This detects bad types, extra properties, etc.
                if (null != options && (property.Type.IsCollection || property.Type.IsObject))
                {
                    var propertyOptions = options.CreateForProperty(property.Name);
                    this.ValidateProperty(property, otherSchemas, detectedErrors, propertyOptions);
                }
                else
                {
                    this.ValidateProperty(property, otherSchemas, detectedErrors, options);
                }
            }

            this.CleanMissingProperties(options, missingProperties);
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
            missingProperties.RemoveRange(this.OptionalProperties);

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

        private static PropertyValidationOutcome ValidateSameDataType(ParameterDefinition expected, ParameterDefinition actual, List<ValidationError> detectedErrors, bool relaxStringValidation)
        {
            if (expected.Type == actual.Type && actual.Type != ParameterDataType.String)
            {
                return PropertyValidationOutcome.Ok;
            }
            else if (expected.Type == actual.Type && actual.Type == ParameterDataType.String)
            {
                // Perform extra validation to see if the string is the right format (iso date, enum value, url, or just a string)
                if (relaxStringValidation)
                {
                    return PropertyValidationOutcome.Ok;
                }
                return ValidateStringFormat(expected, actual, detectedErrors);
            }
            else if (expected.Type == ParameterDataType.String && expected.Type.IsLessSpecificThan(actual.Type))
            {
                return PropertyValidationOutcome.Ok;
            }
            else if (actual.Type.IsLessSpecificThan(expected.Type))
            {
                if (relaxStringValidation)
                {
                    detectedErrors.Add(
                        new ValidationWarning(
                            ValidationErrorCode.ExpectedTypeDifferent,
                            null,
                            "Expected type {0} but actual was {1}, which is less specific than the expected type. Property: {2}, actual value: '{3}'",
                            expected.Type,
                            actual.Type,
                            actual.Name,
                            actual.OriginalValue));
                    return PropertyValidationOutcome.Ok;
                }

                detectedErrors.Add(
                    new ValidationError(
                        ValidationErrorCode.ExpectedTypeDifferent,
                        null,
                        "Expected type {0} but actual was {1}, which is less specific than the expected type. Property: {2}, actual value: '{3}'",
                        expected.Type,
                        actual.Type,
                        actual.Name,
                        actual.OriginalValue));
                return PropertyValidationOutcome.BadStringValue;
            }
            else
            {
                // Type of the inputProperty is mismatched from the expected value.
                detectedErrors.Add(
                    new ValidationError(
                        ValidationErrorCode.ExpectedTypeDifferent,
                        null,
                        "Expected type {0} but was instead {1}. Property: {2}, actual value: '{3}'",
                        expected.Type,
                        actual.Type,
                        actual.Name,
                        actual.OriginalValue));
                return PropertyValidationOutcome.InvalidType;
            }
        }

        /// <summary>
        /// Verify that a property from the json-to-validate matches something in our schema
        /// </summary>
        /// <param name="inputProperty"></param>
        /// <param name="schemas"></param>
        /// <param name="detectedErrors"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private PropertyValidationOutcome ValidateProperty(ParameterDefinition inputProperty, Dictionary<string, JsonSchema> schemas, List<ValidationError> detectedErrors, ValidationOptions options)
        {
            if (this.ExpectedProperties.ContainsKey(inputProperty.Name))
            {
                // The property was expected to be found in this schema! Yay.
                var schemaPropertyDef = this.ExpectedProperties[inputProperty.Name];
                
                // Check for simple value types first
                if (this.SimpleValueTypes(schemaPropertyDef.Type, inputProperty.Type) && this.AllFalse(schemaPropertyDef.Type.IsCollection, inputProperty.Type.IsCollection))
                {
                    return ValidateSameDataType(schemaPropertyDef, inputProperty, detectedErrors, (null != options) ? options.RelaxedStringValidation : false);
                }
                else if (null == inputProperty.OriginalValue)
                {
                    if (null != this.NullableProperties && !this.NullableProperties.Contains(schemaPropertyDef.Name))
                    {
                        detectedErrors.Add(new ValidationWarning(ValidationErrorCode.NullPropertyValue, null, "Non-nullable property {0} had a null value in the response. Expected {1}.", schemaPropertyDef.Name, schemaPropertyDef.Type));
                    }
                    return PropertyValidationOutcome.Ok;
                }
                else if (schemaPropertyDef.Type.IsCollection || inputProperty.Type.IsCollection)
                {
                    // Check for an array
                    if (schemaPropertyDef.Type.IsCollection && !inputProperty.Type.IsCollection)
                    {
                        // Expected an array, but didn't get one
                        detectedErrors.Add(new ValidationError(ValidationErrorCode.ExpectedArrayValue, null, "Expected an array but property was not an array: {0}", inputProperty.Name));
                        return PropertyValidationOutcome.InvalidType;
                    }
                    else if (!schemaPropertyDef.Type.IsCollection && inputProperty.Type.IsCollection)
                    {
                        detectedErrors.Add(new ValidationError(ValidationErrorCode.ExpectedNonArrayValue, null, "Expected a value of type {0} but property was an array: {1}", schemaPropertyDef.Type, inputProperty.Name));
                        return PropertyValidationOutcome.InvalidType;
                    }

                    return this.ValidateArrayProperty(inputProperty, schemas, detectedErrors, options);
                }
                else if (schemaPropertyDef.Type.IsObject && inputProperty.Type.IsObject)
                {
                    // Compare the ODataType schema to the custom schema
                    if (null == schemaPropertyDef.Type.CustomTypeName || !schemas.ContainsKey(schemaPropertyDef.Type.CustomTypeName))
                    {
                        detectedErrors.Add(new ValidationError(ValidationErrorCode.ResourceTypeNotFound, null, "Missing resource: resource {0} was not found (property name '{1}').", schemaPropertyDef.Type.CustomTypeName, inputProperty.Name));
                        return PropertyValidationOutcome.MissingResourceType;
                    }
                    else if (inputProperty.Type.IsObject)
                    {
                        var odataSchema = schemas[schemaPropertyDef.Type.CustomTypeName];
                        ValidationError[] odataErrors;
                        if (null != inputProperty.Type.CustomMembers && !odataSchema.ValidateCustomObject(inputProperty.Type.CustomMembers.ToArray(), out odataErrors, schemas, options))
                        {
                            var propertyError = ValidationError.NewConsolidatedError(ValidationErrorCode.ConsolidatedError, odataErrors, "Schemas validation failed on property '{0}' ['{1}']", inputProperty.Name, odataSchema.ResourceName);
                            detectedErrors.Add(propertyError);

                            return PropertyValidationOutcome.InvalidType;
                        }
                        else if (null == inputProperty.Type.CustomMembers)
                        {
                            detectedErrors.Add(new ValidationError(ValidationErrorCode.NoCustomMembersFound, null, "Property '{0}' is of type Custom but has no custom members.", inputProperty.Name));
                        }
                        return PropertyValidationOutcome.Ok;
                    }
                    else
                    {
                        var odataSchema = schemas[schemaPropertyDef.Type.CustomTypeName];
                        if (inputProperty.Type.CustomMembers == null)
                        {
                            detectedErrors.Add(new ValidationError(ValidationErrorCode.MissingCustomMembers, null, "Property {0} is missing custom members and cannot be validated.", inputProperty.Name));
                            return PropertyValidationOutcome.InvalidType;
                        }
                        else
                        {
                            odataSchema.ValidateObjectProperties(inputProperty.Type.CustomMembers, options, schemas, detectedErrors);
                            return PropertyValidationOutcome.Ok;
                        }
                    }
                }
                else if (schemaPropertyDef.Type.IsObject)
                {
                    detectedErrors.Add(new ValidationWarning(ValidationErrorCode.CustomValidationNotSupported, null, "Schemas type was 'Custom' which is not supported. Add a resource type to the definition of property: {0}", inputProperty.Name));
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

                // Check to see if this property is on the ignorable list
                string[] ignorableUndocumentedProperties = this.OriginalResource?.SourceFile.Parent.Requirements?.IgnorableProperties;

                string propertyName = inputProperty.Name;
                string annotationName = null;
                var indexOfAtSign = propertyName.IndexOf('@');
                if (indexOfAtSign > 0)
                {
                    // attachments@odata.nextLink is an example of what we're looking for here
                    annotationName = propertyName.Substring(indexOfAtSign);
                    propertyName = propertyName.Substring(0, indexOfAtSign);
                }


                if (null != annotationName)
                {
                    // Check to see if propertyName is known or not. If it isn't known, fail.
                    if (this.Properties.Any(x => x.Name.Equals(propertyName)))
                    {
                        // If the cleaned up propertyName is known, then check to see if the annotation is ignorable
                        if (null != ignorableUndocumentedProperties && ignorableUndocumentedProperties.Contains(annotationName))
                        {
                            // If we know of both the property and the annotation, we're good.
                            return PropertyValidationOutcome.Ok;
                        }
                    }
                }

                if (null != ignorableUndocumentedProperties && ignorableUndocumentedProperties.Contains(propertyName))
                {
                    return PropertyValidationOutcome.Ok;
                }

                // This property isn't documented
                detectedErrors.Add(new UndocumentedPropertyWarning(null, inputProperty.Name, inputProperty.Type, ResourceName));
                return PropertyValidationOutcome.MissingFromSchema;
            }
        }

        

        private static PropertyValidationOutcome ValidateStringFormat(ParameterDefinition schemaProperty, ParameterDefinition inputProperty, List<ValidationError> detectedErrorsCollection)
        {
            switch (schemaProperty.StringFormat())
            {
                case ExpectedStringFormat.Iso8601Date:
                    {
                        DateTimeOffset? output = inputProperty.OriginalValue.TryParseIso8601Date();
                        if (!output.HasValue)
                        {
                            detectedErrorsCollection.Add(new ValidationError(ValidationErrorCode.InvalidDateTimeString, null, "Invalid ISO 8601 date-time string in property: {1}: {0}", inputProperty.OriginalValue, schemaProperty.Name));
                            return PropertyValidationOutcome.BadStringValue;
                        }
                        return PropertyValidationOutcome.Ok;
                    }
                case ExpectedStringFormat.AbsoluteUrl:
                    {
                        try
                        {
                            new Uri(inputProperty.OriginalValue, UriKind.Absolute);
                            return PropertyValidationOutcome.Ok;
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
                        return PropertyValidationOutcome.Ok;
                    }
                case ExpectedStringFormat.Generic:
                    return PropertyValidationOutcome.Ok;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Return true if the input types are all simple (value only) types.
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        private bool SimpleValueTypes(params ParameterDataType[] types)
        {
            return types.All(type => !type.IsObject && !type.IsCollection);
        }

        private bool AllFalse(params bool[] input)
        {
            return input.All(value => !value);
        }

        private PropertyValidationOutcome ValidateSimpleArrayProperty(ParameterDefinition actualProperty, ParameterDefinition expectedProperty, List<ValidationError> detectedErrors)
        {
            if (!actualProperty.Type.IsCollection || !expectedProperty.Type.IsCollection)
            {
                throw new SchemaBuildException("Cannot use simple array valiation without array types", null);
            }

            if (actualProperty.Type == expectedProperty.Type)
            {
                return PropertyValidationOutcome.Ok;
            }
            else if (expectedProperty.Type.IsCollection && actualProperty.Type.IsCollection && actualProperty.OriginalValue == "[]")    // Check for the empty array case
            {
                return PropertyValidationOutcome.Ok;
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
        /// <param name="actualProperty"></param>
        /// <param name="schemas"></param>
        /// <param name="detectedErrors"></param>
        /// <param name="options"></param>
        private PropertyValidationOutcome ValidateArrayProperty(ParameterDefinition actualProperty, Dictionary<string, JsonSchema> schemas, List<ValidationError> detectedErrors, ValidationOptions options)
        {
            JArray actualArray = null;
            try
            {
                actualArray = (JArray)JsonConvert.DeserializeObject(actualProperty.OriginalValue);
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException($"Property {actualProperty.Name} expected to be an array, but failed to cast value to an array: {actualProperty.OriginalValue}");
            }

            var expectedPropertyDefinition = this.ExpectedProperties[actualProperty.Name];
            JsonSchema memberSchema = null;
            if (actualProperty.Type.CollectionResourceType == SimpleDataType.Object && expectedPropertyDefinition.Type.CustomTypeName != null)
            {
                // We have an ambigious array, but we know what it's supposed to be so let's use that
                schemas.TryGetValue(expectedPropertyDefinition.Type.CustomTypeName, out memberSchema);
            }

            if (memberSchema == null && string.IsNullOrEmpty(actualProperty.Type.CustomTypeName))
            {
                return this.ValidateSimpleArrayProperty(actualProperty, this.ExpectedProperties[actualProperty.Name], detectedErrors);
            }
            else if (memberSchema == null && !schemas.TryGetValue(actualProperty.Type.CustomTypeName, out memberSchema))
            {
                detectedErrors.Add(new ValidationError(ValidationErrorCode.ResourceTypeNotFound, null, "Failed to locate resource definition for: {0}", actualProperty.Type.CustomTypeName));
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

                    // TODO: Filter out non-unique errors
                    hadErrors |= memberErrors.Count > 0;
                    detectedErrors.AddUniqueErrors(memberErrors);
                }
            }

            return hadErrors ? PropertyValidationOutcome.GenericError : PropertyValidationOutcome.Ok;
        }

        private bool ValidateCustomObject(ParameterDefinition[] properties, out ValidationError[] errors, Dictionary<string, JsonSchema> otherSchemas, ValidationOptions options)
        {
            List<string> missingProperties = new List<string>(this.ExpectedProperties.Keys);
            List<ValidationError> detectedErrors = new List<ValidationError>();
            foreach (var inputProperty in properties)
            {
                missingProperties.Remove(inputProperty.Name);
                this.ValidateProperty(inputProperty, otherSchemas, detectedErrors, options);
            }

            this.CleanMissingProperties(options, missingProperties);

            errors = detectedErrors.ToArray();
            return detectedErrors.Count == 0;
        }

        #endregion

        #region Schemas Building
        private Dictionary<string, ParameterDefinition> BuildSchemaFromJson(string json, IEnumerable<ParameterDefinition> parameters = null)
        {
            Dictionary<string, ParameterDefinition> schema = new Dictionary<string, ParameterDefinition>();
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
                        ParameterDefinition propertyInfo = ParseProperty(token, null);
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

        private static void AddParameterDataToProperty(IEnumerable<ParameterDefinition> parameters, ParameterDefinition schemaPropertyDef)
        {
            if (null != schemaPropertyDef && null != parameters)
            {
                // See if we can look up more data for this new property
                var findParameterQuery = from p in parameters
                                         where p.Name == schemaPropertyDef.Name
                                         select p;
                var parameterData = findParameterQuery.FirstOrDefault();
                if (null != parameterData)
                {
                    schemaPropertyDef.Description = parameterData.Description;
                }
            }
        }

        /// <summary>
        /// Build a ParameterDefinition instance based on a property name and provide value and schema.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="containerSchema"></param>
        /// <returns></returns>
        internal static ParameterDefinition ParseProperty(string name, JToken value, JsonSchema containerSchema)
        {
            ParameterDefinition param = new ParameterDefinition();
            param.Name = name;
            param.OriginalValue = value.ToString();

            switch (value.Type)
            {
                case JTokenType.Boolean:
                    param.Type = ParameterDataType.Boolean;
                    break;
                case JTokenType.Float:
                    param.Type = ParameterDataType.Float;
                    break;
                case JTokenType.Integer:
                    param.Type = ParameterDataType.Int64;
                    break;
                case JTokenType.String:
                    var propValue = value.Value<string>();
                    SimpleDataType customType = ExtensionMethods.ParseSimpleTypeString(propValue.ToLowerInvariant());
                    ParameterDataType paramType = (customType != SimpleDataType.None) ? new ParameterDataType(customType) : ParameterDataType.String;
                    param.Type = paramType;
                    break;
                case JTokenType.Date:
                    param.Type = ParameterDataType.DateTimeOffset;
                    break;
                case JTokenType.Object:
                    {
                        var objectSchema = GeneratePropertyCollection((JObject)value);
                        if (objectSchema.ContainsKey("@odata.type"))
                        {
                            param.Type = new ParameterDataType(objectSchema["@odata.type"].OriginalValue);
                        }
                        else if (objectSchema.ContainsKey("@type"))
                        {
                            param.Type = new ParameterDataType(objectSchema["@type"].OriginalValue);
                        }
                        else
                        {
                            // See if we can infer type from the parent scehma
                            var propertyCollection = GeneratePropertyCollection((JObject)value);
                            ParameterDefinition[] customMembers = null;
                            if (propertyCollection != null)
                            {
                                customMembers = propertyCollection.Values.ToArray();
                            }

                            ParameterDefinition schemaProperty;
                            if (null != containerSchema && containerSchema.ExpectedProperties.TryGetValue(name, out schemaProperty))
                            {
                                param.Type = new ParameterDataType(schemaProperty.Type, customMembers);
                            }
                            else
                            {
                                param.Type = new ParameterDataType(customMembers);
                            }
                        }
                        break;
                    }
                case JTokenType.Array:
                {
                    ParameterDataType propertyType = ParameterDataType.GenericCollection;

                    // Try to infer type from the items in the array
                    var firstChild = value.First;
                    if (null != firstChild)
                    {
                        var objectType = ParseProperty("array[0]", firstChild, null);
                        if (null != objectType)
                        {
                            propertyType = ParameterDataType.CollectionOfType(objectType.Type);
                        }
                    }

                    // See if we can do better than GenericCollection if that's the situation we're in.
                    if (propertyType == ParameterDataType.GenericCollection)
                    {
                        ParameterDefinition schemaProperty;
                        if (null != containerSchema && containerSchema.ExpectedProperties.TryGetValue(name, out schemaProperty))
                        {
                            // Use the parent schema's type indication
                            //propertyType = ParameterDataType.CollectionOfType(schemaProperty.Type);
                            propertyType = schemaProperty.Type;
                        }
                    }

                    Dictionary<string, ParameterDefinition> members = null;
                    if ((propertyType.IsObject || (propertyType.IsCollection && propertyType.CollectionResourceType == SimpleDataType.Object)) &&
                        string.IsNullOrEmpty(propertyType.CustomTypeName))
                    {
                        // If we don't know what kind of object is here, let's record what we see as custom members
                        var firstValue = (JObject)value.First;
                        members = firstValue != null ? GeneratePropertyCollection(firstValue) : new Dictionary<string, ParameterDefinition>();
                    }
                    ParameterDefinition[] customMembers = null;
                    if (members != null) customMembers = members.Values.ToArray();
                    param.Type = new ParameterDataType(propertyType, customMembers);
                    break;
                }
                case JTokenType.Null:
                    param.Type = ParameterDataType.GenericObject;
                    param.OriginalValue = null;
                    break;
                default:
                    Console.WriteLine("Unsupported: Property {0} is of type {1} which is not currently supported.", name, value.Type);
                    throw new NotSupportedException(string.Format("Unsupported: Property {0} is of type {1} which is not currently supported.", name, value.Type));
            }

            return param;
        }

        private static ParameterDefinition ParseProperty(JToken token, JsonSchema containerSchema, List<ValidationError> detectedErrors = null)
        {
            ParameterDefinition propertyInfo = null;
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

        private static Dictionary<string, ParameterDefinition> GeneratePropertyCollection(JObject obj)
        {
            var propertyCollection = new Dictionary<string, ParameterDefinition>();
            foreach (var prop in obj)
            {
                string name = prop.Key;
                JToken value = prop.Value;
                var propertyInfo = ParseProperty(name, value, null);
                if (null != propertyInfo && null != propertyInfo.Name)
                    propertyCollection[propertyInfo.Name] = propertyInfo;
            }
            return propertyCollection;
        }

        enum PropertyValidationOutcome
        {
            Ok,
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
