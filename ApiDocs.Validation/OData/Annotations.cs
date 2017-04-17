using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ApiDocs.Validation.OData
{
    [XmlRoot("Annotations", Namespace = ODataParser.EdmNamespace)]
    public class Annotations
    {
        public Annotations()
        {
            this.AnnotationList = new List<Annotation>();
        }

        [XmlElement("Target")]
        public string Target { get; set; }

        [XmlElement("Annotation", Namespace = ODataParser.EdmNamespace)]
        public List<Annotation> AnnotationList { get; set; }
    }
}
