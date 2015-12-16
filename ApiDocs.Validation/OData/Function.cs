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
    using System.Xml.Serialization;

    /// <summary>
    /// Function in OData is not allowed to modify data
    /// or have side effects (must be idempotent). A 
    /// function must return data back to the caller (ReturnType).
    /// </summary>
    [XmlRoot("Function")]
    public class Function : ActionOrFunctionBase
    {
        public Function() : base()
        {
        }

        public static Function FromXml(XElement xml)
        {
            typeof(Function).ThrowIfWrongElement(xml);

            var obj = new Function
            {
                Name = xml.AttributeValue("Name"),
                IsBound = xml.AttributeValue("IsBound").ToBoolean()
            };

            obj.Parameters.AddRange(from e in xml.Elements()
                                    where e.Name.LocalName == typeof(Parameter).XmlElementName()
                                    select Parameter.FromXml(e));

            obj.ReturnType = (from e in xml.Elements()
                              where e.Name.LocalName == typeof(ReturnType).XmlElementName()
                              select ReturnType.FromXml(e)).FirstOrDefault();

            return obj;
        }
    }
}
