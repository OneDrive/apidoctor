using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ApiDocs.Validation.OData
{
    public class Function
    {
        public Function()
        {
            Parameters = new List<Parameter>();
        }

        public string Name { get; set; }
        public bool IsBound { get; set; }
        public List<Parameter> Parameters { get; set; }
        public ReturnType ReturnType { get; set; }

        public static string ElementName { get { return "Function"; } }
        public static Function FromXml(XElement xml)
        {
            if (xml.Name.LocalName != Function.ElementName) throw new ArgumentException("xml is not an Function element");

            var obj = new Function();
            obj.Name = xml.AttributeValue("Name");
            obj.IsBound = xml.AttributeValue("IsBound").ToBoolean();

            obj.Parameters.AddRange(from e in xml.Elements()
                                    where e.Name.LocalName == Parameter.ElementName
                                    select Parameter.FromXml(e));

            obj.ReturnType = (from e in xml.Elements()
                              where e.Name.LocalName == ReturnType.ElementName
                              select ReturnType.FromXml(e)).FirstOrDefault();

            return obj;
        }
    }
}
