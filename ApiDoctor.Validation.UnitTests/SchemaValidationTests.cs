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
    using System.IO;
    using ApiDoctor.Validation.OData;
    using NUnit.Framework;

    /// <summary>
    /// Tests for SchemaValidation.ValidateSchemaTypes, which walks the entity graph
    /// verifying that all ContainsType-attributed properties reference resolvable types.
    /// </summary>
    [TestFixture]
    public class SchemaValidationTests
    {
        #region Helpers

        private static Schema MakeSchema(string ns,
            List<EntityType> entityTypes = null,
            List<ComplexType> complexTypes = null) =>
            new Schema
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

        private static EntityType MakeEntityType(string name, List<Property> props = null) =>
            new EntityType
            {
                Name = name,
                Properties = props ?? new List<Property>(),
                NavigationProperties = new List<NavigationProperty>()
            };

        private static ComplexType MakeComplexType(string name, List<Property> props = null) =>
            new ComplexType
            {
                Name = name,
                Properties = props ?? new List<Property>()
            };

        private static Property Prop(string name, string type) =>
            new Property { Name = name, Type = type };

        #endregion

        [Test]
        public void ValidateSchemaTypes_EmptySchema_DoesNotThrow()
        {
            var edmx = new EntityFramework(new[] { MakeSchema("test") });
            Assert.DoesNotThrow(() => edmx.ValidateSchemaTypes());
        }

        [Test]
        public void ValidateSchemaTypes_EdmPrimitiveTypesOnly_DoesNotThrow()
        {
            var schema = MakeSchema("test", entityTypes: new List<EntityType>
            {
                MakeEntityType("User", new List<Property>
                {
                    Prop("id", "Edm.String"),
                    Prop("count", "Edm.Int32"),
                    Prop("active", "Edm.Boolean")
                })
            });
            var edmx = new EntityFramework(new[] { schema });

            Assert.DoesNotThrow(() => edmx.ValidateSchemaTypes());
        }

        [Test]
        public void ValidateSchemaTypes_PropertyRefersToKnownEntityType_DoesNotThrow()
        {
            const string ns = "sv.knownentity";
            var schema = MakeSchema(ns,
                entityTypes: new List<EntityType>
                {
                    MakeEntityType("User", new List<Property>
                    {
                        Prop("id", "Edm.String"),
                        Prop("address", $"{ns}.Address")
                    }),
                    MakeEntityType("Address", new List<Property>
                    {
                        Prop("city", "Edm.String")
                    })
                });
            var edmx = new EntityFramework(new[] { schema });

            Assert.DoesNotThrow(() => edmx.ValidateSchemaTypes());
        }

        [Test]
        public void ValidateSchemaTypes_PropertyRefersToKnownComplexType_DoesNotThrow()
        {
            const string ns = "sv.knowncomplex";
            var schema = MakeSchema(ns,
                entityTypes: new List<EntityType>
                {
                    MakeEntityType("User", new List<Property>
                    {
                        Prop("location", $"{ns}.GeoCoords")
                    })
                },
                complexTypes: new List<ComplexType>
                {
                    MakeComplexType("GeoCoords", new List<Property>
                    {
                        Prop("lat", "Edm.Double"),
                        Prop("lon", "Edm.Double")
                    })
                });
            var edmx = new EntityFramework(new[] { schema });

            Assert.DoesNotThrow(() => edmx.ValidateSchemaTypes());
        }

        [Test]
        public void ValidateSchemaTypes_UnknownCustomType_DoesNotThrowButWritesToConsole()
        {
            const string ns = "sv.unknown";
            var schema = MakeSchema(ns,
                entityTypes: new List<EntityType>
                {
                    MakeEntityType("User", new List<Property>
                    {
                        Prop("address", $"{ns}.NonExistentType")
                    })
                });
            var edmx = new EntityFramework(new[] { schema });

            // Capture Console output to confirm error is reported
            var captured = new StringWriter();
            var original = System.Console.Out;
            System.Console.SetOut(captured);
            try
            {
                Assert.DoesNotThrow(() => edmx.ValidateSchemaTypes());
            }
            finally
            {
                System.Console.SetOut(original);
            }

            Assert.That(captured.ToString(), Does.Contain("NonExistentType"));
        }

        [Test]
        public void ValidateSchemaTypes_MultipleSchemas_ResolvesTypeAcrossSchemas()
        {
            const string ns1 = "sv.multi1";
            const string ns2 = "sv.multi2";
            var schema1 = MakeSchema(ns1,
                entityTypes: new List<EntityType>
                {
                    MakeEntityType("User", new List<Property>
                    {
                        Prop("org", $"{ns2}.Org")
                    })
                });
            var schema2 = MakeSchema(ns2,
                entityTypes: new List<EntityType>
                {
                    MakeEntityType("Org", new List<Property> { Prop("id", "Edm.String") })
                });
            var edmx = new EntityFramework(new[] { schema1, schema2 });

            Assert.DoesNotThrow(() => edmx.ValidateSchemaTypes());
        }

        [Test]
        public void ValidateSchemaTypes_CollectionType_DoesNotThrow()
        {
            const string ns = "sv.collection";
            var schema = MakeSchema(ns,
                entityTypes: new List<EntityType>
                {
                    MakeEntityType("User", new List<Property>
                    {
                        Prop("emails", $"Collection(Edm.String)")
                    })
                });
            var edmx = new EntityFramework(new[] { schema });

            Assert.DoesNotThrow(() => edmx.ValidateSchemaTypes());
        }
    }
}
