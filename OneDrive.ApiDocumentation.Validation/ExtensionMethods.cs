namespace OneDrive.ApiDocumentation.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public static class ExtensionMethods
    {

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

        public static bool TryGetPropertyValue<T>(this Newtonsoft.Json.Linq.JContainer container, string propertyName, out T value )
        {
            try
            {
                Newtonsoft.Json.Linq.JValue storedValue = (Newtonsoft.Json.Linq.JValue)container[propertyName];
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

        public static T PropertyValue<T>(this Newtonsoft.Json.Linq.JContainer container, string propertyName, T defaultValue)
        {
            T storedValue;
            if (container.TryGetPropertyValue(propertyName, out storedValue))
                return storedValue;
            else
                return defaultValue;
        }

        public static string TopLineOnly(this string input)
        {
            System.IO.StringReader reader = new System.IO.StringReader(input);
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


        public static string ValueForColumn(this string[] rowValues, MarkdownDeep.IMarkdownTable table, params string[] possibleHeaderNames)
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
                        return tableCellContents.Trim(new char[] { ' ', '`' });
                    else
                        return null;
                }
            }

            System.Diagnostics.Debug.WriteLine("Failed to find header matching '{0}' in table with headers: {1}", 
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

        public static Json.JsonDataType ToDataType(this string value, Action<ValidationError> addErrorAction = null)
        {
            Json.JsonDataType output;
            if (Enum.TryParse<Json.JsonDataType>(value, true, out output))
                return output;

            if (value.ToLower().Contains("string"))
                return Json.JsonDataType.String;
            if (value.Equals("etag", StringComparison.OrdinalIgnoreCase))
                return Json.JsonDataType.String;
            if (value.Equals("range", StringComparison.OrdinalIgnoreCase))
                return Json.JsonDataType.String;
            if (value.ToLower().Contains("timestamp"))
                return Json.JsonDataType.String;

            if (null != addErrorAction)
            {
                addErrorAction(new ValidationWarning(ValidationErrorCode.TypeConversionFailure, "Couldn't convert '{0}' into Json.JsonDataType enumeration. Assuming Object type.", value));
            }
            return Json.JsonDataType.Object;
        }

        public static bool IsRequired(this string description)
        {
            return description.StartsWith("required.", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsHeaderBlock(this MarkdownDeep.Block block)
        {
            switch (block.BlockType)
            {
                case MarkdownDeep.BlockType.h1:
                case MarkdownDeep.BlockType.h2:
                case MarkdownDeep.BlockType.h3:
                case MarkdownDeep.BlockType.h4:
                case MarkdownDeep.BlockType.h5:
                case MarkdownDeep.BlockType.h6:
                    return true;
                default:
                    return false;
            }
        }
    }
}
