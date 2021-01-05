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
    using System.Linq;
    using ApiDoctor.Validation.Error;
    using ApiDoctor.Validation.Http;
    using ApiDoctor.Validation.Json;
    using ApiDoctor.Validation.UnitTests.Properties;
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

            var issues = new IssueLogger();
            Assert.IsTrue(schema.ValidateJson(new JsonExample(json), issues, null, null));
            Assert.AreEqual(0, issues.Issues.Count());
        }

        [Test]
        public void SimpleTypeValidationWrongPropertyType()
        {
            var schema = SchemaBuilderTests.SimpleSchemaExample();

            // There are two errors here, the first is that stringProp is a number, and the
            // the second is that numberProp is a string. API Doctor needs to catch both.
            var newObj = new
            {
                stringProp = 1231234,
                urlProp = "http://example.org",
                numberProp = "1234567901",
                dateProp = DateTime.UtcNow,
                floatProp = 123123.1231231231
            };

            string json = JsonConvert.SerializeObject(newObj, Settings);

            var issues = new IssueLogger();
            Assert.IsFalse(schema.ValidateJson(new JsonExample(json), issues, null, null));
            Assert.IsTrue(issues.Issues.All(error => error.Code == ValidationErrorCode.ExpectedTypeDifferent));
            Assert.AreEqual(2, issues.Issues.Count(), "Expected 2 errors in the response");
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

            var issues = new IssueLogger();
            Assert.IsFalse(schema.ValidateJson(new JsonExample(json), issues, null, null));
            Assert.AreEqual(1, issues.Issues.Count());
            Assert.AreEqual(ValidationErrorCode.AdditionalPropertyDetected, issues.Issues.First().Code);
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

            var issues = new IssueLogger();
            Assert.IsFalse(schema.ValidateJson(new JsonExample(json), issues, null, null));
            Assert.AreEqual(1, issues.Issues.Count());
            Assert.AreEqual(ValidationErrorCode.RequiredPropertiesMissing, issues.Issues.First().Code);
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

            var issues = new IssueLogger();
            Assert.IsFalse(schema.ValidateJson(new JsonExample(json), issues, null, null));
            Assert.AreEqual(1, issues.Issues.Count());
            Assert.AreEqual(ValidationErrorCode.ExpectedNonArrayValue, issues.Issues.First().Code);
        }

        [Test]
        public void TruncatedExampleWithRequiredPropertiesTest()
        {
            DocSet docSet = new DocSet();
            DocFile testFile = new DocFileForTesting(Resources.ExampleValidateResponse, "test.md", "test.md", docSet);

            var issues = new IssueLogger();
            testFile.Scan(string.Empty, issues);

            Assert.IsEmpty(issues.Issues.Where(x => x.IsError));

            docSet.ResourceCollection.RegisterJsonResource(testFile.Resources.First());

            var testMethod = testFile.Requests.First();

            var expectedResponse = HttpParser.ParseHttpResponse(testMethod.ExpectedResponse);
            var actualResponse = HttpParser.ParseHttpResponse(testMethod.ActualResponse);

            testMethod.ValidateResponse(actualResponse, expectedResponse, null, issues);

            Assert.AreEqual(1, issues.Errors.Count());
            var error = issues.Errors.First();
            Assert.AreEqual(ValidationErrorCode.RequiredPropertiesMissing, error.Code);
        }

        [Test]
        public void TruncatedExampleSelectStatementOnChildren()
        {
            DocSet docSet = new DocSet();
            DocFile testFile = new DocFileForTesting(Resources.ExampleValidationSelectStatement, "test.md", "test.md", docSet);

            var issues = new IssueLogger();
            testFile.Scan(string.Empty, issues);

            Assert.IsEmpty(issues.Issues.Where(x => x.IsError));

            docSet.ResourceCollection.RegisterJsonResource(testFile.Resources.First());

            var testMethod = testFile.Requests.First();

            var expectedResponse = HttpParser.ParseHttpResponse(testMethod.ExpectedResponse);
            var actualResponse = HttpParser.ParseHttpResponse(testMethod.ActualResponse);

            testMethod.ValidateResponse(actualResponse, expectedResponse, null, issues);

            Assert.AreEqual(0, issues.Errors.Count());
        }

        [Test]
        public void TruncatedExampleSelectStatementOnChildrenExpectFailure()
        {
            DocSet docSet = new DocSet();
            DocFile testFile = new DocFileForTesting(Resources.ExampleValidationSelectStatementFailure, "test.md", "test.md", docSet);

            var issues = new IssueLogger();
            testFile.Scan(string.Empty, issues);

            Assert.IsEmpty(issues.Issues.Where(x => x.IsError));

            docSet.ResourceCollection.RegisterJsonResource(testFile.Resources.First());

            var testMethod = testFile.Requests.First();

            var expectedResponse = HttpParser.ParseHttpResponse(testMethod.ExpectedResponse);
            var actualResponse = HttpParser.ParseHttpResponse(testMethod.ActualResponse);

            testMethod.ValidateResponse(actualResponse, expectedResponse, null, issues);

            Assert.IsTrue(issues.Issues.Any(x => x.Code == ValidationErrorCode.RequiredPropertiesMissing), "Error with Code = RequiredPropertiesMissing");
            Assert.IsTrue(
                issues.Issues.Any(x => x.Code == ValidationErrorCode.SkippedSimilarErrors),
                "Error with Code = SkippedSimilarErrors");
        }

    }
}
