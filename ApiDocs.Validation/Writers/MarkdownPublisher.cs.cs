namespace ApiDocs.Validation.Writers
{
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using ApiDocs.Validation.Error;

    public class MarkdownPublisher : DocumentPublisher
	{
		public MarkdownPublisher(DocSet docset) : base (docset)
		{
		    this.SourceFileExtensions = ".md,.mdown";
		    this.SkipPaths = "\\internal;\\.git;\\legacy;\\generate_html_docs;\\.gitignore;\\.gitattributes";
		}

	    /// <summary>
	    /// Scans the text content of a file and removes any "internal" comments/references
	    /// </summary>
	    /// <param name="sourceFile">File.</param>
	    /// <param name="destinationRoot"></param>
	    /// <param name="page"></param>
	    protected override async Task PublishFileToDestinationAsync(FileInfo sourceFile, DirectoryInfo destinationRoot, DocFile page)
		{
		    this.LogMessage(new ValidationMessage(sourceFile.Name, "Scanning text file for internal content."));

            var outputPath = this.GetPublishedFilePath(sourceFile, destinationRoot);
            var writer = new StreamWriter(outputPath, false, Encoding.UTF8) { AutoFlush = true };

			StreamReader reader = new StreamReader(sourceFile.OpenRead());

			long lineNumber = 0;
			string nextLine;
			while ( (nextLine = await reader.ReadLineAsync()) != null)
			{
				lineNumber++;
				if (this.IsDoubleBlockQuote(nextLine))
				{
				    this.LogMessage(new ValidationMessage(string.Concat(sourceFile, ":", lineNumber), "Removing DoubleBlockQuote: {0}", nextLine));
					continue;
				}
				await writer.WriteLineAsync(nextLine);
			}
			writer.Close();
			reader.Close();
		}
	}
}
