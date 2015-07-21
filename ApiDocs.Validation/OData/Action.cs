using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ApiDocs.Validation.OData
{
    public class Action
    {
        public string Name { get; set; }
        public bool IsBound { get; set; }
        public List<Parameter> Parameters { get; set; }
        public ReturnType ReturnType { get; set; }

        public Action()
        {
            this.Parameters = new List<Parameter>();
        }


        public static string ElementName { get { return "Action"; } }
        public static OData.Action FromXml(XElement xml)
        {
            if (xml.Name.LocalName != ElementName) throw new ArgumentException("xml was not a Action element");
            var obj = new OData.Action
            {
                Name = xml.AttributeValue("Name"),
                IsBound = xml.AttributeValue("IsBound").ToBoolean()
            };
            obj.Parameters.AddRange(from e in xml.Elements()
                                    where e.Name == Parameter.ElementName
                                    select Parameter.FromXml(e));
            obj.ReturnType = (from e in xml.Elements()
                              where e.Name.LocalName == ReturnType.ElementName
                              select ReturnType.FromXml(e)).FirstOrDefault();
            return obj;
        }

    }
}
