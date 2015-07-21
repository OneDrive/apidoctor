namespace ApiDocs.Validation.OData
{
    using System;
    using System.Linq;
    using System.Xml.Linq;

    public class EntityContainer
    {
        
        public string Name { get; set; }
        public EntitySet EntitySet { get; set; }

        public static string ElementName { get { return "EntityContainer"; } }
        public static EntityContainer FromXml(XElement xml)
        {
            if (xml.Name.LocalName != ElementName) throw new ArgumentException("xml was not an EntityContainer element");
            EntityContainer obj = new EntityContainer
            {
                Name = xml.AttributeValue("Name"),
                EntitySet = (from e in xml.Elements()
                    where e.Name.LocalName == EntitySet.ElementName
                    select EntitySet.FromXml(e)).FirstOrDefault()
            };
            return obj;
        }

    }
}
