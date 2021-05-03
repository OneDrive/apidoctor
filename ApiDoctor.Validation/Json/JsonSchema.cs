/*
 * API Doctor
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

namespace ApiDoctor.Validation.Json
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using ApiDoctor.Validation.Error;
    using ApiDoctor.Validation.Http;
    using ApiDoctor.Validation.OData;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class JsonSchema
    {
        private Dictionary<string, JsonSchema> childTypes = new Dictionary<string, JsonSchema>(StringComparer.OrdinalIgnoreCase);

        public string ResourceName { get { return this.Metadata?.ResourceType; } }

        public string ResourceNameAka { get { return this.Metadata?.ResourceTypeAka; } }

        protected Dictionary<string, ParameterDefinition> ExpectedProperties { get; private set; }

        internal CodeBlockAnnotation Metadata { get; private set; }

        public string[] OptionalProperties { get { return this.Metadata?.OptionalProperties; } }

        public string[] NullableProperties { get { return this.Metadata?.NullableProperties; } }

        public ResourceDefinition OriginalResource { get; set; }


        public ParameterDefinition[] Properties
        {
            get { return this.ExpectedProperties.Values.ToArray(); }
        }

        public override string ToString()
        {
            return $"{ResourceName}";
        }

        #region Constructor
        public JsonSchema(string json, CodeBlockAnnotation annotation)
        {
            this.Metadata = annotation;
            this.ExpectedProperties = this.BuildSchemaFromJson(json);
        }

        public JsonSchema(ResourceDefinition resource)
        {
            this.Metadata = resource.OriginalMetadata;
            this.ExpectedProperties = this.BuildSchemaFromJson(resource.ExampleText, resource);
            this.OriginalResource = resource;
        }
        #endregion

        #region Json Validation Against Schemas

        internal void RegisterChildType(JsonSchema schema)
        {
            this.childTypes[schema.ResourceName] = schema;
        }

        /// <summary>
        /// Checks that the expected response of a method definition is valid with this resource.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="issues"></param>
        /// <returns></returns>
        public bool ValidateExpectedResponse(MethodDefinition method, IssueLogger issues)
        {
            HttpResponse response;
            HttpParser.TryParseHttpResponse(method.ExpectedResponse, out response, issues);
            if (response != null)
            {
                JsonExample example = new JsonExample(response.Body, method.ExpectedResponseMetadata);
                var otherSchemas = new Dictionary<string, JsonSchema>();
                return this.ValidateJson(example, issues, otherSchemas, null);
            }
            return false;
        }

        /// <summary>
        /// Validate the input json against the defined scehma when the instance was created.
        /// </summary>
        /// <param name="jsonInput">Input json to validate against schema</param>
        /// <param name="issues"></param>
        /// <param name="otherSchemas"></param>
        /// <param name="options"></param>
        /// <param name="expectedJson"></param>
        /// <returns>True if validation was successful, otherwise false.</returns>
        public bool ValidateJson(JsonExample jsonInput, IssueLogger issues, Dictionary<string, JsonSchema> otherSchemas, ValidationOptions options, JsonExample expectedJson = null, bool schemaExample = false)
        {
            JContainer obj;
            try
            {
                if (string.IsNullOrWhiteSpace(jsonInput.JsonData))
                    throw new Exception("Expected json string was empty or whitespace only.");

                var settings = new JsonSerializerSettings { DateParseHandling = DateParseHandling.None, NullValueHandling = NullValueHandling.Include, DefaultValueHandling = DefaultValueHandling.Include };
                obj = (JContainer)JsonConvert.DeserializeObject(jsonInput.JsonData, settings);
            }
            catch (Exception ex)
            {
                issues.Error(ValidationErrorCode.JsonParserException, $"Failed to parse json string: {jsonInput.JsonData.Trim()}", ex);
                return false;
            }

            var annotation = jsonInput.Annotation ?? new CodeBlockAnnotation();

            bool expectErrorObject = (jsonInput.Annotation != null) && jsonInput.Annotation.ExpectError;

            // Check for an error response
            try
            {
                // If it's a generated example from the EDMX, we should skip error look-up, since any properties with error type will throw here.
                if (!schemaExample)
                {
                    dynamic errorObject = obj["error"];
                    if (null != errorObject && !expectErrorObject)
                    {
                        string code = errorObject.code;
                        string message = errorObject.message;

                        issues.Error(ValidationErrorCode.JsonErrorObject, $"Error response received. Code: {code}, Message: {message}");
                        return false;
                    }
                    else if (expectErrorObject && null == errorObject)
                    {
                        issues.Error(ValidationErrorCode.JsonErrorObjectExpected, "Expected an error object response, but didn't receive one.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                if (annotation.ExpectError)
                {
                    issues.Error(ValidationErrorCode.JsonErrorObjectExpected, "Expected an error object, but it doesn't look like we got one.", ex);
                }
            }

            // Check to see if this is a "collection" instance
            if (null != annotation && annotation.IsCollection)
            {
                this.ValidateCollectionObject(obj, annotation, otherSchemas, options.CollectionPropertyName, issues, options);
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
                this.ValidateContainerObject(obj, options, otherSchemas, issues);
            }

            return issues.Issues.Count() == 0;
        }

        private void ValidateCollectionObject(JContainer obj, CodeBlockAnnotation annotation, Dictionary<string, JsonSchema> otherSchemas, string collectionPropertyName, IssueLogger issues, ValidationOptions options)
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
                    issues.Error(ValidationErrorCode.JsonErrorObjectExpected, $"Unable to find collection parameter or array to validate: {ex.Message}");
                    return;
                }
            }

            if (null == collection)
            {
                issues.Error(ValidationErrorCode.MissingCollectionProperty, $"Failed to locate collection property '{collectionPropertyName}' in response.");
            }
            else
            {
                if (!collection.Any())
                {
                    if (!annotation.IsEmpty)
                    {
                        issues.Warning(
                                ValidationErrorCode.CollectionArrayEmpty,
                                $"Property contained an empty array that was not validated: {collectionPropertyName}");
                    }
                }
                else if (annotation.IsEmpty)
                {
                    issues.Warning(
                            ValidationErrorCode.CollectionArrayNotEmpty,
                            $"Property contained a non-empty array that was expected to be empty: {collectionPropertyName}");
                }

                foreach (var jToken in collection)
                {
                    var container = jToken as JContainer;
                    if (null != container)
                    {
                        var deeperOptions = new ValidationOptions(options)
                        {
                            AllowTruncatedResponses = annotation.TruncatedResult
                        };

                        this.ValidateContainerObject(
                            container,
                            deeperOptions,
                            otherSchemas,
                            issues.For("container", onlyKeepUniqueErrors: true));
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
        /// <param name="issues"></param>
        private void ValidateContainerObject(JContainer obj, ValidationOptions options, Dictionary<string, JsonSchema> otherSchemas, IssueLogger issues)
        {
            var explicitType = obj["@odata.type"];
            if (explicitType != null)
            {
                var typeName = explicitType.Value<string>();
                if (!string.IsNullOrEmpty(typeName))
                {
                    if (typeName.StartsWith("#"))
                    {
                        typeName = typeName.Substring(1);
                    }

                    JsonSchema childType;
                    if (this.childTypes.TryGetValue(typeName, out childType))
                    {
                        childType.ValidateContainerObject(obj, options, otherSchemas, issues);
                        return;
                    }
                    else if (this.ResourceName != null && !typeName.TypeOnly().IEquals(this.ResourceName.TypeOnly()))
                    {
                        issues.Warning(ValidationErrorCode.ResourceTypeNotFound, $"unrecognized type declaration {typeName}");
                    }
                }
            }

            var containerProperties = from p in obj
                                      select ParseProperty(p, this, issues.For(p.Path));

            this.ValidateObjectProperties(containerProperties.Where(x => null != x), options, otherSchemas, issues);
        }

        private void ValidateObjectProperties(IEnumerable<ParameterDefinition> propertiesOnObject, ValidationOptions options, Dictionary<string, JsonSchema> otherSchemas, IssueLogger issues)
        {
            List<string> missingProperties = new List<string>();
            missingProperties.AddRange(from m in this.ExpectedProperties select m.Key);

            foreach (var property in propertiesOnObject)
            {
                missingProperties.Remove(property.Name);

                if (options?.IgnorablePropertyTypes != null && property?.Type?.CustomTypeName != null)
                {
                    if (options.IgnorablePropertyTypes.Contains(property.Type.CustomTypeName.TypeOnly()))
                    {
                        continue;
                    }
                }

                // This detects bad types, extra properties, etc.
                if (null != options && (property.Type.IsCollection || property.Type.IsObject))
                {
                    var propertyOptions = options.CreateForProperty(property.Name);
                    this.ValidateProperty(property, otherSchemas, issues.For(property.Name), propertyOptions);
                }
                else
                {
                    this.ValidateProperty(property, otherSchemas, issues.For(property.Name), options);
                }
            }

            this.CleanMissingProperties(options, missingProperties);
            if (missingProperties.Count > 0)
            {
                issues.Error(ValidationErrorCode.RequiredPropertiesMissing,
                    $"Missing properties: response was missing these required properties: {missingProperties.ComponentsJoinedByString(", ")}");
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

        private static PropertyValidationOutcome ValidateSameDataType(ParameterDefinition expected, ParameterDefinition actual, IssueLogger issues, bool relaxStringValidation)
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
                return ValidateStringFormat(expected, actual, issues);
            }
            else if (expected.Type == ParameterDataType.String && expected.Type.IsLessSpecificThan(actual.Type))
            {
                return PropertyValidationOutcome.Ok;
            }
            else if (actual.Type.IsLessSpecificThan(expected.Type))
            {
                if (relaxStringValidation)
                {
                    issues.Message(
                        $"Expected type {expected.Type} but actual was {actual.Type}, which is less specific than the expected type. Property: {actual.Name}, actual value: '{actual.OriginalValue}'");
                    return PropertyValidationOutcome.Ok;
                }

                issues.Error(
                        ValidationErrorCode.ExpectedTypeDifferent,
                        $"Expected type {expected.Type} but actual was {actual.Type}, which is less specific than the expected type. Property: {actual.Name}, actual value: '{actual.OriginalValue}'");
                return PropertyValidationOutcome.BadStringValue;
            }
            else
            {
                // Type of the inputProperty is mismatched from the expected value.
                issues.Error(
                        ValidationErrorCode.ExpectedTypeDifferent,
                        $"Expected type {expected.Type} but actual was {actual.Type}. Property: {actual.Name}, actual value: '{actual.OriginalValue}'");
                return PropertyValidationOutcome.InvalidType;
            }
        }

        /// <summary>
        /// Verify that a property from the json-to-validate matches something in our schema
        /// </summary>
        private PropertyValidationOutcome ValidateProperty(ParameterDefinition inputProperty, Dictionary<string, JsonSchema> schemas, IssueLogger issues, ValidationOptions options)
        {
            if (this.ExpectedProperties.ContainsKey(inputProperty.Name))
            {
                // The property was expected to be found in this schema! Yay.
                var schemaPropertyDef = this.ExpectedProperties[inputProperty.Name];

                // Check for simple value types first
                if (this.SimpleValueTypes(schemaPropertyDef.Type, inputProperty.Type) && this.AllFalse(schemaPropertyDef.Type.IsCollection, inputProperty.Type.IsCollection))
                {
                    return ValidateSameDataType(schemaPropertyDef, inputProperty, issues, (null != options) ? options.RelaxedStringValidation : false);
                }
                else if (null == inputProperty.OriginalValue)
                {
                    if (null != this.NullableProperties && !this.NullableProperties.Contains(schemaPropertyDef.Name))
                    {
                        issues.Warning(ValidationErrorCode.NullPropertyValue, $"Non-nullable property {schemaPropertyDef.Name} had a null value in the response. Expected {schemaPropertyDef.Type}.");
                    }
                    return PropertyValidationOutcome.Ok;
                }
                else if (schemaPropertyDef.Type.IsCollection || inputProperty.Type.IsCollection)
                {
                    // Check for an array
                    if (schemaPropertyDef.Type.IsCollection && !inputProperty.Type.IsCollection)
                    {
                        // Expected an array, but didn't get one
                        issues.Error(ValidationErrorCode.ExpectedArrayValue, $"Expected an array but property was not an array: {inputProperty.Name}");
                        return PropertyValidationOutcome.InvalidType;
                    }
                    else if (!schemaPropertyDef.Type.IsCollection && inputProperty.Type.IsCollection)
                    {
                        issues.Error(ValidationErrorCode.ExpectedNonArrayValue, $"Expected a value of type {schemaPropertyDef.Type} but property was an array: {inputProperty.Name}");
                        return PropertyValidationOutcome.InvalidType;
                    }

                    return this.ValidateArrayProperty(inputProperty, schemas, issues, options);
                }
                else if (schemaPropertyDef.Type.IsObject && inputProperty.Type.IsObject)
                {
                    // Compare the ODataType schema to the custom schema
                    if (null == schemaPropertyDef.Type.CustomTypeName || !schemas.ContainsKey(schemaPropertyDef.Type.CustomTypeName))
                    {
                        issues.Error(ValidationErrorCode.ResourceTypeNotFound, $"Missing resource: resource {schemaPropertyDef.Type.CustomTypeName} was not found (property name '{inputProperty.Name}').");
                        return PropertyValidationOutcome.MissingResourceType;
                    }
                    else if (inputProperty.Type.IsObject)
                    {
                        var odataSchema = schemas[schemaPropertyDef.Type.CustomTypeName];
                        if (null != inputProperty.Type.CustomMembers && !odataSchema.ValidateCustomObject(inputProperty.Type.CustomMembers.ToArray(), issues, schemas, options))
                        {
                            if (issues.Errors.Any())
                            {
                                issues.Error(ValidationErrorCode.ConsolidatedError, $"Schema validation failed on property '{inputProperty.Name}' ['{odataSchema.ResourceName}']");
                            }
                            else
                            {
                                issues.Warning(ValidationErrorCode.ConsolidatedError, $"Schema validation failed on property '{inputProperty.Name}' ['{odataSchema.ResourceName}']");
                            }

                            return PropertyValidationOutcome.InvalidType;
                        }
                        else if (null == inputProperty.Type.CustomMembers)
                        {
                            //Temporarily disabled: Issue Tracked at https://github.com/OneDrive/apidoctor/issues/13
                            //issues.Error(ValidationErrorCode.NoCustomMembersFound, $"Property '{inputProperty.Name}' is of type Custom but has no custom members.");
                        }
                        return PropertyValidationOutcome.Ok;
                    }
                    else
                    {
                        var odataSchema = schemas[schemaPropertyDef.Type.CustomTypeName];
                        if (inputProperty.Type.CustomMembers == null)
                        {
                            issues.Error(ValidationErrorCode.MissingCustomMembers, $"Property {inputProperty.Name} is missing custom members and cannot be validated.");
                            return PropertyValidationOutcome.InvalidType;
                        }
                        else
                        {
                            odataSchema.ValidateObjectProperties(inputProperty.Type.CustomMembers, options, schemas, issues);
                            return PropertyValidationOutcome.Ok;
                        }
                    }
                }
                else if (schemaPropertyDef.Type.IsObject)
                {
                    issues.Warning(ValidationErrorCode.CustomValidationNotSupported, $"Schemas type was 'Custom' which is not supported. Add a resource type to the definition of property: {inputProperty.Name}");
                    return PropertyValidationOutcome.MissingResourceType;
                }
                else
                {
                    issues.Error(ValidationErrorCode.ExpectedTypeDifferent, $"Type mismatch: property '{inputProperty.Name}' [{inputProperty.Type}] doesn't match expected type [{schemaPropertyDef.Type}].");
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

                if (this.OriginalResource?.OriginalMetadata?.IsOpenType == true)
                {
                    return PropertyValidationOutcome.Ok;
                }

                // This property isn't documented
                issues.Warning(new UndocumentedPropertyWarning(null, inputProperty.Name, inputProperty.Type, ResourceName));
                return PropertyValidationOutcome.MissingFromSchema;
            }
        }



        private static PropertyValidationOutcome ValidateStringFormat(ParameterDefinition schemaProperty, ParameterDefinition inputProperty, IssueLogger issues)
        {
            switch (schemaProperty.StringFormat())
            {
                case ExpectedStringFormat.Iso8601Date:
                    {
                        DateTimeOffset? output = inputProperty.OriginalValue.TryParseIso8601Date();
                        if (!output.HasValue)
                        {
                            issues.Error(ValidationErrorCode.InvalidDateTimeString, $"Invalid ISO 8601 date-time string in property: {schemaProperty.Name}: {inputProperty.OriginalValue}");
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
                            issues.Error(ValidationErrorCode.InvalidUrlString, $"Invalid absolute URL value in property {schemaProperty.Name}: {inputProperty.OriginalValue}");
                            return PropertyValidationOutcome.BadStringValue;
                        }
                    }
                case ExpectedStringFormat.EnumeratedValue:
                    {
                        if (!schemaProperty.IsValidEnumValue(inputProperty.OriginalValue))
                        {
                            issues.Error(ValidationErrorCode.InvalidEnumeratedValueString, $"Invalid enumerated value in property {schemaProperty.Name}: {inputProperty.OriginalValue}");
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

        private PropertyValidationOutcome ValidateSimpleArrayProperty(ParameterDefinition actualProperty, ParameterDefinition expectedProperty, IssueLogger issues)
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
            else if (expectedProperty.Type.IsLessSpecificThan(actualProperty.Type))
            {
                return PropertyValidationOutcome.Ok;
            }
            else
            {
                issues.Error(ValidationErrorCode.ArrayTypeMismatch, $"Array expected members to be of type {expectedProperty.Type} but found: {actualProperty.Type}");
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
        private PropertyValidationOutcome ValidateArrayProperty(ParameterDefinition actualProperty, Dictionary<string, JsonSchema> schemas, IssueLogger issues, ValidationOptions options)
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
                return this.ValidateSimpleArrayProperty(actualProperty, this.ExpectedProperties[actualProperty.Name], issues);
            }
            else if (memberSchema == null && !schemas.TryGetValue(actualProperty.Type.CustomTypeName, out memberSchema))
            {
                issues.Error(ValidationErrorCode.ResourceTypeNotFound, $"Failed to locate resource definition for: {actualProperty.Type.CustomTypeName}");
                return PropertyValidationOutcome.MissingResourceType;
            }

            bool hadErrors = false;
            var memberIssues = issues.For("member", onlyKeepUniqueErrors: true);
            for (int i = 0; i < actualArray.Count; i++)
            {
                JContainer member = actualArray[i] as JContainer;
                if (member != null)
                {
                    memberSchema.ValidateContainerObject(member, options, schemas, memberIssues);

                    // TODO: Filter out non-unique errors
                    hadErrors |= memberIssues.Issues.Count() > 0;
                }
            }

            return hadErrors ? PropertyValidationOutcome.GenericError : PropertyValidationOutcome.Ok;
        }

        private bool ValidateCustomObject(ParameterDefinition[] properties, IssueLogger issues, Dictionary<string, JsonSchema> otherSchemas, ValidationOptions options)
        {
            List<string> missingProperties = new List<string>(this.ExpectedProperties.Keys);
            foreach (var inputProperty in properties)
            {
                missingProperties.Remove(inputProperty.Name);
                this.ValidateProperty(inputProperty, otherSchemas, issues.For(inputProperty.Name), options);
            }

            this.CleanMissingProperties(options, missingProperties);

            return !issues.Issues.WereWarningsOrErrors();
        }

        #endregion

        #region Schemas Building
        private Dictionary<string, ParameterDefinition> BuildSchemaFromJson(string json, ResourceDefinition resource = null)
        {
            var parameters = resource?.Parameters;
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

                // populate inherited properties too
                if (resource != null)
                {
                    resource = resource.ResolvedBaseTypeReference;
                    while (resource != null)
                    {
                        foreach (var param in resource.Parameters)
                        {
                            if (!schema.ContainsKey(param.Name))
                            {
                                schema[param.Name] = param;
                            }
                        }

                        resource = resource.ResolvedBaseTypeReference;
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
                    param.Type = ParameterDataType.Double;
                    break;
                case JTokenType.Integer:
                    param.Type = ParameterDataType.Int64;
                    break;
                case JTokenType.String:
                    var propValue = value.Value<string>();
                    SimpleDataType customType = Validation.ExtensionMethods.ParseSimpleTypeString(propValue.ToLowerInvariant());
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
                            param.Type = objectSchema["@odata.type"].OriginalValue.ParseParameterDataType();
                        }
                        else if (objectSchema.ContainsKey("@type"))
                        {
                            param.Type = objectSchema["@type"].OriginalValue.ParseParameterDataType();
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
                                //Assume is a collection of Json
                                propertyType = ParameterDataType.CollectionOfType(objectType.Type);
                                if (propertyType.CollectionResourceType == SimpleDataType.Collection)
                                {
                                    //If inner type is collection, assume its a Json collection
                                    propertyType = ParameterDataType.JsonCollection;
                                }
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
                            var firstValue = value.First as JObject;
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

        private static ParameterDefinition ParseProperty(JToken token, JsonSchema containerSchema, IssueLogger issues = null)
        {
            ParameterDefinition propertyInfo = null;
            if (token.Type == JTokenType.Property)
            {
                JProperty tokenProperty = (JProperty)token;
                propertyInfo = ParseProperty(tokenProperty.Name, tokenProperty.Value, containerSchema);
            }
            else
            {
                if (issues != null)
                {
                    issues.Warning(ValidationErrorCode.JsonParserException, "Unhandled token type: " + token.Type);
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
                return new JsonSchema("{ }", new CodeBlockAnnotation { BlockType = CodeBlockType.Response, IsCollection = false, MethodName = new List<string>(new string[] { "EmptyResponse" }) });
            }
        }
    }
}