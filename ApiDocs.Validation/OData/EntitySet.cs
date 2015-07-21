using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ApiDocs.Validation.OData
{
    public class EntitySet
    {
        public string Name { get; set; }
        public string EntityType { get; set; }

        internal static EntitySet FromXml(XElement xml)
        {
            if (xml.Name.LocalName != EntitySet.ElementName) throw new ArgumentException("xml was not an EntitySet element");
            var obj = new EntitySet();
            obj.Name = xml.AttributeValue("Name");
            obj.EntityType = xml.AttributeValue("EntityType");
            return obj;
        }

        public static string ElementName { get { return "EntitySet"; } }
    }
}
