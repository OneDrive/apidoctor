/*
 * Markdown Scanner
 * Copyright (c) Microsoft Corporation
 * All rights reserved. 
 * 
 * MIT License
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of 
 * this software and associated documentation files (the ""Software""), to deal in 
 * the Software without restriction, including without limitation the rights to use, 
 * copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
 * Software, and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all 
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace ApiDocs.Validation.OData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    public class Schema
    {
        public string Namespace { get; set; }
        public List<EntityType> Entities { get; set; }
        public List<ComplexType> ComplexTypes { get; set; }
        public List<EntityContainer> EntityContainers { get; set; }
        public List<Function> Functions { get; set; }
        public List<Action> Actions { get; set; }
        public List<Term> Terms { get; set; }

        public Schema()
        {
            this.Entities = new List<EntityType>();
            this.ComplexTypes = new List<ComplexType>();
            this.EntityContainers = new List<EntityContainer>();
            this.Functions = new List<Function>();
            this.Actions = new List<Action>();
            this.Terms = new List<Term>();
        }


        public static string ElementName { get { return "Schema"; } }

        internal static Schema FromXml(XElement xml)
        {
            if (xml.Name.LocalName != ElementName) throw new ArgumentException("xml was not a Schema element");

            var obj = new Schema { Namespace = xml.AttributeValue("Namespace") };

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
                                 where e.Name.LocalName == Action.ElementName
                                 select Action.FromXml(e));

            obj.Terms.AddRange(from e in xml.Elements()
                               where e.Name.LocalName == Term.ElementName
                               select Term.FromXml(e));

            return obj;
        }
    }
}
