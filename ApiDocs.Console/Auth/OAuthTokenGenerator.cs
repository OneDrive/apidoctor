namespace ApiDocs.ConsoleApp.Auth
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class OAuthTokenGenerator
    {
        public static async Task<TokenResponse> RedeemRefreshTokenAsync(Account account)
        {
            try
            {
                return await RedeemRefreshTokenAsync(account.TokenService, account.RefreshToken, account.ClientId, account.ClientSecret, account.RedirectUri);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error redeeming token: " + ex.Message);
                return null;
            }
        }


        public static async Task<TokenResponse> RedeemRefreshTokenAsync(string tokenService, string refreshToken, string clientId, string clientSecret, string redirectUri)
        {
            HttpWebRequest request = WebRequest.CreateHttp(tokenService);
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";

            var requestStream = await request.GetRequestStreamAsync();
            var writer = new StreamWriter(requestStream);

            var content = string.Format("client_id={0}&redirect_uri={1}&client_secret={2}&refresh_token={3}&grant_type=refresh_token", 
                Uri.EscapeDataString(clientId),
                Uri.EscapeDataString(redirectUri),
                Uri.EscapeDataString(clientSecret),
                Uri.EscapeDataString(refreshToken));

            await writer.WriteAsync(content);
            await writer.FlushAsync();

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
