/*
 * API Doctor
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

namespace ApiDoctor.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using ApiDoctor.Validation.Error;
    using ApiDoctor.Validation.Json;
    using MarkdownDeep;
    using Newtonsoft.Json.Linq;
    using System.Globalization;
    using ApiDoctor.Validation.OData.Transformation;

    public static class ExtensionMethods
    {
        private const char fancyLeftQuote = (char)0x201c;
        private const char fancyRightQuote = (char)0x201d;
        private const char singleQuote = '\'';

        private static readonly Regex likelyBase64Regex = new Regex("^[a-fA-F0-9+=/]+$", RegexOptions.Compiled);


        private static readonly string[] Iso8601Formats =
        {
            "yyyy-MM-dd",
            @"HH\:mm\:ss.fffZ",
            @"HH\:mm\:ssZ",
            @"yyyy-MM-ddTHH\:mm\:ssZ",
            @"yyyy-MM-ddTHH\:mm\:ss.fZ",
            @"yyyy-MM-ddTHH\:mm\:ss.ffZ",
            @"yyyy-MM-ddTHH\:mm\:ss.fffZ",
            @"yyyy-MM-ddTHH\:mm\:ss.ffffZ",
            @"yyyy-MM-ddTHH\:mm\:ss.fffffZ",
            @"yyyy-MM-ddTHH\:mm\:ss.ffffffZ",
            @"yyyy-MM-ddTHH\:mm\:ss.fffffffZ"
        };

        private static readonly string[] TimeOfDayFormats =
        {
            @"HH\:mm\:ss.fff",
            @"HH\:mm\:ss",
        };

        public static bool IEquals(this string target, string value)
        {
            return string.Equals(target, value, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IContains(this string target, string value)
        {
            return target.IndexOf(value, StringComparison.OrdinalIgnoreCase) != -1;
        }

        public static string IReplace(this string target, string oldValue, string newValue)
        {
            string result = target;
            for (int i = 0; i < result.Length;)
            {
                int matchIndex = result.IndexOf(oldValue, i, StringComparison.OrdinalIgnoreCase);
                if (matchIndex == -1)
                {
                    break;
                }

                result = result.Substring(0, matchIndex) + newValue + result.Substring(matchIndex + oldValue.Length);
                i = matchIndex + newValue.Length;
            }

            return result;
        }

        public static string ToStringClean(this object value)
        {
            if (value == null)
            {
                return null;
            }

            return value.ToString().
                Replace(fancyLeftQuote, singleQuote).
                Replace(fancyRightQuote, singleQuote).
                Replace('"', singleQuote).
                Replace('\\', '/');
        }

        public static string[] TokenizedWords(this string value)
        {
            return value?.Split(' ', ',', ';', '.', '\'', '\"', '|', '`') ?? new string[0];
        }
        
        public static string NullIfEmpty(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value;
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

        public static bool TryGetPropertyValue<T>(this JContainer container, string propertyName, out T value ) where T : class
        {
            JProperty prop;
            if (TryGetProperty(container, propertyName, out prop))
            {
                value = prop.Value as T;
                if (prop.Value != null) Debug.Assert(value != null);
                return true;
            }

            value = default(T);
            return false;
        }

        public static bool TryGetProperty(this JContainer container, string propertyName, out JProperty value)
        {
            foreach (var entity in container)
            {
                JProperty prop = entity as JProperty;
                if (null != prop)
                {
                    if (prop.Name == propertyName)
                    {
                        value = prop;
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }

        public static T PropertyValue<T>(this JContainer container, string propertyName, T defaultValue) where T : class
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

        static MarkdownDeep.Markdown converter = new Markdown() { ActiveRenderer = new MarkdownDeep.Formats.RenderToPlainText(), ExtraMode = true };


        public static string ValueForColumn(this string[] rowValues, IMarkdownTable table, string[] possibleHeaderNames, List<string> usedColumns = null, bool removeMarkdownSyntax = true)
        {
            if (usedColumns == null)
            {
                usedColumns = new List<string>();
            }

            var headers = table.ColumnHeaders;

            foreach (var headerName in possibleHeaderNames.Except(usedColumns))
            {
                int index = headers.IndexOf(headerName);
                if (index >= 0 && index < rowValues.Length)
                {
                    // Check to see if we need to clean up / remove any formatting marks
                    string tableCellContents = rowValues[index];

                    if (removeMarkdownSyntax && !string.IsNullOrEmpty(tableCellContents))
                    {
                        tableCellContents = converter.Transform(tableCellContents).TrimEnd();
                    }
                    usedColumns.Add(headerName);
                    return tableCellContents;
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

        /// <summary>
        /// Helper method that converts a string value into a ParameterDataType instance
        /// </summary>
        /// <param name="value"></param>
        /// <param name="addErrorAction"></param>
        /// <returns></returns>
        public static ParameterDataType ParseParameterDataType(this string value, bool isCollection = false, Action<ValidationError> addErrorAction = null, ParameterDataType defaultValue = null )
        {
            const string collectionPrefix = "collection(";
            const string collectionOfPrefix = "collection of";
            const string collectionSuffix = " collection";

            if (value == null)
            {
                return null;
            }

            if (value.StartsWith(collectionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                isCollection = true;
                value = value.Substring(collectionPrefix.Length).TrimEnd(')');
            }
            else if (value.StartsWith(collectionOfPrefix, StringComparison.OrdinalIgnoreCase))
            {
                isCollection = true;
                value = value.Substring(collectionOfPrefix.Length).TrimEnd(')');
            }
            else if (value.EndsWith(collectionSuffix, StringComparison.OrdinalIgnoreCase))
            {
                isCollection = true;
                value = value.Replace(collectionSuffix, string.Empty);
            }

            // Value could have markdown formatting in it, so we do some basic work to try and remove that if it exists
            if (value.IndexOf('[') != -1)
            {
                value = value.TextBetweenCharacters('[', ']');   
            }

            SimpleDataType simpleType = ParseSimpleTypeString(value.ToLowerInvariant());
            if (simpleType != SimpleDataType.None)
            {
                return new ParameterDataType(simpleType, isCollection);
            }

            // some inferences for common descriptions
            ParameterDataType inferredType = null;
            if (value.IContains("etag"))
            {
                inferredType = ParameterDataType.String;
            }
            else if (value.IContains("timestamp"))
            {
                inferredType = ParameterDataType.DateTimeOffset;
            }
            else if (value.IContains("string"))
            {
                inferredType = ParameterDataType.String;
            }

            bool isEnum = false;
            if (value.IContains(" enum"))
            {
                isEnum = true;
                value = value.IReplace(" enum", string.Empty).Trim();
            }

            if (inferredType != null)
            {
                if (isCollection)
                {
                    inferredType = ParameterDataType.CollectionOfType(inferredType);
                }

                return inferredType;
            }

            // if there aren't any spaces or special characters, assume we parsed the name of a type correctly.
            if (value.IndexOfAny(new[] { ' ', '/' }) == -1)
            {
                SchemaConfig config = DocSet.SchemaConfig;

                if (!(config?.NotLowerCamel?.Contains(value)).GetValueOrDefault())
                {
                    value = char.ToLowerInvariant(value[0]) + value.Substring(1);
                }

                if (value.IndexOf('.') == -1 && !string.IsNullOrEmpty(config?.DefaultNamespace))
                {
                    value = config.DefaultNamespace + "." + value;
                }

                return new ParameterDataType(value, isCollection, isEnum);
            }

            if (defaultValue != null)
            {
                return defaultValue;
            }

            if (null != addErrorAction)
            {
                addErrorAction(new ValidationWarning(ValidationErrorCode.TypeConversionFailure, null, null, "Couldn't convert '{0}' into understood data type. Assuming Object type.", value));
            }
            return new ParameterDataType(value, isCollection);
        }

        public static SimpleDataType ParseSimpleTypeString(string lowercaseString)
        {
            if (lowercaseString.StartsWith("edm."))
            {
                lowercaseString = lowercaseString.Substring(4);
            }
            SimpleDataType simpleType = SimpleDataType.None;
            switch (lowercaseString)
            {
                case "string":
                    simpleType = SimpleDataType.String;
                    break;
                case "int64":
                case "long":
                case "number":
                case "integer":
                    simpleType = SimpleDataType.Int64;
                    break;
                case "int":
                case "int32":
                    simpleType = SimpleDataType.Int32;
                    break;
                case "int16":
                case "short":
                    simpleType = SimpleDataType.Int16;
                    break;
                case "single":
                    simpleType = SimpleDataType.Single;
                    break;
                case "double":
                    simpleType = SimpleDataType.Double;
                    break;
                case "float":
                    simpleType = SimpleDataType.Float;
                    break;
                case "guid":
                    simpleType = SimpleDataType.Guid;
                    break;
                case "bool":
                case "boolean":
                    simpleType = SimpleDataType.Boolean;
                    break;
                case "datetime":
                case "datetimeoffset":
                case "timestamp":
                    simpleType = SimpleDataType.DateTimeOffset;
                    break;
                case "timeofday":
                    simpleType = SimpleDataType.TimeOfDay;
                    break;
                case "date":
                    simpleType = SimpleDataType.Date;
                    break;
                case "duration":
                    simpleType = SimpleDataType.Duration;
                    break;
                case "etag":
                case "range":
                case "url":
                    simpleType = SimpleDataType.String;
                    break;
                case "stream":
                    simpleType = SimpleDataType.Stream;
                    break;
                case "binary":
                    simpleType = SimpleDataType.Binary;
                    break;
                case "byte":
                    simpleType = SimpleDataType.Byte;
                    break;
            }

            if (lowercaseString.Contains("timestamp"))
            {
                return SimpleDataType.DateTimeOffset;
            }

            // Check to see if this looks like an ISO 8601 date and call it DateTimeOffset if it does
            var parsedDate = lowercaseString.ToUpperInvariant().TryParseIso8601Date();
            if (parsedDate.HasValue)
            {
                return SimpleDataType.DateTimeOffset;
            }

            // Check to see if it looks like a time of day
            parsedDate = lowercaseString.TryParseTimeOfDay();
            if (parsedDate.HasValue)
            {
                return SimpleDataType.TimeOfDay;
            }

            // Check to see if this can be parsed as a GUID
            Guid testguid;
            if (Guid.TryParse(lowercaseString, out testguid))
            {
                return SimpleDataType.Guid;
            }

            // check to see if it's a base64 example string
            if (lowercaseString.StartsWith("base64"))
            {
                return SimpleDataType.Binary;
            }

            return simpleType;
        }

        public static bool? IsRequired(this string description)
        {
            if (null == description)
                return null;
            if (description.StartsWith("required.", StringComparison.OrdinalIgnoreCase))
                return true;

            return null;
        }

        public static bool? IsOptional(this string description)
        {
            if (description?.StartsWith("optional", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            return null;
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

        public static int? ToInt32(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            int output;
            if (int.TryParse(input, out output))
            {
                return output;
            }

            throw new NotSupportedException($"Couldn't convert '{input}' to an Int32");
        }

        public static Regex PathVariableRegex = new Regex(@"\{(?<var>.+?)\}", RegexOptions.Compiled);

        public static string FlattenVariableNames(this string input)
        {
            return PathVariableRegex.Replace(input, "{}");
        }

        /// <summary>
        /// Returns the string between the first and second characters from the source.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static string TextBetweenCharacters(this string source, char first, char second)
        {
            if (source == null)
                return null;
            int startIndex = source.IndexOf(first);
            if (startIndex == -1)
                return source;

            int endIndex = source.IndexOf(second, startIndex+1);
            if (endIndex == -1)
                return source.Substring(startIndex+1);
            else
                return source.Substring(startIndex+1, endIndex - startIndex - 1);
        }

        public static string TextBetweenCharacters(this string source, char character)
        {
            return TextBetweenCharacters(source, character, character);
        }

        /// <summary>
        /// Replaces the text between first and second characters with the value of replacement. Does this for all instances of the first/second in the string.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="replacement"></param>
        /// <returns></returns>
        public static string ReplaceTextBetweenCharacters(
            this string source,
            char first,
            char second,
            string replacement,
            bool requireSecondChar = true, 
            bool removeTargetChars = false)
        {
            StringBuilder output = new StringBuilder(source);
            for (int i = 0; i < output.Length; i++)
            {
                if (output[i] == first)
                {
                    bool foundLastChar = false;
                    int j = i + 1;
                    for (; j < output.Length; j++)
                    {
                        if (output[j] == second)
                        {
                            foundLastChar = true;
                            break;
                        }
                    }
                    if (foundLastChar || !requireSecondChar)
                    {
                        if (removeTargetChars)
                        {
                            output.Remove(i, j - i + (foundLastChar ? 1 : 0));
                            output.Insert(i, replacement);
                        }
                        else
                        {
                            output.Remove(i + 1, j - i - (foundLastChar ? 1 : 0));
                            output.Insert(i + 1, replacement);
                        }
                        
                        i += replacement.Length;
                    }
                }
            }

            return output.ToString();
        }


        internal static ExpectedStringFormat StringFormat(this ParameterDefinition param)
        {
            if (param.Type != ParameterDataType.String)
                return ExpectedStringFormat.Generic;

            if (param.OriginalValue == "timestamp" || param.OriginalValue == "datetime" || param.OriginalValue.Contains("timestamp") )
                return ExpectedStringFormat.Iso8601Date;
            if (param.OriginalValue == "url" || param.OriginalValue == "absolute url")
                return ExpectedStringFormat.AbsoluteUrl;
            if (param.OriginalValue.IndexOf('|') > 0)
                return ExpectedStringFormat.EnumeratedValue;

            return ExpectedStringFormat.Generic;
        }

        public static string[] PossibleEnumValues(this ParameterDefinition param)
        {
            if (param.Type != ParameterDataType.String)
            {
                throw new InvalidOperationException("Cannot provide possible enum values on non-string data types");
            }

            if (param.OriginalValue == null)
            {
                return new string[0];
            }

            string[] possibleValues = param.OriginalValue.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            return (from v in possibleValues select v.Trim()).ToArray();
        }

        public static bool IsValidEnumValue(this ParameterDefinition param, string input)
        {
            string[] values = param.PossibleEnumValues();
            if (null != values && values.Length > 0)
            {
                return values.Contains(input);
            }
            return false;
        }

        public static bool IsLikelyBase64Encoded(this string input)
        {
            if (likelyBase64Regex.IsMatch(input))
            {
                if (input.Contains("=") || input.Any(char.IsDigit))
                {
                    return true;
                }

                // even though it's base-64 decodable,
                // the fact that it only contains text might mean that it's a real
                // function name, like 'getByIds'
                return false;
            }

            return false;
        }

        public static DateTimeOffset? TryParseIso8601Date(this string input)
        {
            DateTimeOffset value;
            if (DateTimeOffset.TryParseExact(input, Iso8601Formats, DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.None, out value))
            {
                return value;
            }

            return null;
        }

        public static DateTimeOffset? TryParseTimeOfDay(this string input)
        {
            DateTimeOffset value;
            if (DateTimeOffset.TryParseExact(input, TimeOfDayFormats, DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.None, out value))
            {
                return value;
            }

            return null;
        }

        /// <summary>
        /// Checks to see if a collection of errors already includes a similar error (matching Code + Message string)
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static bool ContainsSimilarError(this IEnumerable<ValidationError> collection, ValidationError error)
        {
            return collection.Any(x => x.Code == error.Code && x.Message == error.Message);
        }

        public static void AddUniqueErrors(
            this List<ValidationError> collection,
            IEnumerable<ValidationError> errorsToEvaluate)
        {
            foreach (var error in errorsToEvaluate)
            {
                if (!collection.ContainsSimilarError(error))
                {
                    collection.Add(error);
                }
                else
                {
                    if (!collection.Any(x => x.Code == ValidationErrorCode.SkippedSimilarErrors))
                    {
                        ValidationWarning skippedSimilarErrors =
                            new ValidationWarning(
                                ValidationErrorCode.SkippedSimilarErrors,
                                null,
                                null,
                                "Similar errors were skipped.");
                        collection.Add(skippedSimilarErrors);
                    }

                }
            }
        }
    }

    public enum ExpectedStringFormat
    {
        Generic,
        Iso8601Date,
        AbsoluteUrl,
        EnumeratedValue
    }
}
