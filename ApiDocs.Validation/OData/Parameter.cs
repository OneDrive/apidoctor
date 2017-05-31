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
    using System.ComponentModel;
    using System.Xml.Serialization;
    using Transformation;

    [XmlRoot("Parameter", Namespace = ODataParser.EdmNamespace)]
    public class Parameter : XmlBackedTransformableObject
    {
        public Parameter()
        {
            //Unicode = true;
            //Nullable = false;
        }

        [XmlAttribute("Name"), SortBy]
        public string Name { get; set; }

        [XmlAttribute("Type"), ContainsType]
        public string Type { get; set; }


        [XmlAttribute("Nullable")]
        public bool ValueOfNullableProperty { get; set; }

        [XmlIgnore]
        public bool ValueOfNullablePropertySpecified { get; set; }

        [XmlIgnore]
        public bool? Nullable
        {
            get
            {
                if (ValueOfNullablePropertySpecified)
                {
                    return ValueOfNullableProperty;
                }
                return null;
            }

            set
            {
                if (!value.HasValue)
                {
                    ValueOfNullablePropertySpecified = false;
                }
                else
                {
                    ValueOfNullablePropertySpecified = true;
                    ValueOfNullableProperty = value.Value;
                }
            }
        }

        [XmlAttribute("Unicode")]
        public bool UnicodePropertyValue { get; set; }

        [XmlIgnore]
        public bool UnicodePropertyValueSpecified { get; set; }

        [XmlIgnore]
        public bool? Unicode
        {
            get
            {
                if (UnicodePropertyValueSpecified)
                {
                    return UnicodePropertyValue;
                }
                return null;
            }
            set
            {
                if (!value.HasValue)
                {
                    UnicodePropertyValueSpecified = false;
                }
                else
                {
                    UnicodePropertyValueSpecified = true;
                    UnicodePropertyValue = value.Value;
                }
            }
        }

        [XmlIgnore]
        public override string ElementIdentifier { get { return this.Name; } set { this.Name = value; } }

        /// <summary>
        /// Specify a parameter index so that parameter order can be maintained even if we sort the collections.
        /// This would come from a transform rule and not from CSDL
        /// </summary>
        [XmlIgnore, SortBy(0)]
        public int? ParameterIndex { get; set; }

        public override void ApplyTransformation(BaseModifications value, EntityFramework edmx, string[] versions)
        {
            base.ApplyTransformation(value, edmx, versions);
        }
    }
}
