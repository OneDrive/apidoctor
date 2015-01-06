using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;

namespace MicrosoftAccountSDK.Windows
{
    public static class MicrosoftAccountOAuth
    {

        public static async Task<string> LoginOneTimeAuthorizationAsync(string clientId, string[] scopes, IWin32Window owner = null)
        {
            return await FormMicrosoftAccountAuth.GetAuthenticationToken(clientId, scopes, OAuthFlow.ImplicitGrant, owner);
        }

        public static async Task<AppTokenResult> LoginAuthorizationCodeFlowAsync(string clientId, string clientSecret, string[] scopes, IWin32Window owner = null)
        {
            var authorizationCode = await FormMicrosoftAccountAuth.GetAuthenticationToken(clientId, scopes, OAuthFlow.AuthorizationCodeGrant, owner);
            if (string.IsNullOrEmpty(authorizationCode))
                return null;

            var tokens = await RedeemAuthorizationCodeAsync(clientId, FormMicrosoftAccountAuth.OAuthDesktopEndPoint, clientSecret, authorizationCode);
            return tokens;
        }

        private static async Task<AppTokenResult> RedeemAuthorizationCodeAsync(string clientId, string redirectUrl, string clientSecret, string authCode)
        {
            QueryStringBuilder queryBuilder = new QueryStringBuilder();
            queryBuilder.Add("client_id", clientId);
            queryBuilder.Add("redirect_uri", redirectUrl);
            queryBuilder.Add("client_secret", clientSecret);
            queryBuilder.Add("code", authCode);
            queryBuilder.Add("grant_type", "authorization_code");

            HttpWebRequest request = WebRequest.CreateHttp("https://login.live.com/oauth20_token.srf");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            var stream = await request.GetRequestStreamAsync();
            StreamWriter requestWriter = new StreamWriter(stream);
            await requestWriter.WriteAsync(queryBuilder.ToString());
            await requestWriter.FlushAsync();

            var response = await request.GetResponseAsync();
            var httpResponse = response as HttpWebResponse;
            
            // TODO: better error handling
            if (httpResponse == null) return null;
            if (httpResponse.StatusCode != HttpStatusCode.OK) return null;

            var responseBodyStreamReader = new StreamReader(httpResponse.GetResponseStream());
            var responseBody = await responseBodyStreamReader.ReadToEndAsync();

            return Newtonsoft.Json.JsonConvert.DeserializeObject<AppTokenResult>(responseBody);
        }


    }
}
