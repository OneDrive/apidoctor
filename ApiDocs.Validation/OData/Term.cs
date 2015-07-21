namespace ApiDocs.Validation.OData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    /*
       <Term Name="sourceUrl" Type="Edm.String" AppliesTo="oneDrive.item">
            <Annotation Term="Org.OData.Core.V1.LongDescription" String="When used on a PUT or POST call to an Item, causes the item's content to be copied from the URL specified in the attribute."/>
       </Term>
     */
    public class Term
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string AppliesTo { get; set; }
        public List<Annotation> Annotations { get; set; }

        public Term()
        {
            this.Annotations = new List<Annotation>();
        }

        public static string ElementName { get { return "Term"; } }

        public static Term FromXml(XElement xml)
        {
            if (xml.Name.LocalName != ElementName) throw new ArgumentException("xml was not a Term element");

            var obj = new Term
            {
                Name = xml.AttributeValue("Name"),
                Type = xml.AttributeValue("Type"),
                AppliesTo = xml.AttributeValue("AppliesTo")
            };
            obj.Annotations.AddRange(from e in xml.Elements()
                                     where e.Name.LocalName == Annotation.ElementName
                                     select Annotation.FromXml(e));
            return obj;
        }


    }
}
