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
    using System.ComponentModel;
    using System.Xml.Serialization;
    using Transformation;
    using Utility;

    [XmlRoot("ReturnType", Namespace = ODataParser.EdmNamespace), Mergable]
    public class ReturnType : XmlBackedTransformableObject
    {
        public ReturnType()
        {
            this.Unicode = true;
        }

        [XmlAttribute("Type"), ContainsType]
        public string Type { get; set; }

        private bool _isNullable;

        [XmlAttribute("Nullable"), MergePolicy(MergePolicy.PreferFalseValue)]
        public bool Nullable
        {
            get { return _isNullable; }
            set
            {
                _isNullable = value;
                NullableSpecified = true;
            }
        }

        [XmlIgnore]
        public bool NullableSpecified { get; set; }

        [XmlAttribute("Unicode"), DefaultValue(true)]
        public bool Unicode { get; set; }

        public override string ElementIdentifier
        {
            get
            {
                return null;
            }

            set
            {
                
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as ReturnType;
            if (other != null)
            {
                return
                    string.Equals(this.Type, other.Type) &&
                    this.Nullable == other.Nullable &&
                    this.Unicode == other.Unicode;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return this.Type.GetHashCode() ^ this.Nullable.GetHashCode() ^ this.Unicode.GetHashCode();
        }
    }
}
