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
    using System.ComponentModel;
    using System.Xml.Serialization;
    using Transformation;
    using Utility;

    [XmlRoot("Property", Namespace = ODataParser.EdmNamespace), Mergable(CollectionIdentifier = "Name")]
    public class Property : XmlBackedTransformableObject, IODataAnnotatable
    {
        public Property()
        {
            this.Annotation = new List<Annotation>();
        }

        [XmlAttribute("Name"), SortBy]
        public string Name { get; set; }

        [XmlAttribute("Type"), ContainsType, MergePolicy(MergePolicy.PreferLesserValue)]
        public string Type { get; set; }

        [XmlIgnore]
        private bool _nullable;

        [XmlAttribute("Nullable"), MergePolicy(MergePolicy.PreferFalseValue)]
        public bool Nullable {
            get { return _nullable; }
            set
            {
                _nullable = value;
                NullableSpecified = true;
            }
        }

        [XmlIgnore]
        public bool NullableSpecified{ get; set;}

        [XmlIgnore]
        private bool _isUnicode;

        [XmlAttribute("Unicode"), MergePolicy(MergePolicy.PreferFalseValue)]
        public bool Unicode
        {
            get => _isUnicode;
            set
            {
                _isUnicode = value;
                UnicodeSpecified = true;
            }
        }

        [XmlIgnore]
        private bool _isUnicodeSpecified;

        [XmlIgnore]
        public bool UnicodeSpecified
        {
            get => _isUnicodeSpecified;
            set { _isUnicodeSpecified = value; }
        }

        [XmlElement("Annotation", Namespace = ODataParser.EdmNamespace), Sortable]
        public List<Annotation> Annotation { get; set; }

        [XmlAttribute("CreateVirtualNavigationProperty", Namespace = ODataParser.AgsNamespace)]
        public bool ValueOfCreateVirtualNavigationProperty { get; set; }

        [XmlIgnore]
        public bool ValueOfCreateVirtualNavigationPropertySpecified { get; set; }

        [XmlIgnore]
        public bool? CreateVirtualNavigationProperty
        {
            get
            {
                if (ValueOfCreateVirtualNavigationPropertySpecified)
                {
                    return ValueOfCreateVirtualNavigationProperty;
                }
                return null;
            }
            set
            {
                if (!value.HasValue)
                {
                    ValueOfCreateVirtualNavigationPropertySpecified = false;
                }
                else
                {
                    ValueOfCreateVirtualNavigationPropertySpecified = true;
                    ValueOfCreateVirtualNavigationProperty = value.Value;
                }
            }
        }

        [XmlAttribute("VirtualNavigationPropertyName", Namespace = ODataParser.AgsNamespace)]
        public string VirtualNavigationPropertyName { get; set; }

        [XmlAttribute("TargetEntityType", Namespace = ODataParser.AgsNamespace), ContainsType]
        public string TargetEntityType { get; set; }

        [XmlAttribute("KeyPropertyPath", Namespace = ODataParser.AgsNamespace)]
        public string KeyPropertyPath { get; set; }

        [XmlAttribute("WorkloadName", Namespace = ODataParser.AgsNamespace)]
        public string WorkloadName { get; set; }

        /// <summary>
        /// Indicates that this property can be used in a $select query parameter
        /// </summary>
        [XmlIgnore]
        public bool Selectable { get; set; }

        /// <summary>
        /// Indicates this property can be used in a $filter query parameter
        /// </summary>
        [XmlIgnore]
        public bool Filterable { get; set; }

        public override void ApplyTransformation(BaseModifications mods, EntityFramework edmx, string[] versions)
        {
            TransformationHelper.ApplyTransformation(this, mods, edmx, versions, (key, value) =>
            {
                if (key == "GraphPropertyName")
                {
                    this.WorkloadName = this.Name;
                    this.Name = (string)value;
                    return true;
                }
                return false;
            });
        }

        [XmlIgnore]
        public override string ElementIdentifier { get { return this.Name; } set { this.Name = value; } }
    }
}
