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
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using ApiDocs.Validation;

    public class BasicAccount : IServiceAccount
    {
        public BasicAccount()
        {
        }

        public string BaseUrl { get; set; }
        public bool Enabled { get; set; }
        public string Name { get; set; }
        public string[] AdditionalHeaders { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string[] Scopes { get; set; }
        public AccountTransforms Transformations { get; }

        public Task PrepareForRequestAsync()
        {
            return Task.FromResult(true);
        }

        public AuthenicationCredentials CreateCredentials()
        {
            return new BasicCredentials { Username = this.Username, Password = this.Password };
        }
    }
}
