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
    using System.Linq;
    using ApiDoctor.Validation.Error;
    using ApiDoctor.Validation.Json;
    using ApiDoctor.Validation.UnitTests.Properties;
    using NUnit.Framework;

    [TestFixture]
    public class ResourceStringValidationTests
    {

        static DocFile GetDocFile()
        {
            DocSet docSet = new DocSet();
            DocFile testFile = new DocFileForTesting(Resources.ExampleResources, "resources.md", "resources.md", docSet);

            var issues = new IssueLogger();
            testFile.Scan(string.Empty, issues);

            Assert.That(issues.Issues.WereWarningsOrErrors(), Is.False, "Detected warnings or errors when reading the example markdown file.");
            
            return testFile;
        }

        /// <summary>
        /// Verify that resources that are defined to have an ISO-formatted date
        /// are actually an ISO formatted date
        /// </summary>
        [Test]
        public void ResourceStringValidationValidExampleTest()
        {
            var testFile = GetDocFile();

            var resource = testFile.Resources.Single(x => x.Name == "example.resource");
            var schema = new JsonSchema(resource);

            var method = testFile.Requests.Single(x => x.Identifier == "valid-response");

            var issues = new IssueLogger();
            bool result = schema.ValidateExpectedResponse(method, issues);
            Assert.That(result);
            Assert.That(issues.Issues.Where(i => i.IsWarningOrError), Is.Empty, "Validation errors were detected");
        }

        /// <summary>
        /// Verify that resources that are defined to have an ISO-formatted date
        /// are actually an ISO formatted date
        /// </summary>
        [Test]
        public void ResourceStringValidationBadTimestamp()
        {
            var testFile = GetDocFile();
            var resource = testFile.Resources.Single(x => x.Name == "example.resource");
            var schema = new JsonSchema(resource);

            var method = testFile.Requests.Single(x => x.Identifier == "bad-timestamp");

            var issues = new IssueLogger();
            bool result = schema.ValidateExpectedResponse(method, issues);

            Assert.That(result, Is.False);
            Assert.That(issues.Issues.WereErrors());

            Assert.That(issues.Issues.SingleOrDefault(x => x.Code == ValidationErrorCode.ExpectedTypeDifferent), Is.Not.Null);
        }

        /// <summary>
        /// Verify that resources that are defined to have an enumerated value actually
        /// contain that enumerated value
        /// </summary>
        [Test]
        public void ResourceStringValidationBadEnumValue()
        {
            var testFile = GetDocFile();
            var resource = testFile.Resources.Single(x => x.Name == "example.resource");
            var schema = new JsonSchema(resource);

            var method = testFile.Requests.Single(x => x.Identifier == "bad-enum-value");

            var issues = new IssueLogger();
            bool result = schema.ValidateExpectedResponse(method, issues);

            Assert.That(result, Is.False);
            Assert.That(issues.Issues.WereErrors());

            Assert.That(issues.Issues.SingleOrDefault(x => x.Code == ValidationErrorCode.InvalidEnumeratedValueString), Is.Not.Null);
        }

        /// <summary>
        /// Verify that resources that are defined to have an URL actually
        /// contain a URL value
        /// </summary>
        [Test]
        public void ResourceStringValidationBadUrlValue()
        {
            var testFile = GetDocFile();
            var resource = testFile.Resources.Single(x => x.Name == "example.resource");
            var schema = new JsonSchema(resource);

            var method = testFile.Requests.Single(x => x.Identifier == "bad-url");

            var issues = new IssueLogger();
            bool result = schema.ValidateExpectedResponse(method, issues);

            Assert.That(result, Is.False);
            Assert.That(issues.Issues.WereErrors());

            Assert.That(issues.Issues.SingleOrDefault(x => x.Code == ValidationErrorCode.InvalidUrlString), Is.Not.Null);
        }

    }



}
