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
    using System.Xml.Serialization;
    using Transformation;
    using Utility;

    [XmlRoot("Parameter", Namespace = ODataParser.EdmNamespace), Mergable(CollectionIdentifier = "Name")]
    public class Parameter : XmlBackedTransformableObject
    {
        private bool nullable;
        private bool isUnicode;

        public Parameter()
        {
        }

        [XmlAttribute("Name"), SortBy, MergePolicy(EquivalentValues = "this=bindingParameter")]
        public string Name { get; set; }

        [XmlAttribute("Type"), ContainsType]
        public string Type { get; set; }

        [XmlAttribute("Nullable"), MergePolicy(MergePolicy.PreferFalseValue)]
        public bool Nullable
        {
            get { return this.nullable; }
            set
            {
                this.nullable = value;
                this.NullableSpecified = true;
            }
        }

        /// <summary>
        /// Nullable + NullableSpecified are based on some convention  
        /// </summary>
        [XmlIgnore]
        public bool NullableSpecified { get; set; }

        [XmlIgnore, MergePolicy(MergePolicy.Ignore)]
        public bool ValueOfNullablePropertySpecified { get; set; }

        /// <summary>
        /// This appears to be used for merging.
        /// </summary>
        [XmlIgnore, MergePolicy(MergePolicy.PreferFalseValue)]
        public bool? IsNullable
        {
            get
            {
                if (this.ValueOfNullablePropertySpecified)
                {
                    return this.Nullable;
                }
                return null;
            }

            set
            {
                if (!value.HasValue)
                {
                    this.ValueOfNullablePropertySpecified = false;
                }
                else
                {
                    this.ValueOfNullablePropertySpecified = true;
                    this.Nullable = value.Value;
                }
            }
        }

        [XmlAttribute("Unicode"), MergePolicy(MergePolicy.Ignore)]
        public bool UnicodePropertyValue
        {
            get { return this.isUnicode; }
            set
            {
                this.isUnicode = value;
                this.UnicodePropertyValueSpecified = true;
            }
        }

        [XmlIgnore]
        public bool UnicodePropertyValueSpecified { get; set; }

        [XmlIgnore, MergePolicy(MergePolicy.PreferFalseValue)]
        public bool? Unicode
        {
            get
            {
                if (UnicodePropertyValueSpecified)
                {
                    return this.UnicodePropertyValue;
                }

                return null;
            }
            set
            {
                if (value.HasValue)
                {
                    this.UnicodePropertyValue = value.Value;
                }
            }
        }

        [XmlIgnore, MergePolicy(MergePolicy.Ignore)]
        public override string ElementIdentifier { get { return this.Name; } set { this.Name = value; } }

        public override void ApplyTransformation(BaseModifications value, EntityFramework edmx, string[] versions)
        {
            base.ApplyTransformation(value, edmx, versions);
        }
    }
}
