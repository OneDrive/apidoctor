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

namespace ApiDocs.DocumentationGeneration.Tests
{
    using System.Collections.Generic;

    using ApiDocs.Validation.OData;

    using NUnit.Framework;

    public class DocumentationTestBase
    {
        protected DataServices dataServices;

        protected DocumentationGenerator documentationGenerator;

        protected EntityFramework entityFramework;

        protected Schema schema;

        protected List<Schema> schemas;

        [SetUp]
        public void TestSetUp()
        {
            this.schemas = new List<Schema>();
            this.dataServices = new DataServices { Schemas = this.schemas };
            this.entityFramework = new EntityFramework { DataServices = this.dataServices };
            this.documentationGenerator = new DocumentationGenerator();
            this.schema = this.GetSchema("microsoft.graph");
        }

        protected void AddNavigationProperty(EntityType et, string name, string type, bool contained = false, string inlineDescription = null, string schemaDescription = null)
        {
            NavigationProperty p = new NavigationProperty { Name = name, Type = type, ContainsTarget = contained };

            if (inlineDescription != null)
            {
                p.Annotation.Add(this.GetDescriptionAnnotation(inlineDescription));
            }

            if (schemaDescription != null)
            {
                Annotations typeAnnotations = new Annotations { Target = $"{this.schema.Namespace}.{et.Name}/{name}" };
                typeAnnotations.AnnotationList.Add(this.GetDescriptionAnnotation(schemaDescription));
                this.schema.Annotations.Add(typeAnnotations);
            }

            et.NavigationProperties.Add(p);
        }

        protected void AddProperty(ComplexType ct, string name, string type = "Edm.String", string inlineDescription = null, string schemaDescription = null)
        {
            Property p = new Property { Name = name, Type = type };

            if (inlineDescription != null)
            {
                p.Annotation.Add(this.GetDescriptionAnnotation(inlineDescription));
            }

            if (schemaDescription != null)
            {
                Annotations typeAnnotations = new Annotations { Target = $"{this.schema.Namespace}.{ct.Name}/{name}" };
                typeAnnotations.AnnotationList.Add(this.GetDescriptionAnnotation(schemaDescription));
                this.schema.Annotations.Add(typeAnnotations);
            }

            ct.Properties.Add(p);
        }

        protected ComplexType GetComplexType(Schema schema, string name, string inlineDescription = null, string schemaDescription = null)
        {
            ComplexType ct = new ComplexType { Name = name, Namespace = schema.Namespace};
            if (inlineDescription != null)
            {
                ct.Annotation.Add(this.GetDescriptionAnnotation(inlineDescription));
            }

            if (schemaDescription != null)
            {
                Annotations typeAnnotations = new Annotations { Target = $"{schema.Namespace}.{name}" };
                typeAnnotations.AnnotationList.Add(this.GetDescriptionAnnotation(schemaDescription));
                schema.Annotations.Add(typeAnnotations);
            }

            schema.ComplexTypes.Add(ct);
            return ct;
        }

        protected Annotation GetDescriptionAnnotation(string description)
        {
            return new Annotation { Term = Term.DescriptionTerm, String = description };
        }

        protected EntityType GetEntityType(Schema schema, string name, string inlineDescription = null, string schemaDescription = null)
        {
            EntityType et = new EntityType { Name = name, Namespace = schema.Namespace};
            if (inlineDescription != null)
            {
                et.Annotation.Add(this.GetDescriptionAnnotation(inlineDescription));
            }

            if (schemaDescription != null)
            {
                Annotations typeAnnotations = new Annotations { Target = $"{schema.Namespace}.{name}" };
                typeAnnotations.AnnotationList.Add(this.GetDescriptionAnnotation(schemaDescription));
                schema.Annotations.Add(typeAnnotations);
            }

            schema.EntityTypes.Add(et);
            return et;
        }

        protected Schema GetSchema(string schemaNamespace)
        {
            Schema s = new Schema { Namespace = schemaNamespace };
            this.schemas.Add(s);
            return s;
        }
    }
}