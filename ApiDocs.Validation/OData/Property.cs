using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ApiDocs.Validation.OData
{
    public class Property
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Nullable { get; set; }


        public static string ElementName { get { return "Property"; } }

        public static Property FromXml(XElement xml)
        {
            if (xml.Name.LocalName != Property.ElementName) throw new ArgumentException("xml wasn't a Property element");

            var obj = new Property();
            obj.Name = xml.AttributeValue("Name");
            obj.Type = xml.AttributeValue("Type");
            obj.Nullable = xml.AttributeValue("Nullable").ToBoolean();

            return obj;
        }

    }
}
