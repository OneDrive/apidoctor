using ApiDocs.Validation.OData.Transformation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ApiDocs.Validation.OData
{
    [XmlRoot("Annotations", Namespace = ODataParser.EdmNamespace)]
    public class Annotations : XmlBackedObject, Transformation.ITransformable
    {
        [XmlElement("Annotation")]
        public List<Annotation> Annotation { get; set; }

        [XmlAttribute("Target"), SortBy]
        public string Target { get; set; }

        public void ApplyTransformation(Transformation.BaseModifications mods, EntityFramework edmx, string[] versions)
        {
            TransformationHelper.ApplyTransformation(this, mods, edmx, versions);
        }

        [XmlIgnore]
        public string ElementIdentifier { get { return this.Target; } set { this.Target = value; } }

    }
}
