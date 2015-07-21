using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace OneDrive.ApiDocumentation.Validation.OData
{
    public class NavigationProperty : Property
    {
        public bool ContainsTarget { get; set; }

        public static new string ElementName { get { return "NavigationProperty"; } }
        public static new NavigationProperty FromXml(XElement xml)
        {
            if (xml.Name.LocalName != NavigationProperty.ElementName) throw new ArgumentException("xml wasn't a Property element");

            var obj = new NavigationProperty();
            obj.Name = xml.AttributeValue("Name");
            obj.Type = xml.AttributeValue("Type");
            obj.Nullable = xml.AttributeValue("Nullable").ToBoolean();
            obj.ContainsTarget = xml.AttributeValue("ContainsTarget").ToBoolean();
            return obj;
        }

    }
}
