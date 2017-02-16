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

namespace ApiDocs.Validation.Params
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using System.Linq;

    /// <summary>
    /// Class represents information about a set of parameters that are used to make a request
    /// to the service.
    /// </summary>
    public class ScenarioDefinition : BasicRequestDefinition
    {
        #region Json-Fed Properties
        [JsonProperty("name")]
        public string Description { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("test-setup", DefaultValueHandling=DefaultValueHandling.Ignore)]
        public List<TestSetupRequestDefinition> TestSetupRequests { get; set; }

        [JsonProperty("status-codes-to-retry", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int[] StatusCodesToRetry { get; set; }

        [JsonProperty("scopes", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string[] RequiredScopes
        {
            get; set;
        }

        #endregion

        public ScenarioDefinition()
        {
            this.RequiredScopes = new string[0];
        }

        [JsonIgnore]
        public string DisplayText
        {
            get
            {
                return string.Concat(this.Description, " (", this.MethodName, ")");
            }
        }

        internal MethodDefinition GetMethodDefinition(DocSet documents)
        {
            return documents.Methods.FirstOrDefault(x => x.Identifier == this.MethodName);
        }
    }

    /// <summary>
    /// Class represents a canned request that can be referenced across test sceanrios
    /// </summary>
    public class CannedRequestDefinition : TestSetupRequestDefinition
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("implicit-variable")]
        public string ImplicitVariable { get; set; }

        /// <summary>
        /// Copy all the paramters of this to a new instance so modifications to canned requests don't stack on each other
        /// </summary>
        /// <returns></returns>
        public CannedRequestDefinition CopyInstance()
        {
            string data = Newtonsoft.Json.JsonConvert.SerializeObject(this);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<CannedRequestDefinition>(data);
        }
    }

    public enum PlaceholderLocation
    {
        Invalid,
        Url,
        Json,
        HttpHeader,
        Body,
        StoredValue,
        BodyBase64Encoded,
        CSharpCode
    }
}
