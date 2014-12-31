using System;
using NUnit.Framework;
using Newtonsoft.Json;
using OneDrive.ApiDocumentation.Validation.Json;

namespace OneDrive.UnitTests.ApiDocumentation.Validation
{
    [TestFixture]
    public class JsonPathTest
    {

        public string GetJson()
        {
            var obj = new 
                { 
                    id = "1234", 
                    path = "/root/Documents/something", 
                    thumbnails = new { 
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

            return JsonConvert.SerializeObject(obj);
        }


        [Test]
        public void JsonPathRootObject()
        {
            var value = JsonPath.ValueFromJsonPath(GetJson(), "$");
        }

        [Test]
        public void JsonPathTopLevelValue()
        {
            var value = JsonPath.ValueFromJsonPath(GetJson(), "$.id");
            Assert.AreEqual("1234", value);
        }

        [Test]
        public void JsonPathSecondLevelValue()
        {
            var value = JsonPath.ValueFromJsonPath(GetJson(), "$.thumbnails.small");
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
        }

        [Test]
        public void JsonPathArrayWithSecondLevelTest()
        {
            var value = JsonPath.ValueFromJsonPath(GetJson(), "$.children[0].name");
            Assert.AreEqual("first_file.txt", value);
        }
    }
}

