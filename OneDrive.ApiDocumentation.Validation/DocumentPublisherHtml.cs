using System;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace OneDrive.ApiDocumentation.Validation
{
    public class DocumentPublisherHtml : DocumentPublisher
    {
        public DocumentPublisherHtml(string sourceFolder) 
            : base(sourceFolder)
        {

        }

        protected override async Task PublishFileToDestination(FileInfo sourceFile, DirectoryInfo destinationRoot)
        {
            LogMessage(new ValidationMessage(sourceFile.Name, "Publishing file to HTML"));

            var destinationPath = PublishedFilePath(sourceFile, destinationRoot, ".htm");

            StringWriter writer = new StringWriter();
            StreamReader reader = new StreamReader(sourceFile.OpenRead());

            long lineNumber = 0;
            string nextLine;
            while ((nextLine = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;
                if (IsDoubleBlockQuote(nextLine))
                {
                    LogMessage(new ValidationMessage(string.Concat(sourceFile.Name, ":", lineNumber), "Removing DoubleBlockQuote"));
                    continue;
                }
                await writer.WriteLineAsync(nextLine);
            }

            var converter = new MarkdownDeep.Markdown();
            converter.ExtraMode = true;
            converter.NewWindowForExternalLinks = true;
            converter.SafeMode = true;
            converter.AutoHeadingIDs = true;
            converter.QualifyUrl = (string url) =>
            {
                if (url.StartsWith("#"))
                    return url;
                if (MarkdownDeep.Utils.IsUrlFullyQualified(url))
                    return url;

                // if the URL is a relative URL to a SourceFileExtension
                // we rewrite the URL to be to an .htm file.
                foreach(var extension in scannableExtensions)
                {
                    if (url.EndsWith(extension))
                    {
                        return url.Substring(0, url.Length - extension.Length) + ".htm";
                    }
                }

                return url;
            };

            var html = converter.Transform(writer.ToString());

            var title = (from b in converter.Blocks
                                      where b.BlockType == MarkdownDeep.BlockType.h1
                select b.Content).FirstOrDefault();


            using (var outputWriter = new StreamWriter(destinationPath))
            {
                outputWriter.Write(htmlHeader, title, htmlStyles);
                outputWriter.Write(html);
                outputWriter.Write(htmlFooter);
            }
        }

        private const string htmlHeader = @"<html>
<head>
<title>{0}</title>
<style>
{1}
</style>
</head>
<body>
";
        private const string htmlStyles = @"
body { font-family: sans-serif; }
p { font-family: sans-serif; }
tr { font-family: sans-serif; }
table { border-collapse: collapse; }
table td, table th { border: 1px solid #ddd; padding: 6px 13px } 
table th { font-weight: bold; }
table tr:nth-child(2n) { background: #f8f8f8; }
table pre { font-size: 100%; }
pre { background-color: rgba(0,0,0,0.04); margin: 1em 0px; padding: 16px; 
      overflow: auto; font-size: 85%; line-height: 1.45; 
      background-color: #f7f7f7; border-radius: 3px; width: 90%}
code { padding: 0; padding-top: 0.2em; padding-bottom: 0.2em; margin: 0; 
font-size: 85%; border-radius: 3px; background-color: rgba(0,0,0,0.04); }
pre code { background-color: transparent; font-size: 100%; }
a { text-decoration: none; color: #4183c4 }
h1 { padding-bottom: 0.3em; font-size: 2.25em; line-height: 1.2; border-bottom: 1px solid #eee; }
h2 { padding-bottom: 0.3em; font-size: 1.75em; line-height: 1.225; border-bottom: 1px solid #eee; }
h5 { font-size: 1em; }
h1, h2, h3, h4, h5 { margin-top: 1em; margin-bottom: 16px; font-weight: bold; line-height: 1.4;}
";

        private const string htmlFooter = @"
</body></html>
";
    }
}

