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
    using ApiDocs.Validation.OData;

    using NUnit.Framework;

    [TestFixture]
    public class DescriptionTests : DocumentationTestBase
    {
        [Test]
        public void TestInlineComplexTypeDescription()
        {
            string inlineDescription = "Inline description";
            ComplexType ct = this.GetComplexType(this.schema, "Test", inlineDescription: inlineDescription);
            string markDown = this.documentationGenerator.GetMarkDownForType(this.entityFramework, ct);

            Assert.IsTrue(markDown.Contains(inlineDescription), "Generated markdown should contain inline description annotation");
        }

        [Test]
        public void TestInlinePropertyDescription()
        {
            string inlineDescription = "Inline property description";
            ComplexType ct = this.GetComplexType(this.schema, "Test", inlineDescription: inlineDescription);
            this.AddProperty(ct, "TestProperty", inlineDescription: inlineDescription);
            string markDown = this.documentationGenerator.GetMarkDownForType(this.entityFramework, ct);

            Assert.IsTrue(markDown.Contains(inlineDescription), "Generated markdown should contain inline description annotation");
        }

        [Test]
        public void TestSchemaComplexTypeDescription()
        {
            string schemaDescription = "Schema description";
            ComplexType ct = this.GetComplexType(this.schema, "Test", schemaDescription: schemaDescription);
            string markDown = this.documentationGenerator.GetMarkDownForType(this.entityFramework, ct);

            Assert.IsTrue(markDown.Contains(schemaDescription), "Generated markdown should contain schema description annotation");
        }

        [Test]
        public void TestSchemaPropertyDescription()
        {
            string schemaDescription = "Schema description";
            ComplexType ct = this.GetComplexType(this.schema, "Test", schemaDescription: schemaDescription);
            string markDown = this.documentationGenerator.GetMarkDownForType(this.entityFramework, ct);

            Assert.IsTrue(markDown.Contains(schemaDescription), "Generated markdown should contain schema description annotation");
        }
    }
}