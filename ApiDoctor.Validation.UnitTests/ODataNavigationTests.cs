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
    using System.Linq;
    using ApiDoctor.Validation.Error;
    using ApiDoctor.Validation.OData;
    using NUnit.Framework;

    [TestFixture]
    public class ODataNavigationTests
    {
        #region Helpers

        private static EntityFramework BuildEdmx(string ns, List<EntityType> entityTypes = null, List<ComplexType> complexTypes = null, List<EntityContainer> containers = null)
        {
            var schema = new Schema
            {
                Namespace = ns,
                EntityTypes = entityTypes ?? new List<EntityType>(),
                ComplexTypes = complexTypes ?? new List<ComplexType>(),
                EntityContainers = containers ?? new List<EntityContainer>(),
                Functions = new List<Function>(),
                Actions = new List<OData.Action>(),
                Terms = new List<Term>(),
                Annotations = new List<Annotations>(),
                Enumerations = new List<EnumType>()
            };
            return new EntityFramework(new[] { schema });
        }

        private static EntityType MakeEntity(string name, string ns, List<Property> props = null, List<NavigationProperty> navProps = null, string baseType = null) =>
            new EntityType
            {
                Name = name,
                Namespace = ns,
                BaseType = baseType,
                Properties = props ?? new List<Property>(),
                NavigationProperties = navProps ?? new List<NavigationProperty>()
            };

        private static ComplexType MakeComplex(string name, string ns, List<Property> props = null, string baseType = null) =>
            new ComplexType
            {
                Name = name,
                Namespace = ns,
                BaseType = baseType,
                Properties = props ?? new List<Property>()
            };

        private static NavigationProperty NavProp(string name, string type) =>
            new NavigationProperty { Name = name, Type = type };

        private static Property Prop(string name, string type) =>
            new Property { Name = name, Type = type };

        #endregion

        #region EntityType.NavigateByUriComponent — navigation properties

        [Test]
        public void EntityType_Navigate_CollectionNavProperty_ReturnsODataCollection()
        {
            var ns = "nav.test";
            var message = MakeEntity("NavCollMessage", ns, navProps: new List<NavigationProperty>
            {
                NavProp("attachments", $"Collection({ns}.Attachment)")
            });
            var attachment = MakeEntity("Attachment", ns, props: new List<Property> { Prop("id", "Edm.String") });
            var edmx = BuildEdmx(ns, entityTypes: new List<EntityType> { message, attachment });

            var issues = new IssueLogger();
            var result = message.NavigateByUriComponent("attachments", edmx, issues, isLastSegment: false);

            Assert.That(result, Is.InstanceOf<ODataCollection>());
            Assert.That(((ODataCollection)result).TypeIdentifier, Is.EqualTo($"{ns}.Attachment"));
            Assert.That(issues.Issues.Any(i => i.IsError), Is.False);

            // Also verify the collection's element type is resolvable in the edmx — catches
            // cases where the identifier string is correct but the type isn't registered.
            var element = ((ODataCollection)result).NavigateByEntityTypeKey(edmx, issues);
            Assert.That(element, Is.InstanceOf<EntityType>());
            Assert.That(((EntityType)element).Name, Is.EqualTo("Attachment"));
        }

        [Test]
        public void EntityType_Navigate_ScalarNavProperty_ReturnsEntityType()
        {
            var ns = "nav.scalar";
            var manager = MakeEntity("NavScalarManager", ns, props: new List<Property> { Prop("id", "Edm.String") });
            var user = MakeEntity("NavScalarUser", ns,
                props: new List<Property> { Prop("id", "Edm.String") },
                navProps: new List<NavigationProperty> { NavProp("manager", $"{ns}.NavScalarManager") });
            var edmx = BuildEdmx(ns, entityTypes: new List<EntityType> { user, manager });

            var issues = new IssueLogger();
            var result = user.NavigateByUriComponent("manager", edmx, issues, isLastSegment: false);

            Assert.That(result, Is.InstanceOf<EntityType>());
            Assert.That(((EntityType)result).Name, Is.EqualTo("NavScalarManager"));
        }

        [Test]
        public void EntityType_Navigate_UnknownComponent_ReturnsNull()
        {
            var ns = "nav.unknown";
            var user = MakeEntity("NavUnknownUser", ns);
            var edmx = BuildEdmx(ns, entityTypes: new List<EntityType> { user });

            var issues = new IssueLogger();
            var result = user.NavigateByUriComponent("nonexistent", edmx, issues, isLastSegment: false);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void EntityType_Navigate_CaseMismatchNavProperty_LogsTypeMismatchError()
        {
            var ns = "nav.casemismatch";
            var user = MakeEntity("NavCaseMismatchUser", ns,
                navProps: new List<NavigationProperty> { NavProp("Messages", $"Collection({ns}.NavCaseMismatchMessage)") });
            var message = MakeEntity("NavCaseMismatchMessage", ns);
            var edmx = BuildEdmx(ns, entityTypes: new List<EntityType> { user, message });

            var issues = new IssueLogger();
            // Navigate with wrong case
            user.NavigateByUriComponent("messages", edmx, issues, isLastSegment: false);

            Assert.That(issues.Issues.Any(i => i.Code == ValidationErrorCode.TypeNameMismatch), Is.True);
        }

        [Test]
        public void EntityType_Navigate_NavPropertyInBaseType_ResolvedThroughInheritance()
        {
            var ns = "nav.basenav";
            var baseEntity = MakeEntity("NavBaseItem", ns,
                navProps: new List<NavigationProperty> { NavProp("owner", $"{ns}.NavBaseUser") });
            var derivedEntity = MakeEntity("NavDerivedItem", ns, baseType: $"{ns}.NavBaseItem");
            var user = MakeEntity("NavBaseUser", ns, props: new List<Property> { Prop("id", "Edm.String") });
            var edmx = BuildEdmx(ns, entityTypes: new List<EntityType> { baseEntity, derivedEntity, user });

            var issues = new IssueLogger();
            var result = derivedEntity.NavigateByUriComponent("owner", edmx, issues, isLastSegment: false);

            Assert.That(result, Is.InstanceOf<EntityType>());
            Assert.That(((EntityType)result).Name, Is.EqualTo("NavBaseUser"));
        }

        [Test]
        public void EntityType_Navigate_PropertyOnSelf_ReturnsResolvedType()
        {
            var ns = "nav.selfprop";
            var user = MakeEntity("NavSelfPropUser", ns,
                props: new List<Property> { Prop("displayName", "Edm.String") });
            var edmx = BuildEdmx(ns, entityTypes: new List<EntityType> { user });

            var issues = new IssueLogger();
            var result = user.NavigateByUriComponent("displayName", edmx, issues, isLastSegment: false);

            Assert.That(result, Is.InstanceOf<ODataSimpleType>());
        }

        #endregion

        #region ComplexType.NavigateByUriComponent

        [Test]
        public void ComplexType_Navigate_MatchingProperty_ReturnsResolvedType()
        {
            var ns = "ct.navigate";
            var address = MakeComplex("CtNavAddress", ns,
                props: new List<Property> { Prop("city", "Edm.String") });
            var edmx = BuildEdmx(ns, complexTypes: new List<ComplexType> { address });

            var issues = new IssueLogger();
            var result = address.NavigateByUriComponent("city", edmx, issues, isLastSegment: false);

            Assert.That(result, Is.InstanceOf<ODataSimpleType>());
        }

        [Test]
        public void ComplexType_Navigate_UnknownComponent_ReturnsNull()
        {
            var ns = "ct.unknown";
            var address = MakeComplex("CtUnknownAddress", ns,
                props: new List<Property> { Prop("city", "Edm.String") });
            var edmx = BuildEdmx(ns, complexTypes: new List<ComplexType> { address });

            var issues = new IssueLogger();
            var result = address.NavigateByUriComponent("nonexistent", edmx, issues, isLastSegment: false);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ComplexType_Navigate_CaseMismatchProperty_LogsTypeMismatchError()
        {
            var ns = "ct.casemismatch";
            var address = MakeComplex("CtCaseMismatchAddress", ns,
                props: new List<Property> { Prop("City", "Edm.String") });
            var edmx = BuildEdmx(ns, complexTypes: new List<ComplexType> { address });

            var issues = new IssueLogger();
            address.NavigateByUriComponent("city", edmx, issues, isLastSegment: false);

            Assert.That(issues.Issues.Any(i => i.Code == ValidationErrorCode.TypeNameMismatch), Is.True);
        }

        [Test]
        public void ComplexType_Navigate_PropertyInBaseType_ResolvedThroughInheritance()
        {
            var ns = "ct.baseprop";
            var baseComplex = MakeComplex("CtBasePropBase", ns,
                props: new List<Property> { Prop("baseField", "Edm.String") });
            var derivedComplex = MakeComplex("CtBasePropDerived", ns,
                baseType: $"{ns}.CtBasePropBase");
            var edmx = BuildEdmx(ns, complexTypes: new List<ComplexType> { baseComplex, derivedComplex });

            var issues = new IssueLogger();
            var result = derivedComplex.NavigateByUriComponent("baseField", edmx, issues, isLastSegment: false);

            Assert.That(result, Is.InstanceOf<ODataSimpleType>());
        }

        #endregion

        #region EntityContainer.NavigateByUriComponent

        [Test]
        public void EntityContainer_Navigate_EntitySet_ReturnsODataCollection()
        {
            var ns = "ec.entityset";
            var user = MakeEntity("EcUser", ns, props: new List<Property> { Prop("id", "Edm.String") });
            var container = new EntityContainer
            {
                Name = "DefaultContainer",
                EntitySets = new List<EntitySet> { new EntitySet { Name = "users", EntityType = $"{ns}.EcUser" } },
                Singletons = new List<Singleton>()
            };
            var edmx = BuildEdmx(ns, entityTypes: new List<EntityType> { user }, containers: new List<EntityContainer> { container });

            var issues = new IssueLogger();
            var result = container.NavigateByUriComponent("users", edmx, issues, isLastSegment: false);

            Assert.That(result, Is.InstanceOf<ODataCollection>());
            Assert.That(((ODataCollection)result).TypeIdentifier, Is.EqualTo($"{ns}.EcUser"));
        }

        [Test]
        public void EntityContainer_Navigate_Singleton_ReturnsEntityType()
        {
            var ns = "ec.singleton";
            var me = MakeEntity("EcMeUser", ns, props: new List<Property> { Prop("id", "Edm.String") });
            var container = new EntityContainer
            {
                Name = "DefaultContainer",
                EntitySets = new List<EntitySet>(),
                Singletons = new List<Singleton> { new Singleton { Name = "me", Type = $"{ns}.EcMeUser" } }
            };
            var edmx = BuildEdmx(ns, entityTypes: new List<EntityType> { me }, containers: new List<EntityContainer> { container });

            var issues = new IssueLogger();
            var result = container.NavigateByUriComponent("me", edmx, issues, isLastSegment: false);

            Assert.That(result, Is.InstanceOf<EntityType>());
            Assert.That(((EntityType)result).Name, Is.EqualTo("EcMeUser"));
        }

        [Test]
        public void EntityContainer_Navigate_UnknownComponent_ReturnsNull()
        {
            var container = new EntityContainer
            {
                Name = "DefaultContainer",
                EntitySets = new List<EntitySet>(),
                Singletons = new List<Singleton>()
            };
            var edmx = BuildEdmx("ec.unknown", containers: new List<EntityContainer> { container });

            var issues = new IssueLogger();
            var result = container.NavigateByUriComponent("nonexistent", edmx, issues, isLastSegment: false);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void EntityContainer_NavigateByEntityTypeKey_ThrowsNotSupportedException()
        {
            var container = new EntityContainer { Name = "DefaultContainer" };
            var edmx = BuildEdmx("ec.key");
            var issues = new IssueLogger();

            Assert.That(() => container.NavigateByEntityTypeKey(edmx, issues),
                Throws.TypeOf<System.NotSupportedException>());
        }

        #endregion

        #region ODataCollection.NavigateByUriComponent

        [Test]
        public void ODataCollection_Navigate_GuidKey_LogsWarningAndNavigatesToEntityType()
        {
            var ns = "col.guid";
            var user = MakeEntity("ColGuidUser", ns, props: new List<Property> { Prop("id", "Edm.String") });
            var edmx = BuildEdmx(ns, entityTypes: new List<EntityType> { user });
            var collection = new ODataCollection($"{ns}.ColGuidUser");

            var issues = new IssueLogger();
            var result = collection.NavigateByUriComponent(
                "9F328426-8A81-40D1-8F35-D619AA90A12C", edmx, issues, isLastSegment: false);

            Assert.That(issues.Issues.Any(i => i.Code == ValidationErrorCode.AmbiguousExample), Is.True);
            Assert.That(result, Is.InstanceOf<EntityType>());
        }

        [Test]
        public void ODataCollection_Navigate_LongKey_LogsWarningAndNavigatesToEntityType()
        {
            var ns = "col.long";
            var item = MakeEntity("ColLongItem", ns, props: new List<Property> { Prop("id", "Edm.Int64") });
            var edmx = BuildEdmx(ns, entityTypes: new List<EntityType> { item });
            var collection = new ODataCollection($"{ns}.ColLongItem");

            var issues = new IssueLogger();
            var result = collection.NavigateByUriComponent("1234567890", edmx, issues, isLastSegment: false);

            Assert.That(issues.Issues.Any(i => i.Code == ValidationErrorCode.AmbiguousExample), Is.True);
            Assert.That(result, Is.InstanceOf<EntityType>());
        }

        [Test]
        public void ODataCollection_Navigate_TypeCastSegment_ReturnsNewCollectionOfDerivedType()
        {
            var ns = "col.typecast";
            var baseEntity = MakeEntity("ColBaseEntity", ns);
            var derivedEntity = MakeEntity("ColDerivedEntity", ns, baseType: $"{ns}.ColBaseEntity");
            var edmx = BuildEdmx(ns, entityTypes: new List<EntityType> { baseEntity, derivedEntity });
            var collection = new ODataCollection($"{ns}.ColBaseEntity");

            var issues = new IssueLogger();
            var result = collection.NavigateByUriComponent($"{ns}.ColDerivedEntity", edmx, issues, isLastSegment: false);

            Assert.That(result, Is.InstanceOf<ODataCollection>());
            Assert.That(((ODataCollection)result).TypeIdentifier, Is.EqualTo($"{ns}.ColDerivedEntity"));
        }

        [Test]
        public void ODataCollection_Navigate_UnknownSegment_ReturnsNull()
        {
            var ns = "col.unknown";
            var entity = MakeEntity("ColUnknownEntity", ns);
            var edmx = BuildEdmx(ns, entityTypes: new List<EntityType> { entity });
            var collection = new ODataCollection($"{ns}.ColUnknownEntity");

            var issues = new IssueLogger();
            var result = collection.NavigateByUriComponent("unknownSegment", edmx, issues, isLastSegment: false);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ODataCollection_NavigateByEntityTypeKey_ReturnsElementType()
        {
            var ns = "col.key";
            var user = MakeEntity("ColKeyUser", ns, props: new List<Property> { Prop("id", "Edm.String") });
            var edmx = BuildEdmx(ns, entityTypes: new List<EntityType> { user });
            var collection = new ODataCollection($"{ns}.ColKeyUser");

            var issues = new IssueLogger();
            var result = collection.NavigateByEntityTypeKey(edmx, issues);

            Assert.That(result, Is.InstanceOf<EntityType>());
            Assert.That(((EntityType)result).Name, Is.EqualTo("ColKeyUser"));
        }

        #endregion
    }
}
