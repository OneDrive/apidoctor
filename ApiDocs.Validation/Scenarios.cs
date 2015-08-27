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
    using System.Collections.Generic;
    using System.Linq;
    using ApiDocs.Validation.Config;
    using ApiDocs.Validation.Params;
    using Newtonsoft.Json;

    public class ScenarioFile : ConfigFile
    {
        [JsonProperty("scenarios")]
        public ScenarioDefinition[] Scenarios { get; set; }

        [JsonProperty("canned-requests")]
        public CannedRequestDefinition[] CannedRequests { get; set; }

        public override bool IsValid
        {
            get
            {
                return this.Scenarios != null || this.CannedRequests != null;
            }
        }
    }

    public static class ScenarioExtensionMethods
    {
        public static ScenarioDefinition[] ScenariosForMethod(this IEnumerable<ScenarioDefinition> scenarios, MethodDefinition method)
        {
            var id = method.Identifier;
            var query = from p in scenarios
                        where p.MethodName == id
                        select p;

            if (method.Scenarios != null && method.Scenarios.Count > 0)
            {
                query = query.Union(method.Scenarios);
            }

            return query.ToArray();
        }
    }
}
