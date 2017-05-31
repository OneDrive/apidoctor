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
        public Annotations()
        {
            this.AnnotationList = new List<Annotation>();
        }

        [XmlAttribute("Target"), SortBy]
        public string Target { get; set; }

        public string ElementIdentifier { get; set; }

        public void ApplyTransformation(Transformation.BaseModifications mods, EntityFramework edmx, string[] versions)
        {
            this.AnnotationList = new List<Annotation>();
        }

        [XmlElement("Annotation", Namespace = ODataParser.EdmNamespace)]
        public List<Annotation> AnnotationList { get; set; }
    }
}
