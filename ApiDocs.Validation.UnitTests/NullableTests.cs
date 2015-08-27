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
