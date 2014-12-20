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
        private Dictionary<string, Resource> m_Resources = new Dictionary<string, Resource>();
        private List<RequestResponse> m_Requests = new List<RequestResponse>();

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

            for (int i = 0; i < m_CodeBlocks.Count; i += 2)
            {
                var htmlComment = m_CodeBlocks[i];
                var codeBlock = m_CodeBlocks[i + 1];

                try 
                {
                    ParseCodeBlock(htmlComment, codeBlock);
                } 
                catch (Exception)
                {
                    Console.WriteLine("Warning: file has an invalid format.");
                }
            }

        }

        public void ParseCodeBlock(MarkdownDeep.Block metadata, MarkdownDeep.Block code)
        {
            if (metadata.BlockType != MarkdownDeep.BlockType.html)
                throw new ArgumentException("metadata block does not appear to be metadata");
            if (code.BlockType != MarkdownDeep.BlockType.codeblock)
                throw new ArgumentException("code block does not appear to be code");

            var metadataJsonString = metadata.Content.Substring(4, metadata.Content.Length - 8);
            var metadataObject = Newtonsoft.Json.JsonConvert.DeserializeObject(metadataJsonString);

            var resourceType = (string)((Newtonsoft.Json.Linq.JContainer)metadataObject)["@odata.type"];
            var blockType = (string)((Newtonsoft.Json.Linq.JContainer)metadataObject)["blockType"];

            if (blockType == "resource")
            {
                m_Resources.Add(resourceType, new Resource { OdataType = resourceType, JsonFormat = code.Content });
            }
            else if (blockType == "request")
            {
                m_Requests.Add(new RequestResponse { Request = code.Content });
            }
            else if (blockType == "response")
            {
                m_Requests.Last().Response = code.Content;
            }
        }

        public MarkdownDeep.Block[] CodeBlocks
        {
            get { return m_CodeBlocks.ToArray(); }
        }

        public IReadOnlyDictionary<string, Resource> Resources
        {
            get { return m_Resources; }
        }

        public RequestResponse[] Requests
        {
            get { return m_Requests.ToArray(); }
        }
    }

    public enum DocType
    {
        Unknown = 0,
        Resource,
        MethodRequest
    }
}
