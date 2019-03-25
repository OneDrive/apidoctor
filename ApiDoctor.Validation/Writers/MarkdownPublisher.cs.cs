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

namespace ApiDoctor.Validation.Writers
{
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using ApiDoctor.Validation.Error;

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
		    this.LogMessage(new ValidationMessage(sourceFile.Name, sourceFile.Name, "Scanning text file for internal content."));

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
