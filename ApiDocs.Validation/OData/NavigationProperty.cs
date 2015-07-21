using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ApiDocs.Validation.OData
{
    public class NavigationProperty : Property
    {
        public bool ContainsTarget { get; set; }

        public static new string ElementName { get { return "NavigationProperty"; } }
        public static new NavigationProperty FromXml(XElement xml)
        {
            if (xml.Name.LocalName != ElementName) throw new ArgumentException("xml wasn't a Property element");

            var obj = new NavigationProperty
            {
                Name = xml.AttributeValue("Name"),
                Type = xml.AttributeValue("Type"),
                Nullable = xml.AttributeValue("Nullable").ToBoolean(),
                ContainsTarget = xml.AttributeValue("ContainsTarget").ToBoolean()
            };
            return obj;
        }

    }
}
