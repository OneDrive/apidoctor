using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

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

        [JsonIgnore]
        public string AccessToken { get; set; }


        /// <summary>
        /// Search for a .JSON file that includes accounts and load accounts from it.
        /// </summary>
        /// <returns></returns>
        public static Account[] LoadFromConfig(string docSetPath)
        {
            DirectoryInfo source = new DirectoryInfo(docSetPath);
            var potentialFiles = source.GetFiles("*.json", SearchOption.AllDirectories);
            foreach (var file in potentialFiles)
            {
                // See if this file maps to what we're looking for.
                using (var reader = file.OpenText())
                {
                    try
                    {
                        var configFile = JsonConvert.DeserializeObject<AccountConfigFile>(reader.ReadToEnd());
                        if (configFile.Accounts != null && configFile.Accounts.Length > 0)
                        {
                            Console.WriteLine("Using account configuration file: {0}", file.FullName);
                            return configFile.Accounts;
                        }
                    }
                    catch { } 
                }
            }
            return null;
        }

        /// <summary>
        /// Read command environmental variables to build an account
        /// </summary>
        /// <returns></returns>
        public static Account CreateAccountFromEnvironmentVariables()
        {
            string clientId = GetEnvVariable("oauth-client-id");
            string clientSecret = GetEnvVariable("oauth-client-secret");
            string tokenService = GetEnvVariable("oauth-token-service");
            string redirectUri = GetEnvVariable("oauth-redirect-uri");
            string refreshToken = GetEnvVariable("oauth-refresh-token");

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) ||
                string.IsNullOrEmpty(tokenService) || string.IsNullOrEmpty(refreshToken))
            {
                throw new InvalidOperationException("Missing value for one or more required environmental variables.");
            }

            return new Account
            {
                Name = "FromEnvironmentVariables",
                Enabled = true,
                ClientId = clientId,
                ClientSecret = clientSecret,
                TokenService = tokenService,
                RedirectUri = redirectUri,
                RefreshToken = refreshToken
            };
        }

        private static string GetEnvVariable(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            //if (string.IsNullOrEmpty(value))
            //{
            //    Console.WriteLine("No value for variable {0}", name);
            //}
            return value;
        }


    }


    internal class AccountConfigFile
    {
        [JsonProperty("accounts")]
        public Account[] Accounts { get; set; }
    }
}
