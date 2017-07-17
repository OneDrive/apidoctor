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

namespace ApiDocs.DocumentationGeneration.UnitTests
{
    using ApiDocs.DocumentationGeneration.Tests;
    using ApiDocs.Validation.OData;

    using NUnit.Framework;

    [TestFixture]
    public class PropertyTests : DocumentationTestBase
    {
        [Test]
        public void PropertyTableEntryIsCorrectlyFormattedForComplexType()
        {
            string propertyType = "propertyComplexType";
            ComplexType ct = this.GetComplexType(this.schema, "test");
            ComplexType ct2 = this.GetComplexType(this.schema, "propertyComplexType");
            string description = "Inline property description";
            string propertyName = "ComplexProperty";
            this.AddProperty(ct, propertyName, type: $"{this.schema.Namespace}.{propertyType}", inlineDescription: description);

            string expected = $"|{propertyName}|[{propertyType}]({propertyType}.md)|{description}|";

            string markDown = this.documentationGenerator.GetMarkDownForType(this.entityFramework, ct);

            Assert.IsTrue(markDown.Contains(expected), "Generated markdown should contain '{0}' Actual:\n{1}", expected, markDown);
        }

        [Test]
        public void PropertyTableEntryIsCorrectlyFormattedForComplexTypeCollection()
        {
            string propertyType = "propertyComplexType";
            ComplexType ct = this.GetComplexType(this.schema, "test");
            ComplexType ct2 = this.GetComplexType(this.schema, "propertyComplexType");
            string description = "Inline property description";
            string propertyName = "ComplexProperty";
            this.AddProperty(ct, propertyName, type: $"Collection({this.schema.Namespace}.{propertyType})", inlineDescription: description);

            string expected = $"|{propertyName}|[{propertyType}]({propertyType}.md) collection|{description}|";

            string markDown = this.documentationGenerator.GetMarkDownForType(this.entityFramework, ct);

            Assert.IsTrue(markDown.Contains(expected), "Generated markdown should contain '{0}' Actual:\n{1}", expected, markDown);
        }

        [Test]
        public void PropertyTableEntryIsCorrectlyFormattedForPrimitiveType()
        {
            string description = "Inline property description";
            string propertyType = "Int32";
            string propertyName = "PrimitiveProperty";
            string expected = $"|{propertyName}|{propertyType}|{description}|";
            ComplexType ct = this.GetComplexType(this.schema, "Test");
            this.AddProperty(ct, propertyName, type: $"Edm.{propertyType}", inlineDescription: description);
            string markDown = this.documentationGenerator.GetMarkDownForType(this.entityFramework, ct);

            Assert.IsTrue(markDown.Contains(expected), "Generated markdown should contain '{0}' Actual:\n{1}", expected, markDown);
        }

        [Test]
        public void PropertyTableEntryIsCorrectlyFormattedForPrimitiveTypeCollection()
        {
            string description = "Inline property description";
            string propertyType = "Boolean";
            string propertyName = "PrimitiveProperty";
            string expected = $"|{propertyName}|{propertyType} collection|{description}|";
            ComplexType ct = this.GetComplexType(this.schema, "Test");
            this.AddProperty(ct, propertyName, type: $"Collection(Edm.{propertyType})", inlineDescription: description);
            string markDown = this.documentationGenerator.GetMarkDownForType(this.entityFramework, ct);

            Assert.IsTrue(markDown.Contains(expected), "Generated markdown should contain '{0}' Actual:\n{1}", expected, markDown);
        }
    }
}