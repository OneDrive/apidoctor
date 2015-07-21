namespace ApiDocs.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using ApiDocs.Validation.Error;
    using ApiDocs.Validation.Json;
    using MarkdownDeep;
    using Newtonsoft.Json.Linq;

    public static class ExtensionMethods
    {

        public static bool ContainsIgnoreCase(this string target, string value)
        {
            return target.IndexOf(value, StringComparison.OrdinalIgnoreCase) != -1;
        }

        public static string ComponentsJoinedByString(this IEnumerable<string> source, string separator, int startIndex = 0)
        {
            StringBuilder sb = new StringBuilder();
            int index = 0;
            foreach (var component in source)
            {
                if (index < startIndex)
                {
                    index++;
                    continue;
                }

                if (sb.Length > 0)
                    sb.Append(separator);
                sb.Append(component);

                index++;
            }
            return sb.ToString();
        }

        public static string ComponentsJoinedByString(this IEnumerable<int> source, string separator, int startIndex = 0)
        {
            StringBuilder sb = new StringBuilder();
            int index = 0;
            foreach (var component in source)
            {
                if (index < startIndex)
                {
                    index++;
                    continue;
                }

                if (sb.Length > 0)
                    sb.Append(separator);
                sb.Append(component);

                index++;
            }
            return sb.ToString();
        }

        public static void RemoveRange<T>(this IList<T> source, IEnumerable<T> objectsToRemove)
        {
            if (null == source) return;
            if (null == objectsToRemove) return;

            foreach (var objToRemove in objectsToRemove)
            {
                source.Remove(objToRemove);
            }
        }

        public static void IntersectInPlace<T>(this IList<T> source, IList<T> otherSet)
        {
            var existingList = source.ToArray();
            foreach (var item in existingList)
            {
                if (!otherSet.Contains(item))
                    source.Remove(item);
            }
        }

        public static bool TryGetPropertyValue<T>(this JContainer container, string propertyName, out T value )
        {
            try
            {
                JValue storedValue = (JValue)container[propertyName];
                if (storedValue == null)
                {
                    value = default(T);
                    return false;
                }
                else
                {
                    value = (T)storedValue.Value;
                    return true;
                }
            }
            catch 
            {
                value = default(T);
                return false;
            }
        }

        public static T PropertyValue<T>(this JContainer container, string propertyName, T defaultValue)
        {
            T storedValue;
            if (container.TryGetPropertyValue(propertyName, out storedValue))
                return storedValue;
            else
                return defaultValue;
        }

        public static string FirstLineOnly(this string input)
        {
            StringReader reader = new StringReader(input);
            return reader.ReadLine();
        }

        public static bool WereErrors(this IEnumerable<ValidationError> errors)
        {
            return errors.Any(x => x.IsError);
        }

        public static bool WereWarnings(this IEnumerable<ValidationError> errors)
        {
            return errors.Any(x => x.IsWarning);
        }

        public static bool WereWarningsOrErrors(this IEnumerable<ValidationError> errors)
        {
            return errors.Any(x => x.IsError || x.IsWarning);
        }


        public static string ValueForColumn(this string[] rowValues, IMarkdownTable table, params string[] possibleHeaderNames)
        {
            var headers = table.ColumnHeaders;

            foreach (var headerName in possibleHeaderNames)
            {
                int index = headers.IndexOf(headerName);
                if (index >= 0 && index < rowValues.Length)
                {
                    // Check to see if we need to clean up / remove any ` marks
                    string tableCellContents = rowValues[index];
                    if (null != tableCellContents)
                        return tableCellContents.Trim(' ', '`');
                    else
                        return null;
                }
            }

            Debug.WriteLine("Failed to find header matching '{0}' in table with headers: {1}", 
                possibleHeaderNames.ComponentsJoinedByString(","),
                table.ColumnHeaders.ComponentsJoinedByString(","));
            return null;
        }

        public static int IndexOf(this string[] array, string value, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (value.Equals(array[i], comparison))
                    return i;
            }
            return -1;
        }

        public static JsonDataType ToDataType(this string value, Action<ValidationError> addErrorAction = null)
        {
            JsonDataType output;
            if (Enum.TryParse(value, true, out output))
                return output;
            if (null == value)
                return JsonDataType.Object;
            if (value.ToLower().Contains("string"))
                return JsonDataType.String;
            if (value.Equals("etag", StringComparison.OrdinalIgnoreCase))
                return JsonDataType.String;
            if (value.Equals("range", StringComparison.OrdinalIgnoreCase))
                return JsonDataType.String;
            if (value.ToLower().Contains("timestamp"))
                return JsonDataType.String;

            if (null != addErrorAction)
            {
                addErrorAction(new ValidationWarning(ValidationErrorCode.TypeConversionFailure, "Couldn't convert '{0}' into Json.JsonDataType enumeration. Assuming Object type.", value));
            }
            return JsonDataType.Object;
        }

        public static bool IsRequired(this string description)
        {
            if (null == description) 
                return false;
            return description.StartsWith("required.", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsHeaderBlock(this Block block)
        {
            switch (block.BlockType)
            {
                case BlockType.h1:
                case BlockType.h2:
                case BlockType.h3:
                case BlockType.h4:
                case BlockType.h5:
                case BlockType.h6:
                    return true;
                default:
                    return false;
            }
        }


        public static void SplitUrlComponents(this string inputUrl, out string path, out string queryString)
        {
            int index = inputUrl.IndexOf('?');
            if (index == -1)
            {
                path = inputUrl;
                queryString = null;
            }
            else
            {
                path = inputUrl.Substring(0, index);
                queryString = inputUrl.Substring(index + 1);
            }
        }

        public static bool ToBoolean(this string input)
        {
            bool output;
            if (bool.TryParse(input, out output))
                return output;

            if (string.IsNullOrEmpty(input))
                return false;

            if (input.Equals("no", StringComparison.OrdinalIgnoreCase))
                return false;
            if (input.Equals("yes", StringComparison.OrdinalIgnoreCase))
                return true;

            throw new NotSupportedException(string.Format("Couldn't convert this value to a boolean: {0}", input));
        }

        public static Regex PathVariableRegex = new Regex(@"\{(?<var>.+?)\}", RegexOptions.Compiled);

        public static string FlattenVariableNames(this string input)
        {
            return PathVariableRegex.Replace(input, "{}");
        }



    }
}
