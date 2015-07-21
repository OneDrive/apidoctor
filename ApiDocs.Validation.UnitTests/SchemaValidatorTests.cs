namespace ApiDocs.Validation.UnitTests
{
    using System;
    using System.Linq;
    using ApiDocs.Validation.Error;
    using ApiDocs.Validation.Http;
    using ApiDocs.Validation.Json;
    using ApiDocs.Validation.UnitTests.Properties;
    using Newtonsoft.Json;
    using NUnit.Framework;

    [TestFixture]

    public class SchemaValidatorTests
    {

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.IsoDateFormat };

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

            string json = JsonConvert.SerializeObject(newObj, Settings);

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

            string json = JsonConvert.SerializeObject(newObj, Settings);

            ValidationError[] errors;
            Assert.IsFalse(schema.ValidateJson(new JsonExample(json), out errors, null, null));
            Assert.IsTrue(errors.All(error => error.Code == ValidationErrorCode.ExpectedTypeDifferent));
            Assert.AreEqual(2, errors.Length, "Expected 2 errors in the response");
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

            string json = JsonConvert.SerializeObject(newObj, Settings);

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

            string json = JsonConvert.SerializeObject(newObj, Settings);

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

            string json = JsonConvert.SerializeObject(newObj, Settings);

            ValidationError[] errors;
            Assert.IsFalse(schema.ValidateJson(new JsonExample(json), out errors, null, null));
            Assert.AreEqual(1, errors.Length);
            Assert.AreEqual(ValidationErrorCode.ExpectedNonArrayValue, errors.First().Code);
        }

        [Test]
        public void TruncatedExampleWithRequiredPropertiesTest()
        {
            DocSet docSet = new DocSet();
            DocFile testFile = new DocFileForTesting(Resources.ExampleValidateResponse, "\test\test.md", "test.md", docSet);

            ValidationError[] detectedErrors;
            testFile.Scan(out detectedErrors);

            Assert.IsEmpty(detectedErrors.Where(x => x.IsError));

            docSet.ResourceCollection.RegisterJsonResource(testFile.Resources.First());

            HttpParser parser = new HttpParser();
            var testMethod = testFile.Requests.First();

            var expectedResponse = parser.ParseHttpResponse(testMethod.ExpectedResponse);
            var actualResponse = parser.ParseHttpResponse(testMethod.ActualResponse);

            docSet.ValidateApiMethod(testMethod, actualResponse, expectedResponse, out detectedErrors, false, null);

            Assert.AreEqual(1, detectedErrors.Length);
            var error = detectedErrors.First();
            Assert.AreEqual(ValidationErrorCode.RequiredPropertiesMissing, error.Code);
        }

        [Test]
        public void TruncatedExampleSelectStatementOnChildren()
        {
            DocSet docSet = new DocSet();
            DocFile testFile = new DocFileForTesting(Resources.ExampleValidationSelectStatement, "\test\test.md", "test.md", docSet);

            ValidationError[] detectedErrors;
            testFile.Scan(out detectedErrors);

            Assert.IsEmpty(detectedErrors.Where(x => x.IsError));

            docSet.ResourceCollection.RegisterJsonResource(testFile.Resources.First());

            HttpParser parser = new HttpParser();
            var testMethod = testFile.Requests.First();

            var expectedResponse = parser.ParseHttpResponse(testMethod.ExpectedResponse);
            var actualResponse = parser.ParseHttpResponse(testMethod.ActualResponse);

            docSet.ValidateApiMethod(testMethod, actualResponse, expectedResponse, out detectedErrors, false, null);

            Assert.AreEqual(0, detectedErrors.Length);
        }

        [Test]
        public void TruncatedExampleSelectStatementOnChildrenExpectFailure()
        {
            DocSet docSet = new DocSet();
            DocFile testFile = new DocFileForTesting(Resources.ExampleValidationSelectStatementFailure, "\test\test.md", "test.md", docSet);

            ValidationError[] detectedErrors;
            testFile.Scan(out detectedErrors);

            Assert.IsEmpty(detectedErrors.Where(x => x.IsError));

            docSet.ResourceCollection.RegisterJsonResource(testFile.Resources.First());

            HttpParser parser = new HttpParser();
            var testMethod = testFile.Requests.First();

            var expectedResponse = parser.ParseHttpResponse(testMethod.ExpectedResponse);
            var actualResponse = parser.ParseHttpResponse(testMethod.ActualResponse);

            docSet.ValidateApiMethod(testMethod, actualResponse, expectedResponse, out detectedErrors, false, null);

            Assert.AreEqual(4, detectedErrors.Length);
            foreach (var error in detectedErrors)
            {
                Assert.AreEqual(error.Code, ValidationErrorCode.RequiredPropertiesMissing);
            }
        }

    }
}
