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
        private List<MarkdownDeep.Block> m_CodeBlocks = new List<MarkdownDeep.Block>();
        private List<ResourceDefinition> m_Resources = new List<ResourceDefinition>();
        private List<MethodDefinition> m_Requests = new List<MethodDefinition>();
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

        /// <summary>
        /// Raw Markdown parsed blocks
        /// </summary>
        private MarkdownDeep.Block[] Blocks { get; set; }

        #endregion

        #region Constructor
        public DocFile(string basePath, string relativePath)
        {
            FullPath = Path.Combine(basePath, relativePath.Substring(1));
            DisplayName = relativePath;
        }
        #endregion

        #region Markdown Parsing
        /// <summary>
        /// Read the contents of the file into blocks and generate any resource or method definitions from the contents
        /// </summary>
        public void Scan()
        {
            MarkdownDeep.Markdown md = new MarkdownDeep.Markdown();
            md.SafeMode = false;
            md.ExtraMode = true;
            
            using (StreamReader reader = File.OpenText(this.FullPath))
            {
                HtmlContent = md.Transform(reader.ReadToEnd());
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
                var htmlComment = m_CodeBlocks[i];
                if (htmlComment.BlockType != MarkdownDeep.BlockType.html)
                {
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
                    Console.WriteLine("Warning: file {0} has an invalid format: {1}", FullPath, ex.Message);
                }
                i += 2;
            }

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
                        m_Resources.Add(new ResourceDefinition(annotation, code.Content));
                        break;
                    }
                case CodeBlockType.Request:
                    {
                        var method = MethodDefinition.FromRequest(code.Content, annotation);
                        method.DisplayName = string.Format("{0} #{1}", DisplayName, m_Requests.Count);
                        m_Requests.Add(method);
                        break;
                    }

                case CodeBlockType.Response:
                    {
                        var method = m_Requests.Last();
                        method.AddResponse(code.Content, annotation);
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

    }

    public enum DocType
    {
        Unknown = 0,
        Resource,
        MethodRequest
    }
}
