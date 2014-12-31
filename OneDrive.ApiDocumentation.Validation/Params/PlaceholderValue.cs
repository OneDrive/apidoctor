namespace OneDrive.ApiDocumentation.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using System.ComponentModel;

    public class PlaceholderValue
    {
        public PlaceholderValue()
        {

        }

        [JsonProperty("placeholder")]
        public string PlaceholderText { get; set; }

        [JsonProperty("location"), JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public PlaceholderLocation Location { get; set; }

        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public string Value { get; set; }

        [JsonProperty("path", NullValueHandling = NullValueHandling.Ignore)]
        public string Path { get; set; }

    }
}
