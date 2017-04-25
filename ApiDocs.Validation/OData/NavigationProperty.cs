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
    using System.ComponentModel;
    using System.Xml.Serialization;
    using Transformation;

    [XmlRoot("NavigationProperty", Namespace = ODataParser.EdmNamespace)]
    public class NavigationProperty : Property
    {
        public NavigationProperty()
        {
            ContainsTarget = false;
        }

        [XmlAttribute("ContainsTarget"), DefaultValue(false)]
        public bool ContainsTarget { get; set; }

        /// <summary>
        /// Indicates that this property can be included in a $expand query
        /// </summary>
        [XmlIgnore]
        public bool Expandable { get; set; }

        /// <summary>
        /// Indicates that the target of this property can be enumerated (e.g. GET /items)
        /// </summary>
        [XmlIgnore]
        public bool Enumerable { get; set; }

        /// <summary>
        /// Indicates how this property can be navigated via the URL.
        /// </summary>
        [XmlIgnore]
        public Navigability Navigation { get; set; }

        /// <summary>
        /// Indicates that change tracking can be used on this target.
        /// </summary>
        [XmlIgnore]
        public bool ChangeTracking { get; set; }

        //public override void ApplyTransformation(BaseModifications transform, EntityFramework edmx, string version)
        //{
        //    Transformation.PropertyModification value = (Transformation.PropertyModification)transform;
        //    if (value.ContainsTarget.HasValue)
        //    {
        //        ContainsTarget = value.ContainsTarget.Value;
        //    }

        //    base.ApplyTransformation(transform, edmx, version);
        //}
    }

    public enum Navigability
    {
        Recursive,
        Single,
        None
    }
}
