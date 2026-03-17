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

namespace ApiDoctor.Validation.UnitTests
{
    using System.Collections.Generic;
    using ApiDoctor.Validation.OData;
    using NUnit.Framework;

    [TestFixture]
    public class ODataExtensionMethodTests
    {
        #region Helpers

        private static EntityFramework BuildEdmx(string ns, List<EntityType> entityTypes = null, List<ComplexType> complexTypes = null)
        {
            var schema = new Schema
            {
                Namespace = ns,
                EntityTypes = entityTypes ?? new List<EntityType>(),
                ComplexTypes = complexTypes ?? new List<ComplexType>(),
                EntityContainers = new List<EntityContainer>(),
                Functions = new List<Function>(),
                Actions = new List<OData.Action>(),
                Terms = new List<Term>(),
                Annotations = new List<Annotations>(),
                Enumerations = new List<EnumType>()
            };
            return new EntityFramework(new[] { schema });
        }

        #endregion

        #region NamespaceOnly

        [Test]
        public void NamespaceOnly_QualifiedType_ReturnsNamespace()
        {
            Assert.That("microsoft.graph.User".NamespaceOnly(), Is.EqualTo("microsoft.graph"));
        }

        [Test]
        public void NamespaceOnly_SingleSegmentNamespace_ReturnsNamespace()
        {
            Assert.That("graph.User".NamespaceOnly(), Is.EqualTo("graph"));
        }

        [Test]
        public void NamespaceOnly_CollectionType_ReturnsNamespaceOfElementType()
        {
            Assert.That("Collection(microsoft.graph.User)".NamespaceOnly(), Is.EqualTo("microsoft.graph"));
        }

        [Test]
        public void NamespaceOnly_NoNamespace_ThrowsInvalidOperationException()
        {
            Assert.That(() => "User".NamespaceOnly(), Throws.InvalidOperationException);
        }

        #endregion

        #region TypeOnly

        [Test]
        public void TypeOnly_QualifiedType_ReturnsTypeName()
        {
            Assert.That("microsoft.graph.User".TypeOnly(), Is.EqualTo("User"));
        }

        [Test]
        public void TypeOnly_CollectionType_ReturnsElementTypeName()
        {
            Assert.That("Collection(microsoft.graph.User)".TypeOnly(), Is.EqualTo("User"));
        }

        [Test]
        public void TypeOnly_UnqualifiedType_ReturnsTypeAsIs()
        {
            Assert.That("User".TypeOnly(), Is.EqualTo("User"));
        }

        #endregion

        #region HasNamespace

        [Test]
        public void HasNamespace_QualifiedType_ReturnsTrue()
        {
            Assert.That("microsoft.graph.User".HasNamespace(), Is.True);
        }

        [Test]
        public void HasNamespace_UnqualifiedType_ReturnsFalse()
        {
            Assert.That("User".HasNamespace(), Is.False);
        }

        #endregion

        #region IsCollection

        [Test]
        public void IsCollection_CollectionString_ReturnsTrue()
        {
            Assert.That("Collection(microsoft.graph.User)".IsCollection(), Is.True);
        }

        [Test]
        public void IsCollection_SimpleEdmType_ReturnsFalse()
        {
            Assert.That("Edm.String".IsCollection(), Is.False);
        }

        [Test]
        public void IsCollection_QualifiedType_ReturnsFalse()
        {
            Assert.That("microsoft.graph.User".IsCollection(), Is.False);
        }

        #endregion

        #region ElementName

        [Test]
        public void ElementName_CollectionType_ReturnsElementType()
        {
            Assert.That("Collection(microsoft.graph.User)".ElementName(), Is.EqualTo("microsoft.graph.User"));
        }

        [Test]
        public void ElementName_NonCollectionType_ReturnsInputUnchanged()
        {
            Assert.That("microsoft.graph.User".ElementName(), Is.EqualTo("microsoft.graph.User"));
        }

        [Test]
        public void ElementName_EdmCollection_ReturnsEdmType()
        {
            Assert.That("Collection(Edm.String)".ElementName(), Is.EqualTo("Edm.String"));
        }

        #endregion

