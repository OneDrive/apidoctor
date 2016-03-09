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
        /// <summary>
        /// Filter an array of scenarios to those defined for the method + scopes provided by the account
        /// </summary>
        /// <param name="scenarios">List of all available scenarios</param>
        /// <param name="method">Method to match scenarios with</param>
        /// <param name="providedScopes">Scopes provided by the account</param>
        /// <param name="ignoreScopes">Disable checking for matching scopes</param>
        /// <returns></returns>
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

        /// <summary>
        /// Returns true if all of the required scopes are in the provided scopes array, or if scope requirements are being ignored.
        /// </summary>
        /// <param name="requiredScopes"></param>
        /// <param name="providedScopes"></param>
        /// <returns></returns>
        public static bool ProvidesScopes(this string[] providedScopes, string[] requiredScopes, bool ignoreScopes)
        {
            if (ignoreScopes || requiredScopes == null || requiredScopes.Length == 0)
            {
                return true;
            }

            if (providedScopes == null || providedScopes.Length == 0)
            {
                return false;
            }

            var intersection = providedScopes.Intersect(requiredScopes).ToArray();
            return (intersection.Length == requiredScopes.Length);
        }
    }
}
