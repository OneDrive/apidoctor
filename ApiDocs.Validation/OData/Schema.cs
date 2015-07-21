using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation.OData
{
    public class Schema
    {
        public string Namespace { get; set; }
        public List<EntityType> Entities { get; set; }
        public List<ComplexType> ComplexTypes { get; set; }
        public List<EntityContainer> EntityContainers { get; set; }
        public List<Function> Functions { get; set; }
        public List<OData.Action> Actions { get; set; }
        public List<Term> Terms { get; set; }

        public Schema()
        {
            Entities = new List<EntityType>();
            ComplexTypes = new List<ComplexType>();
            EntityContainers = new List<EntityContainer>();
            Functions = new List<Function>();
            Actions = new List<Action>();
            Terms = new List<Term>();
        }


        public static string ElementName { get { return "Schema"; } }

        internal static Schema FromXml(System.Xml.Linq.XElement xml)
        {
            if (xml.Name.LocalName != Schema.ElementName) throw new ArgumentException("xml was not a Schema element");

            var obj = new Schema();
            obj.Namespace = xml.AttributeValue("Namespace");

            obj.Entities.AddRange(from e in xml.Elements()
                                  where e.Name.LocalName == EntityType.ElementName
                                  select EntityType.FromXml(e));

            obj.ComplexTypes.AddRange(from e in xml.Elements()
                                      where e.Name.LocalName == ComplexType.ElementName
                                      select ComplexType.FromXml(e));

            obj.EntityContainers.AddRange(from e in xml.Elements()
                                          where e.Name.LocalName == EntityContainer.ElementName
                                          select EntityContainer.FromXml(e));

            obj.Functions.AddRange(from e in xml.Elements()
                                   where e.Name.LocalName == Function.ElementName
                                   select Function.FromXml(e));

            obj.Actions.AddRange(from e in xml.Elements()
                                 where e.Name.LocalName == OData.Action.ElementName
                                 select OData.Action.FromXml(e));

            obj.Terms.AddRange(from e in xml.Elements()
                               where e.Name.LocalName == Term.ElementName
                               select Term.FromXml(e));

            return obj;
        }
    }
}
