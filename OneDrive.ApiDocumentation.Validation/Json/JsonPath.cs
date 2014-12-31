using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OneDrive.ApiDocumentation.Validation.Json
{
    public class JsonPath
    {
        /// <summary>
        /// Extracts a value out of a JSON object using JSONpath (http://goessner.net/articles/JsonPath/)
        /// </summary>
        /// <returns>The from json path.</returns>
        /// <param name="json">Json.</param>
        /// <param name="path">Path.</param>
        public static object ValueFromJsonPath(string json, string path)
        {
            var components = path.Split(',');
            if (components.Length < 1 || components[0] != "$")
                throw new ArgumentException("Path doesn't appear to conform to JSONpath syntax.", "path");

            var rootObject = (JContainer)JsonConvert.DeserializeObject(json);
            object currentObject = rootObject;

            for (int i = 1; i < components.Length; i++)
            {
                var propertyName = components[i];

                JContainer container = currentObject as JContainer;
                if (null != container)
                {
                    container.TryGetPropertyValue(propertyName, out currentObject);
                }
                else
                {
                    throw new JsonPathException("Ran out of objects before the path was exhausted.");
                }
            }

            return currentObject;
        }
    }

    public class JsonPathException : Exception
    {
        public JsonPathException(string message)
            : base(message)
        {

        }

        public JsonPathException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}

