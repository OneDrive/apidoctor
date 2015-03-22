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


        public HtmlMustacheWriter(DocSet docs, string templateFolderPath) : base(docs, templateFolderPath)
        {

        }

        protected override void LoadTemplate()
        {
            base.LoadTemplate();

            if (!string.IsNullOrEmpty(_templateHtml))
            {
                Mustache.FormatCompiler compiler = new Mustache.FormatCompiler() { RemoveNewLines = false };
                _generator = compiler.Compile(_templateHtml);
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
            var templateObject = new { Page = pageData };
            var pageHtml = _generator.Render(templateObject);
            using (var outputWriter = new StreamWriter(destinationFile))
            {
                await outputWriter.WriteAsync(pageHtml);
            }
        }
    }
}
