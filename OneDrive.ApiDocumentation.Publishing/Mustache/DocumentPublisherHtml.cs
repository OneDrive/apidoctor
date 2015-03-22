using System;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OneDrive.ApiDocumentation.Validation
{
    public class DocumentPublisherHtml : DocumentPublisher
    {
        protected string _templateFolderPath;
        protected string _templateHtml;

        private const string HtmlOutputExtension = ".htm";
        private const string TemplateHtmlFilename = "template.htm";

        public DocumentPublisherHtml(DocSet docs, string templateFolderPath) 
            : base(docs)
        {
            _templateFolderPath = templateFolderPath;
        }

        /// <summary>
        /// Load the HTML template into memory
        /// </summary>
        protected virtual void LoadTemplate()
        {
            DirectoryInfo dir = new DirectoryInfo(_templateFolderPath);
            if (!dir.Exists)
            {
                return;
            }

            _templateFolderPath = dir.FullName;

            var templateFilePath = Path.Combine(dir.FullName, TemplateHtmlFilename);
            if (!File.Exists(templateFilePath))
            {
                return;
            }

            Console.WriteLine("Using template: {0}", templateFilePath);
            _templateHtml = File.ReadAllText(templateFilePath);
        }

        /// <summary>
        /// Copy everything from the html-template directory to the output directory
        /// except the template file.
        /// </summary>
        /// <param name="destinationRoot"></param>
        protected override void ConfigureOutputDirectory(DirectoryInfo destinationRoot)
        {
            // Copy everything in the template folder to the output folder except template.htm
            if (null != _templateFolderPath)
            {
                DirectoryInfo templateFolder = new DirectoryInfo(_templateFolderPath);

                var templateFiles = templateFolder.GetFiles();
                foreach (var file in templateFiles)
                {
                    if (!file.Name.Equals(TemplateHtmlFilename))
                    {
                        file.CopyTo(Path.Combine(destinationRoot.FullName, file.Name), true);
                    }
                }
                var templateFolders = templateFolder.GetDirectories();
                foreach (var folder in templateFolders)
                {
                    DirectoryCopy(folder, Path.Combine(destinationRoot.FullName, folder.Name), true);
                }

                if (string.IsNullOrEmpty(_templateHtml) && !string.IsNullOrEmpty(_templateFolderPath))
                {
                    LoadTemplate();
                }
            }
        }

        /// <summary>
        /// Copy a directory and it's children
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="destDirName"></param>
        /// <param name="copySubDirs"></param>
        protected static void DirectoryCopy(DirectoryInfo sourceDir, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo[] dirs = sourceDir.GetDirectories();

            if (!sourceDir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDir.FullName);
            }

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = sourceDir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location. 
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir, temppath, copySubDirs);
                }
            }
        }

        /// <summary>
        /// Parse a relative URL into the path and bookmark segments
        /// </summary>
        /// <param name="input"></param>
        /// <param name="url"></param>
        /// <param name="bookmark"></param>
        protected static void SplitUrlPathAndBookmark(string input, out string url, out string bookmark)
        {
            int position = input.IndexOf('#');
            if (position < 0)
            {
                bookmark = string.Empty;
                url = input;
            }
            else
            {
                url = input.Substring(0, position);
                bookmark = input.Substring(position);
            }
        }

        /// <summary>
        /// Initialize and configure the Markdown to HTML converter.
        /// </summary>
        /// <returns></returns>
        protected virtual MarkdownDeep.Markdown GetMarkdownConverter()
        {
            var converter = new MarkdownDeep.Markdown();
            converter.ExtraMode = true;
            converter.NewWindowForExternalLinks = true;
            converter.SafeMode = true;
            converter.AutoHeadingIDs = true;
            converter.QualifyUrl = QualifyUrl;
            return converter;
        }

        /// <summary>
        /// Convert URLs in the markdown file into their HTML equivelents.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        protected virtual string QualifyUrl(string url)
        {
            if (url.StartsWith("#"))
                return url;
            if (MarkdownDeep.Utils.IsUrlFullyQualified(url))
                return url;

            string filePath, bookmark;
            SplitUrlPathAndBookmark(url, out filePath, out bookmark);
            foreach (var extension in scannableExtensions)
            {
                if (filePath.EndsWith(extension))
                {
                    return filePath.Substring(0, filePath.Length - extension.Length) + HtmlOutputExtension + bookmark;
                }
            }
            return url;
        }

        protected override async Task PublishFileToDestination(FileInfo sourceFile, DirectoryInfo destinationRoot, PageAnnotation pageData)
        {
            LogMessage(new ValidationMessage(sourceFile.Name, "Publishing file to HTML"));

            var destinationPath = GetPublishedFilePath(sourceFile, destinationRoot, HtmlOutputExtension);

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

            var converter = GetMarkdownConverter();
            var html = converter.Transform(writer.ToString());

            if (null == pageData)
            {
                pageData = new PageAnnotation();
            }
            if (string.IsNullOrEmpty(pageData.Title))
            {
               pageData.Title = (from b in converter.Blocks
                             where b.BlockType == MarkdownDeep.BlockType.h1
                             select b.Content).FirstOrDefault();
            }
            await WriteHtmlDocumentAsync(html, pageData, destinationPath, destinationRoot.FullName);
        }

        /// <summary>
        /// Apply the HTML template to the parameters and write the file to the destination.
        /// </summary>
        /// <param name="bodyHtml"></param>
        /// <param name="pageData"></param>
        /// <param name="destinationFile"></param>
        /// <param name="rootDestinationFolder"></param>
        /// <returns></returns>
        protected virtual async Task WriteHtmlDocumentAsync(string bodyHtml, PageAnnotation pageData, string destinationFile, string rootDestinationFolder)
        {
            List<string> variablesToReplace = new List<string>();
            string pageHtml = null;
            if (_templateHtml != null)
            {
                var matches = ExtensionMethods.PathVariableRegex.Matches(_templateHtml);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    variablesToReplace.Add(match.Groups[0].Value);
                }

                string templateHtmlForThisPage = _templateHtml;

                foreach (var key in variablesToReplace.ToArray())
                {
                    if (key == "{page.title}")
                    {
                        templateHtmlForThisPage = templateHtmlForThisPage.Replace(key, pageData.Title);
                    }
                    else if (key == "{body.html}")
                    {
                        templateHtmlForThisPage = templateHtmlForThisPage.Replace(key, bodyHtml);
                    }
                    else if (key.StartsWith("{if "))
                    {
                        string value = ParseDocumentIfStatement(key, destinationFile);
                        templateHtmlForThisPage = templateHtmlForThisPage.Replace(key, value);
                    }
                    else
                    {
                        string filename = key.Substring(1, key.Length - 2);
                        string value = DocSet.RelativePathToRootFromFile(destinationFile, Path.Combine(rootDestinationFolder, filename), true);
                        templateHtmlForThisPage = templateHtmlForThisPage.Replace(key, value);
                    }
                }

                pageHtml = templateHtmlForThisPage;
            }
            else
            {
                pageHtml = string.Concat(string.Format(htmlHeader, pageData.Title, htmlStyles), bodyHtml, htmlFooter);
            }

            using (var outputWriter = new StreamWriter(destinationFile))
            {
                await outputWriter.WriteAsync(pageHtml);
            }
        }

        private string ParseDocumentIfStatement(string key, string containingFilePath)
        {
            // {if file.name="readme.htm" then class="active"}
            bool looksValid = key.StartsWith("{if [") && key.EndsWith("]}");
            if (!looksValid)
                throw new ArgumentException("key doesn't look like a valid if query");

            string query = key.Substring(5, key.Length - 7);
            IfQueryData data = Newtonsoft.Json.JsonConvert.DeserializeObject<IfQueryData>("{" + query + "}");

            string returnValue = string.Empty;
            switch (data.field)
            {
                case "filename":
                    bool filenameMatches = data.value.Equals(Path.GetFileName(containingFilePath), StringComparison.OrdinalIgnoreCase);
                    if (data.invert)
                        filenameMatches = !filenameMatches;
                    if (filenameMatches)
                    {
                        returnValue = data.output;
                    }
                    break;
                default:
                    throw new NotSupportedException("Unsupported query on field named " + data.field);
            }

            return returnValue;

        }

        class IfQueryData
        {
            public string field { get; set; }
            public string value { get; set; }
            public string output { get; set; }
            public bool invert { get; set; }
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

