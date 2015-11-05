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

namespace ApiDocs.Validation.Config
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class DocumentOutlineFile : ConfigFile
    {
        [JsonProperty("allowedDocumentHeaders")]
        public DocumentHeader[] AllowedHeaders { get; set; }

        public override bool IsValid
        {
            get
            {
                return this.AllowedHeaders != null;
            }
        }
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
        public int Level { get; set; }

        /// <summary>
        /// Indicates that a header at this level is required.
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Indicates that a header at this level is prohibited (cannot be present in the doc)
        /// </summary>
        public bool Prohibited { get; set; }

        /// <summary>
        /// The expected value of a title or empty to indicate any value
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Specifies the headers that are allowed under this header.
        /// </summary>
        public List<DocumentHeader> ChildHeaders { get; set; }
    }

}
