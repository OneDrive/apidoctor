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
    /// Action in OData is allowed to modify data on the 
    /// server (can have side-effects). Action does not have to
    /// return data.
    /// </summary>
    [XmlRoot("Action", Namespace = ODataParser.EdmNamespace)]
    public class Action : ActionOrFunctionBase
    {
        public Action() : base()
        {
        }
    }

    public class ActionOrFunctionBase
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("IsBound")]
        public bool IsBound { get; set; }

        [XmlElement("Parameter")]
        public List<Parameter> Parameters { get; set; }

        [XmlElement("ReturnType")]
        public ReturnType ReturnType { get; set; }

        protected ActionOrFunctionBase()
        {
            this.Parameters = new List<Parameter>();
        }
    }
}
