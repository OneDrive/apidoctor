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
    using System.Linq;
    using System.Xml.Linq;

    /*
       <Term Name="sourceUrl" Type="Edm.String" AppliesTo="oneDrive.item">
            <Annotation Term="Org.OData.Core.V1.LongDescription" String="When used on a PUT or POST call to an Item, causes the item's content to be copied from the URL specified in the attribute."/>
       </Term>
     */
    public class Term
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string AppliesTo { get; set; }
        public List<Annotation> Annotations { get; set; }

        public Term()
        {
            this.Annotations = new List<Annotation>();
        }

        public static string ElementName { get { return "Term"; } }

        public static Term FromXml(XElement xml)
        {
            if (xml.Name.LocalName != ElementName) throw new ArgumentException("xml was not a Term element");

            var obj = new Term
            {
                Name = xml.AttributeValue("Name"),
                Type = xml.AttributeValue("Type"),
                AppliesTo = xml.AttributeValue("AppliesTo")
            };
            obj.Annotations.AddRange(from e in xml.Elements()
                                     where e.Name.LocalName == Annotation.ElementName
                                     select Annotation.FromXml(e));
            return obj;
        }


    }
}
