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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using System.Xml.Serialization;
    using Transformation;
    using Utility;

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

        [XmlAttribute("EntitySetPath"), MergePolicy(MergePolicy.EqualOrNull)]
        public string EntitySetPath { get; set; }
    }

    [Mergable(CollectionIdentifier = "ElementIdentifier", CollapseSingleItemMatchingProperty = "Name")]
    public class ActionOrFunctionBase : XmlBackedTransformableObject, IODataAnnotatable
    {
        [XmlAttribute("Name"), SortBy, MergePolicy(MergePolicy.EqualOrNull)]
        public string Name { get; set; }

        [XmlIgnore]
        public string ParameterizedName { get; set; }

        [XmlAttribute("IsBound"), MergePolicy(MergePolicy.PreferGreaterValue)]
        public bool IsBound { get; set; }

        [XmlElement("Parameter"), Sortable]
        public List<Parameter> Parameters { get; set; }

        [XmlElement("ReturnType")]
        public ReturnType ReturnType { get; set; }

        [XmlIgnore, MergePolicy(MergePolicy.Ignore)]
        public object SourceMethods { get; set; }

        [XmlAttribute("SourceFiles"), MergePolicy(MergePolicy.EqualOrNull)]
        public string SourceFiles { get; set; }

        [XmlElement("Annotation", Namespace = ODataParser.EdmNamespace), MergePolicy(MergePolicy.EqualOrNull)]
        public List<Annotation> Annotation { get; set; }

        protected ActionOrFunctionBase()
        {
            this.Parameters = new List<Parameter>();
        }

        // Func1 can substitute for Func2 if Func1 is contravariant on the input types and covariant on the output type.
        // That is, if Func1
        //      - is bound to the same type or a base type of what Func2 is bound to
        //      - returns the same type or a derived type of what Func2 returns
        // then Func1 can be safely used instead of Func2.
        // in practice, this is almost always just an identical function bound to a base type.
        // but it could also indicate quirky examples.
        // for now we'll match on exact return type because there aren't any scenarios that require otherwise.
        // but if one arises, consider this comment permission to relax that check.
        public bool CanSubstituteFor(ActionOrFunctionBase other, EntityFramework edmx)
        {
            if (other != null &&
                other.Name == this.Name &&
                other.IsBound == this.IsBound &&
                other.GetType() == this.GetType() &&
                other.Parameters?.Count == this.Parameters?.Count &&
                ((other.ReturnType != null && this.ReturnType != null && other.ReturnType.Equals(this.ReturnType)) ||
                 (other.ReturnType == null && this.ReturnType == null)))
            {
                // each parameter must match contravariantly
                foreach (var thisParameter in this.Parameters)
                {
                    var otherParameter = other.Parameters.SingleOrDefault(p => p.Name == thisParameter.Name);
                    if (otherParameter == null ||
                        otherParameter.Type == null ||
                        thisParameter.Type == null ||
                        otherParameter.IsNullable != thisParameter.IsNullable ||
                        otherParameter.Unicode != thisParameter.Unicode)
                    {
                        return false;
                    }

                    var thisTypeName = thisParameter.Type.ElementName();
                    var otherTypeName = otherParameter.Type.ElementName();

                    if (thisTypeName != otherTypeName)
                    {
                        var otherTypeObj = edmx.DataServices.Schemas.FindTypeWithIdentifier(otherTypeName) as ComplexType;

                        bool match = false;
                        while (!match && otherTypeObj != null)
                        {
                            if (otherTypeObj.Name == thisTypeName.TypeOnly())
                            {
                                match = true;
                                break;
                            }

                            otherTypeObj = edmx.DataServices.Schemas.FindTypeWithIdentifier(otherTypeObj.BaseType) as ComplexType;
                        }

                        if (!match)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            return false;
        }

        #region ITransformable
        [XmlIgnore, MergePolicy(MergePolicy.Ignore)]
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
                    if (parts[1].StartsWith("Collection("))
                    {
                        var collectionClosingParen = parts[1].IndexOf(")", "Collection(".Length);
                        if (collectionClosingParen > 0)
                        {
                            this.BindingParameterType = parts[1].Substring(0, collectionClosingParen+1);
                            parts[1] = parts[1].Substring(collectionClosingParen + 1);
                        }
                    }

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
