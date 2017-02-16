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


namespace ApiDocs.Validation.Json
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Linq;
    using System.Collections.Generic;

    public class JsonRewriter
    {

        /// <summary>
        /// Rewrite an input JSON string to adhere to the approriate output schema. This method will use the
        /// rewriteMap to convert from name -> value in the rewriteMap.
        /// </summary>
        /// <param name="sourceJson"></param>
        /// <param name="rewriteMap"></param>
        /// <returns></returns>
        public static string RewriteJsonProperties(string sourceJson, Dictionary<string, string> rewriteMap)
        {
            if (string.IsNullOrWhiteSpace(sourceJson))
                return sourceJson;

            JContainer source = (JContainer)JsonConvert.DeserializeObject(sourceJson);
            ApplyMapToJsonObject(source, rewriteMap);
            return JsonConvert.SerializeObject(source);
        }

        private static void ApplyMapToPropertyNames(JContainer source, Dictionary<string, string> rewriteMap)
        {
            if (null == source) return;

            foreach (var key in source.ToArray())
            {
                JProperty prop = key as JProperty;
                if (prop != null)
                {
                    var rule = (from m in rewriteMap.Keys where prop.Name.Contains(m) select m).FirstOrDefault();
                    if (null != rule)
                    {
                        var newPropName = prop.Name.Replace(rule, rewriteMap[rule]);
                        var newProp = new JProperty(newPropName, prop.Value);
                        prop.Replace(newProp);
                        prop = newProp;
                    }

                    ApplyMapToJsonObject(prop.Value, rewriteMap);
                }
            }
        }

        private static void ApplyMapToJsonObject(JToken token, Dictionary<string, string> rewriteMap)
        {
            if (token is JArray)
            {
                JArray array = (JArray)token;
                foreach (var item in array)
                {
                    ApplyMapToJsonObject(item, rewriteMap);
                }
            } else if (token is JContainer)
            {
                ApplyMapToPropertyNames((JContainer)token, rewriteMap);
            }
            else if (token is JValue)
            {
                // Nothing to do here, it's a single value
            }
            else
            {
                Console.WriteLine($"Unexpected JToken found: {token.GetType().Name}");
            }
        }


    }
}
