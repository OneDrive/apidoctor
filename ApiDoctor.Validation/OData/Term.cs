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
    using System.Xml.Serialization;
    using Transformation;

    /*
       <Term Name="sourceUrl" Type="Edm.String" AppliesTo="oneDrive.item">
            <Annotation Term="Org.OData.Core.V1.LongDescription" String="When used on a PUT or POST call to an Item, causes the item's content to be copied from the URL specified in the attribute."/>
       </Term>
     */
    [XmlRoot("Term", Namespace = ODataParser.EdmNamespace)]
    [Mergable(CollectionIdentifier = "ElementIdentifier")]
    public class Term : XmlBackedTransformableObject
    {
        [XmlAttribute("Name"), SortBy, MergePolicy(MergePolicy.EqualOrNull)]
        public string Name { get; set; }

        [XmlAttribute("Type"), ContainsType, MergePolicy(MergePolicy.EqualOrNull)]
        public string Type { get; set; }

        [XmlAttribute("AppliesTo"), ContainsType, MergePolicy(MergePolicy.EqualOrNull)]
        public string AppliesTo { get; set; }

        [XmlAttribute("WorkloadTermNamespace", Namespace = ODataParser.AgsNamespace)]
        public string GraphWorkloadTermNamespace { get; set; }

        [XmlElement("Annotation", Namespace = ODataParser.EdmNamespace), Sortable]
        public List<Annotation> Annotations { get; set; }

        public Term()
        {
            this.Annotations = new List<Annotation>();
        }

        public const string DescriptionTerm = "Org.OData.Core.V1.Description";
        public const string LongDescriptionTerm = "Org.OData.Core.V1.LongDescription";
        public const string ExpandRestrictionsTerm = "Org.OData.Capabilities.V1.ExpandRestrictions";
        public const string SearchRestrictionsTerm = "Org.OData.Capabilities.V1.SearchRestrictions";
        public const string ChangeTrackingTerm = "Org.OData.Capabilities.V1.ChangeTracking";
        public const string NavigationRestrictionsTerm = "Org.OData.Capabilities.V1.NavigationRestrictions";


        [XmlIgnore, MergePolicy(MergePolicy.Ignore)]
        public override string ElementIdentifier
        {
            get
            {
                return $"{this.AppliesTo}@{this.Name}";
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    this.AppliesTo = null;
                    this.Name = null;
                }
                else
                {
                    var parts = value.Split('@');
                    if (parts.Length == 1)
                    {
                        this.Name = parts[0];
                    }
                    else if (parts.Length == 2)
                    {
                        this.AppliesTo = parts[0];
                        this.Name = parts[1];
                    }
                    else
                    {
                        throw new ArgumentException("Unexpected number of @ characters in value");
                    }
                }
            }
        }
    }
}
