using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicrosoftAccountSDK.Windows
{
    public class AuthResult
    {
        public AuthResult(Uri resultUri, OAuthFlow flow)
        {
            this.AuthFlow = flow;

            string[] queryParams = null;
            switch (flow)
            {
                case OAuthFlow.ImplicitGrant:
                    int accessTokenIndex = resultUri.AbsoluteUri.IndexOf("#access_token");
                    if (accessTokenIndex > 0)
                    {
                        queryParams = resultUri.AbsoluteUri.Substring(accessTokenIndex + 1).Split('&');
                    }
                    else
                    {
                        queryParams = resultUri.Query.TrimStart('?').Split('&');
                    }
                    break;
                case OAuthFlow.AuthorizationCodeGrant:
                    queryParams = resultUri.Query.TrimStart('?').Split('&');
                    break;
                default:
                    throw new NotSupportedException("flow value not supported");
            }

            foreach (string param in queryParams)
            {
                string[] kvp = param.Split('=');
                switch (kvp[0])
                {
                    case "code":
                        this.AuthorizeCode = kvp[1];
                        break;
                    case "access_token":
                        this.AccessToken = kvp[1];
                        break;
                    case "authorization_token":
                    case "authentication_token":
                        this.AuthenticationToken = kvp[1];
                        break;
                    case "error":
                        this.ErrorCode = kvp[1];
                        break;
                    case "error_description":
                        this.ErrorDescription = Uri.UnescapeDataString(kvp[1]);
                        break;
                    case "token_type":
                        this.TokenType = kvp[1];
                        break;
                    case "expires_in":
                        this.AccessTokenExpiresIn = new TimeSpan(0, 0, int.Parse(kvp[1]));
                        break;
                    case "scope":
                        this.Scopes = kvp[1].Split(new string[] { "%20" }, StringSplitOptions.RemoveEmptyEntries);
                        break;
                    case "user_id":
                        this.UserId = kvp[1];
                        break;
                }
            }
        }

        public OAuthFlow AuthFlow { get; private set; }
        public string AuthorizeCode { get; private set; }
        public string ErrorCode { get; private set; }
        public string ErrorDescription { get; private set; }
        public string AccessToken { get; private set; }
        public string AuthenticationToken { get; private set; }
        public string TokenType { get; private set; }
        public TimeSpan AccessTokenExpiresIn { get; private set; }
        public string[] Scopes { get; private set; }
        public string UserId { get; private set; }
    }
}
