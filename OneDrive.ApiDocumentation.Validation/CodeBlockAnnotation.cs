using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
namespace OneDrive.ApiDocumentation.Validation
{
    public class CodeBlockAnnotation
    {
        /// <summary>
        /// The OData type name of the resource
        /// </summary>
        [JsonProperty("@odata.type", NullValueHandling=NullValueHandling.Ignore )]
        public string ResourceType { get; set; }

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
        /// Speicfy that the result is a collection of the resource type instead of a single instance.
        /// </summary>
        [JsonProperty("isCollection", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsCollection { get; set; }

        [JsonProperty("isEmpty", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsEmpty { get; set; }

        [JsonProperty("truncated", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool TruncatedResult { get; set; }

        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string MethodName { get; set; }

        [JsonProperty("expectError", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool ExpectError { get; set; }

        [JsonProperty("disabled", DefaultValueHandling=DefaultValueHandling.Ignore)]
        public bool Disabled { get; set; }

        public static CodeBlockAnnotation FromJson(string json)
        {
            
            return JsonConvert.DeserializeObject<CodeBlockAnnotation>(json);
        }
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
        Example
    }
}
