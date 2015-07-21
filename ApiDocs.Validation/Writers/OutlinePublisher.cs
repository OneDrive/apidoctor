using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ApiDocs.Validation
{
    public class OutlinePublisher : DocumentPublisher
    {

        public OutlinePublisher(DocSet docset)
            : base(docset)
        {

        }

        public override async Task PublishToFolderAsync(string outputFolder)
        {
            StreamWriter writer = new StreamWriter(Path.Combine(outputFolder, "outline.txt")) { AutoFlush = true };
            foreach (var doc in this.Documents.Files)
            {
                await writer.WriteLineAsync("### File: " + doc.DisplayName + " ###");
                foreach (var topic in doc.ContentOutline)
                {
                    await writer.WriteLineAsync(topic);
                }
                await writer.WriteLineAsync();
            }
            writer.Flush();
            writer.Dispose();
        }
    }
}
