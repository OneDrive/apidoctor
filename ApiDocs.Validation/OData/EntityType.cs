namespace ApiDocs.Validation.OData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    public class EntityType : ComplexType
    {
        public EntityType()
        {
            this.Properties = new List<Property>();
            this.NavigationProperties = new List<NavigationProperty>();
        }

        public List<NavigationProperty> NavigationProperties { get; set; }

        public static new string ElementName { get { return "EntityType"; } }
        public static new EntityType FromXml(XElement xml)
        {
            if (xml.Name.LocalName != ElementName) throw new ArgumentException("xml is not an EntityType element");

            var obj = new EntityType { Name = xml.AttributeValue("Name") };

            obj.Properties.AddRange(from e in xml.Elements()
                                    where e.Name.LocalName == Property.ElementName
                                    select Property.FromXml(e));
            obj.NavigationProperties.AddRange(from e in xml.Elements()
                                              where e.Name.LocalName == NavigationProperty.ElementName
                                              select NavigationProperty.FromXml(e));

            return obj;
        }
    }
}
