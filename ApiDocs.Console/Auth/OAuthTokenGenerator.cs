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
        public static async Task<TokenResponse> RedeemRefreshTokenAsync(Account account)
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


        private static async Task<TokenResponse> RedeemRefreshTokenInternalAsync(Account account)
        {
            HttpWebRequest request = WebRequest.CreateHttp(account.TokenService);
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";

            var requestStream = await request.GetRequestStreamAsync();
            var writer = new StreamWriter(requestStream);

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("client_id={0}", Uri.EscapeDataString(account.ClientId));
            sb.AppendFormat("&redirect_uri={0}", Uri.EscapeDataString(account.RedirectUri));
            sb.AppendFormat("&client_secret={0}", Uri.EscapeDataString(account.ClientSecret));
            sb.AppendFormat("&refresh_token={0}", Uri.EscapeDataString(account.RefreshToken));
            sb.Append("&grant_type=refresh_token");
            if (null != account.Resource)
                sb.AppendFormat("&resource={0}", Uri.EscapeDataString(account.Resource));

            var content = sb.ToString();
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
