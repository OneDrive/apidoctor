using OneDrive.ApiDocumentation.Validation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.Publishing
{
    public class HtmlMustacheWriter : DocumentPublisherHtml
    {
        private Mustache.Generator _generator = null;
        private FileTagDefinition _fileTag;

        public bool CollapseTocToActiveGroup { get; set; }


        public HtmlMustacheWriter(DocSet docs, string templateFolderPath) : base(docs, templateFolderPath)
        {
            CollapseTocToActiveGroup = false;
        }

        protected override void LoadTemplate()
        {
            base.LoadTemplate();

            if (!string.IsNullOrEmpty(_templateHtml))
            {

                _fileTag = new FileTagDefinition();

                Mustache.FormatCompiler compiler = new Mustache.FormatCompiler() { RemoveNewLines = false };
                
                compiler.RegisterTag(_fileTag, true);
                compiler.RegisterTag(new IfMatchTagDefinition(), true);
                compiler.RegisterTag(new ExtendedElseTagDefinition(), false);
                compiler.RegisterTag(new ExtendedElseIfTagDefinition(), false);
                _generator = compiler.Compile(_templateHtml);
                _generator.KeyNotFound += _generator_KeyNotFound;
                //_generator.ValueRequested += _generator_ValueRequested;
                //_generator.KeyFound += _generator_KeyFound;
            }
        }

        void _generator_KeyFound(object sender, Mustache.KeyFoundEventArgs e)
        {
            Console.WriteLine("KeyFound: " + e.Key);
        }

        void _generator_ValueRequested(object sender, Mustache.ValueRequestEventArgs e)
        {
            Console.WriteLine("ValueRequested: " + e.Value);
        }
        void _generator_KeyNotFound(object sender, Mustache.KeyNotFoundEventArgs e)
        {
            //Console.WriteLine("KeyNotFound: " + e.Key);

            e.Substitute = e.Key;
            e.Handled = true;
        }


        /// <summary>
        /// Apply the template to the document
        /// </summary>
        /// <param name="bodyHtml"></param>
        /// <param name="pageData"></param>
        /// <param name="destinationFile"></param>
        /// <param name="rootDestinationFolder"></param>
        /// <returns></returns>
        protected override async Task WriteHtmlDocumentAsync(string bodyHtml, DocFile page, string destinationFile, string rootDestinationFolder)
        {
            var templateObject = new { 
                Page = page.Annotation, 
                Body = bodyHtml, 
                Headers = GetHeadersForSection(page.Annotation.Section, destinationFile, rootDestinationFolder, page)
            };
            _fileTag.DestinationFile = destinationFile;
            _fileTag.RootDestinationFolder = rootDestinationFolder;

            var pageHtml = _generator.Render(templateObject);
            using (var outputWriter = new StreamWriter(destinationFile))
            {
                await outputWriter.WriteAsync(pageHtml);
            }
        }

        protected string RelativeUrlFromCurrentPage(DocFile docFile, string destinationFile, string rootDestinationFolder)
        {
            string linkDestinationRelative = docFile.DisplayName.TrimStart(new char[] { '\\' });
            var relativeLinkToDocFile = DocSet.RelativePathToRootFromFile(destinationFile, Path.Combine(rootDestinationFolder, linkDestinationRelative), true);
            var result = QualifyUrl(relativeLinkToDocFile);
            return result;
        }

        protected IEnumerable<TocItem> GetHeadersForSection(string section, string destinationFile, string rootDestinationFolder, DocFile currentPage)
        {
            var headers = from d in Documents.Files
                          where d.Annotation != null 
                          && d.Annotation.Section.Equals(section, StringComparison.OrdinalIgnoreCase) 
                          && !string.IsNullOrEmpty(d.Annotation.TocPath)
                          orderby d.Annotation.TocPath
                          select new TocItem { DocFile = d, Title = d.Annotation.TocPath.LastPathComponent(), Url = RelativeUrlFromCurrentPage(d, destinationFile, rootDestinationFolder) };
            headers = CollapseHeadersByPath(headers, currentPage);
            return headers;
        }

        /// <summary>
        /// Collpase headers into Headers and NextLevel items based on path hierarchy
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        private IEnumerable<TocItem> CollapseHeadersByPath(IEnumerable<TocItem> headers, DocFile currentPage)
        {
            Dictionary<string, TocItem> topLevelHeaders = new Dictionary<string, TocItem>();
            
            foreach (var header in headers)
            {
                var pathCompoents = header.DocFile.Annotation.TocPath.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                TocItem topLevelHeader = null;
                if (pathCompoents.Length >= 2)
                {
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

            if (CollapseTocToActiveGroup)
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
            NextLevel = new List<TocItem>();
        }
    }

}
