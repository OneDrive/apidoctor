using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ApiDocs.Validation.OData
{
    public class Annotation
    {
        public string Term { get; set; }
        public string String { get; set; }


        public static string ElementName { get { return "Annotation"; } }

        public static Annotation FromXml(System.Xml.Linq.XElement xml)
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
