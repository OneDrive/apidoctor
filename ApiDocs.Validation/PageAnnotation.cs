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
    using System.Collections.Generic;
    using System.Dynamic;

    /// <summary>
    /// Page annotation allows you to make page-level annotations for a variety of reasons.
    /// </summary>
    public class PageAnnotation
    {
        public PageAnnotation()
        {
            this.AdditionalData = new Dictionary<string, object>();
        }

        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Title of the Page
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Description for the page
        /// </summary>
        [JsonProperty("description"), MaxLength(156)]
        public string Description { get; set; }

        /// <summary>
        /// Comma separated keywords for the content on the page.
        /// </summary>
        [JsonProperty("keywords"), MaxLength(156)]
        public string Keywords { get; set; }

        /// <summary>
        /// Canonical URL for SEO.
        /// </summary>
        [JsonProperty("canonicalUrl")]
        public string CanonicalUrl { get; set; }

        /// <summary>
        /// Section of the website that this page appears under. This can be used to
        /// return a table of contents just for other files in the same section.
        /// </summary>
        [JsonProperty("section")]
        public string Section { get; set; }

        /// <summary>
        /// A / separated hierarchy for where this page should be in the table of 
        /// contents when a TOC is generated.
        /// </summary>
        [JsonProperty("tocPath")]
        public string TocPath { get; set; }


        /// <summary>
        /// A dictionary of Table of Contents items for this page. The key of the dictionary
        /// is the TOC Path for the entry, and the value is the URL bookmark (#foo) for the entry.
        /// </summary>
        [JsonProperty("tocItems")]
        public Dictionary<string, string> TocItems { get; set; }

        /// <summary>
        /// A collection of HTML tags that should be added to the head element during HTML serialization.
        /// </summary>
        [JsonProperty("headerAdditions")]
        public string[] HeadHtmlTags { get; set; }

        /// <summary>
        /// A collection of HTML tags that should be added to the end of the body element during HTML serialization.
        /// </summary>
        [JsonProperty("footerAdditions")]
        public string[] BodyFooterHtmlTags { get; set; }





        /// <summary>
        /// Container for any unrecognized properties when deserializing the #page.annotation class.
        /// </summary>
        [JsonExtensionData(ReadData = true)]
        public Dictionary<string, object> AdditionalData { get; set; }

        public object Properties
        {
            get {
                var eo = new ExpandoObject();
                var eoColl = (ICollection<KeyValuePair<string, object>>)eo;
                foreach (var kvp in AdditionalData)
                {
                    eoColl.Add(kvp);
                }
                dynamic eoDynamic = eo;
                return eoDynamic;
            }
        }

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
