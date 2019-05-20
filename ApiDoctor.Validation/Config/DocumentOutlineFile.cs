/*
 * API Doctor
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

namespace ApiDoctor.Validation.Config
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class DocumentOutlineFile : ConfigFile
    {
        [JsonProperty("apiPageType")]
        public List<DocumentHeader> ApiPageType { get; set; }

        [JsonProperty("resourcePageType")]
        public List<DocumentHeader> ResourcePageType { get; set; }

        [JsonProperty("conceptualPageType")]
        public List<DocumentHeader> ConceptualPageType { get; set; }

        [JsonProperty("enumPageType")]
        public List<DocumentHeader> EnumPageType { get; set; }

        public override bool IsValid => this.ApiPageType != null || this.ResourcePageType != null || this.ConceptualPageType != null || this.EnumPageType != null;
    }

    public class DocumentHeader
    {
        public DocumentHeader()
        {
            Level = 1;
            ChildHeaders = new List<DocumentHeader>();
        }

        /// <summary>
        /// Represents the header level using markdown formatting (1=#, 2=##, 3=###, 4=####, 5=#####, 6=######)
        /// </summary>
        [JsonProperty("level")]
        public int Level { get; set; }

        /// <summary>
        /// Indicates that a header at this level is required.
        /// </summary>
        [JsonProperty("required")]
        public bool Required { get; set; }

        /// <summary>
        /// The expected value of a title or empty to indicate any value
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Specifies the headers that are allowed under this header.
        /// </summary>
        [JsonProperty("headers")]
        public List<DocumentHeader> ChildHeaders { get; set; }

        internal bool Matches(DocumentHeader found)
        {
            return this.Level == found.Level && DoTitlesMatch(this.Title, found.Title);
        }

        private static bool DoTitlesMatch(string expectedTitle, string foundTitle)
        {
            if (expectedTitle == foundTitle) return true;
            if (string.IsNullOrEmpty(expectedTitle) || expectedTitle == "*") return true;
            if (expectedTitle.StartsWith("* ") && foundTitle.EndsWith(expectedTitle.Substring(2))) return true;
            if (expectedTitle.EndsWith(" *") && foundTitle.StartsWith(expectedTitle.Substring(0, expectedTitle.Length - 2))) return true;
            return false;
        }
    }

}
