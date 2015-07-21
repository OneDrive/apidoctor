namespace ApiDocs.Validation.OData
{
    using System;
    using System.Xml.Linq;

    //<Parameter Name="bindingParameter" Type="oneDrive.item" Nullable="false"/>

    public class Parameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Nullable { get; set; }
        
        
        public static string ElementName { get { return "Parameter"; } }

        internal static Parameter FromXml(XElement xml)
        {
            if (xml.Name.LocalName != ElementName) throw new ArgumentException("xml wasn't a Parameter element");

            var obj = new Parameter
            {
                Name = xml.Attribute("Name").Value,
                Type = xml.Attribute("Type").Value,
                Nullable = xml.Attribute("Nullable").Value.ToBoolean()
            };


            return obj;
        }
    }
}
