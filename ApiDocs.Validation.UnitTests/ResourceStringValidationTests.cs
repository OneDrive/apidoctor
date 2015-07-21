using NUnit.Framework;
using ApiDocs.Validation;
using ApiDocs.Validation.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ApiDocs.Validation.UnitTests
{
    using ApiDocs.Validation.Error;

    [TestFixture]
    public class ResourceStringValidationTests
    {

        static DocFile GetDocFile()
        {
            DocSet docSet = new DocSet();
            DocFile testFile = new DocFileForTesting(Properties.Resources.ExampleResources, "\resources.md", "\resources.md", docSet);

            ValidationError[] detectedErrors;
            testFile.Scan(out detectedErrors);

            Assert.IsFalse(detectedErrors.WereWarningsOrErrors(), "Detected warnings or errors when reading the example markdown file.");
            
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

            var resource = testFile.Resources.Single(x => x.ResourceType == "example.resource");
            var schema = new JsonSchema(resource);

            var method = testFile.Requests.Single(x => x.Identifier == "valid-response");

            ValidationError[] detectedErrors;
            bool result = schema.ValidateExpectedResponse(method, out detectedErrors);
            Assert.IsTrue(result);
            Assert.IsEmpty(detectedErrors, "Validation errors were detected");
        }

        /// <summary>
        /// Verify that resources that are defined to have an ISO-formatted date
        /// are actually an ISO formatted date
        /// </summary>
        [Test]
        public void ResourceStringValidationBadTimestamp()
        {
            var testFile = GetDocFile();
            var resource = testFile.Resources.Single(x => x.ResourceType == "example.resource");
            var schema = new JsonSchema(resource);

            var method = testFile.Requests.Single(x => x.Identifier == "bad-timestamp");

            ValidationError[] detectedErrors;
            bool result = schema.ValidateExpectedResponse(method, out detectedErrors);

            Assert.IsFalse(result);
            Assert.IsTrue(detectedErrors.WereErrors());

            Assert.IsNotNull(detectedErrors.SingleOrDefault(x => x.Code == ValidationErrorCode.InvalidDateTimeString));
        }

        /// <summary>
        /// Verify that resources that are defined to have an enumerated value actually
        /// contain that enumerated value
        /// </summary>
        [Test]
        public void ResourceStringValidationBadEnumValue()
        {
            var testFile = GetDocFile();
            var resource = testFile.Resources.Single(x => x.ResourceType == "example.resource");
            var schema = new JsonSchema(resource);

            var method = testFile.Requests.Single(x => x.Identifier == "bad-enum-value");

            ValidationError[] detectedErrors;
            bool result = schema.ValidateExpectedResponse(method, out detectedErrors);

            Assert.IsFalse(result);
            Assert.IsTrue(detectedErrors.WereErrors());

            Assert.IsNotNull(detectedErrors.SingleOrDefault(x => x.Code == ValidationErrorCode.InvalidEnumeratedValueString));
        }

        /// <summary>
        /// Verify that resources that are defined to have an URL actually
        /// contain a URL value
        /// </summary>
        [Test]
        public void ResourceStringValidationBadUrlValue()
        {
            var testFile = GetDocFile();
            var resource = testFile.Resources.Single(x => x.ResourceType == "example.resource");
            var schema = new JsonSchema(resource);

            var method = testFile.Requests.Single(x => x.Identifier == "bad-url");

            ValidationError[] detectedErrors;
            bool result = schema.ValidateExpectedResponse(method, out detectedErrors);

            Assert.IsFalse(result);
            Assert.IsTrue(detectedErrors.WereErrors());

            Assert.IsNotNull(detectedErrors.SingleOrDefault(x => x.Code == ValidationErrorCode.InvalidUrlString));
        }

    }



}
