namespace ApiDocs.Validation.UnitTests
{
    using System.Collections.Generic;
    using System.Linq;
    using ApiDocs.Validation.Error;
    using ApiDocs.Validation.Json;
    using Newtonsoft.Json;
    using NUnit.Framework;

    [TestFixture]
    public class NullableTests
    {
        [Test]
        public void NullPropertyShouldBeOk()
        {
            var nullableSchema = this.SchemaForNullTest(expectNulls: true);

            var exampleObj = new
            {
                prop1 = (string)null,
                prop2 = 12345
            };


            string json = JsonConvert.SerializeObject(exampleObj);
            var testExample = new JsonExample(json);
            ValidationError[] errors;

            Assert.IsTrue(nullableSchema.ValidateJson(testExample, out errors, new Dictionary<string,JsonSchema>(), new ValidationOptions()));
            Assert.AreEqual(0, errors.Length);
        }

        [Test]
        public void NullPropertyShouldGenerateWarning()
        {
            var nullableSchema = this.SchemaForNullTest(expectNulls: false);

            var exampleObj = new
            {
                prop1 = (string)null,
                prop2 = 12345
            };


            string json = JsonConvert.SerializeObject(exampleObj);
            var testExample = new JsonExample(json);
            ValidationError[] errors;

            Assert.IsFalse(nullableSchema.ValidateJson(testExample, out errors, new Dictionary<string, JsonSchema>(), new ValidationOptions()));
            Assert.AreEqual(1, errors.Length);

            var error = errors.First();
            Assert.AreEqual(ValidationErrorCode.NullPropertyValue, error.Code);
        }

        public JsonSchema SchemaForNullTest(bool expectNulls)
        {
            var schemaObject = new
            {
                prop1 = "string",
                prop2 = 12345
            };

            string json = JsonConvert.SerializeObject(schemaObject);

            var annotation = new CodeBlockAnnotation {
                NullableProperties = expectNulls ? new string[] { "prop1", "prop2" } : new string[0]
            };

            return new JsonSchema(json, annotation);
        }

    }
}
