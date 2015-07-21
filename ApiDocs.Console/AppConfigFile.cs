namespace ApiDocs.ConsoleApp
{
    using ApiDocs.ConsoleApp.Auth;
    using ApiDocs.Validation.Config;
    using Newtonsoft.Json;

    public class AppConfigFile : ConfigFile
    {
        [JsonProperty("accounts")]
        public Account[] Accounts { get; set; }

        [JsonProperty("checkServiceEnabledBranches")]
        public string[] CheckServiceEnabledBranches { get; set; }


        public override bool IsValid
        {
            get { return null != this.Accounts || null != this.CheckServiceEnabledBranches; }
        }
    }
}
