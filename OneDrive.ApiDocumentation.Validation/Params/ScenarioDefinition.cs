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
    public class ScenarioDefinition : BasicRequestDefinition
    {
        public ScenarioDefinition()
        {
        }

        #region Json-Fed Properties
        [JsonProperty("name")]
        public string Description { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("test-setup", DefaultValueHandling=DefaultValueHandling.Ignore)]
        public List<TestSetupRequestDefinition> TestSetupRequests { get; set; }
        #endregion

        [JsonIgnore]
        public string DisplayText
        {
            get
            {
                return string.Concat(Description, " (", MethodName, ")");
            }
        }

        public static ScenarioDefinition[] ReadFromJson(string json)
        {
            var results = JsonConvert.DeserializeObject<List<ScenarioDefinition>>(json);
            return results.ToArray();
        }

        public ScenarioDefinition Copy()
        {
            string json = JsonConvert.SerializeObject(this);
            ScenarioDefinition copy = JsonConvert.DeserializeObject<ScenarioDefinition>(json);
            copy.Description = "Copy of " + copy.Description;
            return copy;
        }
    }

    public enum PlaceholderLocation
    {
        Invalid,
        Url,
        Json,
        HttpHeader,
        Body,
        StoredValue
    }
}
