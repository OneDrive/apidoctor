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

    /// <summary>
    /// Authenication Credentials for interacting with the service
    /// </summary>
    public abstract class AuthenicationCredentials
    {
        public abstract string AuthenicationToken { get; internal set; }
        public string FirstPartyApplicationHeaderValue { get; protected set; }

        public static AuthenicationCredentials CreateAutoCredentials(string authenicationToken)
        {
            if (String.IsNullOrEmpty(authenicationToken)) { return CreateNoCredentials(); }
            if (authenicationToken.StartsWith("t="))
            {
                return CreateFirstPartyCredentials(authenicationToken);
            }

            return CreateBearerCredentials(authenicationToken);
        }

        public static AuthenicationCredentials CreateBearerCredentials(string authenicationToken)
        {
            if (String.IsNullOrEmpty(authenicationToken)) { return CreateNoCredentials(); }
            return new BearerCredentials { AuthenicationToken = "Bearer " + authenicationToken };
        }

        public static AuthenicationCredentials CreateFirstPartyCredentials(string authenicationToken)
        {
            if (String.IsNullOrEmpty(authenicationToken)) { return CreateNoCredentials(); }
            return new FirstPartyCredentials { AuthenicationToken = "WLID1.1 " + authenicationToken };
        }

        public static AuthenicationCredentials CreateNoCredentials()
        {
            return new NoCredentials();
        }
    }

    public class BearerCredentials : AuthenicationCredentials
    {
        internal BearerCredentials() { }

        public override string AuthenicationToken { get; internal set; }
    }

    public class FirstPartyCredentials : AuthenicationCredentials
    {
        internal FirstPartyCredentials()
        {
            this.FirstPartyApplicationHeaderValue = "SaveToOneDriveWidget";
        }

        public override string AuthenicationToken { get; internal set; }
    }

    public class NoCredentials : AuthenicationCredentials
    {
        internal NoCredentials() { }

        public override string AuthenicationToken { get { return null; } internal set { } }
    }
}
