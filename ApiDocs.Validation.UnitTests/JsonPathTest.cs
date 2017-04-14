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
    using ApiDocs.Validation.Json;
    using Newtonsoft.Json;
    using NUnit.Framework;

    [TestFixture]
    public class JsonPathTest
    {

        public object GetJsonObject()
        {
            Dictionary<string, object> obj = new Dictionary<string, object>();
            obj["id"] = "1234";
            obj["path"] = "/root/Documents/something";
            obj["another"] = new {
                    value = "test-string",
                    number = 1234
                };
            obj["thumbnails"] = new
                {
                    small = new { id = "small", width = "100", height = "100", url = "http://small" },
                    medium = new { id = "medium", width = "200", height = "200", url = "http://medium" },
                    large = new { id = "medium", width = "1000", height = "1000", url = "http://large" }
                };
            obj["children"] = new object[] {
                        new { id = "1234.1", name = "first_file.txt" },
                        new { id = "1234.2", name = "second_file.txt"},
                        new { id = "1234.3", name = "third_file.txt"},
                        new { id = "1234.4", name = "fourth_file.txt"}
                    };
            obj["@content.downloadUrl"] = "https://foobar.com/something";
            return obj;
        }

        public string GetJson()
        {
            var jsonString = JsonConvert.SerializeObject(this.GetJsonObject());
            return jsonString;
        }


        [Test]
        public void JsonPathRootObject()
        {
            var json = this.GetJson();
            var value = JsonPath.ValueFromJsonPath(json, "$");

            var resultJson = JsonConvert.SerializeObject(value);
            Assert.AreEqual(json, resultJson);
        }

        [Test]
        public void JsonPathInvalidPath()
        {
            Assert.Throws<JsonPathException>(() => JsonPath.ValueFromJsonPath(this.GetJson(), "$.nothing.foo"));
        }

        [Test]
        public void JsonPathTopLevelValue()
        {
            var value = JsonPath.ValueFromJsonPath(this.GetJson(), "$.id");
            Assert.AreEqual("1234", value);
        }

        [Test]
        public void JsonPathSecondLevelObjectValue()
        {
            var value = JsonPath.ValueFromJsonPath(this.GetJson(), "$.thumbnails.small");

            dynamic obj = this.GetJsonObject();
            var smallThumbnailObject = obj["thumbnails"].small;

            var foundObjectJson = JsonConvert.SerializeObject(value);
            var dynamicObjectJson = JsonConvert.SerializeObject(smallThumbnailObject);

            Assert.AreEqual(dynamicObjectJson, foundObjectJson);
        }

        [Test]
        public void JsonPathThirdLevelValue()
        {
            var value = JsonPath.ValueFromJsonPath(this.GetJson(), "$.thumbnails.small.url");
            Assert.AreEqual("http://small", value);
        }

        [Test]
        public void JsonPathArrayTest()
        {
            var value = JsonPath.ValueFromJsonPath(this.GetJson(), "$.children[0]");

            dynamic obj = this.GetJsonObject();
            var firstChild = obj["children"][0];

            var foundObjectJson = JsonConvert.SerializeObject(value);
            var dynamicObjectJson = JsonConvert.SerializeObject(firstChild);

            Assert.AreEqual(dynamicObjectJson, foundObjectJson);
        }

        [Test]
        public void JsonPathArrayWithSecondLevelTest()
        {
            var value = JsonPath.ValueFromJsonPath(this.GetJson(), "$.children[0].name");
            Assert.AreEqual("first_file.txt", value);
        }

        [Test]
        public void JsonPathWithPeriodInPropertyNameTest()
        {
            var value = JsonPath.ValueFromJsonPath(this.GetJson(), "$.['@content.downloadUrl']");
            Assert.AreEqual("https://foobar.com/something", value);
        }

        [Test]
        public void JsonPathSetTopLevelValue()
        {
            string modifiedJson = JsonPath.SetValueForJsonPath(this.GetJson(), "$.id", "5678");

            dynamic result = JsonConvert.DeserializeObject(modifiedJson);
            Assert.AreEqual(JsonPath.ConvertValueForOutput(result.id), "5678");
        }

        [Test]
        public void JsonPathSetSecondLevelValue()
        {
            string modifiedJson = JsonPath.SetValueForJsonPath(this.GetJson(), "$.another.value", "something-else-completely");

            dynamic result = JsonConvert.DeserializeObject(modifiedJson);
            Assert.AreEqual(JsonPath.ConvertValueForOutput(result.another.value), "something-else-completely");
        }

        [Test]
        public void JsonPathSetArrayValue()
        {
            string modifiedJson = JsonPath.SetValueForJsonPath(this.GetJson(), "$.children[0].name", "something-else-completely");

            dynamic result = JsonConvert.DeserializeObject(modifiedJson);
            Assert.AreEqual(JsonPath.ConvertValueForOutput(result.children[0].name), "something-else-completely");
        }

        [Test]
        public void JsonPathSetNewTopLevelValue()
        {
            string modifiedJson = JsonPath.SetValueForJsonPath(this.GetJson(), "$.zippy", "do-dah");

            dynamic result = JsonConvert.DeserializeObject(modifiedJson);
            Assert.AreEqual(JsonPath.ConvertValueForOutput(result.zippy), "do-dah");
        }

        [Test]
        public void JsonPathSetNewSecondLevelValue()
        {
            string modifiedJson = JsonPath.SetValueForJsonPath(this.GetJson(), "$.zippy.foo", "do-dah");

            dynamic result = JsonConvert.DeserializeObject(modifiedJson);
            Assert.AreEqual(JsonPath.ConvertValueForOutput(result.zippy.foo), "do-dah");
        }

        [Test]
        public void JsonPathComplexTypeName()
        {
            string json = "{ \"name\": \"foobar\" }";

            string modifiedJson = JsonPath.SetValueForJsonPath(json, "$.['@name.conflictBehavior']", "replace");
            dynamic result = JsonConvert.DeserializeObject(modifiedJson);
            Assert.AreEqual(JsonPath.ConvertValueForOutput(result["@name.conflictBehavior"]), "replace");

        }
    }
}

