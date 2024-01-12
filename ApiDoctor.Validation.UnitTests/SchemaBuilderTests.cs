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
    using System;
    using ApiDoctor.Validation.Json;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class SchemaBuilderTests
    {

        public static JsonSchema SimpleSchemaExample()
        {
            var obj = new { stringProp = "string", urlProp = "url", numberProp = 123, dateProp = "datetime", floatProp = 123.121 };
            string json = JsonConvert.SerializeObject(obj);
            return new JsonSchema(json, null);
        }

        public static JsonSchema FacetSchemaExample()
        {
            string json = "{ \"complexTypeA\": {\"@odata.type\": \"resource.a\", \"name\": \"foobar\" }, \"complexTypeB\": { \"@odata.type\": \"resource.b\"} }";
            return new JsonSchema(json, null);
        }

        public static JsonSchema ArraySchemaExample()
        {
            string json = "{ \"arrayTypeA\": [ {\"@odata.type\": \"resource.a\" } ], \"complexTypeB\": { \"@odata.type\": \"resource.b\"}, \"simpleType\": \"string\" }";
            return new JsonSchema(json, null);
        }

        [Test]
        public void SimpleTypesSchemaTest()
        {
            JsonSchema schema = SimpleSchemaExample();

            Assert.That(5, Is.EqualTo(schema.Properties.Length));

            foreach (var property in schema.Properties)
            {
                switch (property.Name)
                {
                    case "stringProp":
                        Assert.That(property.Type, Is.EqualTo(ParameterDataType.String));
                        Assert.That("string", Is.EqualTo(property.OriginalValue));
                        break;
                    case "urlProp":
                        Assert.That(property.Type, Is.EqualTo(ParameterDataType.String));
                        Assert.That("url", Is.EqualTo(property.OriginalValue));
                        break;
                    case "numberProp":
                        Assert.That(property.Type, Is.EqualTo(ParameterDataType.Int64));
                        Assert.That(123, Is.EqualTo(int.Parse(property.OriginalValue)));
                        break;
                    case "floatProp":
                        Assert.That(property.Type, Is.EqualTo(ParameterDataType.Double));
                        Assert.That(123.121, Is.EqualTo(double.Parse(property.OriginalValue)));
                        break;
                    case "dateProp":
                        Assert.That(property.Type, Is.EqualTo(ParameterDataType.DateTimeOffset));
                        Assert.That("datetime", Is.EqualTo(property.OriginalValue));
                        break;
                    default:
                        Assert.Fail("Unexpected property name: " + property.Name);
                            break;
                }
            }
        }

        [Test]
        public void FacetTypeSchema()
        {
            var schema = FacetSchemaExample();

            Assert.That(2, Is.EqualTo(schema.Properties.Length));
            foreach (var prop in schema.Properties)
            {
                Assert.That(prop.Type.IsObject);
                Assert.That(prop.Type.CustomMembers, Is.Null);
                switch (prop.Name)
                {
                    case "complexTypeA":
                        Assert.That(prop.Type.IsObject);
                        Assert.That("resource.a", Is.EqualTo(prop.Type.CustomTypeName));
                        
                        break;
                    case "complexTypeB":
                        Assert.That(prop.Type.IsObject);
                        Assert.That("resource.b", Is.EqualTo(prop.Type.CustomTypeName));
                        break;
                }
            }
        }


        [Test]
        public void ArrayTypeSchema()
        {
            JsonSchema schema = ArraySchemaExample();

            Assert.That(3, Is.EqualTo(schema.Properties.Length));
            foreach (var prop in schema.Properties)
            {
                switch (prop.Name)
                {
                    case "arrayTypeA":
                        this.CheckJsonProperty(prop, expectedType: new ParameterDataType("resource.a", true), customMembersIsNull: true);
                        break;
                    case "complexTypeB":
                        this.CheckJsonProperty(prop, expectedType: new ParameterDataType("resource.b"), customMembersIsNull: true);
                        break;
                    case "simpleType":
                        this.CheckJsonProperty(prop, expectedType: ParameterDataType.String, customMembersIsNull: true);
                        break;
                    default:
                        Assert.Fail("Unexpected property name: " + prop.Name);
                        break;
                }
            }
        }

        [Test]
        public void InvalidJsonMissingCommaSchema()
        {
            string badJson = "{ \"prop\": \"value\" \"prop2\": \"value\" }";
            Assert.Throws<SchemaBuildException>(() => new JsonSchema(badJson, null));
        }

        [Test]
        public void InvalidJsonMissingBraceSchema()
        {
            string badJson = "{ \"prop\": \"value\", \"prop2\": \"value\" ";
            Assert.Throws<SchemaBuildException>(() => new JsonSchema(badJson, null));
        }


        [Test]
        public void TestStringEnumCollection()
        {
            string json = "{ \"roles\": [\"read|write\"] }";

            JsonSchema schema = new JsonSchema(json, null);
            var property = schema.Properties.Single();

            Assert.That(property.Type.IsCollection);
            Assert.That(property.Type.CollectionResourceType == SimpleDataType.String);
        }

        [Test]
        public void TestStringSpecificFormatDateTime()
        {
            string json = "{ \"createdDateTime\": \"datetime\", \"takenDateTime\": \"timestamp\" }";
            JsonSchema schema = new JsonSchema(json, null);
            foreach (var prop in schema.Properties)
            {
                Assert.That(prop.Type.Type == SimpleDataType.DateTimeOffset);
            }
        }

        public void CheckJsonProperty(ParameterDefinition prop, ParameterDataType expectedType = null, bool? customMembersIsNull = null)
        {
            if (expectedType != null)
                Assert.That(expectedType, Is.EqualTo(prop.Type));
            if (customMembersIsNull != null && customMembersIsNull.Value)
                Assert.That(prop.Type.CustomMembers, Is.Null);
            else if (customMembersIsNull != null && !customMembersIsNull.Value)
                Assert.That(prop.Type.CustomMembers, Is.Not.Null);
                
        }
    }
}
