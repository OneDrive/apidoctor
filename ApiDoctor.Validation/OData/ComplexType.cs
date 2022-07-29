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
    using Utility;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using Transformation;
    using ApiDoctor.Validation.Error;

    [XmlRoot("ComplexType", Namespace = ODataParser.EdmNamespace)]
    [Mergable(CollectionIdentifier = "Name")]
    public class ComplexType : XmlBackedTransformableObject, IODataNavigable, IODataAnnotatable
    {
        public ComplexType()
        {
            this.Properties = new List<Property>();
            this.Annotation = new List<Annotation>();
        }

        [XmlAttribute("Name"), SortBy]
        public string Name { get; set; }

        [XmlAttribute("BaseType"), ContainsType, MergePolicy(MergePolicy.EqualOrNull)]
        public string BaseType { get; set; }

        [XmlAttribute("Abstract"), DefaultValue(false), MergePolicy(MergePolicy.PreferTrueValue)]
        public bool Abstract { get; set; }

        [XmlAttribute("OpenType"), DefaultValue(false), MergePolicy(MergePolicy.PreferGreaterValue)]
        public bool OpenType { get; set; }

        [XmlElement("Property", Namespace = ODataParser.EdmNamespace), Sortable]
        public List<Property> Properties { get; set; }

        [XmlElement("Annotation", Namespace = ODataParser.EdmNamespace), MergePolicy(MergePolicy.EqualOrNull)]
        public List<Annotation> Annotation { get; set; }

        [XmlAttribute("WorkloadName", Namespace = ODataParser.AgsNamespace), MergePolicy(MergePolicy.EqualOrNull)]
        public string WorkloadName { get; set; }

        [XmlIgnore]
        public HashSet<ResourceDefinition> Contributors { get; } = new HashSet<ResourceDefinition>();

        public virtual IODataNavigable NavigateByEntityTypeKey(EntityFramework edmx, IssueLogger issues)
        {
            if (this.OpenType)
            {
                // TODO: This isn't illegal, but we don't know what you're going to get back anyway, so we just treat it the same for now.
            }
            throw new NotSupportedException("ComplexType cannot be navigated by key.");
        }

        public virtual IODataNavigable NavigateByUriComponent(string component, EntityFramework edmx, IssueLogger issues, bool isLastSegment)
        {
            var func = this.NavigateByFunction(component, edmx, issues);
            if (func != null)
            {
                return func;
            }

            var match = this.Properties.Where(p => p.Name == component).Select(p => p.Type).FirstOrDefault();

            if (match != null)
            {
                return edmx.ResourceWithIdentifier<IODataNavigable>(match);
            }

            // check for case-insensitive matches and throw if one exists
            var otherCaseName =
                this.Properties.Where(p => p.Name.IEquals(component)).Select(p => p.Name).FirstOrDefault();

            if (otherCaseName != null)
            {
                throw new ArgumentException($"ERROR: case mismatch between URL segment '{component}' and schema element '{otherCaseName}'");
            }

            return null;
        }

        public string TypeIdentifier
        {
            get { return Name; }
        }

        public override void ApplyTransformation(BaseModifications mods, EntityFramework edmx, string[] versions)
        {
            TransformationHelper.ApplyTransformation(this, mods, edmx, versions, (name, modPropValue) =>
            {
                if (name == "GraphEntityTypeName")
                {
                    WorkloadName = this.Name;
                    this.Name = (string)modPropValue;
                    // Need to rename the references to this entity type through the EntityFramework   
                    edmx.RenameEntityType(this);
                    return true;
                }
                return false;
            });
        }

        [XmlIgnore, MergePolicy(MergePolicy.Ignore)]
        public override string ElementIdentifier { get { return this.Name; } set { this.Name = value; } }

        [XmlIgnore]
        public string Namespace { get; set; }

    }
}
