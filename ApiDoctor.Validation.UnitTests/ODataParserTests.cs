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
    using ApiDoctor.Validation.Config;
    using ApiDoctor.Validation.Error;
    using ApiDoctor.Validation.OData;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class ODataParserTests
    {
        #region Helpers

        private const string EdmxOpen = @"<?xml version=""1.0"" encoding=""utf-8""?>
<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>";

        private const string EdmxClose = @"
  </edmx:DataServices>
</edmx:Edmx>";

        private static string WrapSchema(string schemaBody, string ns = "microsoft.graph") =>
            EdmxOpen + $@"
    <Schema Namespace=""{ns}"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"" xmlns:ags=""http://aggregator.microsoft.com/internal"">
{schemaBody}
    </Schema>" + EdmxClose;

        #endregion

        #region DeserializeEntityFramework — basic structure

        [Test]
        public void Deserialize_SimpleEntityType_ParsesNameAndProperties()
        {
            var xml = WrapSchema(@"
      <EntityType Name=""Message"">
        <Key><PropertyRef Name=""id""/></Key>
        <Property Name=""id"" Type=""Edm.String"" Nullable=""false""/>
        <Property Name=""subject"" Type=""Edm.String""/>
      </EntityType>");

            var ef = ODataParser.DeserializeEntityFramework(xml);

            var schema = ef.DataServices.Schemas.Single();
            Assert.That(schema.Namespace, Is.EqualTo("microsoft.graph"));

            var entity = schema.EntityTypes.Single();
            Assert.That(entity.Name, Is.EqualTo("Message"));
            Assert.That(entity.Properties.Select(p => p.Name), Is.EquivalentTo(new[] { "id", "subject" }));
        }

        [Test]
        public void Deserialize_ComplexType_ParsesNameAndProperties()
        {
            var xml = WrapSchema(@"
      <ComplexType Name=""EmailAddress"">
        <Property Name=""name"" Type=""Edm.String""/>
        <Property Name=""address"" Type=""Edm.String""/>
      </ComplexType>");

            var ef = ODataParser.DeserializeEntityFramework(xml);

            var ct = ef.DataServices.Schemas.Single().ComplexTypes.Single();
            Assert.That(ct.Name, Is.EqualTo("EmailAddress"));
            Assert.That(ct.Properties.Count, Is.EqualTo(2));
        }

        [Test]
        public void Deserialize_MultipleSchemas_ParsesAll()
        {
            var xml = EdmxOpen + @"
    <Schema Namespace=""microsoft.graph"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <EntityType Name=""User"">
        <Key><PropertyRef Name=""id""/></Key>
        <Property Name=""id"" Type=""Edm.String""/>
      </EntityType>
    </Schema>
    <Schema Namespace=""microsoft.graph.callRecords"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <ComplexType Name=""Session"">
        <Property Name=""id"" Type=""Edm.String""/>
      </ComplexType>
    </Schema>" + EdmxClose;

            var ef = ODataParser.DeserializeEntityFramework(xml);

            Assert.That(ef.DataServices.Schemas.Count, Is.EqualTo(2));
            Assert.That(ef.DataServices.Schemas.Select(s => s.Namespace),
                Is.EquivalentTo(new[] { "microsoft.graph", "microsoft.graph.callRecords" }));
        }

        [Test]
        public void Deserialize_Property_NullableFlag_ParsedCorrectly()
        {
            var xml = WrapSchema(@"
      <ComplexType Name=""NullableTest"">
        <Property Name=""required"" Type=""Edm.String"" Nullable=""false""/>
        <Property Name=""optional"" Type=""Edm.String"" Nullable=""true""/>
      </ComplexType>");

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var props = ef.DataServices.Schemas.Single().ComplexTypes.Single().Properties;

            var required = props.First(p => p.Name == "required");
            var optional = props.First(p => p.Name == "optional");

            Assert.That(required.Nullable, Is.False);
            Assert.That(optional.Nullable, Is.True);
        }

        #endregion

        #region DeserializeEntityFramework — ags: custom attributes

        [Test]
        public void Deserialize_EntityType_AgsIsMaster_ParsedCorrectly()
        {
            var xml = WrapSchema(@"
      <EntityType Name=""AgsIsMasterEntity"" ags:IsMaster=""true"">
        <Key><PropertyRef Name=""id""/></Key>
        <Property Name=""id"" Type=""Edm.String""/>
      </EntityType>");

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var entity = ef.DataServices.Schemas.Single().EntityTypes.Single();

            Assert.That(entity.GraphIsMaster, Is.True);
        }

        [Test]
        public void Deserialize_EntityType_AgsIsMaster_DefaultsFalse()
        {
            var xml = WrapSchema(@"
      <EntityType Name=""AgsIsMasterDefaultEntity"">
        <Key><PropertyRef Name=""id""/></Key>
        <Property Name=""id"" Type=""Edm.String""/>
      </EntityType>");

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var entity = ef.DataServices.Schemas.Single().EntityTypes.Single();

            Assert.That(entity.GraphIsMaster, Is.False);
        }

        [Test]
        public void Deserialize_EntityType_AgsAddressUrl_ParsedCorrectly()
        {
            var xml = WrapSchema(@"
      <EntityType Name=""AgsAddressUrlEntity""
          ags:AddressUrl=""https://graph.microsoft.com/v1.0/me""
          ags:AddressUrlMSA=""https://graph.microsoft.com/v1.0/msa/me"">
        <Key><PropertyRef Name=""id""/></Key>
        <Property Name=""id"" Type=""Edm.String""/>
      </EntityType>");

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var entity = ef.DataServices.Schemas.Single().EntityTypes.Single();

            Assert.That(entity.GraphAddressUrl, Is.EqualTo("https://graph.microsoft.com/v1.0/me"));
            Assert.That(entity.GraphAddressUrlMsa, Is.EqualTo("https://graph.microsoft.com/v1.0/msa/me"));
        }

        [Test]
        public void Deserialize_EntityType_AgsAddressContainsEntitySetSegment_ParsedCorrectly()
        {
            var xml = WrapSchema(@"
      <EntityType Name=""AgsEntitySetSegmentEntity"" ags:AddressContainsEntitySetSegment=""true"">
        <Key><PropertyRef Name=""id""/></Key>
        <Property Name=""id"" Type=""Edm.String""/>
      </EntityType>");

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var entity = ef.DataServices.Schemas.Single().EntityTypes.Single();

            Assert.That(entity.GraphAddressContainsEntitySetSegment, Is.True);
        }

        [Test]
        public void Deserialize_EntityType_AgsAddressContainsEntitySetSegment_AbsentIsNull()
        {
            var xml = WrapSchema(@"
      <EntityType Name=""AgsEntitySetSegmentAbsentEntity"">
        <Key><PropertyRef Name=""id""/></Key>
        <Property Name=""id"" Type=""Edm.String""/>
      </EntityType>");

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var entity = ef.DataServices.Schemas.Single().EntityTypes.Single();

            Assert.That(entity.GraphAddressContainsEntitySetSegment, Is.Null);
        }

        [Test]
        public void Deserialize_EntityType_AgsInstantOnUrl_ParsedCorrectly()
        {
            var xml = WrapSchema(@"
      <EntityType Name=""AgsInstantOnEntity"" ags:InstantOnUrl=""https://graph.microsoft.com/v1.0/instantOn"">
        <Key><PropertyRef Name=""id""/></Key>
        <Property Name=""id"" Type=""Edm.String""/>
      </EntityType>");

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var entity = ef.DataServices.Schemas.Single().EntityTypes.Single();

            Assert.That(entity.GraphInstantOnUrl, Is.EqualTo("https://graph.microsoft.com/v1.0/instantOn"));
        }

        [Test]
        public void Deserialize_ComplexType_AgsWorkloadName_ParsedCorrectly()
        {
            var xml = WrapSchema(@"
      <ComplexType Name=""RenamedAddress"" ags:WorkloadName=""OriginalAddress"">
        <Property Name=""street"" Type=""Edm.String""/>
      </ComplexType>");

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var ct = ef.DataServices.Schemas.Single().ComplexTypes.Single();

            Assert.That(ct.WorkloadName, Is.EqualTo("OriginalAddress"));
        }

        [Test]
        public void Deserialize_Property_AgsVirtualNavigation_AllAttributesParsed()
        {
            var xml = WrapSchema(@"
      <ComplexType Name=""VirtualNavComplexType"">
        <Property Name=""managerId"" Type=""Edm.String""
            ags:CreateVirtualNavigationProperty=""true""
            ags:VirtualNavigationPropertyName=""manager""
            ags:TargetEntityType=""microsoft.graph.User""
            ags:KeyPropertyPath=""id""/>
      </ComplexType>");

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var prop = ef.DataServices.Schemas.Single().ComplexTypes.Single().Properties.Single();

            Assert.That(prop.CreateVirtualNavigationProperty, Is.True);
            Assert.That(prop.VirtualNavigationPropertyName, Is.EqualTo("manager"));
            Assert.That(prop.TargetEntityType, Is.EqualTo("microsoft.graph.User"));
            Assert.That(prop.KeyPropertyPath, Is.EqualTo("id"));
        }

        [Test]
        public void Deserialize_Property_AgsCreateVirtualNavigation_AbsentIsNull()
        {
            var xml = WrapSchema(@"
      <ComplexType Name=""NoVirtualNavComplexType"">
        <Property Name=""simpleId"" Type=""Edm.String""/>
      </ComplexType>");

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var prop = ef.DataServices.Schemas.Single().ComplexTypes.Single().Properties.Single();

            Assert.That(prop.CreateVirtualNavigationProperty, Is.Null);
        }

        [Test]
        public void Deserialize_Property_AgsWorkloadName_ParsedCorrectly()
        {
            var xml = WrapSchema(@"
      <ComplexType Name=""WorkloadNamePropComplexType"">
        <Property Name=""renamedProp"" Type=""Edm.String"" ags:WorkloadName=""originalProp""/>
      </ComplexType>");

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var prop = ef.DataServices.Schemas.Single().ComplexTypes.Single().Properties.Single();

            Assert.That(prop.WorkloadName, Is.EqualTo("originalProp"));
        }

        #endregion

        #region DeserializeEntityFramework — inheritance merging

        [Test]
        public void Deserialize_EntityType_InheritsBaseTypeProperties()
        {
            var xml = WrapSchema(@"
      <EntityType Name=""BaseItem"">
        <Key><PropertyRef Name=""id""/></Key>
        <Property Name=""id"" Type=""Edm.String""/>
        <Property Name=""createdDateTime"" Type=""Edm.DateTimeOffset""/>
      </EntityType>
      <EntityType Name=""DriveItem"" BaseType=""microsoft.graph.BaseItem"">
        <Property Name=""name"" Type=""Edm.String""/>
      </EntityType>");

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var schema = ef.DataServices.Schemas.Single();
            var driveItem = schema.EntityTypes.Single(e => e.Name == "DriveItem");

            var propertyNames = driveItem.Properties.Select(p => p.Name).ToList();
            Assert.That(propertyNames, Does.Contain("id"));
            Assert.That(propertyNames, Does.Contain("createdDateTime"));
            Assert.That(propertyNames, Does.Contain("name"));
        }

        [Test]
        public void Deserialize_EntityType_InheritedPropertiesPrependedBeforeOwn()
        {
            var xml = WrapSchema(@"
      <EntityType Name=""BaseItemOrder"">
        <Key><PropertyRef Name=""id""/></Key>
        <Property Name=""id"" Type=""Edm.String""/>
      </EntityType>
      <EntityType Name=""ChildItemOrder"" BaseType=""microsoft.graph.BaseItemOrder"">
        <Property Name=""childProp"" Type=""Edm.String""/>
      </EntityType>");

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var child = ef.DataServices.Schemas.Single().EntityTypes.Single(e => e.Name == "ChildItemOrder");

            Assert.That(child.Properties.First().Name, Is.EqualTo("id"),
                "Base type properties should be prepended before child's own properties.");
            Assert.That(child.Properties.Last().Name, Is.EqualTo("childProp"));
        }

        [Test]
        public void Deserialize_ComplexType_MultiLevelInheritance_AllPropertiesMerged()
        {
            var xml = WrapSchema(@"
      <ComplexType Name=""Level1"">
        <Property Name=""level1Prop"" Type=""Edm.String""/>
      </ComplexType>
      <ComplexType Name=""Level2"" BaseType=""microsoft.graph.Level1"">
        <Property Name=""level2Prop"" Type=""Edm.String""/>
      </ComplexType>
      <ComplexType Name=""Level3"" BaseType=""microsoft.graph.Level2"">
        <Property Name=""level3Prop"" Type=""Edm.String""/>
      </ComplexType>");

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var level3 = ef.DataServices.Schemas.Single().ComplexTypes.Single(ct => ct.Name == "Level3");

            var names = level3.Properties.Select(p => p.Name).ToList();
            Assert.That(names, Does.Contain("level1Prop"));
            Assert.That(names, Does.Contain("level2Prop"));
            Assert.That(names, Does.Contain("level3Prop"));
        }

        [Test]
        public void Deserialize_EntityType_NoBaseType_PropertiesUnchanged()
        {
            var xml = WrapSchema(@"
      <EntityType Name=""StandaloneEntity"">
        <Key><PropertyRef Name=""id""/></Key>
        <Property Name=""id"" Type=""Edm.String""/>
        <Property Name=""value"" Type=""Edm.Int32""/>
      </EntityType>");

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var entity = ef.DataServices.Schemas.Single().EntityTypes.Single();

            Assert.That(entity.Properties.Count, Is.EqualTo(2));
        }

        #endregion

        #region BuildJsonExample

        [Test]
        public void BuildJsonExample_PrimitiveProperties_ProduceExpectedExampleValues()
        {
            var ct = new ComplexType
            {
                Name = "PrimitivesExample",
                Namespace = "test.primitives",
                Properties = new List<Property>
                {
                    new Property { Name = "strProp", Type = "Edm.String" },
                    new Property { Name = "intProp", Type = "Edm.Int32" },
                    new Property { Name = "boolProp", Type = "Edm.Boolean" },
                    new Property { Name = "guidProp", Type = "Edm.Guid" },
                }
            };

            var json = ODataParser.BuildJsonExample(ct, Enumerable.Empty<Schema>());
            var obj = JObject.Parse(json);

            Assert.That(obj["strProp"]?.Type, Is.EqualTo(JTokenType.String));
            Assert.That(obj["intProp"]?.Type, Is.EqualTo(JTokenType.Integer));
            Assert.That(obj["boolProp"]?.Type, Is.EqualTo(JTokenType.Boolean));
            Assert.That(obj["guidProp"]?.Type, Is.EqualTo(JTokenType.String));
        }

        [Test]
        public void BuildJsonExample_StreamProperty_IsExcluded()
        {
            var ct = new ComplexType
            {
                Name = "StreamExcludeType",
                Namespace = "test.stream",
                Properties = new List<Property>
                {
                    new Property { Name = "id", Type = "Edm.String" },
                    new Property { Name = "content", Type = "Edm.Stream" },
                }
            };

            var json = ODataParser.BuildJsonExample(ct, Enumerable.Empty<Schema>());
            var obj = JObject.Parse(json);

            Assert.That(obj.ContainsKey("id"), Is.True);
            Assert.That(obj.ContainsKey("content"), Is.False);
        }

        [Test]
        public void BuildJsonExample_CollectionProperty_ProducesTwoElementArray()
        {
            var ct = new ComplexType
            {
                Name = "CollectionExampleType",
                Namespace = "test.collection",
                Properties = new List<Property>
                {
                    new Property { Name = "tags", Type = "Collection(Edm.String)" },
                }
            };

            var json = ODataParser.BuildJsonExample(ct, Enumerable.Empty<Schema>());
            var obj = JObject.Parse(json);

            Assert.That(obj["tags"]?.Type, Is.EqualTo(JTokenType.Array));
            Assert.That(obj["tags"]!.Count(), Is.EqualTo(2));
        }

        [Test]
        public void BuildJsonExample_ComplexTypeProperty_ProducesNestedObject()
        {
            var schemas = new List<Schema>
            {
                new Schema
                {
                    Namespace = "test.nested",
                    ComplexTypes = new List<ComplexType>
                    {
                        new ComplexType
                        {
                            Name = "Address",
                            Namespace = "test.nested",
                            Properties = new List<Property>
                            {
                                new Property { Name = "street", Type = "Edm.String" },
                            }
                        }
                    },
                    EntityTypes = new List<EntityType>()
                }
            };

            var ct = new ComplexType
            {
                Name = "PersonWithAddress",
                Namespace = "test.nested",
                Properties = new List<Property>
                {
                    new Property { Name = "homeAddress", Type = "test.nested.Address" },
                }
            };

            var json = ODataParser.BuildJsonExample(ct, schemas);
            var obj = JObject.Parse(json);

            Assert.That(obj["homeAddress"]?.Type, Is.EqualTo(JTokenType.Object));
            Assert.That(obj["homeAddress"]!["street"]?.Type, Is.EqualTo(JTokenType.String));
        }

        [Test]
        public void BuildJsonExample_WithNamespace_IncludesOdataTypeProperty()
        {
            var ct = new ComplexType
            {
                Name = "OdataTypeEntity",
                Namespace = "test.odatatype",
                Properties = new List<Property>
                {
                    new Property { Name = "id", Type = "Edm.String" },
                }
            };

            var json = ODataParser.BuildJsonExample(ct, Enumerable.Empty<Schema>());
            var obj = JObject.Parse(json);

            Assert.That(obj.ContainsKey("@odata.type"), Is.True);
        }

        #endregion

        #region GenerateResourcesFromSchemas

        [Test]
        public void GenerateResourcesFromSchemas_ProducesOneResourcePerType()
        {
            var schemas = new List<Schema>
            {
                new Schema
                {
                    Namespace = "test.generate",
                    EntityTypes = new List<EntityType>
                    {
                        new EntityType
                        {
                            Name = "GenerateEntity",
                            Namespace = "test.generate",
                            Properties = new List<Property> { new Property { Name = "id", Type = "Edm.String" } },
                            NavigationProperties = new List<NavigationProperty>()
                        }
                    },
                    ComplexTypes = new List<ComplexType>
                    {
                        new ComplexType
                        {
                            Name = "GenerateComplex",
                            Namespace = "test.generate",
                            Properties = new List<Property> { new Property { Name = "value", Type = "Edm.String" } }
                        }
                    }
                }
            };

            var issues = new IssueLogger();
            var resources = ODataParser.GenerateResourcesFromSchemas(schemas, issues);

            Assert.That(resources.Count, Is.EqualTo(2));
        }

        [Test]
        public void GenerateResourcesFromSchemas_WithValidateNamespace_ResourceTypeIncludesNamespace()
        {
            var schemas = new List<Schema>
            {
                new Schema
                {
                    Namespace = "microsoft.graph",
                    EntityTypes = new List<EntityType>(),
                    ComplexTypes = new List<ComplexType>
                    {
                        new ComplexType
                        {
                            Name = "NsValidateType",
                            Namespace = "microsoft.graph",
                            Properties = new List<Property> { new Property { Name = "id", Type = "Edm.String" } }
                        }
                    }
                }
            };

            var issues = new IssueLogger();
            var configs = new MetadataValidationConfigs
            {
                ModelConfigs = new ModelConfigs { ValidateNamespace = true }
            };

            var resources = ODataParser.GenerateResourcesFromSchemas(schemas, issues, configs);
            var resource = resources.Single();

            Assert.That(resource.Name, Is.EqualTo("microsoft.graph.NsValidateType"));
        }

        [Test]
        public void GenerateResourcesFromSchemas_WithAliasNamespace_ResourceTypeUsesAlias()
        {
            var schemas = new List<Schema>
            {
                new Schema
                {
                    Namespace = "microsoft.graph",
                    EntityTypes = new List<EntityType>(),
                    ComplexTypes = new List<ComplexType>
                    {
                        new ComplexType
                        {
                            Name = "AliasNsType",
                            Namespace = "microsoft.graph",
                            Properties = new List<Property> { new Property { Name = "id", Type = "Edm.String" } }
                        }
                    }
                }
            };

            var issues = new IssueLogger();
            var configs = new MetadataValidationConfigs
            {
                ModelConfigs = new ModelConfigs
                {
                    ValidateNamespace = true,
                    AliasNamespace = "graph"
                }
            };

            var resources = ODataParser.GenerateResourcesFromSchemas(schemas, issues, configs);
            var resource = resources.Single();

            Assert.That(resource.Name, Is.EqualTo("graph.AliasNsType"));
        }

        [Test]
        public void GenerateResourcesFromSchemas_WithoutValidateNamespace_ResourceTypeIsNameOnly()
        {
            var schemas = new List<Schema>
            {
                new Schema
                {
                    Namespace = "microsoft.graph",
                    EntityTypes = new List<EntityType>(),
                    ComplexTypes = new List<ComplexType>
                    {
                        new ComplexType
                        {
                            Name = "NoNsType",
                            Namespace = "microsoft.graph",
                            Properties = new List<Property> { new Property { Name = "id", Type = "Edm.String" } }
                        }
                    }
                }
            };

            var issues = new IssueLogger();
            var resources = ODataParser.GenerateResourcesFromSchemas(schemas, issues);
            var resource = resources.Single();

            Assert.That(resource.Name, Is.EqualTo("NoNsType"));
        }

        [Test]
        public void GenerateResourcesFromSchemas_MultipleSchemas_ProducesResourcesFromAll()
        {
            var schemas = new List<Schema>
            {
                new Schema
                {
                    Namespace = "ns.one",
                    EntityTypes = new List<EntityType>
                    {
                        new EntityType
                        {
                            Name = "MultiSchemaEntity1",
                            Namespace = "ns.one",
                            Properties = new List<Property> { new Property { Name = "id", Type = "Edm.String" } },
                            NavigationProperties = new List<NavigationProperty>()
                        }
                    },
                    ComplexTypes = new List<ComplexType>()
                },
                new Schema
                {
                    Namespace = "ns.two",
                    EntityTypes = new List<EntityType>
                    {
                        new EntityType
                        {
                            Name = "MultiSchemaEntity2",
                            Namespace = "ns.two",
                            Properties = new List<Property> { new Property { Name = "id", Type = "Edm.String" } },
                            NavigationProperties = new List<NavigationProperty>()
                        }
                    },
                    ComplexTypes = new List<ComplexType>()
                }
            };

            var issues = new IssueLogger();
            var resources = ODataParser.GenerateResourcesFromSchemas(schemas, issues);

            Assert.That(resources.Count, Is.EqualTo(2));
        }

        #endregion

        #region Serialize round-trip

        [Test]
        public void SerializeDeserialize_RoundTrip_PreservesAgsAttributes()
        {
            var xml = WrapSchema(@"
      <EntityType Name=""RoundTripEntity""
          ags:IsMaster=""true""
          ags:AddressUrl=""https://graph.microsoft.com/v1.0/me"">
        <Key><PropertyRef Name=""id""/></Key>
        <Property Name=""id"" Type=""Edm.String""
            ags:WorkloadName=""originalId""
            ags:CreateVirtualNavigationProperty=""true""
            ags:VirtualNavigationPropertyName=""manager""
            ags:TargetEntityType=""microsoft.graph.User""
            ags:KeyPropertyPath=""id""/>
      </EntityType>");

            var ef = ODataParser.DeserializeEntityFramework(xml);
            var serialized = ODataParser.Serialize(ef);
            var ef2 = ODataParser.DeserializeEntityFramework(serialized);

            var entity = ef2.DataServices.Schemas.Single().EntityTypes.Single();
            Assert.That(entity.GraphIsMaster, Is.True);
            Assert.That(entity.GraphAddressUrl, Is.EqualTo("https://graph.microsoft.com/v1.0/me"));

            var prop = entity.Properties.Single();
            Assert.That(prop.WorkloadName, Is.EqualTo("originalId"));
            Assert.That(prop.CreateVirtualNavigationProperty, Is.True);
            Assert.That(prop.VirtualNavigationPropertyName, Is.EqualTo("manager"));
            Assert.That(prop.TargetEntityType, Is.EqualTo("microsoft.graph.User"));
            Assert.That(prop.KeyPropertyPath, Is.EqualTo("id"));
        }

        #endregion
    }
}
