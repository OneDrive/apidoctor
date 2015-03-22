using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.Validation
{
    /// <summary>
    /// Page annotation allows you to make page-level annotations for a variety of reasons
    /// </summary>
    public class PageAnnotation
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description"), MaxLength(156)]
        public string Description { get; set; }

        [JsonProperty("keywords"), MaxLength(156)]
        public string Keywords { get; set; }

        [JsonProperty("cononicalUrl")]
        public string CononicalUrl { get; set; }
    }
    

    public class MaxLengthAttribute : Attribute 
    {
        public MaxLengthAttribute(int maximumLength)
        {
            MaximumLength = maximumLength;
        }

        public int MaximumLength { get; set; }

        public static int GetMaxLength(Type type, string propertyName)
        {
            var attribute = (MaxLengthAttribute)type.GetProperty(propertyName).GetCustomAttributes(true).Where(x => x is MaxLengthAttribute).FirstOrDefault();
            if (null != attribute)
            {
                return attribute.MaximumLength;
            }
            return -1;
        }
    }
}
