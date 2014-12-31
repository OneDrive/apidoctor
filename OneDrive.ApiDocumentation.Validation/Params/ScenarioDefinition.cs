namespace OneDrive.ApiDocumentation.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using System.ComponentModel;

    /// <summary>
    /// Class represents information about a set of parameters that are used to make a request
    /// to the service.
    /// </summary>
    public class ScenarioDefinition
    {
        public ScenarioDefinition()
        {
            this.StaticParameters = new List<PlaceholderValue>();
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("method")]
        public string Method  { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("values")]
        public List<PlaceholderValue> StaticParameters { get; set; }

        [JsonProperty("values-from-request")]
        public PlaceholderRequest DynamicParameters { get ; set; }

        public static ScenarioDefinition[] ReadFromJson(string json)
        {
            var results = JsonConvert.DeserializeObject<List<ScenarioDefinition>>(json);
            return results.ToArray();
        }

    }

    public enum PlaceholderLocation
    {
        Url,
        Json,
        Body
    }
}
