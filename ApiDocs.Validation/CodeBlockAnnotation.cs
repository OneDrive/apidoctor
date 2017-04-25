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

namespace ApiDocs.Validation
{
    using System;
    using System.Diagnostics;
    using MarkdownDeep;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class CodeBlockAnnotation
    {
        /// <summary>
        /// The OData type name of the resource
        /// </summary>
        [JsonProperty("@type", NullValueHandling=NullValueHandling.Ignore )]
        public string ResourceType { get; set; }

        [JsonProperty("@odata.type", NullValueHandling = NullValueHandling.Ignore)]
        public string LegacyResourceType
        {
            get { return ResourceType; }
            set { this.ResourceType = value; }
        }

        /// <summary>
        /// Type of code block
        /// </summary>
        [JsonProperty("blockType", Order=-2), JsonConverter(typeof(StringEnumConverter))]
        public CodeBlockType BlockType { get; set; }

        /// <summary>
        /// Specify the name of properties in the schema which are optional
        /// </summary>
        [JsonProperty("optionalProperties", NullValueHandling=NullValueHandling.Ignore)]
        public string[] OptionalProperties { get; set; }

        /// <summary>
        /// Specify that the result is a collection of the resource type instead of a single instance.
        /// </summary>
        [JsonProperty("isCollection", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsCollection { get; set; }

        /// <summary>
        /// Specify the name of the property that holds the array of items.
        /// </summary>
        [JsonProperty("collectionProperty", DefaultValueHandling=DefaultValueHandling.Ignore)]
        public string CollectionPropertyName { get; set; }

        /// <summary>
        /// Indicates that the response is empty (has no value)
        /// </summary>
        [JsonProperty("isEmpty", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsEmpty { get; set; }

        /// <summary>
        /// Specifies that the example is truncated and should not generate warnings about 
        /// missing fields unless those fields are shown in the example.
        /// </summary>
        [JsonProperty("truncated", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool TruncatedResult { get; set; }

        /// <summary>
        /// The name of the request / response method.
        /// </summary>
        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string MethodName { get; set; }

        /// <summary>
        /// Indicates that the response is expected to be an error response.
        /// </summary>
        [JsonProperty("expectError", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool ExpectError { get; set; }

        /// <summary>
        /// By default all properties are expected to be non-null values. If a null value is returned
        /// in the JSON ("foo": null) an error is generated. This can be used on a resource to allow
        /// some properties to be returned as null.
        /// </summary>
        [JsonProperty("nullableProperties", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string[] NullableProperties { get; set; }

        /// <summary>
        /// When provided indicates that the response is a long running operation that will return an 
        /// asyncJobStatus response from a Location URL. When the job is complete, a Location URL will
        /// be returned that returns a response with the resource type indicated by this property.
        /// </summary>
        [JsonProperty("longRunningResponseType", DefaultValueHandling =  DefaultValueHandling.Ignore)]
        public string LongRunningResponseType { get; set; }

        /// <summary>
        /// For blockType=resource, defines the property in the resource that is considered to be 
        /// the "key" / "index" property. This also converts the resource into an EntityType for OData
        /// purposes.
        /// </summary>
        [JsonProperty("keyProperty", DefaultValueHandling=DefaultValueHandling.Ignore)]
        public string KeyPropertyName { get; set; }

        /// <summary>
        /// Use this property to delcare that a custom function request is idempotent (has no side-effects).
        /// </summary>
        [JsonProperty("idempotent", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsIdempotent { get; set; }

        /// <summary>
        /// Space seperated list of OAuth scopes required for this method to be invoked.
        /// </summary>
        [JsonProperty("scopes", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Scopes { get; set; }

        [JsonProperty("baseType", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string BaseType { get; set; }

        public string[] RequiredScopes
        {
            get
            {
                if (string.IsNullOrEmpty(this.Scopes))
                    return new string[0];
                else
                    return this.Scopes.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        /// <summary>
        /// Convert a JSON string into an instance of this class
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static CodeBlockAnnotation ParseMetadata(string json, MarkdownDeep.Block codeBlock = null)
        {
            var response = JsonConvert.DeserializeObject<CodeBlockAnnotation>(json);

            if (codeBlock != null)
            {
                // See if we can infer anything that's missing from response
                if (response.BlockType == CodeBlockType.Unknown)
                {
                    response.BlockType = InferBlockType(codeBlock, response.ResourceType);
                }
            }
            return response;
        }

        /// <summary>
        /// Based on data in the codeBlock, see if we can infer what kind of block this is
        /// </summary>
        /// <param name="codeBlock"></param>
        /// <returns></returns>
        private static CodeBlockType InferBlockType(Block codeBlock, string resourceTypeName = null)
        {
            if (codeBlock.CodeLanguage == "http")
            {
                // See if this is an HTTP request or HTTP response
                Http.HttpParser parser = new Http.HttpParser();
                try
                {
                    parser.ParseHttpRequest(codeBlock.Content);
                    return CodeBlockType.Request;
                }
                catch { }

                try
                {
                    parser.ParseHttpResponse(codeBlock.Content);
                    return CodeBlockType.Response;
                }
                catch { }
            }
            else if (codeBlock.CodeLanguage == "json" && !string.IsNullOrEmpty(resourceTypeName))
            {
                return CodeBlockType.Resource;
            }

            return CodeBlockType.Unknown;
        }

        public ParameterDataType Type
        {
            get { return new ParameterDataType(this.ResourceType, this.IsCollection); }
        }

        /// <summary>
        /// Indicates that a resource is extensible with additional properties that 
        /// may not be defined in the documtnation.
        /// </summary>
        [JsonProperty("openType")]
        public bool IsOpenType { get; set; }

        [JsonProperty("target")]
        public TargetType Target { get; set; }
    }

    public enum TargetType
    {
        Unspecified,
        Object,
        Collection,
        Action,
        Function
    }

    public enum CodeBlockType
    {
        /// <summary>
        /// Default value that indicates parsing failed.
        /// </summary>
        Unknown,

        /// <summary>
        /// Resource type definition
        /// </summary>
        Resource,

        /// <summary>
        /// Raw HTTP request to the API
        /// </summary>
        Request,

        /// <summary>
        /// Raw HTTP response from the API
        /// </summary>
        Response,

        /// <summary>
        /// Ignored code block. No processing is done
        /// </summary>
        Ignored,

        /// <summary>
        /// Example code block. Should be checked for JSON correctness and resources
        /// </summary>
        Example,

        /// <summary>
        /// A simulated response, used for unit testing.
        /// </summary>
        SimulatedResponse,

        /// <summary>
        /// A block representing a test parameter definition for the preceding example
        /// </summary>
        TestParams,
    }
}
