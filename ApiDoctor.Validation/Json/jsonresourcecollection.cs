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
    using System.Linq;
    using ApiDoctor.Validation.Error;
    using ApiDoctor.Validation.Http;

    public class JsonResourceCollection
    {
        private readonly Dictionary<string, JsonSchema> registeredSchema = new Dictionary<string,JsonSchema>();

        public JsonSchema[] RegisteredSchema
        {
            get { return this.registeredSchema.Values.ToArray(); }
        }

        public void RegisterJsonResource(ResourceDefinition resource)
        {
            var schema = new JsonSchema(resource);
            this.registeredSchema[resource.Name] = schema;

            JsonSchema baseTypeSchema;
            if (resource.BaseType != null && this.registeredSchema.TryGetValue(resource.BaseType, out baseTypeSchema))
            {
                baseTypeSchema.RegisterChildType(schema);
            }
        }

        /// <summary>
        /// Validates the value of json according to an implicit schema defined by expectedJson
        /// </summary>
        /// <returns></returns>
        public bool ValidateJsonExample(CodeBlockAnnotation expectedResponseAnnotation, string actualResponseBodyJson, IssueLogger issues, ValidationOptions options = null)
        {
            List<ValidationError> newErrors = new List<ValidationError>();

            var resourceType = expectedResponseAnnotation.ResourceType;
            if (resourceType == "stream")
            {
                // No validation since we're streaming data
                return true;
            }
            else
            {
                JsonSchema schema;
                if (string.IsNullOrEmpty(resourceType))
                {
                    schema = JsonSchema.EmptyResponseSchema;
                }
                else if (!this.registeredSchema.TryGetValue(resourceType, out schema))
                {
                    newErrors.Add(new ValidationWarning(ValidationErrorCode.ResponseResourceTypeMissing, null, "Missing required resource: {0}. Validation limited to basics only.", resourceType));
                    // Create a new schema based on what's available in the json
                    schema = new JsonSchema(actualResponseBodyJson, new CodeBlockAnnotation { ResourceType = expectedResponseAnnotation.ResourceType });
                }

                this.ValidateJsonCompilesWithSchema(schema, new JsonExample(actualResponseBodyJson, expectedResponseAnnotation), issues, options: options);
                return issues.Issues.Count() == 0;
            }
        }

        /// <summary>
        /// Validates that the actual response body matches the schema defined for the response and any additional constraints
        /// from the expected request (e.g. properties that are included in the expected response are required in the actual 
        /// response even if the metadata defines that the response is truncated)
        /// </summary>
        internal bool ValidateResponseMatchesSchema(MethodDefinition method, HttpResponse actualResponse, HttpResponse expectedResponse, IssueLogger issues, ValidationOptions options = null)
        {
            var expectedResourceType = method.ExpectedResponseMetadata.ResourceType;

            switch (expectedResourceType)
            {
                case "stream":
                case "Stream":
                case "Edm.stream":
                case "Edm.Stream":
                    // No validation since we're streaming data
                    return true;
                case "string":
                case "String":
                case "Edm.String":
                case" Edm.string":
                    return true;
            }

            // Get a reference of our JsonSchema that we're checking the response with
            var expectedResponseJson = (null != expectedResponse) ? expectedResponse.Body : null;
            JsonSchema schema = this.GetJsonSchema(expectedResourceType, issues, expectedResponseJson);

            if (null == schema)
            {
                issues.Error(ValidationErrorCode.ResourceTypeNotFound, $"Unable to locate a definition for resource type: {expectedResourceType}");
            }
            else
            {
                this.ValidateJsonCompilesWithSchema(schema, new JsonExample(actualResponse.Body, method.ExpectedResponseMetadata), issues, (null != expectedResponseJson) ? new JsonExample(expectedResponseJson) : null, options);
            }

            if (issues.Issues.WereWarningsOrErrors() && method.ExpectedResponseMetadata.ResourceTypeAka != null)
            {
                var expectedResourceTypeAka = method.ExpectedResponseMetadata.ResourceTypeAka;
                schema = this.GetJsonSchema(expectedResourceTypeAka, issues, expectedResponseJson);
                if (null == schema)
                {
                    issues.Error(ValidationErrorCode.ResourceTypeNotFound, $"Unable to locate a definition for resource type: {expectedResourceTypeAka}");
                }
                else
                {
                    this.ValidateJsonCompilesWithSchema(schema, new JsonExample(actualResponse.Body, method.ExpectedResponseMetadata), issues, (null != expectedResponseJson) ? new JsonExample(expectedResponseJson) : null, options);
                }
            }

            return !issues.Issues.WereWarningsOrErrors();
        }

        /// <summary>
        /// Returns a JSON schema reference, either by finding an example in the registered schema or by 
        /// creating a new temporary schema from the fallback.
        /// </summary>
        /// <param name="resourceType"></param>
        /// <param name="issues"></param>
        /// <param name="jsonStringForFallbackIfMissingResource"></param>
        /// <returns></returns>
        public JsonSchema GetJsonSchema(string resourceType, IssueLogger issues, string jsonStringForFallbackIfMissingResource)
        {
            JsonSchema schema;
            if (string.IsNullOrEmpty(resourceType))
            {
                issues.Error(ValidationErrorCode.ResponseResourceTypeMissing, "Resource type was null or missing in response metadata, so we assume there is no response to validate.");
                schema = JsonSchema.EmptyResponseSchema;
            }
            else if (!this.registeredSchema.TryGetValue(resourceType, out schema) && !string.IsNullOrEmpty(jsonStringForFallbackIfMissingResource))
            {
                issues.Warning(ValidationErrorCode.ResponseResourceTypeMissing, $"Missing required resource: {resourceType}. Validation based on fallback example.");
                // Create a new schema based on what's avaiable in the expected response JSON
                schema = new JsonSchema(jsonStringForFallbackIfMissingResource, new CodeBlockAnnotation { ResourceType = resourceType });
            }
            
            return schema;
        }

        /// <summary>
        /// Examines input json string to ensure that it compiles with the JsonSchema definition. Any errors in the
        /// validation of the schema are returned via the errors out parameter.
        /// </summary>
        /// <param name="schema">Schemas definition used as a reference.</param>
        /// <param name="inputJson">Input json example to be validated</param>
        /// <param name="issues"></param>
        /// <param name="expectedJson"></param>
        /// <returns></returns>
        public bool ValidateJsonCompilesWithSchema(JsonSchema schema, JsonExample inputJson, IssueLogger issues, JsonExample expectedJson = null, ValidationOptions options = null)
        {
            if (null == schema)
            {
                throw new ArgumentNullException("schema");
            }

            if (null == inputJson)
            {
                throw new ArgumentNullException("inputJson");
            }

            string collectionPropertyName = "value";
            if (null != inputJson.Annotation && null != inputJson.Annotation.CollectionPropertyName)
            {
                collectionPropertyName = inputJson.Annotation.CollectionPropertyName;
            }

            // If we didn't get an options, create a new one with some defaults provided by the annotation
            options = options ?? new ValidationOptions();

            var annotationTruncated = (inputJson.Annotation ?? new CodeBlockAnnotation()).TruncatedResult;
            options.AllowTruncatedResponses = annotationTruncated || options.AllowTruncatedResponses;
            options.CollectionPropertyName = collectionPropertyName;

            return schema.ValidateJson(inputJson, issues, this.registeredSchema, options, expectedJson, schemaExample: true);
        }

        internal void RegisterJsonResources(IEnumerable<ResourceDefinition> resources)
        {
            foreach (var resource in resources)
            {
                this.RegisterJsonResource(resource);
            }
        }

        internal void Clear()
        {
            this.registeredSchema.Clear();
        }


    }


   
}
