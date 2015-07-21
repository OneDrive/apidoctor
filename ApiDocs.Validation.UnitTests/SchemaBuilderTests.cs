namespace ApiDocs.Validation.UnitTests
{
    using System;
    using ApiDocs.Validation.Json;
    using Newtonsoft.Json;
    using NUnit.Framework;

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

            Assert.AreEqual(5, schema.Properties.Length);

            foreach (var property in schema.Properties)
            {
                switch (property.Name)
                {
                    case "stringProp":
                        Assert.AreEqual(property.Type, JsonDataType.String);
                        Assert.AreEqual("string", property.OriginalValue);
                        break;
                    case "urlProp":
                        Assert.AreEqual(property.Type, JsonDataType.String);
                        Assert.AreEqual("url", property.OriginalValue);
                        break;
                    case "numberProp":
                        Assert.AreEqual(property.Type, JsonDataType.Number);
                        Assert.AreEqual(123, Int32.Parse(property.OriginalValue));
                        break;
                    case "floatProp":
                        Assert.AreEqual(property.Type, JsonDataType.Number);
                        Assert.AreEqual(123.121, Double.Parse(property.OriginalValue));
                        break;
                    case "dateProp":
                        Assert.AreEqual(property.Type, JsonDataType.String);
                        Assert.AreEqual("datetime", property.OriginalValue);
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

            Assert.AreEqual(2, schema.Properties.Length);
            foreach (var prop in schema.Properties)
            {
                Assert.AreEqual(JsonDataType.ODataType, prop.Type);
                Assert.IsNull(prop.CustomMembers);
                switch (prop.Name)
                {
                    case "complexTypeA":
                        Assert.AreEqual("resource.a", prop.ODataTypeName);
                        
                        break;
                    case "complexTypeB":
                        Assert.AreEqual("resource.b", prop.ODataTypeName);
                        break;
                }
            }
        }


        [Test]
        public void ArrayTypeSchema()
        {
            JsonSchema schema = ArraySchemaExample();

            Assert.AreEqual(3, schema.Properties.Length);
            foreach (var prop in schema.Properties)
            {
                switch (prop.Name)
                {
                    case "arrayTypeA":
                        this.CheckJsonProperty(prop, expectedType: JsonDataType.ODataType, odataTypeName: "resource.a", isArray: true, customMembersIsNull: true);
                        break;
                    case "complexTypeB":
                        this.CheckJsonProperty(prop, expectedType: JsonDataType.ODataType, odataTypeName: "resource.b", isArray: false, customMembersIsNull: true);
                        break;
                    case "simpleType":
                        this.CheckJsonProperty(prop, expectedType: JsonDataType.String, isArray: false, customMembersIsNull: true);
                        break;
                    default:
                        Assert.Fail("Unexpected property name: " + prop.Name);
                        break;
                }
            }
        }

        [Test]
        [ExpectedException(ExpectedException=typeof(SchemaBuildException))]
        public void InvalidJsonMissingCommaSchema()
        {
            string badJson = "{ \"prop\": \"value\" \"prop2\": \"value\" }";
            new JsonSchema(badJson, null);
        }

        [Test]
        [ExpectedException(ExpectedException = typeof(SchemaBuildException))]
        public void InvalidJsonMissingBraceSchema()
        {
            string badJson = "{ \"prop\": \"value\", \"prop2\": \"value\" ";
            new JsonSchema(badJson, null);
        }


        public void CheckJsonProperty(JsonProperty prop, JsonDataType? expectedType = null, string odataTypeName = null, bool? isArray = null, bool? customMembersIsNull = null)
        {
            if (expectedType != null)
                Assert.AreEqual(expectedType.Value, prop.Type);
            if (odataTypeName != null)
                Assert.AreEqual(odataTypeName, prop.ODataTypeName);
            if (isArray != null)
                Assert.AreEqual(isArray.Value, prop.IsArray);
            if (customMembersIsNull != null && customMembersIsNull.Value)
                Assert.IsNull(prop.CustomMembers);
            else if (customMembersIsNull != null && !customMembersIsNull.Value)
                Assert.IsNotNull(prop.CustomMembers);
                
        }
    }
}
