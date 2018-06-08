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

namespace ApiDoctor.Validation.OData
{
    using Utility;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Xml.Serialization;
    using Transformation;

    [XmlRoot("Annotation", Namespace = ODataParser.EdmNamespace)]
    [Mergable(CollectionIdentifier = "Term")]
    public class Annotation : XmlBackedTransformableObject
    {
        private bool boolValue;
        private bool boolSpecified;

        [XmlAttribute("Term"), SortBy]
        public string Term { get; set; }

        [XmlAttribute("String"), DefaultValue(null), MergePolicy(MergePolicy.PreferGreaterValue)]
        public string String { get; set; }

        [XmlAttribute("Bool"), MergePolicy(MergePolicy.EqualOrNull)]
        public bool Bool
        {
            get
            {
                return this.boolValue;
            }

            set
            {
                this.boolValue = value;
                this.boolSpecified = true;
            }
        }

        [XmlIgnore]
        public bool BoolSpecified => this.Bool || this.boolSpecified;

        [XmlElement("EnumMember", Namespace = ODataParser.EdmNamespace), DefaultValue(null), MergePolicy(MergePolicy.EqualOrNull)]
        public string EnumMember { get; set; }

        [XmlElement("Record", Namespace = ODataParser.EdmNamespace), DefaultValue(null), Sortable]
        public List<Record> Records { get; set; }

        [XmlElement("Collection")]
        public RecordCollection Collection { get; set; }

        #region ITransformable

        [XmlIgnore, MergePolicy(MergePolicy.Ignore)]
        public override string ElementIdentifier { get { return this.Term; } set { this.Term = value; } }
        #endregion

    }
}
