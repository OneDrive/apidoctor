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
                compiler.RegisterTag(new ExtendedElseIfTagDefinition(), false);
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
            Console.WriteLine("KeyNotFound: " + e.Key);

            e.Substitute = e.Key;
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
            if (page.Annotation.HeaderAdditions != null)
                htmlHeaders.AddRange(from h in page.Annotation.HeaderAdditions select new ValueObject<string> { Value = h });
            if (page.Annotation.FooterAdditions != null)
                htmlFooters.AddRange(from h in page.Annotation.FooterAdditions select new ValueObject<string> { Value = h });

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

        protected string RelativeUrlFromCurrentPage(DocFile docFile, string destinationFile, string rootDestinationFolder)
        {
            string linkDestinationRelative = docFile.DisplayName.TrimStart(new char[] { '\\', '/' });
            var relativeLinkToDocFile =  DocSet.RelativePathToRootFromFile(destinationFile, Path.Combine(rootDestinationFolder, linkDestinationRelative), true);
            var result = this.QualifyUrl(relativeLinkToDocFile);
            return result;
        }

        protected IEnumerable<TocItem> GetHeadersForSection(string section, string destinationFile, string rootDestinationFolder, DocFile currentPage)
        {
            if (null == section)
                return new TocItem[0];

            var headers = from d in this.Documents.Files
                          where d.Annotation != null 
                          && string.Equals(d.Annotation.Section, section, StringComparison.OrdinalIgnoreCase) 
                          && !string.IsNullOrEmpty(d.Annotation.TocPath)
                          orderby d.Annotation.TocPath
                          select new TocItem { DocFile = d, 
                Title = d.Annotation.TocPath.LastPathComponent(), 
                Url = this.RelativeUrlFromCurrentPage(d, destinationFile, rootDestinationFolder) };
            headers = this.CollapseHeadersByPath(headers, currentPage);
            return headers;
        }

        /// <summary>
        /// Collpase headers into Headers and NextLevel items based on path hierarchy
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="currentPage"></param>
        /// <returns></returns>
        private IEnumerable<TocItem> CollapseHeadersByPath(IEnumerable<TocItem> headers, DocFile currentPage)
        {
            Dictionary<string, TocItem> topLevelHeaders = new Dictionary<string, TocItem>();
            
            foreach (var header in headers)
            {
                var pathCompoents = header.DocFile.Annotation.TocPath.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (pathCompoents.Length >= 2)
                {
                    TocItem topLevelHeader;
                    if (!topLevelHeaders.TryGetValue(pathCompoents[0], out topLevelHeader))
                    {
                        topLevelHeader = new TocItem() { Title = pathCompoents[0] };
                        topLevelHeaders.Add(pathCompoents[0], topLevelHeader);
                    }
                    topLevelHeader.NextLevel.Add(header);
                }
                else
                {
                    topLevelHeaders.Add(header.Title, header);
                }
            }

            if (this.CollapseTocToActiveGroup)
            {
                var pageTocComponents = currentPage.Annotation.TocPath.FirstPathComponent();
                foreach (var header in topLevelHeaders.Values)
                {
                    if (header.Title != pageTocComponents)
                        header.NextLevel.Clear();
                }
            }

            return topLevelHeaders.Values.OrderBy(v => v.Title);
        }
        

    }

    public class TocItem
    {
        public string Title { get; set; }
        public DocFile DocFile { get; set; }
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
