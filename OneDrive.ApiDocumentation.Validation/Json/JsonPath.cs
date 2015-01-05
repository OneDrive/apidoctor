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
            var components = path.Split('.');
            if (components.Length < 1 || components[0] != "$")
                throw new ArgumentException("Path doesn't appear to conform to JSONpath syntax.", "path");

            var rootObject = (JContainer)JsonConvert.DeserializeObject(json);
            object currentObject = rootObject;

            for (int i = 1; i < components.Length; i++)
            {
                var propertyName = components[i];
                int arrayindex = -1;
                if (propertyName.EndsWith("]"))
                {
                    int startIndexPosition = propertyName.LastIndexOf("[");
                    arrayindex = Int32.Parse(propertyName.Substring(startIndexPosition + 1, propertyName.Length - (startIndexPosition + 2)));
                    propertyName = propertyName.Substring(0, startIndexPosition);
                }

                JContainer container = currentObject as JContainer;
                if (null != container)
                {
                    try
                    {
                        currentObject = container[propertyName];
                    }
                    catch (Exception ex)
                    {
                        throw new JsonPathException(string.Format("Couldn't locate property {0}", propertyName), ex);
                    }

                    if (currentObject == null && (i < components.Length - 1 || arrayindex >= 0))
                        throw new JsonPathException(string.Format("Property {0} was null or missing. Cannot continue to evaluate path.", propertyName));
                }
                else
                {
                    throw new JsonPathException("Ran out of objects before the path was exhausted.");
                }

                if (arrayindex >= 0)
                {
                    try
                    {
                        currentObject = ((dynamic)currentObject)[arrayindex];
                    }
                    catch (Exception ex)
                    {
                        throw new JsonPathException("Specified array index was unavailable.", ex);
                    }
                }
            }

            JValue value = currentObject as JValue;
            if (null != value)
            {
                switch (value.Type)
                {
                    case JTokenType.Boolean:
                    case JTokenType.Bytes:
                    case JTokenType.Date:
                    case JTokenType.Float:
                    case JTokenType.Guid:
                    case JTokenType.Integer:
                    case JTokenType.Null:
                    case JTokenType.String:
                    case JTokenType.TimeSpan:
                    case JTokenType.Uri:
                        return value.Value;
                    default:
                        break;
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

