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
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IServiceAccount
    {
        /// <summary>
        /// Display name of the account
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Determine if the account is enabled for use in test runs.
        /// </summary>
        bool Enabled { get; }
        
        /// <summary>
        /// Base URL for all relative API URLs
        /// </summary>
        string BaseUrl { get; }
        
        /// <summary>
        /// Scopes supported by this account / authentication
        /// </summary>
        string[] Scopes {get;}
        
        /// <summary>
        /// Additional HTTP headers added to all requests for this account
        /// </summary>
        string[] AdditionalHeaders { get; }
        
        /// <summary>
        /// Collection of namespaces that are rewritten when a request is generated. Allows conversion between microsoft.graph and workload namespace, for example.
        /// </summary>
        AccountTransforms Transformations { get; }

        /// <summary>
        /// Called the first time an account is created, allowing the account to 
        /// refresh any information required to make a service request.
        /// </summary>
        Task PrepareForRequestAsync();

        /// <summary>
        /// Ask the account to generate a credentials instance which is passed on.
        /// </summary>
        /// <returns></returns>
        AuthenicationCredentials CreateCredentials();
    }
}
