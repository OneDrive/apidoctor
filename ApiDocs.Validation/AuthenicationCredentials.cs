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

namespace ApiDocs.Validation
{
    using System;
    using System.Net;
    using Http;

    /// <summary>
    /// Authenication Credentials for interacting with the service
    /// </summary>
    public abstract class AuthenicationCredentials
    {
        /// <summary>
        /// Add the approriate authentication information to the request.
        /// </summary>
        /// <param name="request"></param>
        public abstract void AuthenticateRequest(Http.HttpRequest request);

        public static AuthenicationCredentials CreateNoCredentials()
        {
            return new NoCredentials();
        }
    }

    /// <summary>
    /// Basic class that implements OAuth Bearer token support.
    /// </summary>
    public class OAuthCredentials : AuthenicationCredentials
    {
        public string AuthorizationHeaderValue { get; protected set; }

        public override void AuthenticateRequest(HttpRequest request)
        {
            if (string.IsNullOrEmpty(request.Authorization))
            {
                request.Authorization = this.AuthorizationHeaderValue;
            }
        }

        /// <summary>
        /// Auto-detect if the accessToken is a first party token or third party token and 
        /// return the approriate instance of AuthenticationCredentials.
        /// </summary>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public static AuthenicationCredentials CreateAutoCredentials(string accessToken)
        {
            if (String.IsNullOrEmpty(accessToken)) { return CreateNoCredentials(); }
            if (accessToken.StartsWith("t="))
            {
                return CreateFirstPartyCredentials(accessToken);
            }

            return CreateBearerCredentials(accessToken);
        }

        /// <summary>
        /// Create a Bearer token AuthenticationCredentails instance.
        /// </summary>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public static AuthenicationCredentials CreateBearerCredentials(string accessToken)
        {
            if (String.IsNullOrEmpty(accessToken)) { return CreateNoCredentials(); }
            return new OAuthCredentials { AuthorizationHeaderValue = "Bearer " + accessToken };
        }

        /// <summary>
        /// Create a first party WLID instance.
        /// </summary>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public static AuthenicationCredentials CreateFirstPartyCredentials(string accessToken)
        {
            if (String.IsNullOrEmpty(accessToken)) { return CreateNoCredentials(); }
            return new FirstPartyCredentials { AuthorizationHeaderValue = "WLID1.1 " + accessToken };
        }
    }

    /// <summary>
    /// Wrapper for first-party authentication which sets an application header and provides
    /// the authorization header with the approriate type.
    /// </summary>
    public class FirstPartyCredentials : OAuthCredentials
    {
        public string FirstPartyApplicationHeaderValue { get; protected set; }

        internal FirstPartyCredentials()
        {
            this.FirstPartyApplicationHeaderValue = "MarkdownScannerTool";
        }

        public override void AuthenticateRequest(HttpRequest request)
        {
            base.AuthenticateRequest(request);

            if (!string.IsNullOrEmpty(this.FirstPartyApplicationHeaderValue) &&
                request.Headers["Application"] == null)
            {
                request.Headers.Add("Application", this.FirstPartyApplicationHeaderValue);
            }
        }
    }

    /// <summary>
    /// No authentication is performed.
    /// </summary>
    public class NoCredentials : AuthenicationCredentials
    {
        internal NoCredentials() { }

        public override void AuthenticateRequest(HttpRequest request)
        {

        }
    }
    
    /// <summary>
    /// Enable username and password credentials.
    /// </summary>
    public class BasicCredentials : AuthenicationCredentials
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public override void AuthenticateRequest(HttpRequest request)
        {
            request.Credentials = new System.Net.NetworkCredential(this.Username, this.Password);
        }
    }

    /// <summary>
    /// Enable NTLM User credentials
    /// </summary>
    public class NtlmCredentials : AuthenicationCredentials
    {
        public ICredentials UserNtlmCreds { get; set; }

        public override void AuthenticateRequest(HttpRequest request)
        {
            request.Credentials = UserNtlmCreds;
        }
    }
}
