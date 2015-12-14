using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ApiDocs.Validation.OData
{
    internal static class XmlParseHelper
    {
        public static void ThrowIfWrongElement(this Type t, System.Xml.Linq.XElement xml)
        {
            var name = t.XmlElementName();
            if (name != xml.Name.LocalName)
                throw new InvalidOperationException(
                    string.Format("Invalid XML element name. Expected {0} but was {1}",
                    name,
                    xml.Name.LocalName));
        }

        public static string XmlElementName(this Type t)
        {
            var attributes = t.GetCustomAttributes(typeof(XmlRootAttribute), true);
            var value = attributes.FirstOrDefault() as XmlRootAttribute;
            if (null == value)
                throw new InvalidOperationException("Missing XmlTagName attribute on type");

            return value.ElementName;
        }
    }
}
