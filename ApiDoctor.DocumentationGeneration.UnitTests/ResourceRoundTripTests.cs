/*
 * API Doctor
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

namespace ApiDoctor.DocumentationGeneration.UnitTests
{
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using ApiDoctor.DocumentationGeneration.Extensions;
    using ApiDoctor.DocumentationGeneration.Tests;
    using ApiDoctor.Validation;
    using ApiDoctor.Validation.Error;
    using ApiDoctor.Validation.OData;
    using ApiDoctor.Validation.UnitTests;

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
            ComplexType complexPropertyType = this.GetComplexType(this.schema, "testComplexPropertyType");
            EntityType entity = this.GetEntityType(this.schema, "entityType", schemaDescription: "Schema Description");
            this.AddProperty(entity, "testProperty", "Edm.Int32", schemaDescription: "Test int32 property");
            this.AddProperty(entity, "testCollectionProperty", "Collection(Edm.String)", "Test collection property");
            this.AddProperty(entity, "testComplexProperty", this.entityFramework.LookupIdentifierForType(complexPropertyType), "Test complex type property");
            this.AddProperty(entity, "testComplexCollectionProperty", "Collection(" + this.entityFramework.LookupIdentifierForType(complexPropertyType) + ")", "Test complex type property");
            this.AddNavigationProperty(entity, "testEntityNavigation", this.entityFramework.LookupIdentifierForType(otherEntity), inlineDescription: "Entity Navigation");
            this.AddNavigationProperty(entity, "testCollectionNavigation", "Collection(" + this.entityFramework.LookupIdentifierForType(otherEntity) + ")", schemaDescription: "Collection Navigation Property");

            DocFile testFile = this.GetDocFileForEntityType(entity);

            Assert.That(1, Is.EqualTo(testFile.Resources.Length), "Expected to find one resource");
            var resource = testFile.Resources[0];
            this.VerifyEntityType(entity, resource);
        }

        [Test]
        public void TestRoundTripComplexType()
        {
            ComplexType complexPropertyType = this.GetComplexType(this.schema, "testComplexPropertyType");
            ComplexType testType = this.GetComplexType(this.schema, "testType", "Test Description");
            this.AddProperty(testType, "testProperty", "Edm.Boolean", "Test bool property");
            this.AddProperty(testType, "testCollectionProperty", "Collection(Edm.String)", "Test collection property");
            this.AddProperty(testType, "testComplexProperty", this.entityFramework.LookupIdentifierForType(complexPropertyType), "Test complex type property");
            this.AddProperty(testType, "testComplexCollectionProperty", "Collection(" + this.entityFramework.LookupIdentifierForType(complexPropertyType) + ")", "Test complex type property");

            DocFile testFile = this.GetDocFileForComplexType(testType);

            Assert.That(1, Is.EqualTo(testFile.Resources.Length), "Expected to find one resource");
            var resource = testFile.Resources[0];
            this.VerifyComplexType(testType, resource);
        }

        private DocFile GetDocFileForComplexType(ComplexType testType)
        {
            string markDown = this.documentationGenerator.GetMarkDownForType(this.entityFramework, testType);

            DocSet docSet = new DocSet(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            DocSet.SchemaConfig.DefaultNamespace = "microsoft.graph";
            DocFile testFile = new DocFileForTesting(markDown, @"\resources.md", @"\resources.md", docSet);
            var issues = new IssueLogger();
            testFile.Scan(string.Empty, issues.For("testFile"));
            Assert.That(issues.Issues.WereWarningsOrErrors(), Is.False, "Expected no validation warnings/errors: {0}", issues.Issues.ErrorsToString());
            return testFile;
        }

        private DocFile GetDocFileForEntityType(EntityType testType)
        {
            string markDown = this.documentationGenerator.GetMarkDownForType(this.entityFramework, testType);

            DocSet docSet = new DocSet(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            DocSet.SchemaConfig.DefaultNamespace = "microsoft.graph";
            DocFile testFile = new DocFileForTesting(markDown, @"\resources.md", @"\resources.md", docSet);
            var issues = new IssueLogger();
            testFile.Scan(string.Empty, issues.For("testFile"));
            Assert.That(issues.Issues.WereWarningsOrErrors(), Is.False, "Expected no validation warnings/errors: {0}", issues.Issues.Where(e => e.IsWarningOrError).ErrorsToString());
            return testFile;
        }

        private void VerifyComplexType(ComplexType testType, ResourceDefinition resource)
        {
            Assert.That($"{this.schema.Namespace}.{testType.Name}", Is.EqualTo(resource.Name), "Resource name does not match");

            // Ignore the odata.type property
            var resourceProperties = resource.Parameters.Where(p => p.Name != "@odata.type" && !p.IsNavigatable).ToList();

            Assert.That(testType.Properties.Count, Is.EqualTo(resourceProperties.Count()), "Property count does not match");

            for (int i = 0; i < resourceProperties.Count; i++)
            {
                Assert.That(testType.Properties[i].Name, Is.EqualTo(resourceProperties[i].Name), "Name of property does not match");
                Assert.That(testType.Properties[i].Type, Is.EqualTo(resourceProperties[i].Type.ODataResourceName()), "Type of property {0} does not match", testType.Properties[i].Name);
                Assert.That(testType.Properties[i].ToDocumentationProperty(this.entityFramework, testType).Description, Is.EqualTo(resourceProperties[i].Description), "Description for property {0} does not match", testType.Properties[i].Name);
            }
        }

        private void VerifyEntityType(EntityType testType, ResourceDefinition resource)
        {
            this.VerifyComplexType(testType, resource);
            Assert.That($"{this.schema.Namespace}.{testType.Name}", Is.EqualTo(resource.Name), "Resource name does not match");

            // Ignore the odata.type property
            var navigationProperties = resource.Parameters.Where(p => p.IsNavigatable).ToList();

            Assert.That(testType.NavigationProperties.Count, Is.EqualTo(navigationProperties.Count()), "Navigation Property count does not match");

            for (int i = 0; i < navigationProperties.Count; i++)
            {
                Assert.That(testType.NavigationProperties[i].Name, Is.EqualTo(navigationProperties[i].Name), "Name of navigation property does not match");

                // TODO: Fix parsing of Navigation Property types
                // Assert.AreEqual(testType.NavigationProperties[i].Type, navigationProperties[i].Type.ODataResourceName(), "Type of navigation property {0} does not match", testType.NavigationProperties[i].Name);
                Assert.That(testType.NavigationProperties[i].ToDocumentationProperty(this.entityFramework, testType).Description, Is.EqualTo(navigationProperties[i].Description), "Description for property {0} does not match", testType.NavigationProperties[i].Name);
            }
        }
    }
}