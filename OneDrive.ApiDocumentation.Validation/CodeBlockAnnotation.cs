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
        [JsonProperty("@odata.type")]
        public string ResourceType { get; set; }

        /// <summary>
        /// Type of code block
        /// </summary>
        [JsonProperty("blockType"), JsonConverter(typeof(StringEnumConverter))]
        public CodeBlockType BlockType { get; set; }

        /// <summary>
        /// Specify the name of properties in the schema which are optional
        /// </summary>
        [JsonProperty("optionalProperties", NullValueHandling=NullValueHandling.Ignore)]
        public string[] OptionalProperties { get; set; }

        /// <summary>
        /// Speicfy that the result is a collection of the resource type instead of a single instance.
        /// </summary>
        [JsonProperty("isCollection")]
        public bool IsCollection { get; set; }


        [JsonProperty("truncated")]
        public bool TruncatedResult { get; set; }

        public static CodeBlockAnnotation FromJson(string json)
        {
            return JsonConvert.DeserializeObject<CodeBlockAnnotation>(json);
        }
    }

    public enum CodeBlockType
    {
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
        Ignored
    }
}
