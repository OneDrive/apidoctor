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

    public class HtmlMustacheWriter : DocumentPublisherHtml
    {
        private Generator generator;
        private FileTagDefinition fileTag;

        public bool CollapseTocToActiveGroup { get; set; }


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
                //compiler.RegisterTag(new ExtendedElseIfTagDefinition(), false);
                this.generator = compiler.Compile(this.TemplateHtml);
                this.generator.KeyNotFound += this.Generator_KeyNotFound;
                //this.generator.ValueRequested += this.Generator_ValueRequested;
                //this.generator.KeyFound += this.Generator_KeyFound;
            }
        }

        //void Generator_KeyFound(object sender, Mustache.KeyFoundEventArgs e)
        //{
        //    Console.WriteLine("KeyFound: " + e.Key);
        //}

        //void Generator_ValueRequested(object sender, Mustache.ValueRequestEventArgs e)
        //{
        //    Console.WriteLine("ValueRequested: " + e.Value);
        //}
        void Generator_KeyNotFound(object sender, KeyNotFoundEventArgs e)
        {
            Console.WriteLine("Mustache template key not found. Empty string was returned: " + e.Key);

            e.Substitute = "";
            e.Handled = true;
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

            dynamic templateObject = new System.Dynamic.ExpandoObject();
            templateObject.Page = page.Annotation;
            templateObject.Body = bodyHtml;
            templateObject.Headers = tocHeaders;
            templateObject.HtmlHeaderAdditions = htmlHeaders;
            templateObject.HtmlFooterAdditions = htmlFooters;
            templateObject.Source = new
            {
                MarkdownPath = page.UrlRelativePathFromRoot()
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
                              Url = this.RelativeUrlFromCurrentPage(d, destinationFile, rootDestinationFolder)
                          };

            List<TocItem> headers = headersQuery.ToList();

            // Generate headers for all tocEntry items
            var multipleTocItemPages = from d in this.Documents.Files
                where d.Annotation != null 
                && d.Annotation.TocItems != null
                && string.Equals(d.Annotation.Section, section, StringComparison.OrdinalIgnoreCase)
                && d.Annotation.TocItems.Count > 0
                select new { DocFile = d, TocItems = d.Annotation.TocItems };

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
            headers = this.CollapseHeadersByPath(headers, currentPage);
            return headers;
        }

        /// <summary>
        /// Collpase headers into Headers and NextLevel items based on path hierarchy
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="currentPage"></param>
        /// <returns></returns>
        private List<TocItem> CollapseHeadersByPath(List<TocItem> headers, DocFile currentPage)
        {
            List<TocItem> topLevelHeaders = new List<TocItem>();
            
            foreach (var header in headers)
            {
                var pathComponents =
                    header.TocPath.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                List<TocItem> headersForTargetLevel = topLevelHeaders;

                while (pathComponents.Count > 1)
                {
                    // Point headersForTargetLevel to the proper level
                    var headerForNextLevel =
                        (from h in headersForTargetLevel where h.Title == pathComponents[0] select h).FirstOrDefault();
                    if (headerForNextLevel == null)
                    {
                        // We encountered a header that doesn't exist yet. Bummer. We should do something about that.
                        headerForNextLevel = new TocItem() { Title = pathComponents[0] };
                        headersForTargetLevel.Add(headerForNextLevel);
                    }
                    headersForTargetLevel = headerForNextLevel.NextLevel;
                    pathComponents.RemoveAt(0);
                }

                headersForTargetLevel.Add(header);
            }

            if (this.CollapseTocToActiveGroup)
            {
                var pageTocComponents = currentPage.Annotation.TocPath.FirstPathComponent();
                foreach (var header in topLevelHeaders)
                {
                    if (header.Title != pageTocComponents)
                        header.NextLevel.Clear();
                }
            }

            return topLevelHeaders.OrderBy(v => v.Title).ToList();
        }
        

    }

    public class TocItem
    {
        public string Title { get; set; }
        public DocFile DocFile { get; set; }
        public string TocPath { get; set; }
        public string Url { get; set; }
        public List<TocItem> NextLevel { get; set; }

        public TocItem()
        {
            this.NextLevel = new List<TocItem>();
        }
    }

    public class ValueObject<T>
    {
        public T Value { get; set; }
    }

}
