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

namespace ApiDoctor.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using ApiDoctor.Validation.Config;
    using ApiDoctor.Validation.Error;
    using ApiDoctor.Validation.TableSpec;
    using Tags;
    using MarkdownDeep;
    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    /// <summary>
    /// A documentation file that may contain one more resources or API methods
    /// </summary>
    public partial class DocFile
    {
        private const string PageAnnotationType = "#page.annotation";
        private readonly List<ResourceDefinition> resources = new List<ResourceDefinition>();
        private readonly List<MethodDefinition> requests = new List<MethodDefinition>();
        private readonly List<ExampleDefinition> examples = new List<ExampleDefinition>();
        private readonly List<SamplesDefinition> samples = new List<SamplesDefinition>();
        private readonly List<EnumerationDefinition> enums = new List<EnumerationDefinition>();
        private readonly List<string> bookmarks = new List<string>();

        private static readonly IDeserializer yamlDeserializer = new DeserializerBuilder().Build();

        protected bool HasScanRun;
        protected string BasePath;

        #region Properties
        /// <summary>
        /// Friendly name of the file
        /// </summary>
        public string DisplayName
        {
            get; protected set;
        }

        /// <summary>
        /// Path to the file on disk
        /// </summary>
        public string FullPath
        {
            get; protected set;
        }

        ///// <summary>
        ///// HTML-rendered version of the markdown source (for displaying). This content is not suitable for publishing.
        ///// </summary>
        //public string HtmlContent { get; protected set; }

        /// <summary>
        /// Contains information on the headers and content blocks found in this document.
        /// </summary>
        public List<string> ContentOutline
        {
            get; set;
        }

        public PageType DocumentPageType { get; protected set; } = PageType.Unknown;

        public ResourceDefinition[] Resources
        {
            get
            {
                return this.resources.ToArray();
            }
        }

        public MethodDefinition[] Requests
        {
            get
            {
                return this.requests.ToArray();
            }
        }

        public ExampleDefinition[] Examples
        {
            get
            {
                return this.examples.ToArray();
            }
        }

        public SamplesDefinition[] Samples
        {
            get
            {
                return this.samples.ToArray();
            }
        }

        public EnumerationDefinition[] Enums
        {
            get
            {
                return this.enums.ToArray();
            }
        }

        public AuthScopeDefinition[] AuthScopes
        {
            get; protected set;
        }

        public ErrorDefinition[] ErrorCodes
        {
            get; protected set;
        }

        public string[] LinkDestinations
        {
            get
            {
                if (this.MarkdownLinks == null)
                {
                    return new string[0];
                }

                var destinations = new List<string>(this.MarkdownLinks.Count);
                foreach (var link in this.MarkdownLinks)
                {
                    if (link.Definition == null)
                    {
                        throw new ArgumentException("Link Definition was null. Link text: " + link.Text, nameof(link.Definition));
                    }

                    destinations.Add(link.Definition.url);
                }

                return destinations.ToArray();
            }
        }

        /// <summary>
        /// Raw Markdown parsed blocks
        /// </summary>
        public Block[] OriginalMarkdownBlocks
        {
            get; set;
        }

        protected List<ILinkInfo> MarkdownLinks
        {
            get; set;
        }

        public DocSet Parent
        {
            get; protected set;
        }

        public PageAnnotation Annotation
        {
            get; set;
        }

        public bool WriteFixesBackToDisk
        {
            get; set;
        }

        public string Namespace
        {
            get; set;
        }
        #endregion

        #region Constructor
        protected DocFile()
        {
            this.ContentOutline = new List<string>();
            this.DocumentHeaders = new List<DocumentHeader>();
        }

        public DocFile(string basePath, string relativePath, DocSet parent)
            : this()
        {
            this.BasePath = basePath;
            this.FullPath = Path.Combine(basePath, relativePath.Substring(1));
            this.DisplayName = relativePath;
            this.Parent = parent;
            this.DocumentHeaders = new List<DocumentHeader>();
        }

        #endregion

        #region Markdown Parsing

        /// <summary>
        /// Populate this object based on input markdown data
        /// </summary>
        /// <param name="inputMarkdown"></param>
        protected void TransformMarkdownIntoBlocksAndLinks(string inputMarkdown, string tags)
        {
            Markdown md = new Markdown
            {
                SafeMode = false,
                ExtraMode = true,
                AutoHeadingIDs = true,
                NewWindowForExternalLinks = true
            };

            var htmlContent = md.Transform(inputMarkdown);
            //this.HtmlContent = PostProcessHtmlTags(htmlContent, tags);
            this.OriginalMarkdownBlocks = md.Blocks;
            this.MarkdownLinks = new List<ILinkInfo>(md.FoundLinks);
            this.bookmarks.AddRange(md.HeaderIdsInDocument);
        }

        protected string GetBlockContent(Block block)
        {
            try
            {
                if (block.Content == null)
                    return string.Empty;
                return block.Content;
            }
            catch
            {
                return string.Empty;
            }
        }

        protected virtual string PostProcessHtmlTags(string inputHtml, string tags)
        {
            TagProcessor tagProcessor = new TagProcessor(tags, this.Parent?.SourceFolderPath);
            var fileInfo = new FileInfo(this.FullPath);
            return tagProcessor.PostProcess(inputHtml, fileInfo, null);
        }

        protected virtual string GetContentsOfFile(string tags, IssueLogger issues = default)
        {
            _ = issues ?? throw new ArgumentNullException(nameof(issues));
            // Preprocess file content
            FileInfo docFile = new FileInfo(this.FullPath);
            TagProcessor tagProcessor = new TagProcessor(tags, Parent.SourceFolderPath);
            return tagProcessor.Preprocess(docFile, issues);
        }

        /// <summary>
        /// Read the contents of the file into blocks and generate any resource or method definitions from the contents
        /// </summary>
        public virtual bool Scan(string tags, IssueLogger issues)
        {
            this.HasScanRun = true;
            List<ValidationError> detectedErrors = new List<ValidationError>();

            try
            {
                string fileContents = this.ReadAndPreprocessFileContents(tags, issues);
                this.TransformMarkdownIntoBlocksAndLinks(fileContents, tags);
            }
            catch (IOException ioex)
            {
                issues.Error(ValidationErrorCode.ErrorOpeningFile, $"Error reading file contents.", ioex);
                return false;
            }
            catch (Exception ex)
            {
                issues.Error(ValidationErrorCode.ErrorReadingFile, $"Error reading file contents.", ex);
                return false;
            }

            return this.ParseMarkdownBlocks(issues);
        }

        /// <summary>
        /// Read the contents of the file and perform all preprocessing activities that transform the input into the final markdown-only data.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public string ReadAndPreprocessFileContents(string tags, IssueLogger issues)
        {
            try
            {
                string fileContents = this.GetContentsOfFile(tags, issues);
                var (yamlFrontMatter, processedContent) = ParseAndRemoveYamlFrontMatter(fileContents, issues);
                // Only Parse Yaml metadata if its present.
                if (!string.IsNullOrWhiteSpace(yamlFrontMatter))
                {
                    ParseYamlMetadata(yamlFrontMatter, issues);
                    SetDocumentTypeFromYamlMetadata(yamlFrontMatter);
                }
                return processedContent;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occuring reading and processing the contents of this file: {this.FullPath}. {ex.Message}.");
                throw;
            }
        }

        /// <summary>
        /// Parses the file contents and removes yaml front matter from the markdown.
        /// </summary>
        /// <param name="contents">Contents.</param>
        /// <param name="issues">The issue logger to use</param>
        /// <param name="isInclude">Whether the file contents is part of another file</param>
        internal static (string YamlFrontMatter, string ProcessedContent) ParseAndRemoveYamlFrontMatter(string contents, IssueLogger issues, bool isInclude = false)
        {
            const string yamlFrontMatterHeader = "---";
            using var reader = new StringReader(contents);
            var currentLine = reader.ReadLine();
            var frontMatter = new System.Text.StringBuilder();
            var currentState = YamlFrontMatterDetectionState.NotDetected;
            while (currentLine != null && currentState != YamlFrontMatterDetectionState.SecondTokenFound)
            {
                var trimmedCurrentLine = currentLine.Trim();
                switch (currentState)
                {
                    case YamlFrontMatterDetectionState.NotDetected:
                        if (!string.IsNullOrWhiteSpace(trimmedCurrentLine) && trimmedCurrentLine != yamlFrontMatterHeader)
                        {
                            var requiredYamlHeaders = DocSet.SchemaConfig.RequiredYamlHeaders;
                            if (requiredYamlHeaders.Any() && !isInclude)//include files don't need the headers
                            {
                                issues.Error(ValidationErrorCode.RequiredYamlHeaderMissing, $"Missing required YAML headers: {requiredYamlHeaders.ComponentsJoinedByString(", ")}");
                            }

                            // This file doesn't have YAML front matter, so we just return the full contents of the file
                            return (YamlFrontMatter: null, ProcessedContent: contents);
                        }
                        else if (trimmedCurrentLine == yamlFrontMatterHeader)
                        {
                            currentState = YamlFrontMatterDetectionState.FirstTokenFound;
                        }
                        break;
                    case YamlFrontMatterDetectionState.FirstTokenFound:
                        if (trimmedCurrentLine == yamlFrontMatterHeader)
                        {
                            // Found the end of the YAML front matter, so move to the final state
                            currentState = YamlFrontMatterDetectionState.SecondTokenFound;
                        }
                        else
                        {
                            // Store the YAML data into our header
                            frontMatter.AppendLine(currentLine);
                        }
                        break;

                    case YamlFrontMatterDetectionState.SecondTokenFound:
                        break;
                }

                if (currentState != YamlFrontMatterDetectionState.SecondTokenFound)
                {
                    currentLine = reader.ReadLine();
                }
            }

            if (currentState == YamlFrontMatterDetectionState.SecondTokenFound)
            {
                return (YamlFrontMatter: frontMatter.ToString(), ProcessedContent: reader.ReadToEnd());
            }
            else
            {
                // Something went wrong along the way, so we just return the full file
                return (YamlFrontMatter: null, ProcessedContent: contents);
            }
        }

        internal static void ParseYamlMetadata(string yamlMetadata, IssueLogger issues)
        {
            Dictionary<string, object> dictionary = null;
            try
            {
                dictionary = yamlDeserializer.Deserialize<Dictionary<string, object>>(yamlMetadata);
            }
            catch (Exception ex)
            {
                issues.Error(ValidationErrorCode.IncorrectYamlHeaderFormat, "Incorrect YAML header format", ex);
                return;
            }

            List<string> missingHeaders = new List<string>();
            foreach (var header in DocSet.SchemaConfig.RequiredYamlHeaders)
            {
                if (dictionary.TryGetValue(header, out object value) && value is string stringValue)
                {
                    stringValue = stringValue.Replace("\"", string.Empty);
                    if (string.IsNullOrWhiteSpace(stringValue))
                    {
                        issues.Warning(ValidationErrorCode.RequiredYamlHeaderMissing, $"Missing value for YAML header: {header}");
                    }
                }
                else
                {
                    missingHeaders.Add(header);
                }
            }

            if (missingHeaders.Any())
            {
                issues.Error(ValidationErrorCode.RequiredYamlHeaderMissing, $"Missing required YAML header(s): {missingHeaders.ComponentsJoinedByString(", ")}");
            }
        }

        /// <summary>
        /// Get document type from YAML front matter
        /// </summary>
        /// <returns></returns>
        private void SetDocumentTypeFromYamlMetadata(string yamlMetadata)
        {
            PageType pageType = PageType.Unknown;
            string[] items = yamlMetadata.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string item in items)
            {
                string[] keyValue = item.Split(':');
                if (keyValue.Length == 2 && keyValue[0].Trim() == "doc_type")
                {
                    Enum.TryParse(keyValue[1].Replace("\"", string.Empty), true, out pageType);
                    break; //no need to keep processing as we've found the doctype tag
                }
            }
            this.DocumentPageType = pageType;
        }

        private enum YamlFrontMatterDetectionState
        {
            NotDetected,
            FirstTokenFound,
            SecondTokenFound
        }

        public enum PageType
        {
            Unknown,
            ResourcePageType,
            ApiPageType,
            ConceptualPageType,
            EnumPageType
        }

        private static bool IsHeaderBlock(Block block, int maxDepth = 2)
        {
            if (null == block)
            {
                return false;
            }

            var blockType = block.BlockType;
            if (maxDepth >= 1 && blockType == BlockType.h1)
                return true;
            if (maxDepth >= 2 && blockType == BlockType.h2)
                return true;
            if (maxDepth >= 3 && blockType == BlockType.h3)
                return true;
            if (maxDepth >= 4 && blockType == BlockType.h4)
                return true;
            if (maxDepth >= 5 && blockType == BlockType.h5)
                return true;
            if (maxDepth >= 6 && blockType == BlockType.h6)
                return true;

            return false;
        }

        protected string PreviewOfBlockContent(Block block)
        {
            if (block == null) return string.Empty;
            var blockContent = GetBlockContent(block);
            if (blockContent == string.Empty)
                return blockContent;

            const int previewLength = 35;

            string contentPreview = block.Content.Length > previewLength ? block.Content.Substring(0, previewLength) : block.Content;
            contentPreview = contentPreview.Replace('\n', ' ').Replace('\r', ' ');
            return contentPreview;
        }

        protected static DocumentHeader CreateHeaderFromBlock(Block block)
        {
            var header = new DocumentHeader();
            switch (block.BlockType)
            {
                case BlockType.h1:
                    header.Level = 1; break;
                case BlockType.h2:
                    header.Level = 2; break;
                case BlockType.h3:
                    header.Level = 3; break;
                case BlockType.h4:
                    header.Level = 4; break;
                case BlockType.h5:
                    header.Level = 5; break;
                case BlockType.h6:
                    header.Level = 6; break;
                default:
                    throw new InvalidOperationException("block wasn't a header!");
            }
            header.Title = block.Content;
            return header;
        }

        /// <summary>
        /// Headers found in the markdown input (#, h1, etc)
        /// </summary>
        public List<DocumentHeader> DocumentHeaders
        {
            get; set;
        }

        /// <summary>
        /// Convert blocks of text found inside the markdown file into things we know how to work
        /// with (methods, resources, examples, etc).
        /// </summary>
        /// <param name="issues"></param>
        /// <returns></returns>
        protected bool ParseMarkdownBlocks(IssueLogger issues)
        {
            string methodTitle = null;
            string methodDescription = null;
            List<string> methodDescriptionsData = new List<string>();

            Block previousHeaderBlock = null;

            List<object> foundElements = new List<object>();

            Stack<DocumentHeader> headerStack = new Stack<DocumentHeader>();
            for (int i = 0; i < this.OriginalMarkdownBlocks.Length; i++)
            {
                var block = this.OriginalMarkdownBlocks[i];

                this.ContentOutline.Add(string.Format("{0} - {1}", block.BlockType, this.PreviewOfBlockContent(block)));

                // Capture GitHub Flavored Markdown Bookmarks
                if (IsHeaderBlock(block, 6))
                {
                    this.AddHeaderToHierarchy(headerStack, block);
                }

                // Capture h1 and/or p element to be used as the title and description for items on this page
                if (block.BlockType == BlockType.h1)
                {
                    methodTitle = block.Content;
                    methodDescription = null;       // Clear this because we don't want new title + old description
                    issues.Message($"Found title: {methodTitle}");
                }
                else if (block.BlockType == BlockType.p)
                {
                    if (null == previousHeaderBlock)
                    {
                        issues.Warning(ValidationErrorCode.MissingHeaderBlock,
                            $"Paragraph text found before a valid header: {block.Content.Substring(0, Math.Min(block.Content.Length, 20))}...");
                    }
                    else if (previousHeaderBlock.BlockType == BlockType.h1)
                    {
                        methodDescriptionsData.Add(block.Content);
                        // make sure we omit the namespace description as well as the national cloud deployments paragraphs
                        methodDescription = string.Join(" ", methodDescriptionsData.Where(static x => !x.StartsWith("Namespace:", StringComparison.OrdinalIgnoreCase) && !x.Contains("[national cloud deployments](/graph/deployments)", StringComparison.OrdinalIgnoreCase))).ToStringClean();
                        issues.Message($"Found description: {methodDescription}");
                    }
                }
                else if (block.BlockType == BlockType.codeblock && (block.CodeLanguage == "json" || block.CodeLanguage == "http"))
                {
                    //If the previous block is html (metadata)
                    var previousBlockIndex = i - 1;
                    if (previousBlockIndex >= 0)
                    {
                        var previousBlock = this.OriginalMarkdownBlocks[previousBlockIndex];
                        if (null != previousBlock && previousBlock.BlockType != BlockType.html)
                        {
                            issues.Error(ValidationErrorCode.MissingMetadataBlock,
                                $"Missing metadata for code block:{Environment.NewLine}{block}");
                        }
                    }
                }
                else if (block.BlockType == BlockType.html)
                {
                    // If the next block is a codeblock we've found a metadata + codeblock pair
                    Block nextBlock = null;
                    if (i + 1 < this.OriginalMarkdownBlocks.Length)
                    {
                        nextBlock = this.OriginalMarkdownBlocks[i + 1];
                    }

                    if (null != nextBlock && nextBlock.BlockType == BlockType.codeblock)
                    {
                        // html + codeblock = likely request or response or resource

                        List<ItemDefinition> definitions = this.ParseCodeBlock(block, nextBlock, issues);

                        if (definitions != null && definitions.Any())
                        {
                            foreach (var definition in definitions)
                            {
                                issues.Message($"Found code block: {definition.Title} [{definition.GetType().Name}]");
                                definition.Title = methodTitle;
                                definition.Description = methodDescription;

                                if (!foundElements.Contains(definition))
                                {
                                    foundElements.Add(definition);
                                }
                            }
                        }
                    }

                    // See if this is the page-level annotation
                    try
                    {
                        this.Annotation = this.ParsePageAnnotation(block);
                        if (this.Annotation != null)
                        {
                            if (this.Annotation.Suppressions != null)
                            {
                                issues.AddSuppressions(this.Annotation.Suppressions);
                            }

                            if (string.IsNullOrWhiteSpace(this.Annotation.Title))
                            {
                                this.Annotation.Title = this.OriginalMarkdownBlocks.FirstOrDefault(b => IsHeaderBlock(b, 1))?.Content;
                            }
                        }
                    }
                    catch (JsonReaderException readerEx)
                    {
                        issues.Warning(ValidationErrorCode.JsonParserException, $"Unable to parse page annotation JSON: {readerEx}");
                    }
                    catch (Exception ex)
                    {
                        issues.Warning(ValidationErrorCode.AnnotationParserException, $"Unable to parse annotation: {ex}");
                    }

                }
                else if (block.BlockType == BlockType.table_spec)
                {
                    try
                    {
                        Block blockBeforeTable = (i - 1 >= 0) ? this.OriginalMarkdownBlocks[i - 1] : null;
                        if (null == blockBeforeTable) continue;

                        var table = this.Parent.TableParser.ParseTableSpec(block, headerStack, issues);

                        issues.Message($"Found table: {table.Type}. Rows:\r\n{table.Rows.Select(r => JsonConvert.SerializeObject(r, Formatting.Indented)).ComponentsJoinedByString(" ,\r\n")}");

                        foundElements.Add(table);
                    }
                    catch (Exception ex)
                    {
                        issues.Error(ValidationErrorCode.MarkdownParserError, $"Failed to parse table.", ex);
                    }
                }

                if (block.IsHeaderBlock())
                {
                    previousHeaderBlock = block;
                }
            }

            this.InferNamespaceFromFoundElements(foundElements, issues);
            this.PostProcessFoundElements(foundElements, issues);

            return issues.Issues.All(x => !x.IsError);
        }

        /// <summary>
        /// Checks the document for outline errors compared to any required document structure.
        /// </summary>
        /// <returns></returns>
        public void CheckDocumentStructure(IssueLogger issues)
        {
            if (this.Parent.DocumentStructure != null)
            {
                var expectedHeaders = this.DocumentPageType switch
                {
                    PageType.ApiPageType => ExpectedDocumentHeader.CopyHeaders(this.Parent.DocumentStructure.ApiPageType),
                    PageType.ResourcePageType => ExpectedDocumentHeader.CopyHeaders(this.Parent.DocumentStructure.ResourcePageType),
                    PageType.EnumPageType => ExpectedDocumentHeader.CopyHeaders(this.Parent.DocumentStructure.EnumPageType),
                    PageType.ConceptualPageType => ExpectedDocumentHeader.CopyHeaders(this.Parent.DocumentStructure.ConceptualPageType),
                    _ => [],
                };
                if (expectedHeaders.Count != 0)
                {
                    CheckDocumentHeaders(expectedHeaders, this.DocumentHeaders, issues);
                }
            }
            ValidateTabStructure(issues);
        }

        private static bool ContainsMatchingDocumentHeader(DocumentHeader header, IReadOnlyList<DocumentHeader> headerCollection,
            bool ignoreCase = false, bool checkStringDistance = false)
        {
            return headerCollection.Any(h => h.Matches(header, ignoreCase, checkStringDistance));
        }

        /// <summary>
        /// Match headers found in doc against expected headers for doc type and report discrepancies
        /// </summary>
        /// <param name="expectedHeaders">Allowed headers to match against</param>
        /// <param name="foundHeaders">Headers to evaluate</param>
        /// <param name="issues"></param>
        private void CheckDocumentHeaders(List<object> expectedHeaders, IReadOnlyList<DocumentHeader> foundHeaders, IssueLogger issues)
        {
            int expectedIndex = 0;
            int foundIndex = 0;

            while (expectedIndex < expectedHeaders.Count && foundIndex < foundHeaders.Count)
            {
                var found = foundHeaders[foundIndex];
                var result = ValidateDocumentHeader(expectedHeaders, foundHeaders, expectedIndex, foundIndex);
                var expected = expectedHeaders[expectedIndex] as ExpectedDocumentHeader; // at this point, if header was conditional, the condition has been removed
                switch (result)
                {
                    case DocumentHeaderValidationResult.Found:
                        CheckDocumentHeaders(expected.ChildHeaders, found.ChildHeaders, issues);
                        foundIndex++;

                        //if expecting multiple headers of the same pattern, do not increment expected until last header matching pattern is found
                        if (!expected.AllowMultiple || (expected.AllowMultiple && foundIndex == foundHeaders.Count))
                            expectedIndex++;

                        break;

                    case DocumentHeaderValidationResult.FoundInWrongCase:
                        issues.Error(ValidationErrorCode.DocumentHeaderInWrongCase, $"Incorrect letter case in document header: {found.Title}");
                        expectedIndex++;
                        foundIndex++;
                        break;

                    case DocumentHeaderValidationResult.MisspeltDocumentHeader:
                        issues.Error(ValidationErrorCode.MisspeltDocumentHeader, $"Found header: {found.Title}. Did you mean: {expected.Title}?");
                        expectedIndex++;
                        foundIndex++;
                        break;

                    case DocumentHeaderValidationResult.MisspeltDocumentHeaderInWrongPosition:
                        issues.Error(ValidationErrorCode.MisspeltDocumentHeader, $"An expected document header (possibly misspelt) was found in the wrong position: {found.Title}");
                        foundIndex++;
                        break;

                    case DocumentHeaderValidationResult.ExtraDocumentHeaderFound:
                        issues.Warning(ValidationErrorCode.ExtraDocumentHeaderFound, $"An extra document header was found: {found.Title}");
                        foundIndex++;
                        break;

                    case DocumentHeaderValidationResult.DocumentHeaderInWrongPosition:
                        issues.Warning(ValidationErrorCode.DocumentHeaderInWrongPosition, $"An expected document header was found in the wrong position: {found.Title}");
                        foundIndex++;
                        break;

                    case DocumentHeaderValidationResult.RequiredDocumentHeaderMissing:
                        issues.Error(ValidationErrorCode.RequiredDocumentHeaderMissing, $"A required document header is missing from the document: {expected.Title}");
                        expectedIndex++;
                        break;

                    case DocumentHeaderValidationResult.OptionalDocumentHeaderMissing:
                        expectedIndex++;
                        break;

                    default:
                        break;

                }

                //if expecting multiple headers of the same pattern, increment expected when last header matching pattern is found
                if (expected.AllowMultiple && foundIndex == foundHeaders.Count)
                {
                    expectedIndex++;
                }
            }

            for (int i = foundIndex; i < foundHeaders.Count; i++)
            {
                issues.Warning(ValidationErrorCode.ExtraDocumentHeaderFound, $"An extra document header was found: {foundHeaders[i].Title}");
            }

            for (int i = expectedIndex; i < expectedHeaders.Count; i++)
            {
                ExpectedDocumentHeader missingHeader;
                if (expectedHeaders[i] is ExpectedDocumentHeader expectedMissingHeader)
                {
                    missingHeader = expectedMissingHeader;
                }
                else
                {
                    missingHeader = (expectedHeaders[i] as ConditionalDocumentHeader).Arguments.OfType<ExpectedDocumentHeader>().First();
                }

                if (!ContainsMatchingDocumentHeader(missingHeader, foundHeaders, true, true) && missingHeader.Required)
                {
                    issues.Error(ValidationErrorCode.RequiredDocumentHeaderMissing, $"A required document header is missing from the document: {missingHeader.Title}");
                }
            }
        }

        /// <summary>
        /// Validates a document header against the found headers.
        /// </summary>
        /// <param name="expectedHeaders">The list of expected headers, which may include conditional headers.</param>
        /// <param name="foundHeaders">The list of found headers.</param>
        /// <param name="expectedIndex">Index of the expected header being validated.</param>
        /// <param name="foundIndex">Index of the found header being compared.</param>
        /// <returns>The validation result.</returns>
        private DocumentHeaderValidationResult ValidateDocumentHeader(List<object> expectedHeaders, IReadOnlyList<DocumentHeader> foundHeaders, int expectedIndex, int foundIndex)
        {
            if (expectedHeaders[expectedIndex] is ConditionalDocumentHeader)
            {
                return ValidateConditionalDocumentHeader(expectedHeaders, foundHeaders, expectedIndex, foundIndex);
            }

            var found = foundHeaders[foundIndex];
            var expected = expectedHeaders[expectedIndex] as ExpectedDocumentHeader;

            if (expected.Matches(found))
            {
                return DocumentHeaderValidationResult.Found;
            }

            // Try case insensitive match
            if (expected.Matches(found, true))
            {
                return DocumentHeaderValidationResult.FoundInWrongCase;
            }

            // Check if header is misspelt
            if (expected.IsMisspelt(found))
            {
                return DocumentHeaderValidationResult.MisspeltDocumentHeader;
            }

            // Check if expected header is in the list of found headers
            if (!ContainsMatchingDocumentHeader(expected, foundHeaders, ignoreCase: true, checkStringDistance: true))
            {
                if (expected.Required)
                {
                    return DocumentHeaderValidationResult.RequiredDocumentHeaderMissing;
                }
                else
                {
                    return DocumentHeaderValidationResult.OptionalDocumentHeaderMissing;
                }
            }

            // Check if found header is in wrong position or is an extra header
            var mergedExpectedHeaders = FlattenDocumentHeaderHierarchy(expectedHeaders);
            if (ContainsMatchingDocumentHeader(found, mergedExpectedHeaders, ignoreCase: true))
            {
                return DocumentHeaderValidationResult.DocumentHeaderInWrongPosition;
            }
            else if (ContainsMatchingDocumentHeader(found, mergedExpectedHeaders, ignoreCase: true, checkStringDistance: true))
            {
                return DocumentHeaderValidationResult.MisspeltDocumentHeaderInWrongPosition;
            }
            else
            {
                return DocumentHeaderValidationResult.ExtraDocumentHeaderFound;
            }
        }

        /// <summary>
        /// Flattens a hierarchical structure of document headers into a single list.
        /// </summary>
        /// <param name="headers">The list of headers, which may contain nested conditional headers.</param>
        /// <returns>A flat list containing all document headers.</returns>
        private static List<DocumentHeader> FlattenDocumentHeaderHierarchy(IReadOnlyList<object> headers)
        {
            var mergedHeaders = new List<DocumentHeader>();
            foreach (var header in headers)
            {
                if (header is ExpectedDocumentHeader expectedHeader)
                {
                    mergedHeaders.Add(expectedHeader);
                }
                else if (header is ConditionalDocumentHeader conditionalHeader)
                {
                    mergedHeaders.AddRange(FlattenDocumentHeaderHierarchy(conditionalHeader.Arguments));
                }
            }
            return mergedHeaders;
        }

        /// <summary>
        /// Validates a conditional document header against the found headers.
        /// </summary>
        /// <param name="expectedHeaders">The list of expected headers.</param>
        /// <param name="foundHeaders">The list of found headers.</param>
        /// <param name="expectedIndex">Index of the expected header being validated.</param>
        /// <param name="foundIndex">Index of the found header being compared.</param>
        /// <returns>The validation result.</returns>
        private DocumentHeaderValidationResult ValidateConditionalDocumentHeader(List<object> expectedHeaders, IReadOnlyList<DocumentHeader> foundHeaders, int expectedIndex, int foundIndex)
        {
            var validationResult = DocumentHeaderValidationResult.None;
            var expectedConditionalHeader = expectedHeaders[expectedIndex] as ConditionalDocumentHeader;
            if (expectedConditionalHeader.Operator == ConditionalOperator.OR)
            {
                foreach (var header in expectedConditionalHeader.Arguments)
                {
                    // Replace conditional header with this argument for validation
                    expectedHeaders[expectedIndex] = header;
                    validationResult = ValidateDocumentHeader(expectedHeaders, foundHeaders, expectedIndex, foundIndex);

                    // If header has been found, stop looking
                    if (validationResult != DocumentHeaderValidationResult.RequiredDocumentHeaderMissing &&
                        validationResult != DocumentHeaderValidationResult.OptionalDocumentHeaderMissing)
                    {
                        break;
                    }
                }
            }
            else if (expectedConditionalHeader.Operator == ConditionalOperator.AND)
            {
                if (expectedConditionalHeader.Arguments != null && expectedConditionalHeader.Arguments.Count > 0)
                {
                    expectedHeaders[expectedIndex] = expectedConditionalHeader.Arguments.First();
                    expectedHeaders.InsertRange(expectedIndex + 1, expectedConditionalHeader.Arguments.Skip(1));

                    validationResult = ValidateDocumentHeader(expectedHeaders, foundHeaders, expectedIndex, foundIndex);
                }
                else
                {
                    validationResult = DocumentHeaderValidationResult.ExtraDocumentHeaderFound;
                }
            }
            return validationResult;
        }

        private enum DocumentHeaderValidationResult
        {
            None,
            Found,
            FoundInWrongCase,
            ExtraDocumentHeaderFound,
            RequiredDocumentHeaderMissing,
            OptionalDocumentHeaderMissing,
            DocumentHeaderInWrongPosition,
            MisspeltDocumentHeader,
            MisspeltDocumentHeaderInWrongPosition
        }

        private void AddHeaderToHierarchy(Stack<DocumentHeader> headerStack, Block block)
        {
            var header = CreateHeaderFromBlock(block);
            if (header.Level == 1 || headerStack.Count == 0)
            {
                DocumentHeaders.Add(header);
                headerStack.Clear();
                headerStack.Push(header);
            }
            else
            {
                var parentHeader = headerStack.Peek();
                if (null != parentHeader && parentHeader.Level < header.Level)
                {
                    // This is a child of that previous level, so we add it and push
                    parentHeader.ChildHeaders.Add(header);
                    headerStack.Push(header);
                }
                else if (null != parentHeader && parentHeader.Level >= header.Level)
                {
                    // We need to pop back and find the right parent for this higher level
                    while (headerStack.Count > 0 && headerStack.Peek().Level >= header.Level)
                    {
                        headerStack.Pop();
                    }
                    if (headerStack.Count > 0)
                    {
                        parentHeader = headerStack.Peek();
                        parentHeader.ChildHeaders.Add(header);
                    }
                    headerStack.Push(header);
                }
                else
                {
                    throw new InvalidOperationException("Something went wrong in the outline creation");
                }
            }
        }

        /// <summary>
        /// Validates code snippets tab section.
        /// Checks:
        /// - No duplicated tabs
        /// - Existence of tab boundary at the end of tab group definition
        /// - Tab groups have the same tabs
        /// - At least two tabs in a tab group
        /// - HTTP tab should be present
        /// - Tabs should only be within the example request sections
        /// </summary>
        private void ValidateTabStructure(IssueLogger issues)
        {
            if (this.DocumentPageType != PageType.ApiPageType)
                return;

            var tabHeaders = new List<string>();
            int foundTabIndex = -1, foundTabGroups = 0;
            var currentState = TabDetectionState.FindExamplesHeader;
            var fileContents = File.ReadAllLines(this.FullPath);

            for (var currentIndex = 0; currentIndex < fileContents.Length; currentIndex++)
            {
                var currentLine = fileContents[currentIndex].Trim();
                bool isTabHeader = currentLine.Contains("#tab/");

                switch (currentState)
                {
                    case TabDetectionState.FindExamplesHeader:
                        if (isTabHeader)
                        {
                            issues.Error(ValidationErrorCode.TabHeaderError, $"Tab group #{foundTabGroups + 1} should be within the examples section");
                            currentState = TabDetectionState.FindStartOfTabGroup;
                            currentIndex--;
                        }

                        if (currentLine.Contains("# Example", StringComparison.OrdinalIgnoreCase))
                            currentState = TabDetectionState.FindStartOfTabGroup;
                        break;
                    case TabDetectionState.FindStartOfTabGroup:
                        if (isTabHeader)
                        {
                            foundTabIndex++;
                            foundTabGroups++;

                            if (foundTabIndex == 0 && !currentLine.Contains("#tab/http"))
                                issues.Error(ValidationErrorCode.TabHeaderError, $"The first tab should be 'HTTP' in tab group #{foundTabGroups}");

                            if (foundTabGroups == 1)
                                tabHeaders.Add(currentLine);

                            currentState = TabDetectionState.FindEndOfTabGroup;
                        }
                        break;
                    case TabDetectionState.FindEndOfTabGroup:
                        if (isTabHeader)
                        {
                            foundTabIndex++;
                            if (foundTabGroups == 1)
                            {
                                if (tabHeaders.Contains(currentLine))
                                    issues.Error(ValidationErrorCode.TabHeaderError, $"Duplicate tab header '{currentLine}' found in tab group #{foundTabGroups}");
                                else
                                    tabHeaders.Add(currentLine);
                            }
                            else
                            {
                                if (foundTabIndex >= tabHeaders.Count)
                                {
                                    issues.Error(ValidationErrorCode.TabHeaderError, $"Inconsistent tab headers in tab group #{foundTabGroups}");
                                    currentState = TabDetectionState.FindStartOfTabGroup;
                                    break;
                                }

                                if (!currentLine.Equals(tabHeaders[foundTabIndex], StringComparison.OrdinalIgnoreCase))
                                    issues.Error(ValidationErrorCode.TabHeaderError, $"Tab header '{currentLine}' is in the wrong order in tab group #{foundTabGroups}");
                            }
                        }

                        if (currentLine.StartsWith("#") && !currentLine.Contains("#tab"))
                            issues.Error(ValidationErrorCode.TabHeaderError, $"Header '{currentLine}' found within tab group #{foundTabGroups}");

                        if (currentLine == "---")
                        {
                            if (foundTabIndex == 0)
                                issues.Error(ValidationErrorCode.TabHeaderError, $"At least two tab headers are required in tab group #{foundTabGroups}");

                            currentState = TabDetectionState.FindStartOfTabGroup;
                            foundTabIndex = -1;
                        }
                        break;
                    default:
                        break;

                }
            }

            if (currentState == TabDetectionState.FindEndOfTabGroup)
                issues.Error(ValidationErrorCode.TabHeaderError, $"Missing tab boundary in document for tab group #{foundTabGroups}");
        }

        private enum TabDetectionState
        {
            FindExamplesHeader,
            FindStartOfTabGroup,
            FindEndOfTabGroup
        }

        /// <summary>
        /// Remove <!-- and --> from the content string
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string StripHtmlCommentTags(string content)
        {
            const string startComment = "<!--";
            const string endComment = "-->";

            int index = content.IndexOf(startComment, StringComparison.Ordinal);
            if (index >= 0)
                content = content.Substring(index + startComment.Length);
            index = content.IndexOf(endComment, StringComparison.Ordinal);
            if (index >= 0)
                content = content.Substring(0, index);

            return content;
        }

        private PageAnnotation ParsePageAnnotation(Block block)
        {
            var commentText = StripHtmlCommentTags(block.Content).Trim();

            if (!commentText.StartsWith("{"))
                return null;

            var response = JsonConvert.DeserializeObject<PageAnnotation>(commentText);
            if (null != response && null != response.Type && response.Type.Equals(PageAnnotationType, StringComparison.OrdinalIgnoreCase))
                return response;

            return null;
        }

        private static Dictionary<char, string> BookmarkReplacmentMap = new Dictionary<char, string>
        {
            [' '] = "-",
            ['.'] = "",
            [','] = "",
            [':'] = ""
        };

        /// <summary>
        /// Run post processing on the collection of elements found inside this doc file.
        /// </summary>
        private void PostProcessFoundElements(List<object> elements, IssueLogger issues)
        {
            /*
            if FoundMethods == 1 then
              Attach all tables found in the document to the method.

            else if FoundMethods > 1 then
              Table.Type == ErrorCodes
                - Attach errors to all methods in the file
              Table.Type == PathParameters
                - Find request with matching parameters
              Table.Type == Query String Parameters
                - Request may not have matching parameters, because query string parameters may not be part of the request
              Table.Type == Header Parameters
                - Find request with matching parameters
              Table.Type == Body Parameters
                - Find request with matching parameters
             */

            var elementsFoundInDocument = elements as IList<object> ?? elements.ToList();
            var foundMethods = elementsFoundInDocument.OfType<MethodDefinition>().ToList();
            var foundTables = elementsFoundInDocument.OfType<TableDefinition>().ToList();
            var foundResources = elementsFoundInDocument.OfType<ResourceDefinition>()
                .Select(c => { c.Namespace = this.Namespace; return c; }).ToList();
            var foundEnums = foundTables.Where(t => t.Type == TableBlockType.EnumerationValues)
                .SelectMany(t => t.Rows).Cast<EnumerationDefinition>()
                .Select(c => { c.Namespace = this.Namespace; return c; }).ToList();

            this.PostProcessAuthScopes(elementsFoundInDocument);
            this.PostProcessResources(foundResources, foundTables, issues);
            this.PostProcessMethods(foundMethods, foundTables, issues);
            this.PostProcessEnums(foundEnums, foundTables, issues);
        }

        private void InferNamespaceFromFoundElements(IList<object> foundElements, IssueLogger issues)
        {
            var foundResource = foundElements.OfType<ResourceDefinition>().FirstOrDefault();
            string inferredNamespace = null;
            if (foundResource != null)
            {
                if (foundResource.Name.Contains('.'))
                {
                    inferredNamespace = foundResource.Name.Substring(0, foundResource.Name.LastIndexOf('.'));
                }
                if (this.Annotation?.Namespace != null && this.Annotation.Namespace != inferredNamespace)
                {
                    issues.Error(ValidationErrorCode.NamespaceMismatch, $"The namespace specified on page level annotation for resource {foundResource.Name} is incorrect.");
                }
            }
            this.Namespace = this.Annotation?.Namespace ?? inferredNamespace ?? DocSet.SchemaConfig?.DefaultNamespace;
        }

        private void PostProcessAuthScopes(IList<object> foundElements)
        {
            var authScopeTables = (from s in foundElements
                                   where s is TableDefinition && ((TableDefinition)s).Type == TableBlockType.AuthScopes
                                   select ((TableDefinition)s));

            List<AuthScopeDefinition> foundScopes = new List<AuthScopeDefinition>();
            foreach (var table in authScopeTables)
            {
                foundScopes.AddRange(table.Rows.Cast<AuthScopeDefinition>());
            }
            this.AuthScopes = foundScopes.ToArray();
        }

        private void PostProcessEnums(List<EnumerationDefinition> foundEnums, List<TableDefinition> foundTables, IssueLogger issues)
        {
            // add all the enum values
            this.enums.AddRange(foundEnums.Where(e => !string.IsNullOrEmpty(e.MemberName) && !string.IsNullOrEmpty(e.TypeName)));

            // if we thought it was a table of type EnumerationValues, it probably holds enum values.
            // throw error if member name is null which could mean a wrong column name
            foreach (var enumType in foundEnums.Where(e => string.IsNullOrEmpty(e.MemberName) && !string.IsNullOrEmpty(e.TypeName))
                .Select(x => new { x.TypeName, x.Namespace }).Distinct())
            {
                var possibleHeaderNames = this.Parent?.TableParserConfig?.TableDefinitions.Rules
                    .Where(r => r.Type == "EnumerationDefinition").SelectMany(r => r.ColumnNames["memberName"]).ToList();

                issues.Error(ValidationErrorCode.ParameterParserError,
                    $"Failed to parse enumeration values for type {enumType.Namespace}.{enumType.TypeName}. " +
                    $"Table requires a column header named one of the following: {string.Join(", ", possibleHeaderNames ?? new List<string>())}");
            }

            // find all the property tables
            // find properties of type string that have a list of `enum`, `values`. see if they match me.
            foreach (var table in foundTables.Where(t => t.Type == TableBlockType.RequestObjectProperties || t.Type == TableBlockType.ResourcePropertyDescriptions))
            {
                var rows = table.Rows.Cast<ParameterDefinition>();
                foreach (var row in rows.Where(r => r.Type?.Type == SimpleDataType.String))
                {
                    var rowIssues = issues.For(row.Name);

                    // if more than one word in the description happens to match an enum value... i'm suspicious
                    var possibleEnumCandidates = row.Description.TokenizedWords();

                    var matches = possibleEnumCandidates?.Intersect(this.enums.Select(e => e.MemberName), StringComparer.OrdinalIgnoreCase).ToList();
                    if (matches?.Count > 1)
                    {
                        rowIssues.Warning(ValidationErrorCode.Unknown, $"Found potential enums in parameter description declared as a string: " +
                            $"({string.Join(",", matches)}) are in enum {this.enums.First(e => e.MemberName.Equals(matches[0], StringComparison.OrdinalIgnoreCase)).TypeName}");

                    }
                }
            }

            foreach (var resource in this.resources)
            {
                foreach (var param in resource.Parameters.Where(p => p.Type?.Type == SimpleDataType.String))
                {
                    var possibleEnums = param.PossibleEnumValues();
                    if (possibleEnums.Length > 1)
                    {
                        var matches = possibleEnums.Intersect(this.enums.Select(e => e.MemberName), StringComparer.OrdinalIgnoreCase).ToList();
                        if (matches.Count < possibleEnums.Length)
                        {
                            issues.Warning(ValidationErrorCode.Unknown, $"Found potential enums in resource example that weren't defined in a table:" +
                                $"({string.Join(",", possibleEnums)}) are in resource, but ({string.Join(",", this.enums.Select(e => e.MemberName))}) are in table");
                        }
                    }
                }
            }
        }

        private void PostProcessResources(List<ResourceDefinition> foundResources, List<TableDefinition> foundTables, IssueLogger issues)
        {
            if (foundResources.Count > 1)
            {
                var resourceNames = string.Join(",", foundResources.Select(r => r.Name));
                issues.Warning(ValidationErrorCode.ErrorReadingFile, $"Multiple resources found in file, but we only support one per file. '{resourceNames}'. Skipping.");
                return;
            }

            var onlyResource = foundResources.FirstOrDefault();

            if (onlyResource != null)
            {
                foreach (var table in foundTables)
                {
                    switch (table.Type)
                    {
                        case TableBlockType.RequestObjectProperties:
                        case TableBlockType.ResourcePropertyDescriptions:
                        case TableBlockType.ResourceNavigationPropertyDescriptions:
                            // Merge information found in the resource property description table with the existing resources
                            if (onlyResource == null)
                            {
                                if (table.Type != TableBlockType.RequestObjectProperties)
                                {
                                    // TODO: this isn't exactly right... we want to fail RequestObjectProperties if the doc doesn't describe the resource anywhere else, either.
                                    // but that check needs to happen after everything has finished being read. this whole thing should probably be a post-check actually.
                                    // like we should aggregate all the object model data centrally, remembering where every bit of info came from, and then validate it all at the end
                                    // and throw about any inconsistencies.
                                    issues.Warning(ValidationErrorCode.ResourceTypeNotFound, $"Resource not found, but descriptive table(s) are present. Skipping.");
                                }

                                return;
                            }

                            var listOfTextToRemoveFromPropertyNames = DocSet.SchemaConfig?.TextToRemoveFromPropertyNames ?? [];
                            var parametersFromTableDefinition = table.Rows.Cast<ParameterDefinition>()
                            .Select(param =>
                            {
                                foreach (string text in listOfTextToRemoveFromPropertyNames)
                                {
                                    param.Name = param.Name.Replace(text, "").Trim();
                                }
                                return param;
                            });


                            table.UsedIn.Add(onlyResource);
                            MergeParametersIntoCollection(
                                onlyResource.Parameters,
                                parametersFromTableDefinition,
                                issues.For(onlyResource.Name),
                                addMissingParameters: true,
                                expectedInResource: true,
                                resourceToFixUp: this.WriteFixesBackToDisk ? onlyResource as JsonResourceDefinition : null);
                            break;
                    }
                }

                // at this point, all parameters should have descriptions from the table.
                var paramsToRemove = new List<ParameterDefinition>();
                foreach (var param in onlyResource.Parameters)
                {
                    if (string.IsNullOrEmpty(param.Description) && !param.Name.Contains("@"))
                    {
                        if (onlyResource.OriginalMetadata.IsOpenType &&
                            onlyResource.OriginalMetadata.OptionalProperties?.Contains(param.Name, StringComparer.OrdinalIgnoreCase) == true)
                        {
                            paramsToRemove.Add(param);
                        }
                    }
                }

                if (paramsToRemove.Count > 0)
                {
                    foreach (var param in paramsToRemove)
                    {
                        onlyResource.Parameters.Remove(param);
                    }
                }
            }
        }


        /// <summary>
        /// Merges parameter definitions from additionalData into the collection list. Pulls in missing data for existing
        /// parameters and adds any additional parameters found in the additionalData.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="additionalData"></param>
        private void MergeParametersIntoCollection(
            List<ParameterDefinition> collection,
            IEnumerable<ParameterDefinition> additionalData,
            IssueLogger issues,
            bool addMissingParameters = false,
            bool expectedInResource = true,
            JsonResourceDefinition resourceToFixUp = null)
        {
            foreach (var param in additionalData)
            {
                // See if this parameter already is known
                var match = collection.SingleOrDefault(x => x.Name == param.Name);
                if (match != null)
                {
                    // The parameter was always known, let's merge in additional data.
                    match.AddMissingDetails(param, issues.For(param.Name));
                }
                else if (addMissingParameters)
                {
                    if (expectedInResource)
                    {
                        // Navigation propeties may not appear in the example text, so don't report and error
                        if (!param.IsNavigatable)
                        {
                            issues.Warning(ValidationErrorCode.AdditionalPropertyDetected, $"Property '{param.Name}' found in markdown table but not in resource definition.");

                            if (resourceToFixUp?.SourceJObject != null)
                            {
                                resourceToFixUp.SourceJObject.Add(param.Name, param.ToExampleJToken());
                                resourceToFixUp.PatchSourceFile();
                                Console.WriteLine("Fixed missing property");
                            }
                        }
                        else
                        {
                            issues.Message($"Navigation property '{param.Name}' found in markdown table but not in resource definition.");
                        }
                    }

                    // The parameter didn't exist in the collection, so let's add it.
                    collection.Add(param);
                }
                else if (!param.IsNavigatable && expectedInResource)
                {
                    // Oops, we didn't find the property in the resource definition
                    issues.Warning(ValidationErrorCode.AdditionalPropertyDetected, $"Property '{param.Name}' found in markdown table but not in resource definition.");
                }
            }
        }


        private void PostProcessMethods(IEnumerable<MethodDefinition> foundMethods, IEnumerable<TableDefinition> foundTables, IssueLogger issues)
        {
            var totalTables = foundTables.Count();
            if (totalTables == 0)
            {
                return;
            }

            var totalMethods = foundMethods.Count();
            if (totalMethods == 0)
            {
                this.SetFoundTablesForFile(foundTables);
            }
            else if (totalMethods == 1)
            {
                var onlyMethod = foundMethods.Single();
                SetFoundTablesOnMethod(foundTables, onlyMethod, issues);
            }
            else
            {
                int distinctMethodNames = 0;
                try
                {
                    // maybe the methods are really all the same and the dupes are just different examples
                    distinctMethodNames = foundMethods.Select(m => Http.HttpParser.ParseHttpRequest(m.Request).Url).Select(
                        url =>
                        {
                            var method = url.Substring(url.LastIndexOf('/') + 1);
                            var endIndex = method.IndexOfAny(new[] { '(', '?', '/' });
                            if (endIndex != -1)
                            {
                                method = method.Substring(0, endIndex);
                            }

                            return method;
                        }).Distinct().Count();
                }
                catch (Exception ex)
                {
                    issues.Error(ValidationErrorCode.HttpParserError, $"Exception while parsing HTTP request", ex);
                }

                if (distinctMethodNames == 1)
                {
                    foreach (var method in foundMethods)
                    {
                        SetFoundTablesOnMethod(foundTables, method, issues);
                    }

                    return;
                }

                // if there's no more than one of each table, we can assume
                // they're meant to apply to all the methods.
                if (foundTables.GroupBy(t => t.Type).All(g => g.Count() <= 1))
                {
                    foreach (var method in foundMethods)
                    {
                        SetFoundTablesOnMethod(foundTables, method, issues);
                    }

                    return;
                }

                // TODO: Figure out how to map stuff when more than one method exists along with separate tables for each
                List<ValidationError> innerErrors = new List<ValidationError>();
                var unmappedMethods = (from m in foundMethods select m.RequestMetadata.MethodName?.FirstOrDefault()).ComponentsJoinedByString(", ");
                if (!string.IsNullOrEmpty(unmappedMethods))
                {
                    innerErrors.Add(new ValidationMessage("Unmapped methods", unmappedMethods));
                }

                var unmappedTables = (from t in foundTables select string.Format("{0} - {1}", t.Title, t.Type)).ComponentsJoinedByString(", ");
                if (!string.IsNullOrEmpty(unmappedTables))
                {
                    innerErrors.Add(new ValidationMessage("Unmapped tables", unmappedTables));
                }

                var unmappedContentsError = new ValidationWarning(ValidationErrorCode.UnmappedDocumentElements, this.DisplayName, "Unable to map some markdown elements into schema.")
                {
                    InnerErrors = innerErrors.ToArray(),
                };

                issues.Warning(unmappedContentsError);
            }
        }

        private void SetFoundTablesOnMethod(IEnumerable<TableDefinition> foundTables, MethodDefinition onlyMethod, IssueLogger issues)
        {
            var methodName = onlyMethod.RequestMetadata.MethodName?.FirstOrDefault();
            foreach (var table in foundTables)
            {
                switch (table.Type)
                {
                    case TableBlockType.Unknown:
                        // Unknown table format, nothing we can do with it.
                        break;
                    case TableBlockType.AuthScopes:
                        // nothing to do on a specific method. handled at the file level
                        break;
                    case TableBlockType.EnumerationValues:
                        // nothing special to do with enums right now.
                        break;
                    case TableBlockType.ErrorCodes:
                        table.UsedIn.Add(onlyMethod);
                        onlyMethod.Errors = table.Rows.Cast<ErrorDefinition>().ToList();
                        break;
                    case TableBlockType.HttpHeaders:
                    case TableBlockType.PathParameters:
                    case TableBlockType.QueryStringParameters:
                        table.UsedIn.Add(onlyMethod);
                        onlyMethod.Parameters.AddRange(table.Rows.Cast<ParameterDefinition>());
                        break;
                    case TableBlockType.RequestObjectProperties:
                        table.UsedIn.Add(onlyMethod);
                        MergeParametersIntoCollection(onlyMethod.RequestBodyParameters, table.Rows.Cast<ParameterDefinition>(), issues.For(methodName), addMissingParameters: true, expectedInResource: false);
                        break;
                    default:
                        if (table.UsedIn.Count == 0)
                        {
                            // if the table wasn't used by anything else, we assume it's intended for this method, so we log a warning
                            issues.Warning(ValidationErrorCode.Unknown, $"Table '{table.Title}' of type {table.Type} is not supported for methods '{onlyMethod.RequestMetadata.MethodName?.FirstOrDefault()}' and was ignored.");
                        }
                        break;
                }
            }
        }

        private void SetFoundTablesForFile(IEnumerable<TableDefinition> foundTables)
        {
            // Assume anything we found is a global resource
            foreach (var table in foundTables)
            {
                switch (table.Type)
                {
                    case TableBlockType.ErrorCodes:
                        this.ErrorCodes = table.Rows.Cast<ErrorDefinition>().ToArray();
                        break;
                }
            }
        }

        /// <summary>
        /// Filters the blocks to just a collection of blocks that may be
        /// relevent for our purposes
        /// </summary>
        /// <returns>The code blocks.</returns>
        /// <param name="blocks">Blocks.</param>
        protected static List<Block> FindCodeBlocks(Block[] blocks)
        {
            var blockList = new List<Block>();
            foreach (var block in blocks)
            {
                switch (block.BlockType)
                {
                    case BlockType.codeblock:
                    case BlockType.html:
                        blockList.Add(block);
                        break;
                    default:
                        break;
                }
            }
            return blockList;
        }

        /// <summary>
        /// Convert an annotation and fenced code block in the documentation into something usable. Adds
        /// the detected object into one of the internal collections of resources, methods, or examples.
        /// </summary>
        public List<ItemDefinition> ParseCodeBlock(Block metadata, Block code, IssueLogger issues)
        {
            List<ValidationError> detectedErrors = new List<ValidationError>();

            if (metadata.BlockType != BlockType.html)
            {
                issues.Error(ValidationErrorCode.MarkdownParserError, "metadata block does not appear to be metadata");
                return null;
            }

            if (code.BlockType != BlockType.codeblock)
            {
                issues.Error(ValidationErrorCode.MarkdownParserError, "code block does not appear to be code");
                return null;
            }

            var metadataJsonString = StripHtmlCommentTags(metadata.Content);

            CodeBlockAnnotation annotation = null;
            try
            {
                annotation = CodeBlockAnnotation.ParseMetadata(metadataJsonString, code, this);
            }
            catch (Exception ex)
            {
                issues.Error(ValidationErrorCode.MarkdownParserError, $"Unable to parse code block metadata.", ex);
                return null;
            }

            switch (annotation.BlockType)
            {
                case CodeBlockType.Resource:
                    {
                        ResourceDefinition resource;
                        if (code.CodeLanguage.Equals("json", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                resource = new JsonResourceDefinition(annotation, code.Content, this, issues);
                            }
                            catch (Exception ex)
                            {
                                issues.Error(ValidationErrorCode.MarkdownParserError, $"Unable to parse resource metadata in {annotation.ResourceType}.", ex);
                                return null;
                            }
                        }
                        else
                        {
                            issues.Error(ValidationErrorCode.MarkdownParserError, $"Unsupported resource definition language: {code.CodeLanguage}");
                            return null;
                        }

                        if (string.IsNullOrEmpty(resource.Name))
                        {
                            issues.Error(ValidationErrorCode.MarkdownParserError, "Resource definition is missing a name.");
                            return null;
                        }

                        this.resources.Add(resource);
                        return new List<ItemDefinition>(new ItemDefinition[] { resource });
                    }
                case CodeBlockType.Request:
                    {
                        var method = MethodDefinition.FromRequest(GetBlockContent(code), annotation, this, issues);
                        if (string.IsNullOrEmpty(method.Identifier))
                        {
                            method.Identifier = string.Format("{0} #{1}", this.DisplayName, this.requests.Count);
                        }

                        this.requests.Add(method);
                        return new List<ItemDefinition>(new ItemDefinition[] { method });
                    }

                case CodeBlockType.Response:
                    {
                        var responses = new List<ItemDefinition>();
                        if (null != annotation.MethodName)
                        {
                            // Look up all the requests mentioned and pair them up
                            foreach (var requestMethodName in annotation.MethodName)
                            {
                                // Look up paired request by name
                                MethodDefinition pairedRequest = (from m in this.requests where m.Identifier == requestMethodName select m).FirstOrDefault();
                                if (pairedRequest != null)
                                {
                                    try
                                    {
                                        pairedRequest.AddExpectedResponse(GetBlockContent(code), annotation);
                                        responses.Add(pairedRequest);
                                    }
                                    catch (Exception ex)
                                    {
                                        detectedErrors.Add(new ValidationError(ValidationErrorCode.MarkdownParserError, this.DisplayName, "Unable to pair response with request {0}: {1}.", annotation.MethodName, ex.Message));
                                    }
                                }
                                else
                                {
                                    detectedErrors.Add(new ValidationError(ValidationErrorCode.MarkdownParserError, this.DisplayName, "Unable to locate the corresponding request for response block: {0}. Requests must be defined before a response.", annotation.MethodName));
                                }
                            }
                        }
                        else if (this.requests.Any())
                        {
                            // Try to match with a previous request on the page. Will throw if the previous request on the page is already paired
                            MethodDefinition pairedRequest = Enumerable.Last(this.requests);
                            if (pairedRequest != null)
                            {
                                try
                                {
                                    pairedRequest.AddExpectedResponse(GetBlockContent(code), annotation);
                                    responses.Add(pairedRequest);
                                }
                                catch (Exception ex)
                                {
                                    detectedErrors.Add(new ValidationError(ValidationErrorCode.MarkdownParserError, this.DisplayName, "Unable to pair response with request {0}: {1}.", annotation.MethodName, ex.Message));
                                }

                            }
                            else
                            {
                                issues.Error(ValidationErrorCode.MarkdownParserError, $"Unable to locate the corresponding request for response block: {annotation.MethodName}. Requests must be defined before a response.");
                            }
                        }

                        return responses;
                    }
                case CodeBlockType.Example:
                    {
                        var example = new ExampleDefinition(annotation, code.Content, this, code.CodeLanguage);
                        this.examples.Add(example);
                        return new List<ItemDefinition>(new ItemDefinition[] { example });
                    }
                case CodeBlockType.Samples:
                    {
                        var sample = new SamplesDefinition(annotation, code.Content);
                        this.samples.Add(sample);
                        return new List<ItemDefinition>(new ItemDefinition[] { sample });
                    }
                case CodeBlockType.Ignored:
                    {
                        return null;
                    }
                case CodeBlockType.SimulatedResponse:
                    {
                        var method = Enumerable.Last(this.requests);
                        method.AddSimulatedResponse(code.Content, annotation);
                        return new List<ItemDefinition>(new ItemDefinition[] { method });
                    }
                case CodeBlockType.TestParams:
                    {
                        var method = Enumerable.Last(this.requests);
                        method.AddTestParams(code.Content);
                        return new List<ItemDefinition>(new ItemDefinition[] { method });
                    }
                default:
                    {
                        issues.Error(ValidationErrorCode.MarkdownParserError,
                            $"Unable to parse metadata block. Possibly an unsupported block type. Line {metadata.LineStart}. Content: {metadata.Content}");
                        return null;
                    }
            }
        }
        #endregion

        #region Link Verification

        public bool ValidateNoBrokenLinks(bool includeWarnings, IssueLogger issues, bool requireFilenameCaseMatch)
        {
            string[] files;
            return this.ValidateNoBrokenLinks(includeWarnings, issues.For(this.DisplayName), out files, requireFilenameCaseMatch);
        }

        /// <summary>
        /// Checks all links detected in the source document to make sure they are valid.
        /// </summary>
        /// <param name="includeWarnings"></param>
        /// <param name="errors">Information about broken links</param>
        /// <param name="linkedDocFiles"></param>
        /// <returns>True if all links are valid. Otherwise false</returns>
        public bool ValidateNoBrokenLinks(bool includeWarnings, IssueLogger issues, out string[] linkedDocFiles, bool requireFilenameCaseMatch)
        {
            if (!this.HasScanRun)
                throw new InvalidOperationException("Cannot validate links until Scan() is called.");

            List<string> linkedPages = new List<string>();

            // If there are no links in this document, just skip the validation.
            if (this.MarkdownLinks == null)
            {
                linkedDocFiles = new string[0];
                return true;
            }

            foreach (var link in this.MarkdownLinks)
            {
                if (null == link.Definition)
                {
                    // Don't treat TAGS or END markers like links
                    if (!link.Text.ToUpper().Equals("END") && !link.Text.ToUpper().StartsWith("TAGS="))
                    {
                        issues.Error(ValidationErrorCode.MissingLinkSourceId,
                            $"Link ID '[{link.Text}]' used in document but not defined. Define with '[{link.Text}]: url' or remove square brackets.");
                    }

                    continue;
                }

                string relativeFileName;
                var result = this.VerifyLink(this.FullPath, link.Definition.url, this.BasePath, out relativeFileName, requireFilenameCaseMatch);
                string suggestion = (relativeFileName != null) ? $"Did you mean: {relativeFileName}" : string.Empty;
                switch (result)
                {
                    case LinkValidationResult.ExternalSkipped:
                        if (includeWarnings)
                            issues.Warning(ValidationErrorCode.LinkValidationSkipped, $"Skipped validation of external link '[{link.Definition.url}]({link.Text})'");
                        break;
                    case LinkValidationResult.FileNotFound:
                        issues.Error(ValidationErrorCode.LinkDestinationNotFound, $"FileNotFound: '[{link.Definition.url}]({link.Text})'. {suggestion}");
                        break;
                    case LinkValidationResult.BookmarkMissing:
                        issues.Error(ValidationErrorCode.LinkDestinationNotFound, $"BookmarkMissing: '[{link.Definition.url}]({link.Text})'. {suggestion}");
                        break;
                    case LinkValidationResult.ParentAboveDocSetPath:
                        //Possible error because of the beta-disclaimer.md file is in the includes [!INCLUDE [beta-disclaimer](../../includes/beta-disclaimer.md)]
                        //Needs further investigation and tests.
                        issues.Error(ValidationErrorCode.LinkDestinationOutsideDocSet, $"Relative link outside of doc set: '[{link.Definition.url}]({link.Text})'.");
                        break;
                    case LinkValidationResult.UrlFormatInvalid:
                        issues.Error(ValidationErrorCode.LinkFormatInvalid, $"InvalidUrlFormat '[{link.Definition.url}]({link.Text})'.");
                        break;
                    case LinkValidationResult.Valid:
                        issues.Message($"Valid link '[{link.Definition.url}]({link.Text})'.");
                        if (null != relativeFileName)
                        {
                            linkedPages.Add(relativeFileName);
                        }
                        break;
                    default:
                        issues.Error(ValidationErrorCode.Unknown, $"{result}: Link '[{link.Text}]({link.Definition.url})'.");
                        break;
                }

            }
            linkedDocFiles = linkedPages.Distinct().ToArray();
            return !(issues.Issues.WereErrors() || issues.Issues.WereWarnings());
        }

        protected enum LinkValidationResult
        {
            Valid,
            FileNotFound,
            UrlFormatInvalid,
            ExternalSkipped,
            BookmarkSkipped,
            ParentAboveDocSetPath,
            BookmarkMissing,
            FileExistsBookmarkValidationSkipped,
            BookmarkSkippedDocFileNotFound
        }

        protected LinkValidationResult VerifyLink(string docFilePath, string linkUrl, string docSetBasePath, out string relativeFileName, bool requireFilenameCaseMatch)
        {
            relativeFileName = null;
            Uri parsedUri;
            var validUrl = Uri.TryCreate(linkUrl, UriKind.RelativeOrAbsolute, out parsedUri);

            FileInfo sourceFile = new FileInfo(docFilePath);

            if (validUrl)
            {
                if (parsedUri.IsAbsoluteUri && (parsedUri.Scheme == "http" || parsedUri.Scheme == "https"))
                {
                    // TODO: verify an external URL is valid by making a HEAD request
                    return LinkValidationResult.ExternalSkipped;
                }
                else if (linkUrl.StartsWith("#"))
                {
                    string bookmarkName = linkUrl.Substring(1);
                    if (this.bookmarks.Contains(bookmarkName))
                    {
                        return LinkValidationResult.Valid;
                    }
                    else
                    {
                        var suggestion = StringSuggestions.SuggestStringFromCollection(bookmarkName, this.bookmarks);
                        if (suggestion != null)
                            relativeFileName = "#" + suggestion;
                        return LinkValidationResult.BookmarkMissing;
                    }
                }
                else
                {
                    return this.VerifyRelativeLink(sourceFile, linkUrl, docSetBasePath, out relativeFileName, requireFilenameCaseMatch);
                }
            }
            else
            {
                return LinkValidationResult.UrlFormatInvalid;
            }
        }

        protected virtual LinkValidationResult VerifyRelativeLink(FileInfo sourceFile, string originalLinkUrl, string docSetBasePath, out string relativeFileName, bool requireFilenameCaseMatch)
        {
            if (sourceFile == null) throw new ArgumentNullException("sourceFile");
            if (string.IsNullOrEmpty(originalLinkUrl)) throw new ArgumentNullException("linkUrl");
            if (string.IsNullOrEmpty(docSetBasePath)) throw new ArgumentNullException("docSetBasePath");


            if (originalLinkUrl.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: Verify that this is an actual email address
                relativeFileName = null;
                return LinkValidationResult.Valid;
            }

            relativeFileName = null;
            var rootPath = sourceFile.DirectoryName;
            string bookmarkName = null;
            var workingLinkUrl = originalLinkUrl;

            if (workingLinkUrl.Contains("#"))
            {
                int indexOfHash = workingLinkUrl.IndexOf('#');
                bookmarkName = workingLinkUrl.Substring(indexOfHash + 1);
                workingLinkUrl = workingLinkUrl.Substring(0, indexOfHash);
            }

            if (this.Parent?.LinkValidationConfig?.IgnoredPaths?.Any(ignoredPath =>
                workingLinkUrl.StartsWith(ignoredPath, StringComparison.OrdinalIgnoreCase)) == true)
            {
                return LinkValidationResult.ExternalSkipped;
            }

            if (workingLinkUrl.StartsWith("/"))
            {
                // URL is relative to the base for the documentation
                rootPath = docSetBasePath;
                workingLinkUrl = workingLinkUrl.Substring(1);
            }

            while (workingLinkUrl.StartsWith("../"))
            {
                var nextLevelParent = new DirectoryInfo(rootPath).Parent;
                if (null != nextLevelParent)
                {
                    rootPath = nextLevelParent.FullName;
                    workingLinkUrl = workingLinkUrl.Substring(3);
                }
                else
                {
                    break;
                }
            }

            if (rootPath.Length < docSetBasePath.Length)
            {
                return LinkValidationResult.ParentAboveDocSetPath;
            }

            try
            {
                workingLinkUrl = workingLinkUrl.Replace('/', Path.DirectorySeparatorChar);     // normalize the path syntax between local file system and web

                var pathToFile = Path.Combine(rootPath, workingLinkUrl);
                FileInfo info = new FileInfo(pathToFile);
                if (!info.Exists)
                {
                    if (info.Directory.Exists)
                    {
                        var candidateFiles = from f in info.Directory.GetFiles() select f.Name;
                        relativeFileName = StringSuggestions.SuggestStringFromCollection(info.Name, candidateFiles);
                    }

                    return LinkValidationResult.FileNotFound;
                }

                relativeFileName = this.Parent.RelativePathToFile(info.FullName, urlStyle: true);

                if (bookmarkName != null)
                {
                    // See if that bookmark exists in the target document, assuming we know about it
                    var otherDocFile = this.Parent.LookupFileForPath(relativeFileName);
                    if (otherDocFile == null)
                    {
                        return LinkValidationResult.BookmarkSkippedDocFileNotFound;
                    }
                    else if (!otherDocFile.bookmarks.Contains(bookmarkName))
                    {
                        var suggestion = StringSuggestions.SuggestStringFromCollection(bookmarkName, otherDocFile.bookmarks);
                        if (null != suggestion)
                            relativeFileName = "#" + suggestion;
                        return LinkValidationResult.BookmarkMissing;
                    }
                }
                return LinkValidationResult.Valid;
            }
            catch (Exception)
            {
                return LinkValidationResult.UrlFormatInvalid;
            }
        }

        #endregion

        public string UrlRelativePathFromRoot()
        {
            var relativePath = this.DisplayName.Replace('\\', '/');
            return relativePath.StartsWith("/") ? relativePath.Substring(1) : relativePath;
        }

        public override string ToString()
        {
            return this.DisplayName;
        }

        public override int GetHashCode()
        {
            return this.DisplayName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.DisplayName.Equals(obj);
        }
    }

}