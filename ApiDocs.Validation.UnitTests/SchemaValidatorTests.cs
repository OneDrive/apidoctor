/*
 * Markdown Scanner
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

            // There are two errors here, the first is that stringProp is a number, and the
            // the second is that numberProp is a string. MDS needs to catch both.
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
            DocFile testFile = new DocFileForTesting(Resources.ExampleValidateResponse, "test.md", "test.md", docSet);

            ValidationError[] detectedErrors;
            testFile.Scan(string.Empty, out detectedErrors);

            Assert.IsEmpty(detectedErrors.Where(x => x.IsError));

            docSet.ResourceCollection.RegisterJsonResource(testFile.Resources.First());

            HttpParser parser = new HttpParser();
            var testMethod = testFile.Requests.First();

            var expectedResponse = parser.ParseHttpResponse(testMethod.ExpectedResponse);
            var actualResponse = parser.ParseHttpResponse(testMethod.ActualResponse);

            testMethod.ValidateResponse(actualResponse, expectedResponse, null, out detectedErrors);

            Assert.AreEqual(1, detectedErrors.Length);
            var error = detectedErrors.First();
            Assert.AreEqual(ValidationErrorCode.RequiredPropertiesMissing, error.Code);
        }

        [Test]
        public void TruncatedExampleSelectStatementOnChildren()
        {
            DocSet docSet = new DocSet();
            DocFile testFile = new DocFileForTesting(Resources.ExampleValidationSelectStatement, "test.md", "test.md", docSet);

            ValidationError[] detectedErrors;
            testFile.Scan(string.Empty, out detectedErrors);

            Assert.IsEmpty(detectedErrors.Where(x => x.IsError));

            docSet.ResourceCollection.RegisterJsonResource(testFile.Resources.First());

            HttpParser parser = new HttpParser();
            var testMethod = testFile.Requests.First();

            var expectedResponse = parser.ParseHttpResponse(testMethod.ExpectedResponse);
            var actualResponse = parser.ParseHttpResponse(testMethod.ActualResponse);

            testMethod.ValidateResponse(actualResponse, expectedResponse, null, out detectedErrors);

            Assert.AreEqual(0, detectedErrors.Length);
        }

        [Test]
        public void TruncatedExampleSelectStatementOnChildrenExpectFailure()
        {
            DocSet docSet = new DocSet();
            DocFile testFile = new DocFileForTesting(Resources.ExampleValidationSelectStatementFailure, "test.md", "test.md", docSet);

            ValidationError[] detectedErrors;
            testFile.Scan(string.Empty, out detectedErrors);

            Assert.IsEmpty(detectedErrors.Where(x => x.IsError));

            docSet.ResourceCollection.RegisterJsonResource(testFile.Resources.First());

            HttpParser parser = new HttpParser();
            var testMethod = testFile.Requests.First();

            var expectedResponse = parser.ParseHttpResponse(testMethod.ExpectedResponse);
            var actualResponse = parser.ParseHttpResponse(testMethod.ActualResponse);

            testMethod.ValidateResponse(actualResponse, expectedResponse, null, out detectedErrors);

            Assert.AreEqual(2, detectedErrors.Length);
            Assert.IsTrue(detectedErrors.Any(x => x.Code == ValidationErrorCode.RequiredPropertiesMissing), "Error with Code = RequiredPropertiesMissing");
            Assert.IsTrue(
                detectedErrors.Any(x => x.Code == ValidationErrorCode.SkippedSimilarErrors),
                "Error with Code = SkippedSimilarErrors");
        }

    }
}
