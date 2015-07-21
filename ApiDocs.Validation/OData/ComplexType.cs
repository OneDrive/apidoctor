using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace OneDrive.ApiDocumentation.Validation.OData
{
    public class ComplexType
    {
        public ComplexType()
        {
            Properties = new List<Property>();
        }

        public string Name { get; set; }
        public List<Property> Properties { get; set; }

        public static string ElementName { get { return "ComplexType"; } }
        public static ComplexType FromXml(XElement xml)
        {
            if (xml.Name.LocalName != ComplexType.ElementName) throw new ArgumentException("xml was not a ComplexType element");
            var obj = new ComplexType();
            obj.Name = xml.AttributeValue("Name");
            obj.Properties.AddRange(from e in xml.Elements()
                                    where e.Name.LocalName == "Property"
                                    select Property.FromXml(e));
            return obj;
        }
    }
}
