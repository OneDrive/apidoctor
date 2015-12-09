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

    [XmlTagName("Action")]
    public class Action
    {
        public string Name { get; set; }
        public bool IsBound { get; set; }
        public List<Parameter> Parameters { get; set; }
        public ReturnType ReturnType { get; set; }

        public Action()
        {
            this.Parameters = new List<Parameter>();
        }

        
        public static Action FromXml(XElement xml)
        {
            typeof(Action).ThrowIfWrongElement(xml);
            var obj = new Action
            {
                Name = xml.AttributeValue("Name"),
                IsBound = xml.AttributeValue("IsBound").ToBoolean()
            };
            obj.Parameters.AddRange(from e in xml.Elements()
                                    where e.Name == typeof(Parameter).XmlElementName()
                                    select Parameter.FromXml(e));
            obj.ReturnType = (from e in xml.Elements()
                              where e.Name.LocalName == typeof(ReturnType).XmlElementName()
                              select ReturnType.FromXml(e)).FirstOrDefault();
            return obj;
        }

    }
}
