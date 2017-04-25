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
    using System.Xml.Serialization;
    using Transformation;

    [XmlRoot("EntityType", Namespace = ODataParser.EdmNamespace)]
    public class EntityType : ComplexType, IODataNavigable, ITransformable
    {
        public EntityType()
        {
            this.Properties = new List<Property>();
            this.NavigationProperties = new List<NavigationProperty>();
        }

        [XmlElement("Key", Namespace = ODataParser.EdmNamespace)]
        public Key Key { get; set; }

        [XmlAttribute("Abstract"), DefaultValue(false)]
        public bool Abstract { get; set; }

        [XmlAttribute("HasStream"), DefaultValue(false)]
        public bool HasStream { get; set; }

        [XmlElement("NavigationProperty", Namespace = ODataParser.EdmNamespace), Sortable]
        public List<NavigationProperty> NavigationProperties { get; set; }

        [XmlAttribute("IsMaster", Namespace = ODataParser.AgsNamespace), DefaultValue(false)]
        public bool GraphIsMaster { get; set; }

        [XmlAttribute("AddressUrl", Namespace = ODataParser.AgsNamespace)]
        public string GraphAddressUrl { get; set; }

        [XmlAttribute("AddressUrlMSA", Namespace = ODataParser.AgsNamespace)]
        public string GraphAddressUrlMsa { get; set; }

        [XmlAttribute("AddressContainsEntitySetSegment", Namespace = ODataParser.AgsNamespace)]
        public bool GraphAddressContainsEntitySetSegmentSerializedValue
        {
            get {  if (this.GraphAddressContainsEntitySetSegment.HasValue)
                {
                    return this.GraphAddressContainsEntitySetSegment.Value;
                }
                else
                {
                    return false;
                }
            }
            set { this.GraphAddressContainsEntitySetSegment = value; }
        }

        [XmlIgnore]
        public bool GraphAddressContainsEntitySetSegmentSerializedValueSpecified
        {
            get
            {
                return this.GraphAddressContainsEntitySetSegment.HasValue;
            }
        }

        [XmlIgnore]
        public bool? GraphAddressContainsEntitySetSegment { get; set; }

        [XmlAttribute("InstantOnUrl", Namespace = ODataParser.AgsNamespace)]
        public string GraphInstantOnUrl { get; set; }

        public override IODataNavigable NavigateByUriComponent(string component, EntityFramework edmx)
        {
            var navigationPropertyMatch = (from n in this.NavigationProperties
                                           where n.Name == component
                                           select n).FirstOrDefault();
            if (null != navigationPropertyMatch)
            {
                var identifier = navigationPropertyMatch.Type;
                if (identifier.StartsWith("Collection("))
                {
                    var innerId = identifier.Substring(11, identifier.Length - 12);
                    return new ODataCollection(innerId);
                }
                return edmx.LookupNavigableType(identifier);
            }

            return base.NavigateByUriComponent(component, edmx);
        }

        public override IODataNavigable NavigateByEntityTypeKey(EntityFramework edmx)
        {
            throw new NotImplementedException();
        }
    }
}
