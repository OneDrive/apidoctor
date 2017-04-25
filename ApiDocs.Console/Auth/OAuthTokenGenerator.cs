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
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class OAuthTokenGenerator
    {
        public static async Task<TokenResponse> RedeemRefreshTokenAsync(OAuthAccount account)
        {
            try
            {
                return await RedeemRefreshTokenInternalAsync(account);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error redeeming token: " + ex.Message);
                return null;
            }
        }


        private static async Task<TokenResponse> RedeemRefreshTokenInternalAsync(OAuthAccount account)
        {
            HttpWebRequest request = WebRequest.CreateHttp(account.TokenService);
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";

            var requestStream = await request.GetRequestStreamAsync();
            var writer = new StreamWriter(requestStream);

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("client_id={0}", Uri.EscapeDataString(account.ClientId));
            sb.AppendFormat("&redirect_uri={0}", Uri.EscapeDataString(account.RedirectUri));
            if (account.ClientSecret != null)
                sb.AppendFormat("&client_secret={0}", Uri.EscapeDataString(account.ClientSecret));
            sb.AppendFormat("&refresh_token={0}", Uri.EscapeDataString(account.RefreshToken));
            sb.Append("&grant_type=refresh_token");
            if (null != account.Resource)
                sb.AppendFormat("&resource={0}", Uri.EscapeDataString(account.Resource));

            var content = sb.ToString();
            await writer.WriteAsync(content);
            await writer.FlushAsync();


            // TODO: Expect to encounter a 401 here, and handle the response: {"error":"invalid_client","error_description":"AADSTS70002: Error validating credentials. AADSTS50012: Invalid client secret is provided.\r\nTrace ID: b416d386-2d5e-40b9-889a-6b2286b30100\r\nCorrelation ID: 2a021806-2134-4610-8bd4-760727cb4465\r\nTimestamp: 2017-04-20 17:47:22Z","error_codes":[70002,50012],"timestamp":"2017-04-20 17:47:22Z","trace_id":"b416d386-2d5e-40b9-889a-6b2286b30100","correlation_id":"2a021806-2134-4610-8bd4-760727cb4465"}

            var response = await request.GetResponseAsync() as HttpWebResponse;
            if (null != response && response.StatusCode == HttpStatusCode.OK)
            {
                var responseStream = response.GetResponseStream();
                if (null != responseStream)
                {
                    var reader = new StreamReader(responseStream);
                    return JsonConvert.DeserializeObject<TokenResponse>(await reader.ReadToEndAsync());
                }
            }
            return null;
        }
    }

    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
    }
}
