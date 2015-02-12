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
            object jsonObject = (JContainer)JsonConvert.DeserializeObject(json);
            var currentComponent = DecomposePath(path);
            if (currentComponent.IsRoot)
                currentComponent = currentComponent.Child;

            while (currentComponent != null)
            {
                jsonObject = currentComponent.GetObjectForPart(jsonObject);
                currentComponent = currentComponent.Child;
            }

            return ConvertValueForOutput(jsonObject);
        }

        public static object ConvertValueForOutput(object input)
        {
            JValue value = input as JValue;
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
            return input;
        }


        /// <summary>
        /// Sets the value of a property in a dynamic object based on a json path
        /// </summary>
        /// <param name="source"></param>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SetValueForJsonPath(string json, string path, object value)
        {
            object originalObject = (JContainer)JsonConvert.DeserializeObject(json);
            object jsonObject = originalObject;

            var currentComponent = DecomposePath(path);
            if (currentComponent.IsRoot)
                currentComponent = currentComponent.Child;

            while (currentComponent != null && currentComponent.Child != null)
            {
                jsonObject = currentComponent.GetObjectForPart(jsonObject, true);
                currentComponent = currentComponent.Child;
            }            
            if (null != jsonObject)
            {
                currentComponent.SetValueForPart(jsonObject, value);
            }

            return JsonConvert.SerializeObject(originalObject, Formatting.Indented);
        }


        private static JsonPathPart DecomposePath(string path)
        {
            var components = path.Split('.');
            if (components.Length < 1 || components[0] != "$")
                throw new ArgumentException(
                    string.Format("Path \"{0}\" doesn't appear to conform to JSONpath syntax.", path),
                    "path");

            JsonPathPart root = JsonPathPart.Root;
            JsonPathPart currentPart = root;

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

                currentPart = new JsonPathPart(currentPart) { PropertyName = propertyName, ArrayIndex = arrayindex };
            }

            return root;
        }

        class JsonPathPart
        {
            public JsonPathPart()
            {
                ArrayIndex = -1;
            }

            public JsonPathPart(JsonPathPart parent) : this()
            {
                Parent = parent;
                if (null != parent)
                    parent.Child = this;
            }

            public bool IsRoot { get { return PropertyName == "$";}}

            public string PropertyName { get; set; }

            public int ArrayIndex { get; set; }

            public JsonPathPart Parent {get;set;}

            public JsonPathPart Child {get;set;}

            public static JsonPathPart Root
            {
                get { return new JsonPathPart { PropertyName = "$" }; }
            }


            /// <summary>
            /// Return the value of the property referred to by the current JsonPathPart instance
            /// </summary>
            /// <param name="jsonObject"></param>
            /// <returns></returns>
            public object GetObjectForPart(object source, bool createIfMissing = false)
            {
                JContainer container = source as JContainer;
                object foundValue = null;
                if (null != container)
                {
                    try
                    {
                        foundValue = container[PropertyName];
                    }
                    catch (Exception ex)
                    {
                        throw new JsonPathException(string.Format("Couldn't locate property {0}", PropertyName), ex);
                    }

                    if (foundValue == null && (Child != null || ArrayIndex >= 0))
                    {
                        if (!createIfMissing)
                        {
                            throw new JsonPathException(string.Format("Property {0} was null or missing. Cannot continue to evaluate path.", PropertyName));
                        }

                        container[PropertyName] = new JObject();
                        foundValue = container[PropertyName];
                    }
                }
                else
                {
                    throw new JsonPathException("Unsupported object type: " + source.ToString());
                }

                if (ArrayIndex >= 0)
                {
                    try
                    {
                        foundValue= ((dynamic)foundValue)[ArrayIndex];
                    }
                    catch (Exception ex)
                    {
                        throw new JsonPathException("Specified array index was unavailable.", ex);
                    }
                }

                return ConvertValueForOutput(foundValue);
            }

            public void SetValueForPart(object source, object value)
            {
                if (this.Child != null)
                {
                    throw new JsonPathException("Cannot set value for part that isn't a leaf node in the path hierarchy.");
                }

                JContainer container = source as JContainer;
                if (null != container)
                {
                    try
                    {
                        if (null == value)
                        {
                            container[PropertyName] = null;
                        }
                        else
                        {
                            container[PropertyName] = JToken.FromObject(value);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new JsonPathException("Unable to set the value of the property.", ex);
                    }
                }
                else
                {
                    throw new JsonPathException("Unsupported object type: " + source.ToString());
                }
            }
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

