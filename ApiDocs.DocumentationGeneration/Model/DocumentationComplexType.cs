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

namespace ApiDocs.DocumentationGeneration.Model
{
    using System.Collections.Generic;
    using System.Linq;

    using ApiDocs.DocumentationGeneration.Extensions;
    using ApiDocs.Validation.OData;

    using Newtonsoft.Json;

    public class DocumentationComplexType
    {
        public DocumentationComplexType(EntityFramework entityFramework, ComplexType complexType)
        {
            this.Name = complexType.Name;
            this.Description = complexType.GetDescription(entityFramework);
            this.Namespace = complexType.Namespace;
            this.Properties = complexType.Properties.Select(p => p.ToDocumentationProperty(entityFramework, complexType)).ToList();
            this.Json = ODataParser.BuildJsonExample(complexType, entityFramework.DataServices.Schemas);//SampleJsonGenerator.GetSampleJson(entityFramework, complexType).ToString(Formatting.Indented);
                   

            if (complexType.BaseType != null)
            {
                var baseComplexType = entityFramework.DataServices.Schemas.FindTypeWithIdentifier(complexType.BaseType) as ComplexType;
                if (baseComplexType != null)
                {
                    this.BaseType = baseComplexType.Name;
                }
            }
        }

        public string Namespace { get; private set; }

        public string BaseType { get; private set; }

        public string Description { get; private set; }

        public string Json { get; private set; }

        public string Name { get; private set; }

        public IList<DocumentationProperty> Properties { get; private set; }
    }
}