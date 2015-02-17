using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace OneDrive.ApiDocumentation.Validation
{
    internal class SwaggerResource
    {

    }

    internal class SwaggerMethod 
    {
        [JsonProperty("summary", NullValueHandling=NullValueHandling.Ignore)]
        public string Summary { get; set; }

        [JsonProperty("description", NullValueHandling=NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("parameters", NullValueHandling=NullValueHandling.Ignore)]
        public List<SwaggerParameter> Parameters { get; set; }

        [JsonProperty("tags", NullValueHandling=NullValueHandling.Ignore)]
        public List<string> Tags { get; set; }

        [JsonProperty("security", NullValueHandling=NullValueHandling.Ignore)]
        public List<Dictionary<string, string[]>> Security { get ; set; }

        /// <summary>
        /// Key is either the response status "200" or "default"
        /// </summary>
        [JsonProperty("responses", NullValueHandling=NullValueHandling.Ignore)]
        public Dictionary<string, SwaggerResponse> Responses { get; set; }

        public SwaggerMethod()
        {
            Parameters = new List<SwaggerParameter>();
            Tags = new List<string>();
            Responses = new Dictionary<string, SwaggerResponse>();

            Security = new List<Dictionary<string, string[]>>();
        }

        public void AddRequiredSecurityRoles(string provider, IEnumerable<string> roles)
        {
            Security.Add(new Dictionary<string, string[]> 
            { 
                { provider, roles.ToArray() }
            });

        }

    }

    internal class SwaggerParameter
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("in")]
        public string In { get; set; }
        [JsonProperty("description", NullValueHandling=NullValueHandling.Ignore)]
        public string Description { get; set; }
        [JsonProperty("required")]
        public bool Required { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("format", NullValueHandling=NullValueHandling.Ignore)]
        public string Format { get; set; }



        //[JsonProperty("default", NullValueHandling = NullValueHandling.Ignore)]
        //public object DefaultValue { get; set; }
        //[JsonProperty("minimum", NullValueHandling = NullValueHandling.Ignore)]
        //public object Minimum { get; set; }
        //[JsonProperty("maximum", NullValueHandling = NullValueHandling.Ignore)]
        //public object Maximum { get; set; }

    }

    internal class SwaggerResponse
    {
        public SwaggerResponse()
        {
            Description = string.Empty;
        }

        [JsonProperty("description", NullValueHandling=NullValueHandling.Ignore)]
        public string Description { get; set; }
        [JsonProperty("schema", NullValueHandling = NullValueHandling.Ignore)]
        public object Schema { get; set; }
    }
}

