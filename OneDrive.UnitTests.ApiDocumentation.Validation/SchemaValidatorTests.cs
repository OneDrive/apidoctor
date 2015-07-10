using Newtonsoft.Json;
using NUnit.Framework;
using OneDrive.ApiDocumentation.Validation;
using OneDrive.ApiDocumentation.Validation.Http;
using OneDrive.ApiDocumentation.Validation.Json;
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

        private static JsonSerializerSettings settings = new JsonSerializerSettings { DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat };

        [Test]
        public void SimpleTypeValidationValid()
        {
            var schema = SchemaBuilderTests.SimpleSchemaExample();

            var newObj = new
            {
                stringProp = "seattle, wa",
                urlProp = "http://example.org",
                numberProp = 1234567901,
                dateProp = DateTime.UtcNow,
                floatProp = 123123.1231231231
            };

            string json = JsonConvert.SerializeObject(newObj, settings);

            ValidationError[] errors;
            Assert.IsTrue(schema.ValidateJson(new JsonExample(json), out errors, null, null));
            Assert.AreEqual(0, errors.Length);
        }

        [Test]
        public void SimpleTypeValidationWrongPropertyType()
        {
            var schema = SchemaBuilderTests.SimpleSchemaExample();

            var newObj = new
            {
                stringProp = 1231234,
                urlProp = "http://example.org",
                numberProp = "1234567901",
                dateProp = DateTime.UtcNow,
                floatProp = 123123.1231231231
            };

            string json = JsonConvert.SerializeObject(newObj, settings);

            ValidationError[] errors;
            Assert.IsFalse(schema.ValidateJson(new JsonExample(json), out errors, null, null));
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
                urlProp = "http://example.org",
                numberProp = 1234567901,
                dateProp = DateTime.UtcNow,
                floatProp = 123123.1231231231
            };

            string json = JsonConvert.SerializeObject(newObj, settings);

            ValidationError[] errors;
            Assert.IsFalse(schema.ValidateJson(new JsonExample(json), out errors, null, null));
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

            string json = JsonConvert.SerializeObject(newObj, settings);

            ValidationError[] errors;
            Assert.IsFalse(schema.ValidateJson(new JsonExample(json), out errors, null, null));
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
                urlProp = "http://example.org",
                numberProp = 1234567901,
                dateProp = DateTime.UtcNow,
                floatProp = 123123.1231231231
            };

            string json = JsonConvert.SerializeObject(newObj, settings);

            ValidationError[] errors;
            Assert.IsFalse(schema.ValidateJson(new JsonExample(json), out errors, null, null));
            Assert.AreEqual(1, errors.Length);
            Assert.AreEqual(ValidationErrorCode.ExpectedNonArrayValue, errors.First().Code);
        }

        [Test]
        public void TruncatedExampleWithRequiredPropertiesTest()
        {
            DocSet docSet = new DocSet();
            DocFile testFile = new DocFileForTesting(Properties.Resources.ExampleValidateResponse, "\test\test.md", "test.md", docSet);

            ValidationError[] detectedErrors;
            testFile.Scan(out detectedErrors);

            Assert.IsEmpty(detectedErrors.Where(x => x.IsError));

            docSet.ResourceCollection.RegisterJsonResource(testFile.Resources.First());

            HttpParser parser = new HttpParser();
            var testMethod = testFile.Requests.First();

            var expectedResponse = parser.ParseHttpResponse(testMethod.ExpectedResponse);
            var actualResponse = parser.ParseHttpResponse(testMethod.ActualResponse);

            docSet.ValidateApiMethod(testMethod, actualResponse, expectedResponse, out detectedErrors, false);

            Assert.AreEqual(1, detectedErrors.Length);
            var error = detectedErrors.First();
            Assert.AreEqual(ValidationErrorCode.RequiredPropertiesMissing, error.Code);
        }

        [Test]
        public void TruncatedExampleSelectStatementOnChildren()
        {
            DocSet docSet = new DocSet();
            DocFile testFile = new DocFileForTesting(Properties.Resources.ExampleValidationSelectStatement, "\test\test.md", "test.md", docSet);

            ValidationError[] detectedErrors;
            testFile.Scan(out detectedErrors);

            Assert.IsEmpty(detectedErrors.Where(x => x.IsError));

            docSet.ResourceCollection.RegisterJsonResource(testFile.Resources.First());

            HttpParser parser = new HttpParser();
            var testMethod = testFile.Requests.First();

            var expectedResponse = parser.ParseHttpResponse(testMethod.ExpectedResponse);
            var actualResponse = parser.ParseHttpResponse(testMethod.ActualResponse);

            docSet.ValidateApiMethod(testMethod, actualResponse, expectedResponse, out detectedErrors, false);

            Assert.AreEqual(0, detectedErrors.Length);
        }

        [Test]
        public void TruncatedExampleSelectStatementOnChildrenExpectFailure()
        {
            DocSet docSet = new DocSet();
            DocFile testFile = new DocFileForTesting(Properties.Resources.ExampleValidationSelectStatementFailure, "\test\test.md", "test.md", docSet);

            ValidationError[] detectedErrors;
            testFile.Scan(out detectedErrors);

            Assert.IsEmpty(detectedErrors.Where(x => x.IsError));

            docSet.ResourceCollection.RegisterJsonResource(testFile.Resources.First());

            HttpParser parser = new HttpParser();
            var testMethod = testFile.Requests.First();

            var expectedResponse = parser.ParseHttpResponse(testMethod.ExpectedResponse);
            var actualResponse = parser.ParseHttpResponse(testMethod.ActualResponse);

            docSet.ValidateApiMethod(testMethod, actualResponse, expectedResponse, out detectedErrors, false);

            Assert.AreEqual(4, detectedErrors.Length);
            foreach (var error in detectedErrors)
            {
                Assert.AreEqual(error.Code, ValidationErrorCode.RequiredPropertiesMissing);
            }
        }

    }
}
