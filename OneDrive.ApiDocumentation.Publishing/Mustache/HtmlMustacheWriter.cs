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


        public HtmlMustacheWriter(DocSet docs, string templateFolderPath) : base(docs, templateFolderPath)
        {
            
        }

        protected override void LoadTemplate()
        {
            base.LoadTemplate();

            if (!string.IsNullOrEmpty(_templateHtml))
            {

                _fileTag = new FileTagDefinition();

                Mustache.FormatCompiler compiler = new Mustache.FormatCompiler() { RemoveNewLines = false };
                
                compiler.RegisterTag(_fileTag, true);
                compiler.RegisterTag(new SectionTagDefinition(), true);
                _generator = compiler.Compile(_templateHtml);
                _generator.KeyNotFound += _generator_KeyNotFound;
            }
        }
        

        /// <summary>
        /// Apply the template to the document
        /// </summary>
        /// <param name="bodyHtml"></param>
        /// <param name="pageData"></param>
        /// <param name="destinationFile"></param>
        /// <param name="rootDestinationFolder"></param>
        /// <returns></returns>
        protected override async Task WriteHtmlDocumentAsync(string bodyHtml, PageAnnotation pageData, string destinationFile, string rootDestinationFolder)
        {
            var templateObject = new { Page = pageData, Body = bodyHtml, DestinationFolder = rootDestinationFolder };
            _fileTag.DestinationFile = destinationFile;
            _fileTag.RootDestinationFolder = rootDestinationFolder;

            var pageHtml = _generator.Render(templateObject);
            using (var outputWriter = new StreamWriter(destinationFile))
            {
                await outputWriter.WriteAsync(pageHtml);
            }
        }

        void _generator_KeyNotFound(object sender, Mustache.KeyNotFoundEventArgs e)
        {
            e.Substitute = e.Key;
            e.Handled = true;
        }
    }
}
