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
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using ApiDocs.DocumentationGeneration.Extensions;
    using ApiDocs.DocumentationGeneration.Tests;
    using ApiDocs.Validation;
    using ApiDocs.Validation.Error;
    using ApiDocs.Validation.OData;
    using ApiDocs.Validation.UnitTests;

    using NUnit.Framework;

    /// <summary>
    /// Verify that the generated MarkDown documentation can be parsed as a <see cref="DocFile"/>
    /// </summary>
    [TestFixture]
    public class ResourceRoundTripTests : DocumentationTestBase
    {
        [Test]
        public void TestRoudTripEntityType()
        {
            EntityType otherEntity = this.GetEntityType(this.schema, "otherEntity");
            ComplexType complexPropertyType = this.GetComplexType(this.schema, "TestComplexPropertyType");
            EntityType entity = this.GetEntityType(this.schema, "EntityType", schemaDescription: "Schema Description");
            this.AddProperty(entity, "TestProperty", "Edm.Int32", schemaDescription: "Test int32 property");
            this.AddProperty(entity, "TestCollectionProperty", "Collection(Edm.String)", "Test collection property");
            this.AddProperty(entity, "TestComplexProperty", this.entityFramework.LookupIdentifierForType(complexPropertyType), "Test complex type property");
            this.AddProperty(entity, "TestComplexCollectionProperty", "Collection(" + this.entityFramework.LookupIdentifierForType(complexPropertyType) + ")", "Test complex type property");
            this.AddNavigationProperty(entity, "TestEntityNavigation", this.entityFramework.LookupIdentifierForType(otherEntity), inlineDescription: "Entity Navigation");
            this.AddNavigationProperty(entity, "TestCollectionNavigation", "Collection(" + this.entityFramework.LookupIdentifierForType(otherEntity) + ")", schemaDescription: "Collection Navigation Property");

            DocFile testFile = this.GetDocFileForEntityType(entity);

            Assert.AreEqual(1, testFile.Resources.Length, "Expected to find one resource");
            var resource = testFile.Resources[0];
            this.VerifyEntityType(entity, resource);
        }

        [Test]
        public void TestRoundTripComplexType()
        {
            ComplexType complexPropertyType = this.GetComplexType(this.schema, "TestComplexPropertyType");
            ComplexType testType = this.GetComplexType(this.schema, "TestType", "Test Description");
            this.AddProperty(testType, "TestProperty", "Edm.Boolean", "Test bool property");
            this.AddProperty(testType, "TestCollectionProperty", "Collection(Edm.String)", "Test collection property");
            this.AddProperty(testType, "TestComplexProperty", this.entityFramework.LookupIdentifierForType(complexPropertyType), "Test complex type property");
            this.AddProperty(testType, "TestComplexCollectionProperty", "Collection(" + this.entityFramework.LookupIdentifierForType(complexPropertyType) + ")", "Test complex type property");

            DocFile testFile = this.GetDocFileForComplexType(testType);

            Assert.AreEqual(1, testFile.Resources.Length, "Expected to find one resource");
            var resource = testFile.Resources[0];
            this.VerifyComplexType(testType, resource);
        }

        private DocFile GetDocFileForComplexType(ComplexType testType)
        {
            string markDown = this.documentationGenerator.GetMarkDownForType(this.entityFramework, testType);

            DocSet docSet = new DocSet(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            DocFile testFile = new DocFileForTesting(markDown, @"\resources.md", @"\resources.md", docSet);
            ValidationError[] errors = null;
            testFile.Scan(string.Empty, out errors);
            Assert.IsFalse(errors.WereWarningsOrErrors(), "Expected no validation warnings/errors: {0}", errors.ErrorsToString());
            return testFile;
        }

        private DocFile GetDocFileForEntityType(EntityType testType)
        {
            string markDown = this.documentationGenerator.GetMarkDownForType(this.entityFramework, testType);

            DocSet docSet = new DocSet(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            DocFile testFile = new DocFileForTesting(markDown, @"\resources.md", @"\resources.md", docSet);
            ValidationError[] errors = null;
            testFile.Scan(string.Empty, out errors);
            Assert.IsFalse(errors.WereWarningsOrErrors(), "Expected no validation warnings/errors: {0}", errors.Where(e => e.IsWarningOrError).ErrorsToString());
            return testFile;
        }

        private void VerifyComplexType(ComplexType testType, ResourceDefinition resource)
        {
            Assert.AreEqual($"{this.schema.Namespace}.{testType.Name}", resource.Name, "Resource name does not match");

            // Ignore the odata.type property
            var resourceProperties = resource.Parameters.Where(p => p.Name != "@odata.type" && !p.IsNavigatable).ToList();

            Assert.AreEqual(testType.Properties.Count, resourceProperties.Count(), "Property count does not match");

            for (int i = 0; i < resourceProperties.Count; i++)
            {
                Assert.AreEqual(testType.Properties[i].Name, resourceProperties[i].Name, "Name of property does not match");
                Assert.AreEqual(testType.Properties[i].Type, resourceProperties[i].Type.ODataResourceName(), "Type of property {0} does not match", testType.Properties[i].Name);
                Assert.AreEqual(testType.Properties[i].ToDocumentationProperty(this.entityFramework, testType).Description, resourceProperties[i].Description, "Description for property {0} does not match", testType.Properties[i].Name);
            }
        }

        private void VerifyEntityType(EntityType testType, ResourceDefinition resource)
        {
            this.VerifyComplexType(testType, resource);
            Assert.AreEqual($"{this.schema.Namespace}.{testType.Name}", resource.Name, "Resource name does not match");

            // Ignore the odata.type property
            var navigationProperties = resource.Parameters.Where(p => p.IsNavigatable).ToList();

            Assert.AreEqual(testType.NavigationProperties.Count, navigationProperties.Count(), "Navigation Property count does not match");

            for (int i = 0; i < navigationProperties.Count; i++)
            {
                Assert.AreEqual(testType.NavigationProperties[i].Name, navigationProperties[i].Name, "Name of navigation property does not match");

                // TODO: Fix parsing of Navigation Property types
                // Assert.AreEqual(testType.NavigationProperties[i].Type, navigationProperties[i].Type.ODataResourceName(), "Type of navigation property {0} does not match", testType.NavigationProperties[i].Name);
                Assert.AreEqual(testType.NavigationProperties[i].ToDocumentationProperty(this.entityFramework, testType).Description, navigationProperties[i].Description, "Description for property {0} does not match", testType.NavigationProperties[i].Name);
            }
        }
    }
}