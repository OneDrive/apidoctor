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
    using System.Threading.Tasks;
    using ApiDocs.Validation;
    using ApiDocs.Validation.Writers;
    using Mustache;
    using System.Dynamic;
    using Newtonsoft.Json;
    public class HtmlMustacheWriter : DocumentPublisherHtml
    {
        private Generator generator;
        private FileTagDefinition fileTag;

        public bool CollapseTocToActiveGroup { get; set; }

        /// <summary>
        /// Switch off the output of HTML files and just generate the TOC file
        /// </summary>
        public bool TocOnly { get; set; }

        public HtmlMustacheWriter(DocSet docs, IPublishOptions options) : base(docs, options)
        {
            this.CollapseTocToActiveGroup = false;
        }

        protected override void LoadTemplate()
        {
            base.LoadTemplate();

            if (!string.IsNullOrEmpty(this.TemplateHtml))
            {
                this.fileTag = new FileTagDefinition();

                FormatCompiler compiler = new FormatCompiler() { RemoveNewLines = false };
                
                compiler.RegisterTag(this.fileTag, true);
                compiler.RegisterTag(new IfMatchTagDefinition(), true);
                compiler.RegisterTag(new ExtendedElseTagDefinition(), false);
                this.generator = compiler.Compile(this.TemplateHtml);
                this.generator.KeyNotFound += this.Generator_KeyNotFound;
            }
        }

        void Generator_KeyNotFound(object sender, KeyNotFoundEventArgs e)
        {
            Console.WriteLine("Mustache template key not found. Empty string was returned: " + e.Key);

            e.Substitute = "";
            e.Handled = true;
        }

        protected override void EnsureDirectoryExists(DirectoryInfo directory, DirectoryInfo destinationRoot, string pathDisplayName, bool isRootPath)
        {
            if (!isRootPath && this.TocOnly)
                return;

            base.EnsureDirectoryExists(directory, destinationRoot, pathDisplayName, isRootPath);
        }

        protected override bool ShouldPublishFile(DocFile file)
        {
            if (this.TocOnly)
                return false;

            return base.ShouldPublishFile(file);
        }

        protected override bool ShouldPublishFile(FileInfo file)
        {
            if (this.TocOnly)
                return false;
            return base.ShouldPublishFile(file);
        }


        /// <summary>
        /// Apply the template to the document
        /// </summary>
        /// <param name="bodyHtml"></param>
        /// <param name="page"></param>
        /// <param name="destinationFile"></param>
        /// <param name="rootDestinationFolder"></param>
        /// <returns></returns>
        protected override async Task WriteHtmlDocumentAsync(string bodyHtml, DocFile page, string destinationFile, string rootDestinationFolder)
        {
            var tocHeaders = this.GetHeadersForSection(page.Annotation.Section, destinationFile, rootDestinationFolder, page);

            List<ValueObject<string>> htmlHeaders = new List<ValueObject<string>>();
            List<ValueObject<string>> htmlFooters = new List<ValueObject<string>>();
            if (page.Annotation.HeadHtmlTags != null)
                htmlHeaders.AddRange(from h in page.Annotation.HeadHtmlTags select new ValueObject<string> { Value = h });
            if (page.Annotation.BodyFooterHtmlTags != null)
                htmlFooters.AddRange(from h in page.Annotation.BodyFooterHtmlTags select new ValueObject<string> { Value = h });

            var templateObject = new PageTemplateInput
            {
                Page = page.Annotation,
                Body = bodyHtml,
                Headers = tocHeaders,
                HtmlHeaderAdditions = htmlHeaders,
                HtmlFooterAdditions = htmlFooters,
                OriginalSourceRelativePath = page.UrlRelativePathFromRoot(),
                Parameters = this.PageParameters
                
            };

            this.fileTag.DestinationFile = destinationFile;
            this.fileTag.RootDestinationFolder = rootDestinationFolder;

            var pageHtml = this.generator.Render(templateObject);
            pageHtml = await this.ConvertLineEndingsAsync(pageHtml, this.OutputLineEndings);

            using (var outputWriter = new StreamWriter(destinationFile))
            {
                await outputWriter.WriteAsync(pageHtml);
            }
        }

        protected override async Task WriteAdditionalFilesAsync()
        {
            if (!string.IsNullOrEmpty(this.Options.TableOfContentsOutputRelativePath))
            {
                var outputFile = Path.Combine(this.OutputFolder, this.Options.TableOfContentsOutputRelativePath);
                await WriteTableOfContentsFileAsync(outputFile);
            }
        }

        private class PageTemplateInput
        {
            public PageAnnotation Page { get; set; }
            public string Body { get; set; }
            public IEnumerable<TocItem> Headers { get; set; }
            public List<ValueObject<string>> HtmlHeaderAdditions { get; set; }
            public List<ValueObject<string>> HtmlFooterAdditions { get; set; }
            public string OriginalSourceRelativePath { get; set; }
            public Dictionary<string, string> Parameters { get; set; }
        }

        private class ParameterPropertyBag : System.Dynamic.DynamicObject
        {
            private Dictionary<string, object> storedValues;

            public ParameterPropertyBag()
            {
                storedValues = new Dictionary<string, object>();
            }

            public override bool TryGetMember(System.Dynamic.GetMemberBinder binder, out object result)
            {
                return storedValues.TryGetValue(binder.Name, out result);
            }

            public override bool TryGetIndex(System.Dynamic.GetIndexBinder binder, object[] indexes, out object result)
            {
                return storedValues.TryGetValue((string)indexes[0], out result);
            }

            public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
            {
                storedValues[(string)indexes[0]] = value;
                return true;
            }

            public override IEnumerable<string> GetDynamicMemberNames()
            {
                return storedValues.Keys;
            }
        }

        protected string RelativeUrlFromCurrentPage(DocFile docFile, string destinationFile, string rootDestinationFolder, string optionalBookmark = null)
        {
            string linkDestinationRelative = docFile.DisplayName.TrimStart(new char[] { '\\', '/' });
            var relativeLinkToDocFile =  DocSet.RelativePathToRootFromFile(destinationFile, Path.Combine(rootDestinationFolder, linkDestinationRelative), true);
            var result = this.QualifyUrl(relativeLinkToDocFile);

            if (optionalBookmark != null)
                result += optionalBookmark;

            return result;
        }

        /// <summary>
        /// Generates the headers for a given section and destination file. This generates a unique set of headers with relative
        /// pathes to the header pages based on the current page.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="destinationFile"></param>
        /// <param name="rootDestinationFolder"></param>
        /// <param name="currentPage"></param>
        /// <returns></returns>
        protected IEnumerable<TocItem> GetHeadersForSection(string section, string destinationFile, string rootDestinationFolder, DocFile currentPage)
        {
            if (null == section)
                return new TocItem[0];

            // Generate headers for all tocPath entries
            var headersQuery = from d in this.Documents.Files
                               where d.Annotation != null
                               && string.Equals(d.Annotation.Section, section, StringComparison.OrdinalIgnoreCase)
                               && !string.IsNullOrEmpty(d.Annotation.TocPath)
                               orderby d.Annotation.TocPath
                               select new TocItem {
                                   DocFile = d,
                                   Title = d.Annotation.TocPath.LastPathComponent(),
                                   TocPath = d.Annotation.TocPath,
                                   Url = this.RelativeUrlFromCurrentPage(d, destinationFile, rootDestinationFolder),
                                   SortOrder = d.Annotation.TocIndex
                          };

            List<TocItem> headers = headersQuery.ToList();

            // Generate headers for all tocEntry items
            var multipleTocItemPages = from d in this.Documents.Files
                where d.Annotation != null 
                && d.Annotation.TocBookmarks != null
                && string.Equals(d.Annotation.Section, section, StringComparison.OrdinalIgnoreCase)
                && d.Annotation.TocBookmarks.Count > 0
                select new { DocFile = d, TocItems = d.Annotation.TocBookmarks };

            foreach (var item in multipleTocItemPages)
            {
                headers.AddRange(
                    from entry in item.TocItems
                    where entry.Value != null && entry.Value.StartsWith("#")
                    select new TocItem
                    {
                        DocFile = item.DocFile,
                        Title = entry.Key.LastPathComponent(),
                        TocPath = entry.Key,
                        Url = this.RelativeUrlFromCurrentPage(item.DocFile, destinationFile, rootDestinationFolder, entry.Value)
                    });
            }
            headers = headers.OrderBy(x => x.TocPath).ToList();
            headers = this.BuildTreeFromList(headers, currentPage);
            return headers;
        }

        /// <summary>
        /// Write the table of contents based on annotations in the page files to the destination filename
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        protected async Task WriteTableOfContentsFileAsync(string destination)
        {
            List<TocItem> allTocEntries = new List<TocItem>();
            foreach (var file in this.Documents.Files)
            {
                allTocEntries.AddRange(TocItem.TocItemsForFile(file));
            }

            // Convert the Url properties to be usable by the output system
            foreach (var item in allTocEntries)
            {
                item.Url = this.QualifyUrl(item.Url.Replace('\\', '/'));
            }

            allTocEntries = allTocEntries.OrderBy(x => x.TocPath).ToList();
            var tree = BuildTreeFromList(allTocEntries, addLevelForSections: true);
            var data = new { toc = tree };

            string output = JsonConvert.SerializeObject(data, Formatting.Indented);

            using (var writer = new StreamWriter(destination, false, new System.Text.UTF8Encoding(false)))
            {
                await writer.WriteLineAsync(output);
                await writer.FlushAsync();
            }
        }

        private static void AddFileToToc(DocFile file, Dictionary<string, TocItem> toc)
        {
            if (file.Annotation == null || file.Annotation.Section == null)
                return;

        }

        /// <summary>
        /// Collpase headers into Headers and NextLevel items based on path hierarchy
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="currentPage"></param>
        /// <returns></returns>
        private List<TocItem> BuildTreeFromList(List<TocItem> headers, DocFile currentPage = null, bool addLevelForSections = false)
        {
            List<TocItem> topLevelHeaders = new List<TocItem>();
            
            foreach (var header in headers)
            {
                string pathForHeader = header.TocPath;
                if (addLevelForSections)
                {
                    pathForHeader = header.Section + "\\" + pathForHeader;
                }

                var pathComponents =
                    pathForHeader.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                List<TocItem> headersForTargetLevel = topLevelHeaders;

                while (pathComponents.Count > 1)
                {
                    // Point headersForTargetLevel to the proper level
                    var headerForNextLevel =
                        (from h in headersForTargetLevel
                         where h.Title == pathComponents[0]
                         select h).FirstOrDefault();

                    if (headerForNextLevel == null)
                    {
                        // We encountered a header that doesn't exist yet. Bummer. We should do something about that.
                        headerForNextLevel = new TocItem()
                        {
                            Title = pathComponents[0],
                            Section = header.Section
                        };

                        headersForTargetLevel.Add(headerForNextLevel);
                    }
                    headersForTargetLevel = headerForNextLevel.NextLevel;
                    pathComponents.RemoveAt(0);
                }

                headersForTargetLevel.Add(header);
            }

            if (this.CollapseTocToActiveGroup && null != currentPage)
            {
                var pageTocComponents = currentPage.Annotation.TocPath.FirstPathComponent();
                foreach (var header in topLevelHeaders)
                {
                    if (header.Title != pageTocComponents)
                        header.NextLevel.Clear();
                }
            }

            return SortTocTree(topLevelHeaders);
        }

        private List<TocItem> SortTocTree(List<TocItem> tree)
        {
            if (tree == null)
                return null;
            if (tree.Count == 0)
                return tree;

            foreach (var item in tree)
            {
                item.NextLevel = SortTocTree(item.NextLevel);
            }

            var sorted = from item in tree
                         orderby item.SortOrder, item.Title
                         select item;

            return sorted.ToList();
        }
    }

    public class TocItem
    {
        [JsonProperty("title", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Title { get; set; }

        [JsonIgnore]
        public DocFile DocFile { get; set; }

        [JsonIgnore]
        public string TocPath { get; set; }

        [JsonProperty("url", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Url { get; set; }

        [JsonProperty("children", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<TocItem> NextLevel { get; set; }

        [JsonProperty("section")]
        public string Section { get; set; }

        [JsonIgnore]
        public int SortOrder
        {
            get; set;
        }

        public TocItem()
        {
            this.NextLevel = new List<TocItem>();
        }

        public static TocItem[] TocItemsForFile(DocFile file)
        {
            if (file.Annotation == null)
                return new TocItem[0];

            List<TocItem> items = new List<TocItem>();
            if (!string.IsNullOrEmpty(file.Annotation.TocPath))
            {
                items.Add(new TocItem
                {
                    DocFile = file,
                    Title = file.Annotation.TocPath.LastPathComponent(),
                    TocPath = file.Annotation.TocPath,
                    Url = file.DisplayName,
                    Section = file.Annotation.Section,
                    SortOrder = file.Annotation.TocIndex
                });
            }

            if (file.Annotation.TocBookmarks != null)
            {
                foreach (var path in file.Annotation.TocBookmarks.Keys)
                {
                    items.Add(new TocItem
                    {
                        DocFile = file,
                        Title = path.LastPathComponent(),
                        TocPath = path,
                        Url = file.DisplayName + file.Annotation.TocBookmarks[path],
                        Section = file.Annotation.Section
                    });
                }
            }

            return items.ToArray();
        }
    }

    public class ValueObject<T>
    {
        public T Value { get; set; }
    }

}
