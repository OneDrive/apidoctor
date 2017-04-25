using ApiDocs.Validation.OData.Transformation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace ApiDocs.Validation.OData
{
    public abstract class XmlBackedObject
    {
        [XmlAnyElement]
        public List<XmlNode> ExtraElements { get; set; }

        [XmlAnyAttribute]
        public List<XmlNode> ExtraAttributes { get; set; }

        public bool HasUnknownMembers { get { return (null != ExtraElements && ExtraElements.Any()) || 
                                                     (null != ExtraAttributes && ExtraAttributes.Any()); } }

    }
}
