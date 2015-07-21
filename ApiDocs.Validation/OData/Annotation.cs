namespace ApiDocs.Validation.OData
{
    using System;
    using System.Xml.Linq;

    public class Annotation
    {
        public string Term { get; set; }
        public string String { get; set; }


        public static string ElementName { get { return "Annotation"; } }

        public static Annotation FromXml(XElement xml)
        {
            if (xml.Name.LocalName != ElementName) throw new ArgumentException("xml was not an Annotation element");

            var obj = new Annotation
            {
                Term = xml.AttributeValue("Term"),
                String = xml.AttributeValue("String")
            };
            return obj;
        }
    }
}
