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
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using Transformation;

    /// <summary>
    /// Holds a representation of an entity framework model (EDMX)
    /// </summary>
    [XmlRoot("Edmx", Namespace = ODataParser.EdmxNamespace)]
    public class EntityFramework : XmlBackedObject
    {
        [XmlAttribute("Version")]
        public string Version { get; set; }

        [XmlElement("DataServices")]
        public DataServices DataServices { get; set; } 

        public EntityFramework()
        {
            this.DataServices = new DataServices();
            this.DataServices.Schemas = new List<OData.Schema>();
            this.Version = "4.0";
        }

        public EntityFramework(IEnumerable<Schema> schemas)
        {
            this.DataServices = new DataServices();
            this.DataServices.Schemas = new List<OData.Schema>(schemas);
            this.Version = "4.0";
        }

        /// <summary>
        /// Apply schema / publishing changes to the EntityFramework
        /// </summary>
        /// <param name="schemaChanges"></param>
        public void ApplyTransformation(PublishSchemaChanges changes, string[] versions)
        {
            if (changes.NamespacesToPublish.Any())
            {
                DataServices.Schemas.RemoveAll(x => !changes.NamespacesToPublish.Contains(x.Namespace));
            }

            TransformationHelper.ApplyTransformationToCollection(changes.Schemas, DataServices.Schemas, this, versions);
        }

        internal void RenameEntityType(ComplexType renamedType)
        {
            // renamedType could be ComplexType or EntityType
            this.RenameTypeInObjectGraph(renamedType.WorkloadName, renamedType.Name);
        }
    }
}
