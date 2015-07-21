namespace ApiDocs.Validation.OData
{
    using System;
    using System.Xml.Linq;

    public class Property
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Nullable { get; set; }


        public static string ElementName { get { return "Property"; } }

        public static Property FromXml(XElement xml)
        {
            if (xml.Name.LocalName != ElementName) throw new ArgumentException("xml wasn't a Property element");

            var obj = new Property
            {
                Name = xml.AttributeValue("Name"),
                Type = xml.AttributeValue("Type"),
                Nullable = xml.AttributeValue("Nullable").ToBoolean()
            };

            return obj;
        }

    }
}
