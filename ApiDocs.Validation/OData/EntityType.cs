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

    public class EntityType : ComplexType
    {
        public EntityType()
        {
            this.Properties = new List<Property>();
            this.NavigationProperties = new List<NavigationProperty>();
        }

        public List<NavigationProperty> NavigationProperties { get; set; }

        public static new string ElementName { get { return "EntityType"; } }
        public static new EntityType FromXml(XElement xml)
        {
            if (xml.Name.LocalName != ElementName) throw new ArgumentException("xml is not an EntityType element");

            var obj = new EntityType { Name = xml.AttributeValue("Name") };

            obj.Properties.AddRange(from e in xml.Elements()
                                    where e.Name.LocalName == Property.ElementName
                                    select Property.FromXml(e));
            obj.NavigationProperties.AddRange(from e in xml.Elements()
                                              where e.Name.LocalName == NavigationProperty.ElementName
                                              select NavigationProperty.FromXml(e));

            return obj;
        }
    }
}