        #region LookupNavigableType (via EntityFramework)

        [Test]
        public void LookupNavigableType_KnownEntityType_ReturnsEntityType()
        {
            var ns = "ext.lookup";
            var user = new EntityType
            {
                Name = "LookupUser",
                Namespace = ns,
                Properties = new List<Property>(),
                NavigationProperties = new List<NavigationProperty>()
            };
            var edmx = BuildEdmx(ns, entityTypes: new List<EntityType> { user });

            var result = edmx.LookupNavigableType($"{ns}.LookupUser");

            Assert.That(result, Is.InstanceOf<EntityType>());
            Assert.That(((EntityType)result).Name, Is.EqualTo("LookupUser"));
        }

        [Test]
        public void LookupNavigableType_KnownComplexType_ReturnsComplexType()
        {
            var ns = "ext.lookup.complex";
            var address = new ComplexType
            {
                Name = "LookupAddress",
                Namespace = ns,
                Properties = new List<Property>()
            };
            var edmx = BuildEdmx(ns, complexTypes: new List<ComplexType> { address });

            var result = edmx.LookupNavigableType($"{ns}.LookupAddress");

            Assert.That(result, Is.InstanceOf<ComplexType>());
        }

        [Test]
        public void LookupNavigableType_EdmPrimitiveType_ReturnsODataSimpleType()
        {
            var edmx = BuildEdmx("ext.primitive");

            var result = edmx.LookupNavigableType("Edm.String");

            Assert.That(result, Is.InstanceOf<ODataSimpleType>());
        }

        [Test]
        public void LookupNavigableType_UnknownType_ThrowsInvalidOperationException()
        {
            var edmx = BuildEdmx("ext.unknown");

            Assert.That(() => edmx.LookupNavigableType("ext.unknown.DoesNotExist"),
                Throws.InvalidOperationException);
        }

        #endregion

        #region ResourceWithIdentifier (via EntityFramework)

        [Test]
        public void ResourceWithIdentifier_EntityType_ReturnsEntityType()
        {
            var ns = "ext.rwi";
            var user = new EntityType
            {
                Name = "RwiUser",
                Namespace = ns,
                Properties = new List<Property>(),
                NavigationProperties = new List<NavigationProperty>()
            };
            var edmx = BuildEdmx(ns, entityTypes: new List<EntityType> { user });

            var result = edmx.ResourceWithIdentifier<IODataNavigable>($"{ns}.RwiUser");

            Assert.That(result, Is.InstanceOf<EntityType>());
        }

        [Test]
        public void ResourceWithIdentifier_EdmString_ReturnsODataSimpleType()
        {
            var edmx = BuildEdmx("ext.rwi.prim");

            var result = edmx.ResourceWithIdentifier<IODataNavigable>("Edm.String");

            Assert.That(result, Is.InstanceOf<ODataSimpleType>());
            Assert.That(((ODataSimpleType)result).Type, Is.EqualTo(SimpleDataType.String));
        }

        [Test]
        public void ResourceWithIdentifier_CollectionOfEntityType_ReturnsODataCollection()
        {
            var ns = "ext.rwi.col";
            var user = new EntityType
            {
                Name = "RwiColUser",
                Namespace = ns,
                Properties = new List<Property>(),
                NavigationProperties = new List<NavigationProperty>()
            };
            var edmx = BuildEdmx(ns, entityTypes: new List<EntityType> { user });

            var result = edmx.ResourceWithIdentifier<IODataNavigable>($"Collection({ns}.RwiColUser)");

            Assert.That(result, Is.InstanceOf<ODataCollection>());
            Assert.That(((ODataCollection)result).TypeIdentifier, Is.EqualTo($"{ns}.RwiColUser"));
        }

        [Test]
        public void ResourceWithIdentifier_UnknownType_ThrowsKeyNotFoundException()
        {
            var edmx = BuildEdmx("ext.rwi.unknown");

            Assert.That(() => edmx.ResourceWithIdentifier<IODataNavigable>("ext.rwi.unknown.Ghost"),
                Throws.TypeOf<System.Collections.Generic.KeyNotFoundException>());
        }

        #endregion
    }
}
