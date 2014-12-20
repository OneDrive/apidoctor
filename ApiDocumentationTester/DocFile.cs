using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ApiDocumentationTester
{
    public class DocFile
    {

        public string DisplayName { get; private set; }

        public string FullPath { get; private set; }

        public DocType Type { get; set; }

        public string HtmlContent { get; private set; }

        private MarkdownDeep.Block[] Blocks { get; set; }

        private List<MarkdownDeep.Block> m_CodeBlocks = new List<MarkdownDeep.Block>();

        public DocFile(string basePath, string relativePath)
        {
            FullPath = Path.Combine(basePath, relativePath.Substring(1));
            DisplayName = relativePath;
            Type = DocType.Unknown;
        }

        /// <summary>
        /// Load details about what's defined in the file into the class
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
                Console.WriteLine("Block: {0}: {1}", block.BlockType, block.Content);    
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
        }

        public MarkdownDeep.Block[] CodeBlocks
        {
            get { return m_CodeBlocks.ToArray(); }
        }
    }

    public enum DocType
    {
        Unknown = 0,
        Resource,
        Method,
        Facet
    }
}
