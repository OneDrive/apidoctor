/* 
 * Copyright(c) Microsoft Corporation
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
 * PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace ApiDoctor.Validation.OData
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Xml.Serialization;
    using ApiDoctor.Validation.Utility;

    [XmlRoot("PropertyValue", Namespace = ODataParser.EdmNamespace)]
    [Mergable(CollectionIdentifier = "Property")]
    public class PropertyValue : XmlBackedObject
    {
        private bool boolValue;
        private bool boolSpecified;

        [XmlAttribute("Property"), MergePolicy(MergePolicy.EqualOrNull)]
        public string Property { get; set; }

        [XmlElement("EnumMember", Namespace = ODataParser.EdmNamespace), DefaultValue(null), MergePolicy(MergePolicy.EqualOrNull)]
        public string EnumMember { get; set; }

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

        [XmlAttribute("String"), DefaultValue(null), MergePolicy(MergePolicy.EqualOrNull)]
        public string String { get; set; }

        [XmlElement("Annotation")]
        public List<Annotation> AnnotationList { get; set; }

        [XmlElement("Record")]
        public List<Record> Records { get; set; }

        [XmlElement("Collection")]
        public RecordCollection Collection { get; set; }
    }

    public class RecordCollection
    {
        [XmlElement("Record")]
        public List<Record> Records { get; set; }

        [XmlElement("String")]
        public List<string> Strings { get; set; }
    }
}
