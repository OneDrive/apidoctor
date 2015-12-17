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
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [XmlRoot("Schemas", Namespace = ODataParser.EdmNamespace)]
    public class Schema
    {
        [XmlAttribute("Namespace", Namespace = ODataParser.EdmNamespace)]
        public string Namespace { get; set; }

        [XmlElement("EntityType", Namespace = ODataParser.EdmNamespace)]
        public List<EntityType> Entities { get; set; }

        [XmlElement("ComplexType", Namespace = ODataParser.EdmNamespace)]
        public List<ComplexType> ComplexTypes { get; set; }

        [XmlElement("EntityContainer", Namespace = ODataParser.EdmNamespace)]
        public List<EntityContainer> EntityContainers { get; set; }

        [XmlElement("Function", Namespace = ODataParser.EdmNamespace)]
        public List<Function> Functions { get; set; }

        [XmlElement("Action", Namespace = ODataParser.EdmNamespace)]
        public List<Action> Actions { get; set; }

        [XmlElement("Term", Namespace = ODataParser.EdmNamespace)]
        public List<Term> Terms { get; set; }

        [XmlElement("Annotations", Namespace = ODataParser.EdmNamespace)]
        public Annotations Annotations { get; set; }

        public Schema()
        {
            this.Entities = new List<EntityType>();
            this.ComplexTypes = new List<ComplexType>();
            this.EntityContainers = new List<EntityContainer>();
            this.Functions = new List<Function>();
            this.Actions = new List<Action>();
            this.Terms = new List<Term>();
        }
     
    }
}
