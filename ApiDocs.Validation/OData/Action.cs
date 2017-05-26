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
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using Transformation;

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

        [XmlAttribute("EntitySetPath")]
        public string EntitySetPath { get; set; }
    }

    public class ActionOrFunctionBase : XmlBackedTransformableObject
    {
        [XmlAttribute("Name"), SortBy]
        public string Name { get; set; }

        [XmlAttribute("IsBound")]
        public bool IsBound { get; set; }

        [XmlElement("Parameter"), Sortable]
        public List<Parameter> Parameters { get; set; }

        [XmlElement("ReturnType")]
        public ReturnType ReturnType { get; set; }

        protected ActionOrFunctionBase()
        {
            this.Parameters = new List<Parameter>();
        }

        #region ITransformable
        [XmlIgnore]
        public override string ElementIdentifier
        {
            get
            {
                return $"{Name}@{BindingParameterType}({ParameterNames})";
            }
            set
            {
                string[] parts = value.Split('@');
                this.Name = parts[0];
                if (parts.Length > 1)
                {
                    string[] parameters = parts[1].Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                    this.BindingParameterType = parameters[0];
                    for(int i=1; i<parameters.Length; i++)
                    {
                        Parameter p = new Parameter() { Name = parameters[i] };
                        this.Parameters.Add(p);
                    }
                }
            }
        }

        private string BindingParameterType {
            get
            {
                return (from p in Parameters where p.Name == "bindingParameter" || p.Name == "this" select p.Type).FirstOrDefault();
            }
            set
            {
                if (this.BindingParameterType == null)
                {
                    Parameter bindingParam = new Parameter() { Name = "bindingParameter", Type = value };
                    this.Parameters.Add(bindingParam);
                }
            }
        }

        private string ParameterNames
        {
            get
            {
                var names = from p in Parameters
                             where p.Name != "bindingParameter" && p.Name != "this"
                             orderby p.Name
                             select p.Name;
                if (names.Any())
                    return names.ComponentsJoinedByString(",");
                return string.Empty;
            }
        }
        #endregion
    }
}
