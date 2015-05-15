using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OneDrive.ApiDocumentation.ConsoleApp
{
    public class Account
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        [JsonProperty("clientSecret")]
        public string ClientSecret { get; set; }

        [JsonProperty("tokenService")]
        public string TokenService { get; set; }

        [JsonProperty("redirectUri")]
        public string RedirectUri { get; set; }

        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; }

        [JsonProperty("serviceUrl")]
        public string ServiceUrl { get; set; }
    }
}
