namespace ApiDocs.Validation.Writers
{
    using System.IO;
    using System.Threading.Tasks;

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
