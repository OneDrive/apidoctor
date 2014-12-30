using Newtonsoft.Json;
using NUnit.Framework;
using OneDrive.ApiDocumentation.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.UnitTests.ApiDocumentation.Validation
{
    [TestFixture]

    public class SchemaValidatorTests
    {

        [Test]
        public void SimpleTypeValidationValid()
        {
            var schema = SchemaBuilderTests.SimpleSchemaExample();

            var newObj = new
            {
                stringProp = "seattle, wa",
                urlProp = "http://onedrive.com",
                numberProp = 1234567901,
                dateProp = DateTime.UtcNow,
                floatProp = 123123.1231231231
            };

            string json = JsonConvert.SerializeObject(newObj);

            ValidationError[] errors;
            Assert.IsTrue(schema.ValidateJson(json, out errors, null));
            Assert.AreEqual(0, errors.Length);
        }

        [Test]
        public void SimpleTypeValidationWrongPropertyType()
        {
            var schema = SchemaBuilderTests.SimpleSchemaExample();

            var newObj = new
            {
                stringProp = 1231234,
                urlProp = "http://onedrive.com",
                numberProp = "1234567901",
                dateProp = DateTime.UtcNow,
                floatProp = 123123.1231231231
            };

            string json = JsonConvert.SerializeObject(newObj);

            ValidationError[] errors;
            Assert.IsFalse(schema.ValidateJson(json, out errors, null));
            Assert.AreEqual(2, errors.Length);
            Assert.IsTrue(errors.All(error => error.Code == ValidationErrorCode.ExpectedTypeDifferent));
        }

        [Test]
        public void SimpleTypeValidationExtraProperty()
        {
            var schema = SchemaBuilderTests.SimpleSchemaExample();

            var newObj = new
            {
                stringProp = "seattle, wa",
                foobar = "another value",
                urlProp = "http://onedrive.com",
                numberProp = 1234567901,
                dateProp = DateTime.UtcNow,
                floatProp = 123123.1231231231
            };

            string json = JsonConvert.SerializeObject(newObj);

            ValidationError[] errors;
            Assert.IsFalse(schema.ValidateJson(json, out errors, null));
            Assert.AreEqual(1, errors.Length);
            Assert.AreEqual(ValidationErrorCode.AdditionalPropertyDetected, errors.First().Code);
        }

        [Test]
        public void SimpleTypeValidationMissingProperty()
        {
            var schema = SchemaBuilderTests.SimpleSchemaExample();

            var newObj = new
            {
                stringProp = "seattle, wa",
                numberProp = 1234567901,
                dateProp = DateTime.UtcNow,
                floatProp = 123123.1231231231
            };

            string json = JsonConvert.SerializeObject(newObj);

            ValidationError[] errors;
            Assert.IsFalse(schema.ValidateJson(json, out errors, null));
            Assert.AreEqual(1, errors.Length);
            Assert.AreEqual(ValidationErrorCode.RequiredPropertiesMissing, errors.First().Code);
        }

        [Test]
        public void SimpleTypeValidationWrongTypeArray()
        {
            var schema = SchemaBuilderTests.SimpleSchemaExample();

            var newObj = new
            {
                stringProp = new string[] {"one", "two", "three"},
                urlProp = "http://onedrive.com",
                numberProp = 1234567901,
                dateProp = DateTime.UtcNow,
                floatProp = 123123.1231231231
            };

            string json = JsonConvert.SerializeObject(newObj);

            ValidationError[] errors;
            Assert.IsFalse(schema.ValidateJson(json, out errors, null));
            Assert.AreEqual(1, errors.Length);
            Assert.AreEqual(ValidationErrorCode.ExpectedNonArrayValue, errors.First().Code);
        }

    }
}
