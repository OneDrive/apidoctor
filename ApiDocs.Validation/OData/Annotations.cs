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
    public class Annotations : XmlBackedTransformableObject
    {
        [XmlElement("Annotation")]
        public List<Annotation> Annotation { get; set; }

        [XmlAttribute("Target"), SortBy]
        public string Target { get; set; }

        [XmlIgnore]
        public override string ElementIdentifier { get { return this.Target; } set { this.Target = value; } }

    }
}
