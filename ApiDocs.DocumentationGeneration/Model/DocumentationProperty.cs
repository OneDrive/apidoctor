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

using ApiDocs.Validation;

namespace ApiDocs.DocumentationGeneration.Model
{
    using System.Collections.Generic;

    using ApiDocs.DocumentationGeneration.Extensions;
    using ApiDocs.Validation.OData;

    public class DocumentationProperty
    {
        public DocumentationProperty(EntityFramework entityFramework, ComplexType complexType, Property property)
        {
            this.Name = property.Name;

            string typeName = property.Type;
            bool isCollection = property.Type.IsCollection();
            if (isCollection)
            {
                typeName = property.Type.ElementName();
            }

            var simpleType = typeName.ToODataSimpleType();
            if (simpleType != SimpleDataType.None)
            {
                this.Type = new ParameterDataType(simpleType, isCollection);
            }
            else
            {
                this.Type = new ParameterDataType(typeName, isCollection);
            }
            this.TypeMarkDown = this.Type.GetMarkDown();
            this.Description = property.GetDescription(entityFramework, complexType);
        }

        public string Description { get; private set; }

        public string Name { get; private set; }

        public ParameterDataType Type { get; private set; }

        public string TypeMarkDown { get; private set; }
    }
}