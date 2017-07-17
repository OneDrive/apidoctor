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

namespace ApiDocs.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using ApiDocs.Validation.Error;
    using ApiDocs.Validation.TableSpec;
    using Tags;
    using MarkdownDeep;
    using Newtonsoft.Json;
    using System.Threading.Tasks;

    /// <summary>
    /// A documentation file that may contain one more resources or API methods
    /// </summary>
    public class DocFile
    {
        private const string PageAnnotationType = "#page.annotation";

        #region Instance Variables
        protected bool HasScanRun;
        protected string BasePath;

        private readonly List<ResourceDefinition> resources = new List<ResourceDefinition>();
        private readonly List<MethodDefinition> requests = new List<MethodDefinition>();
        private readonly List<ExampleDefinition> examples = new List<ExampleDefinition>();
        private readonly List<string> bookmarks = new List<string>();

        #endregion

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

        public AuthScopeDefinition[] AuthScopes { get; protected set; }

        public ErrorDefinition[] ErrorCodes { get; protected set; }

        public string[] LinkDestinations
        {
            get
            {
                var query = from p in this.MarkdownLinks
                    select p.Definition.url;
                return query.ToArray();
            }
        }

        /// <summary>
        /// Raw Markdown parsed blocks
        /// </summary>
        public Block[] OriginalMarkdownBlocks { get; set; }

        protected List<ILinkInfo> MarkdownLinks {get;set;}

        public DocSet Parent { get; protected set; }

        public PageAnnotation Annotation { get; set; }
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
        public virtual bool Scan(string tags, out ValidationError[] errors)
        {
            this.HasScanRun = true;
            List<ValidationError> detectedErrors = new List<ValidationError>();
            
            try
            {
                string fileContents = this.ReadAndPreprocessFileContents(tags);
				this.TransformMarkdownIntoBlocksAndLinks(fileContents, tags);
            }
            catch (IOException ioex)
            {
                detectedErrors.Add(new ValidationError(ValidationErrorCode.ErrorOpeningFile, this.DisplayName, "Error reading file contents: {0}", ioex.Message));
                errors = detectedErrors.ToArray();
                return false;
            }
            catch (Exception ex)
            {
                detectedErrors.Add(new ValidationError(ValidationErrorCode.ErrorReadingFile, this.DisplayName, "Error reading file contents: {0}", ex.Message));
                errors = detectedErrors.ToArray();
                return false;
            }

            return this.ParseMarkdownBlocks(out errors);
        }

        /// <summary>
        /// Read the contents of the file and perform all preprocessing activities that transform the input into the final markdown-only data.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public string ReadAndPreprocessFileContents(string tags)
        {
            try
            {
                string fileContents = this.GetContentsOfFile(tags);
                fileContents = this.ParseAndRemoveYamlFrontMatter(fileContents);
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
        private string ParseAndRemoveYamlFrontMatter(string contents)
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
					ParseYamlMetadata(frontMatter.ToString());
					return reader.ReadToEnd();
				}
				else
				{
					// Something went wrong along the way, so we just return the full file
					return contents;
				}
			}
		}

		private void ParseYamlMetadata(string yamlMetadata)
		{
			// TODO: Implement YAML parsing
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
            if (block.Content == null) return string.Empty;

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
        protected bool ParseMarkdownBlocks(out ValidationError[] errors)
        {
            List<ValidationError> detectedErrors = new List<ValidationError>();

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
                    detectedErrors.Add(new ValidationMessage(null, "Found title: {0}", methodTitle));
                }
                else if (block.BlockType == BlockType.p)
                {
                    if (null == previousHeaderBlock)
                    {
                        detectedErrors.Add(new ValidationWarning(ValidationErrorCode.MissingHeaderBlock, null, "Paragraph text found before a valid header: {0}", this.DisplayName));
                    }
                    else if (IsHeaderBlock(previousHeaderBlock))
                    {
                        methodDescription = block.Content;
                        detectedErrors.Add(new ValidationMessage(null, "Found description: {0}", methodDescription));
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
                        // html + codeblock = likely request or response!
                        ItemDefinition definition = null;
                        try
                        {
                            definition = this.ParseCodeBlock(block, nextBlock);
                        }
                        catch (Exception ex)
                        {
                            detectedErrors.Add(new ValidationError(ValidationErrorCode.MarkdownParserError, this.DisplayName, "{0}", ex.Message));
                        }

                        if (null != definition)
                        {
                            detectedErrors.Add(new ValidationMessage(null, "Found code block: {0} [{1}]", definition.Title, definition.GetType().Name));
                            definition.Title = methodTitle;
                            definition.Description = methodDescription;

                            if (!foundElements.Contains(definition))
                            {
                                foundElements.Add(definition);
                            }
                        }
                    }
                    else if (null == this.Annotation)
                    {
                        // See if this is the page-level annotation
                        PageAnnotation annotation = null;
                        try
                        {
                            annotation = this.ParsePageAnnotation(block);
                        }
                        catch (JsonReaderException readerEx)
                        {
                            detectedErrors.Add(new ValidationWarning(ValidationErrorCode.JsonParserException, this.DisplayName, "Unable to parse page annotation JSON: {0}", readerEx.Message));
                        }
                        catch (Exception ex)
                        {
                            detectedErrors.Add(new ValidationWarning(ValidationErrorCode.AnnotationParserException, this.DisplayName, "Unable to parse annotation: {0}", ex.Message));
                        }


                        if (null != annotation)
                        {
                            this.Annotation = annotation;
                            if (string.IsNullOrEmpty(this.Annotation.Title))
                            {
                                this.Annotation.Title = (from b in this.OriginalMarkdownBlocks where IsHeaderBlock(b, 1) select b.Content).FirstOrDefault();
                            }
                        }
                    }
                }
                else if (block.BlockType == BlockType.table_spec)
                {
                    Block blockBeforeTable = (i - 1 >= 0) ? this.OriginalMarkdownBlocks[i - 1] : null;
                    if (null == blockBeforeTable) continue;

                    ValidationError[] parseErrors;
                    var table = this.Parent.TableParser.ParseTableSpec(block, previousHeaderBlock, out parseErrors);
                    if (null != parseErrors) detectedErrors.AddRange(parseErrors);

                    detectedErrors.Add(new ValidationMessage(null, "Found table: {0}. Rows:\r\n{1}", table.Type,
                        (from r in table.Rows select JsonConvert.SerializeObject(r, Formatting.Indented)).ComponentsJoinedByString(" ,\r\n")));

                    foundElements.Add(table);
                }

                if (block.IsHeaderBlock())
                {
                    previousHeaderBlock = block;
                }
            }

            ValidationError[] postProcessingErrors;
            this.PostProcessFoundElements(foundElements, out postProcessingErrors);
            detectedErrors.AddRange(postProcessingErrors);
          
            errors = detectedErrors.ToArray();
            return !detectedErrors.Any(x => x.IsError);
        }

        /// <summary>
        /// Checks the document for outline errors compared to any required document structure.
        /// </summary>
        /// <returns></returns>
        public ValidationError[] CheckDocumentStructure()
        {
            List<ValidationError> errors = new List<ValidationError>();
            if (this.Parent.DocumentStructure != null)
            {
                errors.AddRange(ValidateDocumentStructure(this.Parent.DocumentStructure.AllowedHeaders, this.DocumentHeaders));
            }
            return errors.ToArray();
        }

        private static bool ContainsMatchingDocumentHeader(Config.DocumentHeader expectedHeader, IReadOnlyList<Config.DocumentHeader> collection)
        {
            return collection.Any(h => h.Matches(expectedHeader));
        }

        private ValidationError[] ValidateDocumentStructure(IReadOnlyList<Config.DocumentHeader> expectedHeaders, IReadOnlyList<Config.DocumentHeader> foundHeaders)
        {
            List<ValidationError> errors = new List<ValidationError>();

            int expectedIndex = 0;
            int foundIndex = 0;

            while (expectedIndex < expectedHeaders.Count && foundIndex < foundHeaders.Count)
            {
                var expected = expectedHeaders[expectedIndex];
                var found = foundHeaders[foundIndex];

                if (expected.Matches(found))
                {
                    errors.AddRange(ValidateDocumentStructure(expected.ChildHeaders, found.ChildHeaders));

                    // Found an expected header, keep going!
                    expectedIndex++;
                    foundIndex++;
                    continue;
                }

                if (!ContainsMatchingDocumentHeader(found, expectedHeaders))
                {
                    // This is an additional header that isn't in the expected header collection
                    errors.Add(new ValidationWarning(ValidationErrorCode.ExtraDocumentHeaderFound, this.DisplayName, "A extra document header was found: {0}", found.Title));
                    errors.AddRange(ValidateDocumentStructure(new Config.DocumentHeader[0], found.ChildHeaders));
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
                        errors.Add(new ValidationWarning(ValidationErrorCode.DocumentHeaderInWrongPosition, this.DisplayName, "An expected document header was found in the wrong position: {0}", found.Title));
                        foundIndex++;
                        continue;
                    }
                    else if (!expectedMatchesInFoundHeaders && expected.Required)
                    {
                        // Missing a required header!
                        errors.Add(new ValidationError(ValidationErrorCode.RequiredDocumentHeaderMissing, this.DisplayName, "A required document header is missing from the document: {0}", expected.Title));
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
                errors.Add(new ValidationWarning(ValidationErrorCode.ExtraDocumentHeaderFound, this.DisplayName, "A extra document header was found: {0}", foundHeaders[i].Title));
            }
            for (int i = expectedIndex; i < expectedHeaders.Count; i++)
            {
                if (expectedHeaders[i].Required)
                    errors.Add(new ValidationError(ValidationErrorCode.RequiredDocumentHeaderMissing, this.DisplayName, "A required document header is missing from the document: {0}", expectedHeaders[i].Title));
            }

            return errors.ToArray();
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
        /// <param name="elements"></param>
        /// <param name="postProcessingErrors"></param>
        private void PostProcessFoundElements(List<object> elements, out ValidationError[] postProcessingErrors)
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

            List<ValidationError> detectedErrors = new List<ValidationError>();

            var elementsFoundInDocument = elements as IList<object> ?? elements.ToList();

            var foundMethods = from s in elementsFoundInDocument
                               where s is MethodDefinition
                               select (MethodDefinition)s;

            var foundResources = from s in elementsFoundInDocument
                                 where s is ResourceDefinition
                                 select (ResourceDefinition)s;

            var foundTables = from s in elementsFoundInDocument
                                   where s is TableDefinition
                                   select (TableDefinition)s;

            this.PostProcessAuthScopes(elementsFoundInDocument);
            PostProcessResources(foundResources, foundTables, detectedErrors);
            this.PostProcessMethods(foundMethods, foundTables, detectedErrors);

            postProcessingErrors = detectedErrors.ToArray();
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

        private void PostProcessResources(IEnumerable<ResourceDefinition> foundResources, IEnumerable<TableDefinition> foundTables, List<ValidationError> detectedErrors)
        {
            if (foundResources.Count() == 1)
            {
                var onlyResource = foundResources.Single();
                foreach (var table in foundTables)
                {
                    switch (table.Type)
                    {
                        case TableBlockType.ResourcePropertyDescriptions:
                        case TableBlockType.ResourceNavigationPropertyDescriptions:
                            // Merge information found in the resource property description table with the existing resources
                            MergeParametersIntoCollection(
                                onlyResource.Parameters,
                                table.Rows.Cast<ParameterDefinition>(), 
                                onlyResource.Name,
                                detectedErrors,
                                table.Type == TableBlockType.ResourceNavigationPropertyDescriptions);
                            break;
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
            string resourceName,
            List<ValidationError> detectedErrors,
            bool addMissingParameters = false)
        {
            foreach (var param in additionalData)
            {
                // See if this parameter already is known
                var match = collection.SingleOrDefault(x => x.Name == param.Name);
                if (match != null)
                {
                    // The parameter was always known, let's merge in additional data.
                    match.AddMissingDetails(param);
                }
                else if (addMissingParameters)
                {
                    // Navigation propeties may not appear in the example text, so don't report and error
                    if (!param.IsNavigatable)
                    {
                        Console.WriteLine($"Found property '{param.Name}' in markdown table that wasn't defined in '{resourceName}': {this.DisplayName}");
                        detectedErrors.Add(new ValidationWarning(ValidationErrorCode.AdditionalPropertyDetected, this.DisplayName, $"Property '{param.Name}' found in markdown table but not in resource definition for '{resourceName}'."));
                    }
                    else
                    {
                        detectedErrors.Add(new ValidationMessage(this.DisplayName, $"Navigation property '{param.Name}' found in markdown table but not in resource definition for '{resourceName}'."));
                    }
                    // The parameter didn't exist in the collection, so let's add it.
                    collection.Add(param);
                }
                else if (!param.IsNavigatable)
                {
                    // Oops, we didn't find the property in the resource definition
                    Console.WriteLine($"Found property '{param.Name}' in markdown table that wasn't defined in '{resourceName}': {this.DisplayName}");
                    detectedErrors.Add(new ValidationWarning(ValidationErrorCode.AdditionalPropertyDetected, this.DisplayName, $"Property '{param.Name}' found in markdown table but not in resource definition for '{resourceName}'."));
                }
            }
        }


        private void PostProcessMethods(IEnumerable<MethodDefinition> foundMethods, IEnumerable<TableDefinition> foundTables, List<ValidationError> errors)
        {
            var totalMethods = foundMethods.Count();
            var totalTables = foundTables.Count();

            if (totalTables == 0)
                return;

            if (totalMethods == 0)
            {
                this.SetFoundTablesForFile(foundTables);
            }
            else if (totalMethods == 1)
            {
                var onlyMethod = foundMethods.Single();
                SetFoundTablesOnMethod(foundTables, onlyMethod, errors);
            }
            else
            {
                // TODO: Figure out how to map stuff when more than one method exists
                if (null != errors)
                {
                    var unmappedContentsError = new ValidationWarning(ValidationErrorCode.UnmappedDocumentElements, this.DisplayName, "Unable to map some markdown elements into schema.");

                    List<ValidationError> innerErrors = new List<ValidationError>();
                    var unmappedMethods = (from m in foundMethods select m.RequestMetadata.MethodName).ComponentsJoinedByString(", ");
                    if (!string.IsNullOrEmpty(unmappedMethods))
                    { 
                        innerErrors.Add(new ValidationMessage("Unmapped methods", unmappedMethods));
                    }

                    var unmappedTables = (from t in foundTables select string.Format("{0} - {1}", t.Title, t.Type)).ComponentsJoinedByString(", ");
                    if (!string.IsNullOrEmpty(unmappedTables))
                    {
                        innerErrors.Add(new ValidationMessage("Unmapped tables", unmappedTables));
                    }
                    unmappedContentsError.InnerErrors = innerErrors.ToArray();
                    errors.Add(unmappedContentsError);
                }
            }
        }

        private void SetFoundTablesOnMethod(IEnumerable<TableDefinition> foundTables, MethodDefinition onlyMethod, List<ValidationError> detectedErrors)
        {
            foreach (var table in foundTables)
            {
                switch (table.Type)
                {
                    case TableBlockType.Unknown:
                        // Unknown table format, nothing we can do with it.
                        break;
                    case TableBlockType.EnumerationValues:
                        // TODO: Support enumeration values
                        detectedErrors.Add(new ValidationWarning(ValidationErrorCode.Unknown, this.DisplayName, $"Table '{table.Title}' for method '{onlyMethod.RequestMetadata.MethodName}' included enum values that weren't parsed."));
                        break;
                    case TableBlockType.ErrorCodes:
                        onlyMethod.Errors = table.Rows.Cast<ErrorDefinition>().ToList();
                        break;
                    case TableBlockType.HttpHeaders:
                    case TableBlockType.PathParameters:
                    case TableBlockType.QueryStringParameters:
                        onlyMethod.Parameters.AddRange(table.Rows.Cast<ParameterDefinition>());
                        break;
                    case TableBlockType.RequestObjectProperties:
                        onlyMethod.RequestBodyParameters.AddRange(table.Rows.Cast<ParameterDefinition>());
                        break;
                    case TableBlockType.ResourcePropertyDescriptions:
                    case TableBlockType.ResponseObjectProperties:
                        detectedErrors.Add(new ValidationWarning(ValidationErrorCode.Unknown, this.DisplayName, $"Table '{table.Title}' for method '{onlyMethod.RequestMetadata.MethodName}' included response properties that were ignored."));
                        break;
                    default:
                        detectedErrors.Add(new ValidationWarning(ValidationErrorCode.Unknown, this.DisplayName, $"Table '{table.Title}' ({table.Type}) for method '{onlyMethod.RequestMetadata.MethodName}' was unsupported and ignored."));
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
        /// <param name="metadata"></param>
        /// <param name="code"></param>
        public ItemDefinition ParseCodeBlock(Block metadata, Block code)
        {
            if (metadata.BlockType != BlockType.html)
                throw new ArgumentException("metadata block does not appear to be metadata");

            if (code.BlockType != BlockType.codeblock)
                throw new ArgumentException("code block does not appear to be code");

            
            var metadataJsonString = StripHtmlCommentTags(metadata.Content);
            CodeBlockAnnotation annotation = CodeBlockAnnotation.ParseMetadata(metadataJsonString, code);

            switch (annotation.BlockType)
            {
                case CodeBlockType.Resource:
                    {
                        ResourceDefinition resource;
                        if (code.CodeLanguage.Equals("json", StringComparison.OrdinalIgnoreCase))
                        {
                            resource = new JsonResourceDefinition(annotation, code.Content, this);
                        }
                        //else if (code.CodeLanguage.Equals("xml", StringComparison.OrdinalIgnoreCase))
                        //{
                        //
                        //}
                        else
                        {
                            throw new NotSupportedException("Unsupported resource definition language: " + code.CodeLanguage);
                        }

                        if (string.IsNullOrEmpty(resource.Name))
                        {
                            throw new InvalidDataException("Resource definition is missing a name");
                        }

                        this.resources.Add(resource);
                        return resource;
                    }
                case CodeBlockType.Request:
                    {
                        var method = MethodDefinition.FromRequest(code.Content, annotation, this);
                        if (string.IsNullOrEmpty(method.Identifier))
                            method.Identifier = string.Format("{0} #{1}", this.DisplayName, this.requests.Count);
                        this.requests.Add(method);
                        return method;
                    }

                case CodeBlockType.Response:
                    {
                        MethodDefinition pairedRequest = null;
                        if (!string.IsNullOrEmpty(annotation.MethodName))
                        {
                            // Look up paired request by name
                            pairedRequest = (from m in this.requests where m.Identifier == annotation.MethodName select m).FirstOrDefault();
                        }
                        else if (this.requests.Any())
                        {
                            pairedRequest = Enumerable.Last(this.requests);
                        }

                        if (null == pairedRequest)
                        {
                            throw new InvalidOperationException(string.Format("Unable to locate the corresponding request for response block: {0}. Requests must be defined before a response.", annotation.MethodName));
                        }

                        pairedRequest.AddExpectedResponse(code.Content, annotation);
                        return pairedRequest;
                    }
                case CodeBlockType.Example:
                    {
                        var example = new ExampleDefinition(annotation, code.Content, this, code.CodeLanguage);
                        this.examples.Add(example);
                        return example;
                    }
                case CodeBlockType.Ignored:
                    return null;
                case CodeBlockType.SimulatedResponse:
                    {
                        var method = Enumerable.Last(this.requests);
                        method.AddSimulatedResponse(code.Content, annotation);
                        return method;
                    }
                case CodeBlockType.TestParams:
                    {
                        var method = Enumerable.Last(this.requests);
                        method.AddTestParams(code.Content);
                        return method;
                    }
                default:
                    {
                    	var errorMessage = string.Format("Unable to parse metadata block or unsupported block type. Line {1}. Content: {0}", metadata.Content, metadata.LineStart);
                        throw new NotSupportedException(errorMessage);
                    }
            }
        }

        #endregion

        #region Link Verification

        public bool ValidateNoBrokenLinks(bool includeWarnings, out ValidationError[] errors, bool requireFilenameCaseMatch)
        {
            string[] files;
            return this.ValidateNoBrokenLinks(includeWarnings, out errors, out files, requireFilenameCaseMatch);
        }

        /// <summary>
        /// Checks all links detected in the source document to make sure they are valid.
        /// </summary>
        /// <param name="includeWarnings"></param>
        /// <param name="errors">Information about broken links</param>
        /// <param name="linkedDocFiles"></param>
        /// <returns>True if all links are valid. Otherwise false</returns>
        public bool ValidateNoBrokenLinks(bool includeWarnings, out ValidationError[] errors, out string[] linkedDocFiles, bool requireFilenameCaseMatch)
        {
            if (!this.HasScanRun)
                throw new InvalidOperationException("Cannot validate links until Scan() is called.");

            List<string> linkedPages = new List<string>();

            // If there are no links in this document, just skip the validation.
            if (this.MarkdownLinks == null)
            {
                errors = new ValidationError[0];
                linkedDocFiles = new string[0];
                return true;
            }

            var foundErrors = new List<ValidationError>();
            foreach (var link in this.MarkdownLinks)
            {
                if (null == link.Definition)
                {
                    // Don't treat TAGS or END markers like links
                    if (!link.Text.ToUpper().Equals("END") && !link.Text.ToUpper().StartsWith("TAGS="))
                    {
                        foundErrors.Add(new ValidationError(ValidationErrorCode.MissingLinkSourceId, this.DisplayName, 
                            "Link ID '[{0}]' used in document by not defined. Define with '[{0}]: url' or remove square brackets.", link.Text));
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
                            foundErrors.Add(new ValidationWarning(ValidationErrorCode.LinkValidationSkipped, this.DisplayName, "Skipped validation of external link '[{1}]({0})'", link.Definition.url, link.Text));
                        break;
                    case LinkValidationResult.FileNotFound:
                        foundErrors.Add(new ValidationError(ValidationErrorCode.LinkDestinationNotFound, this.DisplayName, "FileNotFound: '[{1}]({0})'. {2}", link.Definition.url, link.Text, suggestion));
                        break;
                    case LinkValidationResult.BookmarkMissing:
                        foundErrors.Add(new ValidationError(ValidationErrorCode.LinkDestinationNotFound, this.DisplayName, "BookmarkMissing: '[{1}]({0})'. {2}", link.Definition.url, link.Text, suggestion));
                        break;
                    case LinkValidationResult.ParentAboveDocSetPath:
                        foundErrors.Add(new ValidationError(ValidationErrorCode.LinkDestinationOutsideDocSet, this.DisplayName, "Relative link outside of doc set: '[{1}]({0})'.", link.Definition.url, link.Text));
                        break;
                    case LinkValidationResult.UrlFormatInvalid:
                        foundErrors.Add(new ValidationError(ValidationErrorCode.LinkFormatInvalid, this.DisplayName, "InvalidUrlFormat '[{1}]({0})'.", link.Definition.url, link.Text));
                        break;
                    case LinkValidationResult.Valid:
                        foundErrors.Add(new ValidationMessage(this.DisplayName, "Valid link '[{1}]({0})'.", link.Definition.url, link.Text));
                        if (null != relativeFileName)
                        {
                            linkedPages.Add(relativeFileName);
                        }
                        break;
                    default:
                        foundErrors.Add(new ValidationError(ValidationErrorCode.Unknown, this.DisplayName, "{2}: Link '[{1}]({0})'.", link.Definition.url, link.Text, result));
                        break;

                }
                
            }
            errors = foundErrors.ToArray();
            linkedDocFiles = linkedPages.Distinct().ToArray();
            return !(errors.WereErrors() || errors.WereWarnings());
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
    }

}
