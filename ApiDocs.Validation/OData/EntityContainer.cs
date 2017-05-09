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
    using System.Linq;
    using System.Xml.Linq;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Transformation;

    [XmlRoot("EntityContainer", Namespace = ODataParser.EdmNamespace)]
    public class EntityContainer : XmlBackedObject, IODataNavigable, Transformation.ITransformable
    {
        public EntityContainer()
        {
            this.EntitySets = new List<EntitySet>();
            this.Singletons = new List<Singleton>();
        }
        
        [XmlAttribute("Name"), SortBy]
        public string Name { get; set; }

        [XmlElement("EntitySet"), Sortable]
        public List<EntitySet> EntitySets { get; set; }

        [XmlElement("Singleton"), Sortable]
        public List<Singleton> Singletons { get; set; }


        public IODataNavigable NavigateByUriComponent(string component, EntityFramework edmx)
        {
            string targetType = null;

            var entitySet = (from e in EntitySets
                             where e.Name == component
                             select e).FirstOrDefault();
            if (null != entitySet)
            {
                // EntitySet is always a collection of an item type
                targetType = "Collection(" + entitySet.EntityType + ")";
            }
            else
            {
                var singleton = (from s in Singletons
                                 where s.Name == component
                                 select s).FirstOrDefault();

                if (null != singleton)
                {
                    targetType = singleton.Type;
                }
            }

            if (targetType != null)
            {
                if (targetType.StartsWith("Collection("))
                {
                    var innerId = targetType.Substring(11, targetType.Length - 12);
                    return new ODataCollection(innerId);
                }
                return edmx.ResourceWithIdentifier<IODataNavigable>(targetType);
            }

            return null;
        }

        public IODataNavigable NavigateByEntityTypeKey(EntityFramework edmx)
        {
            throw new NotSupportedException();
        }

        [XmlIgnore]
        public string TypeIdentifier
        {
            get { return Name; }
        }

        public void ApplyTransformation(Transformation.BaseModifications mods, EntityFramework edmx, string[] versions)
        {
            TransformationHelper.ApplyTransformation(this, mods, edmx, versions);
        }

        [XmlIgnore]
        public string ElementIdentifier { get { return this.Name; } set { this.Name = value; } }
    }
}
