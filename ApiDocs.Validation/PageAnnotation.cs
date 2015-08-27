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
    using System.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// Page annotation allows you to make page-level annotations for a variety of reasons
    /// </summary>
    public class PageAnnotation
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description"), MaxLength(156)]
        public string Description { get; set; }

        [JsonProperty("keywords"), MaxLength(156)]
        public string Keywords { get; set; }

        [JsonProperty("cononicalUrl")]
        public string CononicalUrl { get; set; }

        [JsonProperty("section")]
        public string Section { get; set; }

        [JsonProperty("tocPath")]
        public string TocPath { get; set; }

        [JsonProperty("headerAdditions")]
        public string[] HeaderAdditions { get; set; }

        [JsonProperty("footerAdditions")]
        public string[] FooterAdditions { get; set; }
    }
    

    public class MaxLengthAttribute : Attribute 
    {
        public MaxLengthAttribute(int maximumLength)
        {
            this.MaximumLength = maximumLength;
        }

        public int MaximumLength { get; set; }

        public static int GetMaxLength(Type type, string propertyName)
        {
            var attribute = (MaxLengthAttribute)type.GetProperty(propertyName).GetCustomAttributes(true).FirstOrDefault(x => x is MaxLengthAttribute);
            if (null != attribute)
            {
                return attribute.MaximumLength;
            }
            return -1;
        }
    }
}
