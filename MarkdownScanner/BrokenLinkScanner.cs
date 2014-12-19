using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace MarkdownScanner
{
    public class BrokenLinkScanner
    {
        public ScannerOptions Options { get; private set;}

        public BrokenLinkScanner(ScannerOptions options)
        {
            this.Options = options;
        }


        public void Scan()
        {
            Scan(Options.SourcePath);
        }

        private void Scan(string startPath)
        {
            DirectoryInfo source = new DirectoryInfo(startPath);

            var files = from p in source.GetFiles()
                                 where p.Extension == Options.FileExtension
                                 select p.FullName;

            var md = new MarkdownDeep.Markdown();
            md.ExtraMode = true;
            md.SafeMode = false;

            foreach (var filePath in files)
            {
                using (var inputReader = File.OpenText(filePath))
                {
                    var inputText = inputReader.ReadToEnd();
                    string htmlOutput = md.Transform(inputText);

                    var links = md.FoundLinks;
                    foreach (var link in links)
                    {
                        VerifyLink(filePath, link.def.url);
                    }
                }
            }

            if (!Options.Recursive)
            {
                return;
            }

            foreach (var folder in source.GetDirectories())
            {
                Scan(folder.FullName);
            }
        }

        void VerifyLink(string linkSource, string linkUrl)
        {

//            Console.WriteLine("Verify link from {0} to {1}", linkSource, linkUrl);

            Uri parsedUri;
            var validUrl = Uri.TryCreate(linkUrl, UriKind.RelativeOrAbsolute, out parsedUri);

            FileInfo sourceFile = new FileInfo(linkSource);
            var sourceFileName = Path.Combine(sourceFile.DirectoryName.Substring(Options.SourcePath.Length), sourceFile.Name);

            if (validUrl)
            {
                if (parsedUri.IsAbsoluteUri)
                {
                    // TODO: verify the URL is valid
                    if (Options.Verbose) Console.WriteLine("Warning: didn't verify external link: {0}", linkUrl);
                }
                else if (linkUrl.StartsWith("#"))
                {
                    // TODO: bookmark link within the same document
                    if (Options.Verbose) Console.WriteLine("Warning: didn't verify link to bookmark: {0}", linkUrl);
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
                      

                    var pathToFile = Path.Combine(rootPath, linkUrl);

                    if (!File.Exists(pathToFile))
                    {
                        Console.WriteLine("Error: link to missing file: {0} in {1}", parsedUri.OriginalString, sourceFileName);
                    }
                }
            }
            else
            {
                Console.WriteLine("Error: Couldn't parse link URI: {0} in file {1}", linkUrl, sourceFileName);
            }
        }
    }
}

