namespace ApiDoctor.Validation.OData
{
    using ApiDoctor.Validation.Error;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using System.Xml.Serialization;
    using Transformation;
    using Utility;

    [Mergable(CollectionIdentifier = "ElementIdentifier", CollapseSingleItemMatchingProperty = "Name")]
    public class ActionOrFunctionBase : XmlBackedTransformableObject, IODataAnnotatable, IODataNavigable
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

        public IODataNavigable NavigateByUriComponent(string component, EntityFramework edmx, IssueLogger logger, bool isLastSegment)
        {
            throw new NotImplementedException();
        }

        public IODataNavigable NavigateByEntityTypeKey(EntityFramework edmx, IssueLogger issues)
        {
            throw new NotImplementedException();
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
                    for (int i = 1; i<parameters.Length; i++)
                    {
                        Parameter p = new Parameter() { Name = parameters[i] };
                        this.Parameters.Add(p);
                    }
                }
            }
        }

        private string BindingParameterType
        {
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
        [XmlIgnore]
        public string TypeIdentifier
        {
            get { return Name; }
        }
        #endregion
    }
}
