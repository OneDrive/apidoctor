namespace OneDrive.ApiDocumentation.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.IO;

    /// <summary>
    /// A documentation file that may contain one more resources or API methods
    /// </summary>
    public class DocFile
    {
        #region Instance Variables
        private string m_BasePath;
        private List<MarkdownDeep.Block> m_CodeBlocks = new List<MarkdownDeep.Block>();
        private List<ResourceDefinition> m_Resources = new List<ResourceDefinition>();
        private List<MethodDefinition> m_Requests = new List<MethodDefinition>();
        private List<MarkdownDeep.LinkInfo> m_Links = new List<MarkdownDeep.LinkInfo>();
        #endregion

        #region Properties
        /// <summary>
        /// Friendly name of the file
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// Path to the file on disk
        /// </summary>
        public string FullPath { get; private set; }

        /// <summary>
        /// HTML-rendered version of the markdown source (for displaying)
        /// </summary>
        public string HtmlContent { get; private set; }

        public ResourceDefinition[] Resources
        {
            get { return m_Resources.ToArray(); }
        }

        public MethodDefinition[] Requests
        {
            get { return m_Requests.ToArray(); }
        }

        public string[] LinkDestinations
        {
            get
            {
                var query = from p in m_Links
                            select p.def.url;
                return query.ToArray();
            }
        }

        /// <summary>
        /// Raw Markdown parsed blocks
        /// </summary>
        private MarkdownDeep.Block[] Blocks { get; set; }

        #endregion

        #region Constructor
        public DocFile(string basePath, string relativePath)
        {
            m_BasePath = basePath;
            FullPath = Path.Combine(basePath, relativePath.Substring(1));
            DisplayName = relativePath;
        }
        #endregion

        #region Markdown Parsing
        /// <summary>
        /// Read the contents of the file into blocks and generate any resource or method definitions from the contents
        /// </summary>
        public bool Scan(out ValidationError[] errors)
        {
            List<ValidationError> detectedErrors = new List<ValidationError>();

            MarkdownDeep.Markdown md = new MarkdownDeep.Markdown();
            md.SafeMode = false;
            md.ExtraMode = true;
            try
            {
                using (StreamReader reader = File.OpenText(this.FullPath))
                {
                    HtmlContent = md.Transform(reader.ReadToEnd());
                }
            }
            catch (IOException ioex)
            {
                detectedErrors.Add(new ValidationError(ValidationErrorCode.ErrorOpeningFile, DisplayName, "Error reading file contents: {0}", ioex.Message));
                errors = detectedErrors.ToArray();
                return false;
            }
            catch (Exception ex)
            {
                detectedErrors.Add(new ValidationError(ValidationErrorCode.ErrorReadingFile, DisplayName, "Error reading file contents: {0}", ex.Message));
                errors = detectedErrors.ToArray();
                return false;
            }

            Blocks = md.Blocks;
            
            // Scan through the blocks to find something interesting
            m_CodeBlocks.Clear();
            foreach (var block in Blocks)
            {
                switch (block.BlockType)
                {
                    case MarkdownDeep.BlockType.codeblock:
                    case MarkdownDeep.BlockType.html:
                        m_CodeBlocks.Add(block);
                        break;
                    default:
                        break;
                }
            }

            for (int i = 0; i < m_CodeBlocks.Count;)
            {
                // We're looking for pairs of html + code blocks. The HTML block contains metadata about the block.
                // If we don't find an HTML block, then we skip the code block.
                var htmlComment = m_CodeBlocks[i];
                if (htmlComment.BlockType != MarkdownDeep.BlockType.html)
                {
                    detectedErrors.Add(new ValidationMessage(FullPath, "Block skipped - expected HTML comment, found: {0}", htmlComment.BlockType, htmlComment.Content));
                    
                    i++;
                    continue;
                }

                var codeBlock = m_CodeBlocks[i + 1];
                try 
                {
                    ParseCodeBlock(htmlComment, codeBlock);
                } 
                catch (Exception ex)
                {
                    detectedErrors.Add(new ValidationError(ValidationErrorCode.MarkdownParserError, FullPath, "Exception while parsing code block: {0}.", ex.Message));
                }
                i += 2;
            }

            m_Links.AddRange(md.FoundLinks);

            errors = detectedErrors.ToArray();
            return detectedErrors.Count == 0;
        }

        /// <summary>
        /// Convert an annotation and fenced code block in the documentation into something usable
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="code"></param>
        public void ParseCodeBlock(MarkdownDeep.Block metadata, MarkdownDeep.Block code)
        {
            if (metadata.BlockType != MarkdownDeep.BlockType.html)
                throw new ArgumentException("metadata block does not appear to be metadata");
            if (code.BlockType != MarkdownDeep.BlockType.codeblock)
                throw new ArgumentException("code block does not appear to be code");

            var metadataJsonString = metadata.Content.Substring(4, metadata.Content.Length - 9);
            var annotation = CodeBlockAnnotation.FromJson(metadataJsonString);

            switch (annotation.BlockType)
            {
                case CodeBlockType.Resource:
                    {
                        m_Resources.Add(new ResourceDefinition(annotation, code.Content, this));
                        break;
                    }
                case CodeBlockType.Request:
                    {
                        var method = MethodDefinition.FromRequest(code.Content, annotation, this);
                        if (string.IsNullOrEmpty(method.DisplayName))
                            method.DisplayName = string.Format("{0} #{1}", DisplayName, m_Requests.Count);
                        m_Requests.Add(method);
                        break;
                    }

                case CodeBlockType.Response:
                    {
                        var method = m_Requests.Last();
                        method.AddExpectedResponse(code.Content, annotation);
                        break;
                    }
                case CodeBlockType.Ignored:
                    break;
                default:
                    {
                        throw new NotSupportedException("Unsupported block type: " + annotation.BlockType);
                    }
            }
        }

        public MarkdownDeep.Block[] CodeBlocks
        {
            get { return m_CodeBlocks.ToArray(); }
        }
        #endregion

        #region Link Verification

        /// <summary>
        /// Checks all links detected in the source document to make sure they are valid.
        /// </summary>
        /// <param name="errors">Information about broken links</param>
        /// <returns>True if all links are valid. Otherwise false</returns>
        public bool ValidateNoBrokenLinks(bool includeWarnings, out ValidationError[] errors)
        {
            var foundErrors = new List<ValidationError>();
            foreach (var link in m_Links)
            {
                if (null == link.def)
                {
                    foundErrors.Add(new ValidationError(ValidationErrorCode.MissingLinkSourceId, this.DisplayName, "Link specifies ID '{0}' which was not found in the document.", link.link_text));
                    continue;
                }

                var result = VerifyLink(FullPath, link.def.url, m_BasePath);
                switch (result)
                {
                    case LinkValidationResult.BookmarkSkipped:
                    case LinkValidationResult.ExternalSkipped:
                        if (includeWarnings)
                            foundErrors.Add(new ValidationWarning(ValidationErrorCode.LinkValidationSkipped, this.DisplayName, "Skipped validation of link '{1}' to URL '{0}'", link.def.url, link.link_text));
                        break;
                    case LinkValidationResult.FileNotFound:
                        foundErrors.Add(new ValidationError(ValidationErrorCode.LinkDestinationNotFound, this.DisplayName, "Destination missing for link '{1}' to URL '{0}'", link.def.url, link.link_text));
                        break;
                    case LinkValidationResult.ParentAboveDocSetPath:
                        foundErrors.Add(new ValidationError(ValidationErrorCode.LinkDestinationOutsideDocSet, this.DisplayName, "Destination outside of doc set for link '{1}' to URL '{0}'", link.def.url, link.link_text));
                        break;
                    case LinkValidationResult.UrlFormatInvalid:
                        foundErrors.Add(new ValidationError(ValidationErrorCode.LinkFormatInvalid, this.DisplayName, "Invalid URL format for link '{1}' to URL '{0}'", link.def.url, link.link_text));
                        break;
                    case LinkValidationResult.Valid:
                        break;
                    default:
                        foundErrors.Add(new ValidationError(ValidationErrorCode.Unknown, this.DisplayName, "{2}: for link '{1}' to URL '{0}'", link.def.url, link.link_text, result));
                        break;

                }
            }

            errors = foundErrors.ToArray();
            return errors.Length == 0;
        }

        private enum LinkValidationResult
        {
            Valid,
            FileNotFound,
            UrlFormatInvalid,
            ExternalSkipped,
            BookmarkSkipped,
            ParentAboveDocSetPath
        }

        private static LinkValidationResult VerifyLink(string docFilePath, string linkUrl, string docSetBasePath)
        {
            Uri parsedUri;
            var validUrl = Uri.TryCreate(linkUrl, UriKind.RelativeOrAbsolute, out parsedUri);

            FileInfo sourceFile = new FileInfo(docFilePath);

            if (validUrl)
            {
                if (parsedUri.IsAbsoluteUri && (parsedUri.Scheme == "http" || parsedUri.Scheme == "https"))
                {
                    // TODO: verify the URL is valid
                    return LinkValidationResult.ExternalSkipped;
                }
                else if (linkUrl.StartsWith("#"))
                {
                    // TODO: bookmark link within the same document
                    return LinkValidationResult.BookmarkSkipped;
                }
                else
                {
                    var rootPath = sourceFile.DirectoryName;
                    if (linkUrl.Contains("#"))
                    {
                        linkUrl = linkUrl.Substring(0, linkUrl.IndexOf("#"));
                    }
                    while (linkUrl.StartsWith(".." + Path.DirectorySeparatorChar))
                    {
                        var nextLevelParent = new DirectoryInfo(rootPath).Parent;
                        rootPath = nextLevelParent.FullName;
                        linkUrl = linkUrl.Substring(3);
                    }

                    if (rootPath.Length < docSetBasePath.Length)
                    {
                        return LinkValidationResult.ParentAboveDocSetPath;
                    }

                    var pathToFile = Path.Combine(rootPath, linkUrl);
                    if (!File.Exists(pathToFile))
                    {
                        return LinkValidationResult.FileNotFound;
                    }
                }
            }
            else
            {
                return LinkValidationResult.UrlFormatInvalid;
            }

            return LinkValidationResult.Valid;
        }

        #endregion

    }

    public enum DocType
    {
        Unknown = 0,
        Resource,
        MethodRequest
    }
}
