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
    using System.ComponentModel;
    using System.Linq;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using Transformation;

    [XmlRoot("ComplexType", Namespace = ODataParser.EdmNamespace)]
    public class ComplexType : XmlBackedObject, IODataNavigable, Transformation.ITransformable
    {
        public ComplexType()
        {
            this.Properties = new List<Property>();
        }

        [XmlAttribute("Name"), SortBy]
        public string Name { get; set; }

        [XmlAttribute("OpenType"), DefaultValue(false)]
        public bool OpenType { get; set; }

        [XmlAttribute("BaseType"), ContainsType]
        public string BaseType { get; set; }

        [XmlElement("Property", Namespace = ODataParser.EdmNamespace), Sortable]
        public List<Property> Properties { get; set; }

        [XmlAttribute("WorkloadName", Namespace = ODataParser.AgsNamespace)]
        public string WorkloadName { get; set; }

        public virtual IODataNavigable NavigateByEntityTypeKey(EntityFramework edmx)
        {
            if (this.OpenType)
            {
                // TODO: This isn't illegal, but we don't know what you're going to get back anyway, so we just treat it the same for now.
            }
            throw new NotSupportedException("ComplexType cannot be navigated by key.");
        }

        public virtual IODataNavigable NavigateByUriComponent(string component, EntityFramework edmx)
        {
            var propertyMatch = (from p in this.Properties
                where p.Name == component
                select p).FirstOrDefault();
            if (null != propertyMatch)
            {
                var identifier = propertyMatch.Type;
                if (identifier.StartsWith("Collection("))
                {
                    var innerId = identifier.Substring(11, identifier.Length - 12);
                    return new ODataCollection(innerId);
                }
                return edmx.ResourceWithIdentifier<IODataNavigable>(identifier);
            }

            return null;
        }

        public string TypeIdentifier
        {
            get { return Name; }
        }

        public virtual void ApplyTransformation(BaseModifications mods, EntityFramework edmx, string[] versions)
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

        [XmlIgnore]
        public string ElementIdentifier { get { return this.Name; } set { this.Name = value; } }

    }
}
