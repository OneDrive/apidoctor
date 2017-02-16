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

namespace ApiDocs.Validation.Json
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

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

        public static bool TokenEquals(JToken token, object input)
        {
            var convertedValue = ConvertValueForOutput(token);

            if (null != input)
            {
                return input.Equals(convertedValue);
            }
            return null == convertedValue;
        }

        /// <summary>
        /// Sets the value of a property in a dynamic object based on a json path
        /// </summary>
        /// <param name="json"></param>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SetValueForJsonPath(string json, string path, object value)
        {
            object originalObject = (JContainer)JsonConvert.DeserializeObject(json);
            var jsonObject = originalObject;

            var currentComponent = DecomposePath(path);
            if (currentComponent.IsRoot)
                currentComponent = currentComponent.Child;

            while (currentComponent != null && currentComponent.Child != null)
            {
                jsonObject = currentComponent.GetObjectForPart(jsonObject, true);
                currentComponent = currentComponent.Child;
            }            

            if (null != jsonObject && null != currentComponent)
            {
                currentComponent.SetValueForPart(jsonObject, value);
            }

            return JsonConvert.SerializeObject(originalObject, Formatting.Indented);
        }


        /// <summary>
        /// Decompose path string into the individual navigation members (propertyName[arrayindex])
        /// Property names can be separated by periods ($.foo.bar) and can be enclosed in square brackets ($.['foo'].['bar'])
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static JsonPathPart DecomposePath(string path)
        {
            const string jsonPathStarter = "$.";

            if (path == "$")
                return JsonPathPart.Root;

            if (!path.StartsWith(jsonPathStarter))
                throw new ArgumentException(
                    string.Format("Path \"{0}\" doesn't appear to conform to JSONpath syntax.", path),
                    "path");

            List<JsonPathPart> pathParts = new List<JsonPathPart> { JsonPathPart.Root };

            StringBuilder reader = new StringBuilder();
            JsonPathPart partBeingRead = new JsonPathPart();

            PathParserState state = PathParserState.ReadingPropertyName;

            for (int i = jsonPathStarter.Length; i < path.Length; i++)
            {
                // Walk through all the characters of the string and build up the path parts.
                char thisChar = path[i];
                if (thisChar == '.' && state != PathParserState.ReadingEscapedPropertyName)
                {
                    // End of a part name, save it and start a new path part.
                    if (reader.Length > 0)
                    {
                        if (state == PathParserState.ReadingPropertyName)
                        {
                            partBeingRead.PropertyName = reader.ToString();
                            reader.Clear();
                        }
                        else
                        {
                            throw new InvalidOperationException("Unexpected state. Malformed JsonPath syntax?");
                        }
                    }
                    pathParts.Add(partBeingRead);
                    partBeingRead = new JsonPathPart();
                    state = PathParserState.ReadingPropertyName;
                }
                else if (thisChar == '[' && state != PathParserState.ReadingEscapedPropertyName)
                {
                    if (reader.Length > 0)
                    {
                        if (state == PathParserState.ReadingPropertyName)
                        {
                            partBeingRead.PropertyName = reader.ToString();
                            reader.Clear();
                        }
                        else
                        {
                            throw new InvalidOperationException("Unexpected state. Malformed JsonPath syntax?");
                        }
                    }
                    state = PathParserState.ReadingIndexer;
                }
                else if (thisChar == ']' && state != PathParserState.ReadingEscapedPropertyName)
                {
                    if (state != PathParserState.ReadingIndexer)
                    {
                        throw new InvalidOperationException("Unexpected state. Malformed JsonPath syntax?");
                    }
                    if (reader.Length > 0)
                    {
                        partBeingRead.ArrayIndex = Int32.Parse(reader.ToString());
                        reader.Clear();
                    }
                    state = PathParserState.PartComplete;
                }
                else if (thisChar == '\'')
                {
                    if (state == PathParserState.ReadingIndexer && reader.Length == 0)
                    {
                        state = PathParserState.ReadingEscapedPropertyName;
                    }
                    else if (state == PathParserState.ReadingEscapedPropertyName && reader.Length > 0)
                    {
                        partBeingRead.PropertyName = reader.ToString();
                        reader.Clear();
                        state = PathParserState.ReadingIndexer;
                    }
                    else
                    {
                        throw new InvalidOperationException("Unexpected state. Malformed JsonPath syntax?");
                    }
                }
                else
                {
                    reader.Append(thisChar);
                }
            }

            if (reader.Length > 0 && (state == PathParserState.ReadingPropertyName || state == PathParserState.PartComplete))
            {
                partBeingRead.PropertyName = reader.ToString();
            }
            else if (reader.Length > 0)
            {
                throw new InvalidOperationException("Unexpected content in reader buffer. Malformed JsonPath syntax?");
            }

            // Add the final part, if it wasn't there already.
            if (!string.IsNullOrEmpty(partBeingRead.PropertyName))
            {
                pathParts.Add(partBeingRead);
            }

            for (int i = 0; i < pathParts.Count; i++)
            {
                if (i < pathParts.Count - 1)
                    pathParts[i].Child = pathParts[i + 1];
            }

            return pathParts[0];
        }

        enum PathParserState
        {
            PartComplete,
            ReadingPropertyName,
            ReadingIndexer,
            ReadingEscapedPropertyName
        }

        class JsonPathPart
        {
            public JsonPathPart()
            {
                this.ArrayIndex = -1;
            }

            public bool IsRoot { get { return this.PropertyName == "$";}}

            public string PropertyName { get; set; }

            public int ArrayIndex { get; set; }

            public JsonPathPart Child { get; set; }

            public static JsonPathPart Root
            {
                get { return new JsonPathPart { PropertyName = "$" }; }
            }

            /// <summary>
            /// Return the value of the property referred to by the current JsonPathPart instance
            /// </summary>
            /// <param name="source"></param>
            /// <param name="createIfMissing"></param>
            /// <returns></returns>
            public object GetObjectForPart(object source, bool createIfMissing = false)
            {
                JContainer container = source as JContainer;
                object foundValue = null;
                if (null != container)
                {
                    if (!container.TryGetPropertyValue(this.PropertyName, out foundValue) && !createIfMissing)
                    {
                        throw new JsonPathException($"Couldn't locate property {this.PropertyName} in JSON object.");
                    }

                    if (foundValue == null && (this.Child != null || this.ArrayIndex >= 0))
                    {
                        if (!createIfMissing)
                        {
                            throw new JsonPathException(string.Format("Property {0} was null or missing. Cannot continue to evaluate path.", this.PropertyName));
                        }

                        container[this.PropertyName] = new JObject();
                        foundValue = container[this.PropertyName];
                    }
                }
                else if (null == source)
                {
                    throw new JsonPathException($"Null object encountered while parsing JsonPath");
                }
                else
                {
                    throw new JsonPathException($"Unsupported object type: {source}");
                }

                if (this.ArrayIndex >= 0)
                {
                    try
                    {
                        if (foundValue != null)
                        {
                            foundValue= ((dynamic)foundValue)[this.ArrayIndex];
                        }
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
                            JProperty prop;
                            if (container.TryGetProperty(this.PropertyName, out prop))
                            {
                                prop.Remove();
                            }
                        }
                        else
                        {
                            container[this.PropertyName] = JToken.FromObject(value);
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

    [Serializable]
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

