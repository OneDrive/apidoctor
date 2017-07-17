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

namespace ApiDocs.Validation.OData
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Transformation;

	[XmlRoot("EnumType", Namespace = ODataParser.EdmNamespace)]
    public class EnumType : XmlBackedTransformableObject, IODataAnnotatable, IODataNavigable
    {
        public EnumType()
        {
            this.Annotation = new List<Annotation>();
        }

        [XmlElement("Annotation", Namespace = ODataParser.EdmNamespace)]
        public List<Annotation> Annotation { get; set; }

        [XmlAttribute("Name"), SortBy]
        public string Name { get; set; }

        [XmlAttribute("UnderlyingType"), ContainsType]
        public string UnderlyingType { get; set; }

        [XmlAttribute("IsFlags")]
        public bool IsFlags { get; set; }

        [XmlElement("Member"), Sortable]
        public List<EnumMember> Members { get; set; }


        [XmlIgnore]
        public override string ElementIdentifier { get { return this.Name; } set { this.Name = value; } }

        [XmlIgnore]
        public string TypeIdentifier { get { return Name; } }

        public IODataNavigable NavigateByEntityTypeKey(EntityFramework edmx)
        {
            throw new NotImplementedException();
        }

        public IODataNavigable NavigateByUriComponent(string component, EntityFramework edmx)
        {
            throw new NotImplementedException();
        }        

    }

    public class EnumMember : XmlBackedTransformableObject
    {
        [XmlAttribute("Name"), SortBy]
        public string Name { get; set; }

        [XmlAttribute("Value")]
        public string Value { get; set; }

        [XmlIgnore]
        public override string ElementIdentifier { get { return this.Name; } set { this.Name = value; } }

    }
}