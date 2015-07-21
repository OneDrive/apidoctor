using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OneDrive.ApiDocumentation.Validation.OData
{
    public class Annotation
    {
        public string Term { get; set; }
        public string String { get; set; }


        public static string ElementName { get { return "Annotation"; } }

        public static Annotation FromXml(System.Xml.Linq.XElement xml)
        {
            if (xml.Name.LocalName != Annotation.ElementName) throw new ArgumentException("xml was not an Annotation element");

            var obj = new Annotation();
            obj.Term = xml.AttributeValue("Term");
            obj.String = xml.AttributeValue("String");
            return obj;
        }
    }
}
