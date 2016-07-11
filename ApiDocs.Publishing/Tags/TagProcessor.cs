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

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ApiDocs.Validation.Error;
using System.Text.RegularExpressions;

namespace ApiDocs.Publishing.Tags
{
    class TagProcessor
    {
        private string[] TagsToInclude = null;
        private static string[] tagSeparators = { ",", " " };

        private static Regex ValidTagFormat = new Regex(@"^\[TAGS=[-\.\w]+(?:,\s?[-\.\w]*)*\]", RegexOptions.IgnoreCase);
        private static Regex GetTagList = new Regex(@"\[TAGS=([-\.,\s\w]+)\]", RegexOptions.IgnoreCase);

        private Action<ValidationError> LogMessage = null;

        public TagProcessor(string tags, Action<ValidationError> logMethod = null)
        {
            if (!string.IsNullOrEmpty(tags))
            {
                TagsToInclude = tags.ToUpper().Split(TagProcessor.tagSeparators,
                    StringSplitOptions.RemoveEmptyEntries);
            }

            // If not logging method supplied default to a no-op
            LogMessage = logMethod ?? DefaultLogMessage;
        }

        /// <summary>
        /// Loads Markdown content from a file and removes unwanted content in preparation for passing to MarkdownDeep converter.
        /// </summary>
        /// <param name="sourceFile">The file containing the Markdown contents to preprocess.</param>
        /// <returns>The preprocessed contents of the file.</returns>
        public async Task<string> Preprocess(FileInfo sourceFile)
        {   
            StringWriter writer = new StringWriter();
            StreamReader reader = new StreamReader(sourceFile.OpenRead());

            long lineNumber = 0;
            bool inTag = false;
            bool dropTagContent = false;
            string nextLine;
            while ((nextLine = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;

                // Check if this is an [END] marker
                if (IsEndLine(nextLine))
                {
                    // We SHOULD be in a tag
                    if (inTag)
                    {
                        if (!dropTagContent)
                        {
                            await writer.WriteLineAsync(nextLine);
                        }

                        // Reset tag state
                        inTag = false;
                        dropTagContent = false;
                    }
                    else
                    {
                        LogMessage(new ValidationError(ValidationErrorCode.MarkdownParserError,
                            string.Concat(sourceFile.Name, ":", lineNumber), "Unexpected [END] marker."));
                    }

                    continue;
                }

                if (inTag && dropTagContent)
                {
                    // Inside of a tag that shouldn't be included
                    LogMessage(new ValidationMessage(string.Concat(sourceFile.Name, ":", lineNumber), "Removing tagged content"));
                    continue;
                }

                // Remove double blockquotes (">>")
                if (IsDoubleBlockQuote(nextLine))
                {
                    LogMessage(new ValidationMessage(string.Concat(sourceFile.Name, ":", lineNumber), "Removing DoubleBlockQuote"));
                    continue;
                }

                // Check if this is a [TAGS] marker
                if (IsTagLine(nextLine, sourceFile.Name, lineNumber))
                {
                    if (inTag)
                    {
                        // Nested tags not allowed
                        LogMessage(new ValidationError(ValidationErrorCode.MarkdownParserError,
                            string.Concat(sourceFile.Name, ":", lineNumber), "Nested tags are not supported."));
                    }

                    string[] tags = GetTags(nextLine);

                    LogMessage(new ValidationMessage(string.Concat(sourceFile.Name, ":", lineNumber), "Found TAGS line with {0}", string.Join(",", tags)));

                    inTag = true;
                    dropTagContent = !TagsAreIncluded(tags);
                    if (dropTagContent)
                    {
                        LogMessage(new ValidationMessage(string.Concat(sourceFile.Name, ":", lineNumber), "{0} are not found in the specified tags to include, content will be dropped.", string.Join(",", tags)));
                    }
                    else
                    {
                        // Replace line with div
                        await writer.WriteLineAsync(nextLine);
                    }

                    continue;
                }

                await writer.WriteLineAsync(nextLine);
            }

            if (inTag)
            {
                // If inTag is true, there was a missing [END] tag somewhere
                LogMessage(new ValidationError(ValidationErrorCode.MarkdownParserError,
                    sourceFile.Name, "The file ended while still in a [TAGS] tag. All [TAGS] must be closed with an [END] tag."));
            }

            return writer.ToString();
        }

        /// <summary>
        /// Loads HTML from MarkdownDeep conversion process and replaces tags with &lt;div&gt; markers.
        /// </summary>
        /// <param name="html">The HTML content returned from MarkdownDeep.</param>
        /// <returns>The postprocessed HTML content.</returns>
        public async Task<string> PostProcess(string html)
        {
            StringWriter writer = new StringWriter();
            StringReader reader = new StringReader(html);

            // Checks for closed tag and nesting were handled in preprocessing,
            // so not repeating them here

            string nextLine;
            while ((nextLine = await reader.ReadLineAsync()) != null)
            {
                // Replace with <div class="content-<tag>">
                if (IsConvertedTagLine(nextLine))
                {
                    string[] tags = GetTags(nextLine);
                    await writer.WriteLineAsync(GetDivMarker(tags));
                    continue;
                }

                // Replace with </div>
                if (IsConvertedEndLine(nextLine))
                {
                    await writer.WriteLineAsync(GetEndDivMarker());
                    continue;
                }

                await writer.WriteLineAsync(nextLine);
            }

            return writer.ToString();
        }

        private bool IsDoubleBlockQuote(string text)
        {
            return text.StartsWith(">>") || text.StartsWith(" >>");
        }

        private bool IsTagLine(string text, string fileName, long lineNumber)
        {
            bool looksLikeTag = text.Trim().ToUpper().StartsWith("[TAGS=");

            if (!looksLikeTag) return false;

            // It looks like a tag, but is it legit?
            if(!ValidTagFormat.IsMatch(text.Trim()))
            {
                LogMessage(new ValidationError(ValidationErrorCode.MarkdownParserError,
                    string.Concat(fileName, ":", lineNumber), "Invalid TAGS line detected, ignoring..."));
                return false;
            }

            return true;
        }

        private string[] GetTags(string text)
        {
            Match m = GetTagList.Match(text.Trim());

            if (m.Success && m.Groups.Count == 2)
            {
                return m.Groups[1].Value.Split(TagProcessor.tagSeparators,
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

            foreach (string tag in tags)
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

        private bool IsConvertedTagLine(string text)
        {
            return ValidTagFormat.IsMatch(text.Replace("<p>", "").Replace("</p>", ""));
        }

        private bool IsConvertedEndLine(string text)
        {
            return text.Equals("<p>[END]</p>");
        }

        private string GetDivMarker(string[] tags)
        {
            for (int i = 0; i < tags.Length; i++)
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
}
