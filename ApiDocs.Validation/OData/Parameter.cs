using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ApiDocs.Validation.OData
{

    //<Parameter Name="bindingParameter" Type="oneDrive.item" Nullable="false"/>

    public class Parameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Nullable { get; set; }
        
        
        public static string ElementName { get { return "Parameter"; } }

        internal static Parameter FromXml(System.Xml.Linq.XElement xml)
        {
            if (xml.Name.LocalName != Parameter.ElementName) throw new ArgumentException("xml wasn't a Parameter element");

            var obj = new Parameter();
            
            obj.Name = xml.Attribute("Name").Value;
            obj.Type = xml.Attribute("Type").Value;
            obj.Nullable = xml.Attribute("Nullable").Value.ToBoolean();
            
            return obj;
        }
    }
}
