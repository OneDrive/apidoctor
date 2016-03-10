/*
 * Markdown Scanner
 * Copyright (c) Microsoft Corporation
 * All rights reserved. 
 * 
 * MIT License
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of 
 * this software and associated documentation files (the ""Software""), to deal in 
 * the Software without restriction, including without limitation the rights to use, 
 * copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
 * Software, and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all 
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace ApiDocs.ConsoleApp.Auth
{
    using System;
    using ApiDocs.Validation;
    using Newtonsoft.Json;
    using System.Threading.Tasks;

    public class OAuthAccount : IServiceAccount
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("oauthType")]
        public OAuthAccountType Type { get; set; }

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
        public string BaseUrl { get; set; }

        [JsonProperty("resource")]
        public string Resource { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("additionalHeaders")]
        public string[] AdditionalHeaders { get; set; }

        [JsonProperty("scopes")]
        public string[] Scopes { get; set; }

        [JsonIgnore]
        public string AccessToken { get; set; }

        /// <summary>
        /// Read command environmental variables to build an account
        /// </summary>
        /// <returns></returns>
        public static OAuthAccount CreateAccountFromEnvironmentVariables()
        {
            return new OAuthAccount
            {
                Name = "DefaultAccount",
                Enabled = true,
                RefreshToken = ReadVariable("oauth-refresh-token"),
                ClientId = ReadVariable("oauth-client-id"),
                ClientSecret = ReadVariable("oauth-client-secret"),
                TokenService = ReadVariable("oauth-token-service"),
                RedirectUri = ReadVariable("oauth-redirect-uri"),
                Scopes = ReadVariable("oauth-scopes").Split(',')
            };
        }

        private static string ReadVariable(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException(
                    string.Format("No value was found for environment variable: {0}", name));
            }
            return value;
        }

        public async Task PrepareForRequestAsync()
        {
            switch (this.Type)
            {
                case OAuthAccountType.RefreshToken:
                    await RedeemRefreshTokenAsync();
                    break;

                case OAuthAccountType.UserPassword:
                    await RedeemUsernameAndPasswordAsync();
                    break;

                default:
                    throw new NotSupportedException("Unsupported oauthMode value:" + this.Type.ToString());
            }
        }

        private async Task RedeemRefreshTokenAsync()
        {
            // Make sure we have an access token for this API
            if (string.IsNullOrEmpty(this.AccessToken))
            {
                var tokens = await OAuthTokenGenerator.RedeemRefreshTokenAsync(this);
                if (null != tokens)
                {
                    this.AccessToken = tokens.AccessToken;
                }
                else
                {
                    throw new InvalidOperationException(
                        string.Format("Failed to retrieve access token for account: {0}", this.Name));
                }
            }
        }

        private async Task RedeemUsernameAndPasswordAsync()
        {
            if (string.IsNullOrEmpty(this.AccessToken))
            {
                var creds = new Microsoft.IdentityModel.Clients.ActiveDirectory.UserCredential(this.Username, this.Password);
                var context = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(this.TokenService);
                var token = await context.AcquireTokenAsync(this.Resource, this.ClientId, creds);
                if (null != token)
                {
                    this.AccessToken = token.AccessToken;
                }
                else
                {
                    throw new InvalidOperationException(
                        string.Format("Failed to convert username + password to access token for account: {0}", this.Name));
                }
            }
        }

        public AuthenicationCredentials CreateCredentials()
        {
            return OAuthCredentials.CreateAutoCredentials(this.AccessToken);
        }
    }

    public enum OAuthAccountType
    {
        RefreshToken,
        UserPassword
    }


    


}
