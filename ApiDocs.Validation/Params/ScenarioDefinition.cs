namespace ApiDocs.Validation.Params
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Class represents information about a set of parameters that are used to make a request
    /// to the service.
    /// </summary>
    public class ScenarioDefinition : BasicRequestDefinition
    {
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
                return string.Concat(this.Description, " (", this.MethodName, ")");
            }
        }
    }

    /// <summary>
    /// Class represents a canned request that can be referenced across test sceanrios
    /// </summary>
    public class CannedRequestDefinition : TestSetupRequestDefinition
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public enum PlaceholderLocation
    {
        Invalid,
        Url,
        Json,
        HttpHeader,
        Body,
        StoredValue,
        BodyBase64Encoded
    }
}
