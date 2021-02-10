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
    using ApiDoctor.Validation.Error;
    using ApiDoctor.Validation.TableSpec;
    using Tags;
    using MarkdownDeep;
    using Newtonsoft.Json;


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

        protected bool HasScanRun;
        protected string BasePath;

        #region Properties
        /// <summary>
        /// Friendly name of the file
        /// </summary>
        public string DisplayName { get; protected set; }

        /// <summary>
        /// Path to the file on disk
        /// </summary>
        public string FullPath { get; protected set; }

        ///// <summary>
        ///// HTML-rendered version of the markdown source (for displaying). This content is not suitable for publishing.
        ///// </summary>
        //public string HtmlContent { get; protected set; }

        /// <summary>
        /// Contains information on the headers and content blocks found in this document.
        /// </summary>
        public List<string> ContentOutline { get; set; }

        public ResourceDefinition[] Resources
        {
            get { return this.resources.ToArray(); }
        }

        public MethodDefinition[] Requests
        {
            get { return this.requests.ToArray(); }
        }

        public ExampleDefinition[] Examples { get { return this.examples.ToArray(); } }

        public SamplesDefinition[] Samples { get { return this.samples.ToArray(); } }

        public EnumerationDefinition[] Enums { get { return this.enums.ToArray(); } }

        public AuthScopeDefinition[] AuthScopes { get; protected set; }

        public ErrorDefinition[] ErrorCodes { get; protected set; }

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
        public Block[] OriginalMarkdownBlocks { get; set; }

        protected List<ILinkInfo> MarkdownLinks { get; set; }

        public DocSet Parent { get; protected set; }

        public PageAnnotation Annotation { get; set; }

        public bool WriteFixesBackToDisk { get; set; }

        public string Namespace { get; set; }
        #endregion

        #region Constructor
        protected DocFile()
        {
            this.ContentOutline = new List<string>();
            this.DocumentHeaders = new List<Config.DocumentHeader>();
        }

        public DocFile(string basePath, string relativePath, DocSet parent)
            : this()
        {
            this.BasePath = basePath;
            this.FullPath = Path.Combine(basePath, relativePath.Substring(1));
            this.DisplayName = relativePath;
            this.Parent = parent;
            this.DocumentHeaders = new List<Config.DocumentHeader>();
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

        protected virtual string GetContentsOfFile(string tags)
        {
            // Preprocess file content
            FileInfo docFile = new FileInfo(this.FullPath);
            TagProcessor tagProcessor = new TagProcessor(tags, Parent.SourceFolderPath);
            return tagProcessor.Preprocess(docFile);
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
                string fileContents = this.GetContentsOfFile(tags);
                fileContents = this.ParseAndRemoveYamlFrontMatter(fileContents, issues);
                return fileContents;
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
        private string ParseAndRemoveYamlFrontMatter(string contents, IssueLogger issues)
        {
            const string YamlFrontMatterHeader = "---";
            using (StringReader reader = new StringReader(contents))
            {
                string currentLine = reader.ReadLine();
                System.Text.StringBuilder frontMatter = new System.Text.StringBuilder();
                YamlFrontMatterDetectionState currentState = YamlFrontMatterDetectionState.NotDetected;
                while (currentLine != null && currentState != YamlFrontMatterDetectionState.SecondTokenFound)
                {
                    string trimmedCurrentLine = currentLine.Trim();
                    switch (currentState)
                    {
                        case YamlFrontMatterDetectionState.NotDetected:
                            if (!string.IsNullOrWhiteSpace(trimmedCurrentLine) && trimmedCurrentLine != YamlFrontMatterHeader)
                            {
                                var requiredYamlHeaders = DocSet.SchemaConfig.RequiredYamlHeaders;
                                if (requiredYamlHeaders.Any())
                                {
                                    issues.Error(ValidationErrorCode.RequiredYamlHeaderMissing, $"Missing required YAML headers: {requiredYamlHeaders.ComponentsJoinedByString(", ")}");
                                }

                                // This file doesn't have YAML front matter, so we just return the full contents of the file
                                return contents;
                            }
                            else if (trimmedCurrentLine == YamlFrontMatterHeader)
                            {
                                currentState = YamlFrontMatterDetectionState.FirstTokenFound;
                            }
                            break;
                        case YamlFrontMatterDetectionState.FirstTokenFound:
                            if (trimmedCurrentLine == YamlFrontMatterHeader)
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
                    // Parse YAML metadata
                    ParseYamlMetadata(frontMatter.ToString(), issues);
                    return reader.ReadToEnd();
                }
                else
                {
                    // Something went wrong along the way, so we just return the full file
                    return contents;
                }
            }
        }

        private void ParseYamlMetadata(string yamlMetadata, IssueLogger issues)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            string[] items = yamlMetadata.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string item in items)
            {
                try
                {
                    string[] keyValue = item.Split(':');
                    dictionary.Add(keyValue[0].Trim(), keyValue[1].Trim());
                }
                catch (Exception)
                {
                    issues.Error(ValidationErrorCode.IncorrectYamlHeaderFormat, $"Incorrect YAML header format after `{dictionary.Keys.Last()}`");
                }
            }

            List<string> missingHeaders = new List<string>();
            foreach (var header in DocSet.SchemaConfig.RequiredYamlHeaders)
            {
                string value;
                if (dictionary.TryGetValue(header, out value))
                {
                    value = value.Replace("\"", string.Empty);
                    if (string.IsNullOrWhiteSpace(value))
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

        private enum YamlFrontMatterDetectionState
        {
            NotDetected,
            FirstTokenFound,
            SecondTokenFound
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

        protected Config.DocumentHeader CreateHeaderFromBlock(Block block)
        {
            var header = new Config.DocumentHeader();
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
        public List<Config.DocumentHeader> DocumentHeaders
        {
            get; set;
        }

        /// <summary>
        /// Convert blocks of text found inside the markdown file into things we know how to work
        /// with (methods, resources, examples, etc).
        /// </summary>
        /// <param name="errors"></param>
        /// <returns></returns>
        protected bool ParseMarkdownBlocks(IssueLogger issues)
        {
            string methodTitle = null;
            string methodDescription = null;

            Block previousHeaderBlock = null;

            List<object> foundElements = new List<object>();

            Stack<Config.DocumentHeader> headerStack = new Stack<Config.DocumentHeader>();
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
                if (IsHeaderBlock(block))
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
                    else if (IsHeaderBlock(previousHeaderBlock))
                    {
                        methodDescription = block.Content;
                        issues.Message($"Found description: {methodDescription}");
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
            List<ValidationError> errors = new List<ValidationError>();
            if (this.Parent.DocumentStructure != null)
            {
                ValidateDocumentStructure(this.Parent.DocumentStructure.AllowedHeaders, this.DocumentHeaders, issues);
            }
        }

        private static bool ContainsMatchingDocumentHeader(Config.DocumentHeader expectedHeader, IReadOnlyList<Config.DocumentHeader> collection)
        {
            return collection.Any(h => h.Matches(expectedHeader));
        }

        private void ValidateDocumentStructure(IReadOnlyList<Config.DocumentHeader> expectedHeaders, IReadOnlyList<Config.DocumentHeader> foundHeaders, IssueLogger issues)
        {
            int expectedIndex = 0;
            int foundIndex = 0;

            while (expectedIndex < expectedHeaders.Count && foundIndex < foundHeaders.Count)
            {
                var expected = expectedHeaders[expectedIndex];
                var found = foundHeaders[foundIndex];

                if (expected.Matches(found))
                {
                    ValidateDocumentStructure(expected.ChildHeaders, found.ChildHeaders, issues);

                    // Found an expected header, keep going!
                    expectedIndex++;
                    foundIndex++;
                    continue;
                }

                if (!ContainsMatchingDocumentHeader(found, expectedHeaders))
                {
                    // This is an additional header that isn't in the expected header collection
                    issues.Warning(ValidationErrorCode.ExtraDocumentHeaderFound, $"A extra document header was found: {found.Title}");
                    ValidateDocumentStructure(new Config.DocumentHeader[0], found.ChildHeaders, issues);
                    foundIndex++;
                    continue;
                }
                else
                {
                    // If the current expected header is optional, we can move past it 
                    if (!expected.Required)
                    {
                        expectedIndex++;
                        continue;
                    }

                    bool expectedMatchesInFoundHeaders = ContainsMatchingDocumentHeader(expected, foundHeaders);
                    if (expectedMatchesInFoundHeaders)
                    {
                        // This header exists, but is in the wrong position
                        issues.Warning(ValidationErrorCode.DocumentHeaderInWrongPosition, $"An expected document header was found in the wrong position: {found.Title}");
                        foundIndex++;
                        continue;
                    }
                    else if (!expectedMatchesInFoundHeaders && expected.Required)
                    {
                        // Missing a required header!
                        issues.Error(ValidationErrorCode.RequiredDocumentHeaderMissing, $"A required document header is missing from the document: {expected.Title}");
                        expectedIndex++;
                    }
                    else
                    {
                        // Expected wasn't found and is optional, that's fine.
                        expectedIndex++;
                        continue;
                    }
                }
            }

            for (int i = foundIndex; i < foundHeaders.Count; i++)
            {
                issues.Warning(ValidationErrorCode.ExtraDocumentHeaderFound, $"A extra document header was found: {foundHeaders[i].Title}");
            }
            for (int i = expectedIndex; i < expectedHeaders.Count; i++)
            {
                if (expectedHeaders[i].Required)
                {
                    issues.Error(ValidationErrorCode.RequiredDocumentHeaderMissing, $"A required document header is missing from the document: {expectedHeaders[i].Title}");
                }
            }
        }

        private void AddHeaderToHierarchy(Stack<Config.DocumentHeader> headerStack, Block block)
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
        /// Remove <!-- and --> from the content string
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private static string StripHtmlCommentTags(string content)
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
                inferredNamespace = foundResource.Name.Substring(0, foundResource.Name.LastIndexOf('.'));
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
                .Select(x => new { x.TypeName, x.Namespace}).Distinct())
            {
                var possibleHeaderNames = this.Parent?.TableParserConfig?.TableDefinitions.Rules
                    .Where(r => r.Type == "EnumerationDefinition").SelectMany(r => r.ColumnNames["memberName"]) .ToList();

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

                            table.UsedIn.Add(onlyResource);
                            MergeParametersIntoCollection(
                                onlyResource.Parameters,
                                table.Rows.Cast<ParameterDefinition>(),
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
                        else
                        {
                            issues.Warning(ValidationErrorCode.AdditionalPropertyDetected,
                                $"Property '{param.Name}' found in resource definition for '{onlyResource.Name}', but not described in markdown table.");
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
                                    pairedRequest.AddExpectedResponse(GetBlockContent(code), annotation);
                                    responses.Add(pairedRequest);
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