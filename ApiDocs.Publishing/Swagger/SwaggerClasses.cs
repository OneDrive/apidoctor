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

namespace ApiDocs.Publishing.Swagger
{
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    internal class SwaggerResource
    {

    }

    internal class SwaggerMethod 
    {
        [JsonProperty("summary", NullValueHandling=NullValueHandling.Ignore)]
        public string Summary { get; set; }

        [JsonProperty("description", NullValueHandling=NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("parameters", NullValueHandling=NullValueHandling.Ignore)]
        public List<SwaggerParameter> Parameters { get; set; }

        [JsonProperty("tags", NullValueHandling=NullValueHandling.Ignore)]
        public List<string> Tags { get; set; }

        [JsonProperty("security", NullValueHandling=NullValueHandling.Ignore)]
        public List<Dictionary<string, string[]>> Security { get ; set; }

        /// <summary>
        /// Key is either the response status "200" or "default"
        /// </summary>
        [JsonProperty("responses", NullValueHandling=NullValueHandling.Ignore)]
        public Dictionary<string, SwaggerResponse> Responses { get; set; }

        public SwaggerMethod()
        {
            this.Parameters = new List<SwaggerParameter>();
            this.Tags = new List<string>();
            this.Responses = new Dictionary<string, SwaggerResponse>();

            this.Security = new List<Dictionary<string, string[]>>();
        }

        public void AddRequiredSecurityRoles(string provider, IEnumerable<string> roles)
        {
            this.Security.Add(new Dictionary<string, string[]> 
            { 
                { provider, roles.ToArray() }
            });

        }

    }

    internal class SwaggerParameter
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("in")]
        public string In { get; set; }
        [JsonProperty("description", NullValueHandling=NullValueHandling.Ignore)]
        public string Description { get; set; }
        [JsonProperty("required")]
        public bool Required { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("format", NullValueHandling=NullValueHandling.Ignore)]
        public string Format { get; set; }



        //[JsonProperty("default", NullValueHandling = NullValueHandling.Ignore)]
        //public object DefaultValue { get; set; }
        //[JsonProperty("minimum", NullValueHandling = NullValueHandling.Ignore)]
        //public object Minimum { get; set; }
        //[JsonProperty("maximum", NullValueHandling = NullValueHandling.Ignore)]
        //public object Maximum { get; set; }

    }

    internal class SwaggerResponse
    {
        public SwaggerResponse()
        {
            this.Description = string.Empty;
        }

        [JsonProperty("description", NullValueHandling=NullValueHandling.Ignore)]
        public string Description { get; set; }
        [JsonProperty("schema", NullValueHandling = NullValueHandling.Ignore)]
        public object Schema { get; set; }
    }
}

