﻿/*
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

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ApiDoctor.Validation.Error;
using System.Text.RegularExpressions;
using MarkdownDeep;
using System.Collections.Generic;

namespace ApiDoctor.Validation.Tags
{
    public class TagProcessor
    {
        private string[] TagsToInclude = null;
        private string DocSetRoot = null;
        private static string[] tagSeparators = { ",", " " };
        private static string[] docFxAlerts = new[] { "NOTE", "TIP", "IMPORTANT", "CAUTION", "WARNING" };

        private static Regex ValidTagFormat = new Regex(@"^\[TAGS=[-\.\w]+(?:,\s?[-\.\w]*)*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex GetTagList = new Regex(@"\[TAGS=([-\.,\s\w]+)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex IncludeFormat = new Regex(@"\[!INCLUDE\s*\[[-/.\w]+\]\(([-/.\w]+)\)\s*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex AlertFormat = new Regex(@"\[(!NOTE|!TIP|!IMPORTANT|!CAUTION|!WARNING)\s*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex DivFormat = new Regex(@"\[(!div (([\w]*=""[\w]*"")\s*)*)\s*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex VideoFormat = new Regex(@"\[!VIDEO ((https]?):\/)?\/?([^:\/\s]+)((\/\w+)*\/)([\w\-\.]+[^#?\s]+)(.*)?(#[\w\-]+)?\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex CodeSnippetFormat = new Regex(@"\[!(code)(-)(\w*)\[(\w*)\]\((.*\))\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private Action<ValidationError> LogMessage = null;

        public TagProcessor(string tags, string docSetRoot, Action<ValidationError> logMethod = null)
        {
            if (!string.IsNullOrEmpty(tags))
            {
                TagsToInclude = tags.ToUpper().Split(TagProcessor.tagSeparators,
                    StringSplitOptions.RemoveEmptyEntries);
            }

            DocSetRoot = docSetRoot;

            // If not logging method supplied default to a no-op
            LogMessage = logMethod ?? DefaultLogMessage;
        }

        /// <summary>
        /// Loads Markdown content from a file and removes unwanted content in preparation for passing to MarkdownDeep converter.
        /// </summary>
        /// <param name="sourceFile">The file containing the Markdown contents to preprocess.</param>
        /// <returns>The preprocessed contents of the file.</returns>
        public string Preprocess(FileInfo sourceFile)
        {
            using (var writer = new StringWriter())
            using (var reader = new StreamReader(sourceFile.OpenRead()))
            {
                long lineNumber = 0;
                var tagCount = 0;
                var dropCount = 0;
                string nextLine;

                while ((nextLine = reader.ReadLine()) != null)
                {
                    lineNumber++;

                    // Check if this is an [END] marker
                    if (IsEndLine(nextLine))
                    {
                        // We SHOULD be in a tag
                        if (tagCount > 0)
                        {
                            if (dropCount <= 0)
                            {
                                // To keep output clean if author did not insert blank line before
                                writer.WriteLine("");
                                writer.WriteLine(nextLine);
                            }
                            else
                            {
                                dropCount--;
                            }

                            // Decrement tag count
                            tagCount--;
                        }
                        else
                        {
                            LogMessage(new ValidationError(ValidationErrorCode.MarkdownParserError,
                                string.Concat(sourceFile.Name, ":", lineNumber), null, "Unexpected [END] marker."));
                        }

                        continue;
                    }

                    // Check if this is a [TAGS] marker
                    if (IsTagLine(nextLine, sourceFile.Name, lineNumber))
                    {
                        var tags = GetTags(nextLine);

                        LogMessage(new ValidationMessage(string.Concat(sourceFile.Name, ":", lineNumber), null, "Found TAGS line with {0}", string.Join(",", tags)));

                        tagCount++;

                        if (dropCount > 0 || !TagsAreIncluded(tags))
                        {
                            dropCount++;
                        }

                        if (dropCount == 1)
                        {
                            LogMessage(new ValidationMessage(string.Concat(sourceFile.Name, ":", lineNumber), null,
                                "{0} not found in the specified tags to include, content will be dropped.", string.Join(",", tags)));
                        }
                        else if (dropCount > 1)
                        {
                            LogMessage(new ValidationMessage(string.Concat(sourceFile.Name, ":", lineNumber), null,
                                "Dropping content due to containing [TAGS]"));
                        }
                        else
                        {
                            // Keep line
                            writer.WriteLine(nextLine);
                            // To keep output clean if author did not insert blank line after
                            writer.WriteLine("");
                        }

                        continue;
                    }

                    if (tagCount > 0 && dropCount > 0)
                    {
                        // Inside of a tag that shouldn't be included
                        LogMessage(new ValidationMessage(string.Concat(sourceFile.Name, ":", lineNumber), null, "Removing tagged content"));
                        continue;
                    }

                    // Remove double blockquotes (">>")
                    if (IsDoubleBlockQuote(nextLine))
                    {
                        LogMessage(new ValidationMessage(string.Concat(sourceFile.Name, ":", lineNumber), null, "Removing DoubleBlockQuote"));
                        continue;
                    }

                    // Import include file content
                    if (IsIncludeLine(nextLine))
                    {
                        var includeFile = GetIncludeFile(nextLine, sourceFile);
                        if (!includeFile.Exists)
                        {
                            LogMessage(new ValidationError(ValidationErrorCode.ErrorOpeningFile, nextLine, null, "The included file {0} was not found", includeFile.FullName));
                            continue;
                        }

                        if (includeFile != null)
                        {
                            if (includeFile.FullName.Equals(sourceFile.FullName))
                            {
                                LogMessage(new ValidationError(ValidationErrorCode.MarkdownParserError, nextLine, null, "A Markdown file cannot include itself"));
                                continue;
                            }
                            var includeContent = Preprocess(includeFile);

                            writer.WriteLine(includeContent);
                        }
                        else
                        {
                            LogMessage(new ValidationError(ValidationErrorCode.ErrorReadingFile, nextLine, null, "Could not load include content from {0}", includeFile.FullName));
                        }

                        continue;
                    }
                    if (IsAlertLine(nextLine))
                    {
                        LogMessage(new ValidationMessage(string.Concat(sourceFile.Name, ":", lineNumber), null, "Removing docfx Alerts"));
                        continue;
                    }

                    if (IsDocFxDivLine(nextLine))
                    {
                        LogMessage(new ValidationMessage(string.Concat(sourceFile.Name, ":", lineNumber), null, "Removing docfx Div"));
                        continue;
                    }
                    if (IsDocFxVideoLine(nextLine))
                    {
                        LogMessage(new ValidationMessage(string.Concat(sourceFile.Name, ":", lineNumber), null, "Removing docfx Video"));
                        continue;
                    }

                    if (IsDocFxCodeSnippet(nextLine))
                    {
                        LogMessage(new ValidationMessage(string.Concat(sourceFile.Name, ":", lineNumber), null, "Removing docfx code snippet"));
                        continue;
                    }

                    writer.WriteLine(nextLine);
                }

                if (tagCount > 0)
                {
                    // If inTag is true, there was a missing [END] tag somewhere
                    LogMessage(new ValidationError(ValidationErrorCode.MarkdownParserError,
                        sourceFile.Name, null, "The file ended while still in a [TAGS] tag. All [TAGS] must be closed with an [END] tag."));
                }

                return writer.ToString();
            }
        }

        /// <summary>
        /// Loads HTML from MarkdownDeep conversion process and replaces tags with &lt;div&gt; markers.
        /// </summary>
        /// <param name="html">The HTML content returned from MarkdownDeep.</param>
        /// <param name="sourceDirectory">The original Markdown file.</param>
        /// <param name="converter">The Markdown object to use for converting include files.</param>
        /// <returns>The postprocessed HTML content.</returns>
        public string PostProcess(string html, FileInfo sourceFile, Markdown converter)
        {
            var writer = new StringWriter();
            var reader = new StringReader(html);

            // Checks for closed tag and nesting were handled in preprocessing,
            // so not repeating them here

            string nextLine;
            while ((nextLine = reader.ReadLine()) != null)
            {
                // Replace with <div class="content-<tag>">
                if (IsConvertedTagLine(nextLine))
                {
                    var tags = GetTags(nextLine);
                    writer.WriteLine(GetDivMarker(tags));
                    continue;
                }

                // Replace with </div>
                if (IsConvertedEndLine(nextLine))
                {
                    writer.WriteLine(GetEndDivMarker());
                    continue;
                }

                writer.WriteLine(nextLine);
            }

            return writer.ToString();
        }

        private bool IsDoubleBlockQuote(string text)
        {
            return text.StartsWith(">>") || text.StartsWith(" >>");
        }

        private bool IsTagLine(string text, string fileName, long lineNumber)
        {
            var looksLikeTag = text.Trim().ToUpper().StartsWith("[TAGS=");

            if (!looksLikeTag) return false;

            // It looks like a tag, but is it legit?
            if (!ValidTagFormat.IsMatch(text.Trim()))
            {
                LogMessage(new ValidationError(ValidationErrorCode.MarkdownParserError,
                    string.Concat(fileName, ":", lineNumber), null, "Invalid TAGS line detected, ignoring..."));
                return false;
            }

            return true;
        }

        private string[] GetTags(string text)
        {
            var m = GetTagList.Match(text.Trim());

            if (m.Success && m.Groups.Count == 2)
            {
                return m.Groups[1].Value.ToUpper().Split(TagProcessor.tagSeparators,
                    StringSplitOptions.RemoveEmptyEntries);
            }

            return null;
        }

        private bool TagsAreIncluded(string[] tags)
        {
            if (TagsToInclude == null)
            {
                return false;
            }

            foreach (var tag in tags)
            {
                // If any tag matches included tags, return true
                if (TagsToInclude.Contains(tag))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsEndLine(string text)
        {
            return text.Trim().ToUpper().Equals("[END]");
        }

        private bool IsIncludeLine(string text)
        {
            if (text.ToUpper().Contains("INCLUDE"))
            {
                return IncludeFormat.IsMatch(text);
            }

            return false;
        }

        private bool IsConvertedTagLine(string text)
        {
            // To handle edge case where you have a [TAGS] type entry inside
            // a code block.
            if (!text.StartsWith("<p>"))
            {
                return false;
            }

            return ValidTagFormat.IsMatch(text.Replace("<p>", "").Replace("</p>", ""));
        }

        private bool IsConvertedEndLine(string text)
        {
            return text.Equals("<p>[END]</p>");
        }
        /// <summary>
        /// Checks if the line contains any of the docfx alert syntax items. 
        /// </summary>
        /// <param name="text">line to process</param>
        /// <returns>true when there is a match otherwise false.</returns>
        private static bool IsAlertLine(string text)
        {
            var upperNextLine = text.ToUpper();
            return docFxAlerts.Any(alert => upperNextLine.Contains(alert) && AlertFormat.IsMatch(text));
        }
        /// <summary>
        /// Checks if the line contains any of the docfx div syntax items. 
        /// </summary>
        /// <param name="text">line to process</param>
        /// <returns>true when there is a match otherwise false.</returns>
        private static bool IsDocFxDivLine(string text)
        {
            if (text.Contains("div"))
            {
                return DivFormat.IsMatch(text);
            }

            return false;
        }
        /// <summary>
        /// Checks if the line contains any of the docfx video syntax items.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static bool IsDocFxVideoLine(string text)
        {
            var upperNextLine = text.ToUpper();
            if (text.ToUpper().Contains("VIDEO"))
            {
                return VideoFormat.IsMatch(text);
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static bool IsDocFxCodeSnippet(string text)
        {
            if (text.Contains("code"))
            {
                return CodeSnippetFormat.IsMatch(text);
            }

            return false;
        }
        private FileInfo GetIncludeFile(string text, FileInfo sourceFile)
        {
            var m = IncludeFormat.Match(text);

            if (m.Success && m.Groups.Count == 2)
            {
                var relativePath = Path.ChangeExtension(m.Groups[1].Value, "md");

                var fullPathToIncludeFile = string.Empty;

                if (Path.IsPathRooted(relativePath))
                {
                    // Path is relative to the root of the doc set
                    relativePath = relativePath.TrimStart('/');
                    fullPathToIncludeFile = Path.Combine(DocSetRoot, relativePath);
                }
                else
                {
                    fullPathToIncludeFile = Path.Combine(sourceFile.Directory.FullName, relativePath);
                }

                return new FileInfo(fullPathToIncludeFile);
            }

            return null;
        }

        private string GetDivMarker(string[] tags)
        {
            for (var i = 0; i < tags.Length; i++)
            {
                tags[i] = string.Format("content-{0}", tags[i].ToLower().Replace('.', '-'));
            }

            return string.Format("<div class=\"{0}\">", string.Join(" ", tags));
        }

        private string GetEndDivMarker()
        {
            return "</div>";
        }

        private void DefaultLogMessage(ValidationError msg)
        {
            // Empty method
        }
    }

    //public static class TagProcessorExtensions
    //{
    //    public static T ValueForKey<T>(this Dictionary<string, T> source, string key, StringComparison comparison)
    //    {
    //        T value;
    //        if (source.TryGetValueForKey(key, comparison, out value))
    //            return (T)value;

    //        return default(T);
    //    }

    //    public static bool TryGetValueForKey<T>(this Dictionary<string, T> source, string key, StringComparison comparison, out T value)
    //    {
    //        if (source != null)
    //        {
    //            string keyName = source.Keys.Where(x => x.Equals(key, comparison)).FirstOrDefault();
    //            if (null != keyName)
    //            {
    //                value = (T)source[keyName];
    //                return true;
    //            }
    //        }

    //        value = default(T);
    //        return false;
    //    }

    //    public static bool TryGetValueForKey(this Dictionary<string, object> source, string key, StringComparison comparison, out object value)
    //    {
    //        return source.TryGetValueForKey<object>(key, comparison, out value);
    //    }

    //    public static void SetValueForKey<T>(this Dictionary<string, T> source, string key, StringComparison comparison, T value)
    //    {
    //        if (source == null)
    //            throw new ArgumentNullException("source");
    //        string keyName = source.Keys.Where(x => x.Equals(key, comparison)).FirstOrDefault();
    //        if (null != keyName)
    //        {
    //            source[keyName] = value;
    //        }
    //        else
    //        {
    //            source[key] = value;
    //        }
    //    }

    //}
}
