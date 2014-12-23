namespace OneDrive.ApiDocumentation.Validation.Param
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class RequestParameters
    {

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("note")]
        public string Note { get; set; }

        [JsonProperty("parameters")]
        public ParameterValue[] Parameters { get; set; }

        public static RequestParameters[] ReadFromJson(string json)
        {
            var results = JsonConvert.DeserializeObject<List<RequestParameters>>(json);
            return results.ToArray();
        }
    }

    public class ParameterValue
    {
        [JsonProperty("name")]
        public string Id { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("location")]
        public ParameterLocation Location { get; set; }
    }

    public enum ParameterLocation
    {
        Url,
        Json
    }
}
