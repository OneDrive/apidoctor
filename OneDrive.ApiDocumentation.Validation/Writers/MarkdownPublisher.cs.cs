namespace OneDrive.ApiDocumentation.Validation
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.ComponentModel;
    using MarkdownDeep;
    using System.Linq;

	public class MarkdownPublisher : DocumentPublisher
	{
		public MarkdownPublisher(DocSet docset) : base (docset)
		{
            SourceFileExtensions = ".md,.mdown";
            SkipPaths = "\\internal;\\.git;\\legacy;\\generate_html_docs;\\.gitignore;\\.gitattributes";
		}

		/// <summary>
		/// Scans the text content of a file and removes any "internal" comments/references
		/// </summary>
		/// <param name="file">File.</param>
        protected override async Task PublishFileToDestination(FileInfo sourceFile, DirectoryInfo destinationRoot, DocFile page)
		{
            LogMessage(new ValidationMessage(sourceFile.Name, "Scanning text file for internal content."));

            var outputPath = GetPublishedFilePath(sourceFile, destinationRoot);
            var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8) { AutoFlush = true };

			StreamReader reader = new StreamReader(sourceFile.OpenRead());

			long lineNumber = 0;
			string nextLine;
			while ( (nextLine = await reader.ReadLineAsync()) != null)
			{
				lineNumber++;
				if (IsDoubleBlockQuote(nextLine))
				{
                    LogMessage(new ValidationMessage(string.Concat(sourceFile, ":", lineNumber), "Removing DoubleBlockQuote: {0}", nextLine));
					continue;
				}
				await writer.WriteLineAsync(nextLine);
			}
			writer.Close();
			reader.Close();
		}
	}
}
