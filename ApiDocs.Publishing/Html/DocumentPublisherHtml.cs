/*
 * Markdown Scanner
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

namespace ApiDocs.Publishing.Html
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using ApiDocs.Validation;
    using ApiDocs.Validation.Error;
    using ApiDocs.Validation.Writers;
    using MarkdownDeep;
    using Newtonsoft.Json;
    using Validation.Tags;

    public class DocumentPublisherHtml : DocumentPublisher
    {
        protected string TemplateHtml;

        public string HtmlOutputExtension { get; set; }
        public string TemplateHtmlFilename { get; set; }
        protected Dictionary<string, string> PageParameters { get; set; }

        /// <summary>
        /// Allow HTML tags in the markdown source to pass through to the converted HTML. This is considered
        /// unsafe.
        /// </summary>
        public bool EnableHtmlTagPassThrough { get; set; }

        public DocumentPublisherHtml(DocSet docs, IPublishOptions options) 
            : base(docs, options)
        {
            TemplateHtmlFilename =  options.TemplateFilename ?? "template.htm";
            HtmlOutputExtension = options.OutputExtension ?? ".htm";
            EnableHtmlTagPassThrough = options.AllowUnsafeHtmlContentInMarkdown;
            PageParameters = options.PageParameterDict;
        }

        /// <summary>
        /// Load the HTML template into memory
        /// </summary>
        protected virtual void LoadTemplate()
        {
            DirectoryInfo dir = new DirectoryInfo(this.Options.TemplatePath);
            if (!dir.Exists)
            {
                return;
            }

            this.Options.TemplatePath = dir.FullName;

            var templateFilePath = Path.Combine(dir.FullName, TemplateHtmlFilename);
            if (!File.Exists(templateFilePath))
            {
                return;
            }

            Console.WriteLine("Using template: {0}", templateFilePath);
            this.TemplateHtml = File.ReadAllText(templateFilePath);
        }

        /// <summary>
        /// Copy everything from the html-template directory to the output directory
        /// except the template file.
        /// </summary>
        /// <param name="destinationRoot"></param>
        protected override void ConfigureOutputDirectory(DirectoryInfo destinationRoot)
        {
            // Copy everything in the template folder to the output folder except template.htm
            var templatePath = this.Options.TemplatePath;
            if (null != templatePath)
            {
                DirectoryInfo templateFolder = new DirectoryInfo(templatePath);

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

                if (string.IsNullOrEmpty(this.TemplateHtml) && !string.IsNullOrEmpty(templatePath))
                {
                    this.LoadTemplate();
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
        protected virtual Markdown GetMarkdownConverter()
        {
            //var converter = new Markdown
            var converter = new Markdown
            {
                ExtraMode = true,
                NewWindowForExternalLinks = true,
                SafeMode = this.EnableHtmlTagPassThrough,
                AutoHeadingIDs = true,
                QualifyUrl = this.QualifyUrl
            };
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
            if (LinkDefinition.IsUrlFullyQualified(url))
                return url;

            string filePath, bookmark;
            SplitUrlPathAndBookmark(url, out filePath, out bookmark);
            foreach (var extension in this.ScannableExtensions)
            {
                if (filePath.EndsWith(extension))
                {
                    return filePath.Substring(0, filePath.Length - extension.Length) + HtmlOutputExtension + bookmark;
                }
            }
            return url;
        }

        protected override async Task PublishFileToDestinationAsync(FileInfo sourceFile, DirectoryInfo destinationRoot, DocFile page)
        {
            this.LogMessage(new ValidationMessage(sourceFile.Name, "Publishing file to HTML"));

            var destinationPath = this.GetPublishedFilePath(sourceFile, destinationRoot, HtmlOutputExtension);
            
            // Create a tag processor
            string tagsInput;
            if (null == PageParameters || !PageParameters.TryGetValue("tags", out tagsInput))
            {
                tagsInput = string.Empty;
            }
            var markdownSource = page.ReadAndPreprocessFileContents(tagsInput);

            var converter = this.GetMarkdownConverter();
            var html = converter.Transform(markdownSource);

            // Post-process the resulting HTML for any remaining tags
            TagProcessor tagProcessor = new TagProcessor(tagsInput,
                page.Parent.SourceFolderPath, LogMessage);
            html = tagProcessor.PostProcess(html, sourceFile, converter);

            var pageData = page.Annotation ?? new PageAnnotation();
            if (string.IsNullOrEmpty(pageData.Title))
            {
               pageData.Title = (from b in page.OriginalMarkdownBlocks
                             where b.BlockType == BlockType.h1
                             select b.Content).FirstOrDefault();
            }
            page.Annotation = pageData;
            await this.WriteHtmlDocumentAsync(html, page, destinationPath, destinationRoot.FullName);
        }

        /// <summary>
        /// Apply the HTML template to the parameters and write the file to the destination.
        /// </summary>
        /// <param name="bodyHtml"></param>
        /// <param name="page"></param>
        /// <param name="destinationFile"></param>
        /// <param name="rootDestinationFolder"></param>
        /// <returns></returns>
        protected virtual async Task WriteHtmlDocumentAsync(string bodyHtml, DocFile page, string destinationFile, string rootDestinationFolder)
        {
            List<string> variablesToReplace = new List<string>();
            string pageHtml = null;
            if (this.TemplateHtml != null)
            {
                var matches = ExtensionMethods.PathVariableRegex.Matches(this.TemplateHtml);
                variablesToReplace.AddRange(from Match match in matches select match.Groups[0].Value);

                string templateHtmlForThisPage = this.TemplateHtml;

                foreach (var key in variablesToReplace.ToArray())
                {
                    if (key == "{page.title}")
                    {
                        templateHtmlForThisPage = templateHtmlForThisPage.Replace(key, page.Annotation.Title);
                    }
                    else if (key == "{body.html}")
                    {
                        templateHtmlForThisPage = templateHtmlForThisPage.Replace(key, bodyHtml);
                    }
                    else if (key.StartsWith("{if "))
                    {
                        string value = this.ParseDocumentIfStatement(key, destinationFile);
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
                pageHtml = string.Concat(string.Format(HtmlHeader, page.Annotation.Title, HtmlStyles), bodyHtml, HtmlFooter);
            }

            pageHtml = await this.ConvertLineEndingsAsync(pageHtml, this.OutputLineEndings);
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
            IfQueryData data = JsonConvert.DeserializeObject<IfQueryData>("{" + query + "}");

            string returnValue = string.Empty;
            switch (data.Field)
            {
                case "filename":
                    bool filenameMatches = data.Value.Equals(Path.GetFileName(containingFilePath), StringComparison.OrdinalIgnoreCase);
                    if (data.Invert)
                        filenameMatches = !filenameMatches;
                    if (filenameMatches)
                    {
                        returnValue = data.Output;
                    }
                    break;
                default:
                    throw new NotSupportedException("Unsupported query on field named " + data.Field);
            }

            return returnValue;

        }

        // ReSharper disable once ClassNeverInstantiated.Local
        class IfQueryData
        {
            [JsonProperty("field")]
            public string Field { get; set; }
            [JsonProperty("value")]
            public string Value { get; set; }
            [JsonProperty("output")]
            public string Output { get; set; }
            [JsonProperty("invert")]
            public bool Invert { get; set; }
        }

        private const string HtmlHeader = @"<html>
<head>
<title>{0}</title>
<style>
{1}
</style>
</head>
<body>
";
        private const string HtmlStyles = @"
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

        private const string HtmlFooter = @"
</body></html>
";
    }
}

