﻿/*
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
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Transformation;
    using Utility;

    [XmlRoot("EntitySet", Namespace = ODataParser.EdmNamespace)]
    [Mergable(CollectionIdentifier = "Name")]
    public class EntitySet : XmlBackedTransformableObject, ISet
    {
        public EntitySet()
        {
            this.NavigationPropertyBinding = new List<OData.NavigationPropertyBinding>();
        }

        [XmlAttribute("Name"), SortBy]
        public string Name { get; set; }

        [XmlAttribute("EntityType"), ContainsType, MergePolicy(MergePolicy.EqualOrNull)]
        public string EntityType { get; set; }

        [XmlElement("NavigationPropertyBinding"), Sortable]
        public List<NavigationPropertyBinding> NavigationPropertyBinding { get; set; }

        [XmlElement("Annotation", Namespace = ODataParser.EdmNamespace), MergePolicy(MergePolicy.EqualOrNull)]
        public List<Annotation> Annotation { get; set; }

        [XmlIgnore, MergePolicy(MergePolicy.Ignore)]
        public override string ElementIdentifier { get { return this.Name; } set { this.Name = value; } }

        [XmlIgnore, MergePolicy(MergePolicy.Ignore)]
        public object SourceMethods { get; set; }
    }

    [Mergable(CollectionIdentifier = "Path")]
    public class NavigationPropertyBinding: ITransformable
    {
        [XmlAttribute("Path"), SortBy]
        public string Path { get; set; }

        [XmlAttribute("Target"), ContainsType, MergePolicy(MergePolicy.EqualOrNull)]
        public string Target { get; set; }

        [XmlIgnore]
        public string ElementIdentifier
        {
            get { return this.Path; }
            set { this.Path = value; }
        }

        public void ApplyTransformation(BaseModifications mods, EntityFramework edmx, string[] versions)
        {
            TransformationHelper.ApplyTransformation(this, mods, edmx, versions);
        }
    }
}
