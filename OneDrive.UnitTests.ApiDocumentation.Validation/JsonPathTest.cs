using System;
using NUnit.Framework;
using Newtonsoft.Json;
using OneDrive.ApiDocumentation.Validation.Json;

namespace OneDrive.UnitTests.ApiDocumentation.Validation
{
    [TestFixture]
    public class JsonPathTest
    {

        public object GetJsonObject()
        {
            var obj = new
            {
                id = "1234",
                path = "/root/Documents/something",
                thumbnails = new
                {
                    small = new { id = "small", width = "100", height = "100", url = "http://small" },
                    medium = new { id = "medium", width = "200", height = "200", url = "http://medium" },
                    large = new { id = "medium", width = "1000", height = "1000", url = "http://large" }
                },
                children = new object[] {
                        new { id = "1234.1", name = "first_file.txt" },
                        new { id = "1234.2", name = "second_file.txt"},
                        new { id = "1234.3", name = "third_file.txt"},
                        new { id = "1234.4", name = "fourth_file.txt"}
                    }
            };
            return obj;
        }

        public string GetJson()
        {
            return JsonConvert.SerializeObject(GetJsonObject());
        }


        [Test]
        public void JsonPathRootObject()
        {
            var json = GetJson();
            var value = JsonPath.ValueFromJsonPath(json, "$");

            var resultJson = JsonConvert.SerializeObject(value);
            Assert.AreEqual(json, resultJson);
        }

        [Test]
        [ExpectedException(ExpectedException=typeof(JsonPathException))]
        public void JsonPathInvalidPath()
        {
            var value = JsonPath.ValueFromJsonPath(GetJson(), "$.nothing.foo");
        }

        [Test]
        public void JsonPathTopLevelValue()
        {
            var value = JsonPath.ValueFromJsonPath(GetJson(), "$.id");
            Assert.AreEqual("1234", value);
        }

        [Test]
        public void JsonPathSecondLevelObjectValue()
        {
            var value = JsonPath.ValueFromJsonPath(GetJson(), "$.thumbnails.small");

            dynamic obj = GetJsonObject();
            var smallThumbnailObject = obj.thumbnails.small;

            var foundObjectJson = JsonConvert.SerializeObject(value);
            var dynamicObjectJson = JsonConvert.SerializeObject(smallThumbnailObject);

            Assert.AreEqual(dynamicObjectJson, foundObjectJson);
        }

        [Test]
        public void JsonPathThirdLevelValue()
        {
            var value = JsonPath.ValueFromJsonPath(GetJson(), "$.thumbnails.small.url");
            Assert.AreEqual("http://small", value);
        }

        [Test]
        public void JsonPathArrayTest()
        {
            var value = JsonPath.ValueFromJsonPath(GetJson(), "$.children[0]");

            dynamic obj = GetJsonObject();
            var firstChild = obj.children[0];

            var foundObjectJson = JsonConvert.SerializeObject(value);
            var dynamicObjectJson = JsonConvert.SerializeObject(firstChild);

            Assert.AreEqual(dynamicObjectJson, foundObjectJson);
        }

        [Test]
        public void JsonPathArrayWithSecondLevelTest()
        {
            var value = JsonPath.ValueFromJsonPath(GetJson(), "$.children[0].name");
            Assert.AreEqual("first_file.txt", value);
        }
    }
}

