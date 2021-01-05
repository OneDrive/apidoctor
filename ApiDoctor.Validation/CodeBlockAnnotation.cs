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

namespace ApiDoctor.Validation
{
    using System;
    using System.Diagnostics;
    using MarkdownDeep;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using ApiDoctor.Validation.Error;

    public class FlexibleLowerCamelStringEnumConverter : StringEnumConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var val = value?.ToString();
            if (val != null && char.IsUpper(val[0]))
            {
                val = char.ToLowerInvariant(val[0]) + val.Substring(1);
            }

            writer.WriteValue(val);
        }
    }

    public class ODataAnnotations
    {
        [JsonProperty("property", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Property { get; set; }

        [JsonProperty("capabilities", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ODataCapabilities Capabilities { get; set; }
    }

    public class ODataCapabilities
    {
        [JsonProperty("changeTracking", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? ChangeTracking;

        [JsonProperty("computed", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? Computed;

        [JsonProperty("countable", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? Countable;

        [JsonProperty("deletable", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? Deletable;

        [JsonProperty("expandable", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? Expandable;

        [JsonProperty("filterable", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? Filterable;

        [JsonProperty("insertable", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? Insertable;

        [JsonProperty("navigability", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Navigability;

        [JsonProperty("permissions", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Permissions;

        [JsonProperty("referenceable", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? Referenceable;

        [JsonProperty("searchable", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? Searchable;

        [JsonProperty("sortable", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? Sortable;

        [JsonProperty("selectable", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? Selectable;

        [JsonProperty("skippable", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? Skippable;

        [JsonProperty("toppable", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? Toppable;

        [JsonProperty("updatable", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? Updatable;


        public void MergeWith(ODataCapabilities other, IssueLogger issues)
        {
            if (!string.IsNullOrEmpty(other.Navigability))
            {
                if (string.IsNullOrEmpty(this.Navigability))
                {
                    this.Navigability = other.Navigability;
                }

                if (!string.Equals(this.Navigability, other.Navigability))
                {
                    issues.Warning(ValidationErrorCode.Unknown, "Mismatched 'navigability' attributes.");
                }
            }

            if (!string.IsNullOrEmpty(other.Permissions))
            {
                if (string.IsNullOrEmpty(this.Permissions))
                {
                    this.Permissions = other.Permissions;
                }

                if (!string.Equals(this.Permissions, other.Permissions))
                {
                    issues.Warning(ValidationErrorCode.Unknown, "Mismatched 'permissions' attributes.");
                }
            }

            MergeNullables(ref this.ChangeTracking, other.ChangeTracking);
            MergeNullables(ref this.Computed, other.Computed);
            MergeNullables(ref this.Countable, other.Countable);
            MergeNullables(ref this.Deletable, other.Deletable);
            MergeNullables(ref this.Expandable, other.Expandable);
            MergeNullables(ref this.Filterable, other.Filterable);
            MergeNullables(ref this.Insertable, other.Insertable);
            MergeNullables(ref this.Referenceable, other.Referenceable);
            MergeNullables(ref this.Searchable, other.Searchable);
            MergeNullables(ref this.Selectable, other.Selectable);
            MergeNullables(ref this.Skippable, other.Skippable);
            MergeNullables(ref this.Sortable, other.Sortable);
            MergeNullables(ref this.Toppable, other.Toppable);
            MergeNullables(ref this.Updatable, other.Updatable);
        }

        private void MergeNullables(ref bool? original, bool? other)
        {
            if (other.HasValue)
            {
                original = other.GetValueOrDefault() || original.GetValueOrDefault();
            }
        }
    }

    public class CodeBlockAnnotation
    {
        private string originalRawJson { get; set; }
        private DocFile sourceFile { get; set; }

        [JsonIgnore]
        public string ResourceType
        {
            get { return this.NonAnnotatedResourceType ?? this.OdataAnnotatedResourceType; }
            set
            {
                if (this.OdataAnnotatedResourceType != null)
                {
                    this.OdataAnnotatedResourceType = value;
                }
                else
                {
                    this.NonAnnotatedResourceType = value;
                }
            }
        }
        /// <summary>
        /// Type of code block
        /// </summary>

        [JsonProperty("blockType", Order=-2), JsonConverter(typeof(FlexibleLowerCamelStringEnumConverter))]
        public CodeBlockType BlockType { get; set; }

        /// <summary>
        /// Specify that the result is a collection of the resource type instead of a single instance.
        /// </summary>
        [JsonProperty("abstract", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Abstract { get; set; }

        [JsonProperty("baseType", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string BaseType { get; set; }

        /// <summary>
        /// Indicates that a resource is extensible with additional properties that 
        /// may not be defined in the documtnation.
        /// </summary>
        [JsonProperty("openType")]
        public bool IsOpenType { get; set; }

        /// <summary>
        /// Indicates that the URL in the example should not be used to infer API shape.
        /// The URL is completely opaque and should be used as-returned from a previous API call.
        /// This tool will avoid assuming the structure of the URL contributes to the resource model.
        /// </summary>
        [JsonProperty("opaqueUrl")]
        public bool OpaqueUrl { get; set; }

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
        /// Indicates that the function is composable.
        /// Only required if there are no examples in the documentation of the function being composed.
        /// </summary>
        [JsonProperty("isComposable", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsComposable { get; set; }

        /// <summary>
        /// Indicates that the resource is an odata media entity. Sets the HasStream attribute to true in the edmx.
        /// </summary>
        [JsonProperty("isMediaEntity", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsMediaEntity { get; set; }

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
        [JsonConverter(typeof(SingleOrArrayConverter<string>))]
        public List<string> MethodName { get; set; }

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
        /// Examples typically include variables like {item-id} instead of example keys.
        /// In cases where an example is needed, list the example key in this array.
        /// </summary>
        [JsonProperty("sampleKeys", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string[] SampleKeys { get; set; }

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
        /// Use this property to declare that a custom function request is idempotent (has no side-effects).
        /// </summary>
        [JsonProperty("idempotent", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsIdempotent { get; set; }

        /// <summary>
        /// Space separated list of OAuth scopes required for this method to be invoked.
        /// </summary>
        [JsonProperty("scopes", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Scopes { get; set; }

        /// <summary>
        /// Space separated list of api versions required for this method to be invoked.
        /// </summary>
        [JsonProperty("apiVersions", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ApiVersions { get; set; }

        /// <summary>
        /// Space separated list of tags that represent capabilities required for this method to be invoked.
        /// </summary>
        [JsonProperty("tags", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Tags
        {
            get
            {
                if (this.TagList != null && this.TagList.Count > 0)
                {
                    return string.Join(" ", this.TagList);
                }

                return null;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    this.TagList = null;
                }

                this.TagList = value.
                    Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).
                    Select(s => s.Trim()).
                    ToList();
            }
        }

        [JsonIgnore]
        public List<string> TagList { get; set; }

        /// <summary>
        /// The OData type name of the resource
        /// </summary>
        [JsonProperty("@type", NullValueHandling = NullValueHandling.Ignore)]
        public string NonAnnotatedResourceType { get; set; }

        [JsonProperty("@type.aka", NullValueHandling = NullValueHandling.Ignore)]
        public string ResourceTypeAka { get; set; }

        [JsonProperty("@odata.type", NullValueHandling = NullValueHandling.Ignore)]
        public string OdataAnnotatedResourceType { get; set; }

        [JsonProperty("@odata.annotations", NullValueHandling = NullValueHandling.Ignore)]
        public ODataAnnotations[] OdataAnnotations { get; set; }

        [JsonIgnore]
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

        [JsonIgnore]
        public string[] RequiredApiVersions
        {
            get
            {
                if (string.IsNullOrEmpty(this.ApiVersions))
                    return new string[0];
                else
                    return this.ApiVersions.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        [JsonIgnore]
        public string[] RequiredTags
        {
            get
            {
                if (string.IsNullOrEmpty(this.Tags))
                    return new string[0];
                else
                    return this.Tags.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }        

        public void PatchSourceFile()
        {
            if (this.sourceFile == null)
            {
                throw new InvalidOperationException("can only patch a resource that was read from a file");
            }

            var newJson = JsonConvert.SerializeObject(this, serializerSettings);
            var originalFile = File.ReadAllText(this.sourceFile.FullPath);
            var modified = originalFile.Replace(this.originalRawJson, newJson);
            File.WriteAllText(this.sourceFile.FullPath, modified);
        }

        private static JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Formatting = Formatting.Indented,
        };

        /// <summary>
        /// Convert a JSON string into an instance of this class
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static CodeBlockAnnotation ParseMetadata(string json, MarkdownDeep.Block codeBlock = null, DocFile sourceFile = null)
        {
            var response = JsonConvert.DeserializeObject<CodeBlockAnnotation>(json);

            response.originalRawJson = json;
            response.sourceFile = sourceFile;

            // Check for Collection() syntax
            if (!string.IsNullOrEmpty(response.ResourceType))
            {
                const string collectionPrefix = "collection(";
                const string collectionSuffix = ")";
                if (response.ResourceType.StartsWith(collectionPrefix, StringComparison.OrdinalIgnoreCase) && response.ResourceType.EndsWith(collectionSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    response.IsCollection = true;
                    var newResourceType = response.ResourceType.Substring(collectionPrefix.Length);
                    newResourceType = newResourceType.Substring(0, newResourceType.Length - collectionSuffix.Length);
                    response.ResourceType = newResourceType;
                }
            }

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

        private static HashSet<string> httpVerbs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "GET", "POST", "HEAD", "DELETE", "PATCH", "PUT", "OPTIONS",
        };

        /// <summary>
        /// Based on data in the codeBlock, see if we can infer what kind of block this is
        /// </summary>
        /// <param name="codeBlock"></param>
        /// <returns></returns>
        private static CodeBlockType InferBlockType(Block codeBlock, string resourceTypeName = null)
        {
            if (codeBlock.CodeLanguage == "http")
            {
                Http.HttpRequest request;
                if (Http.HttpParser.TryParseHttpRequest(codeBlock.Content, out request))
                    return CodeBlockType.Request;

                Http.HttpResponse response;
                if (Http.HttpParser.TryParseHttpResponse(codeBlock.Content, out response))
                    return CodeBlockType.Response;

                try
                {
                    var lines = codeBlock.Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines?.Length > 1 &&
                        lines.All(line => httpVerbs.Contains(line.Substring(0, line.IndexOf(' ') + 1).Trim())))
                    {
                        return CodeBlockType.Samples;
                    }
                }
                catch { }
            }
            else if (codeBlock.CodeLanguage == "json" && !string.IsNullOrEmpty(resourceTypeName))
            {
                return CodeBlockType.Resource;
            }

            return CodeBlockType.Unknown;
        }

        [JsonIgnore]
        public ParameterDataType Type
        {
            get
            {
                return this.ResourceType == null
                    ? null
                    : this.ResourceType.ParseParameterDataType(isCollection: this.IsCollection); }
        }

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
        /// Samples code block. Should be used to infer alternate ways of calling an API/function.
        /// </summary>
        Samples,

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
