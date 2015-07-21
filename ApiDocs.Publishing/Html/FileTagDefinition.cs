using Scope = Mustache.Scope;
using TagDefinition = Mustache.TagDefinition;
using TagParameter = Mustache.TagParameter;

namespace ApiDocs.Publishing.Html
{
    using System.Collections.Generic;
    using System.IO;
    using ApiDocs.Validation;

    public class FileTagDefinition : TagDefinition
    {
        public FileTagDefinition()
            : base("url")
        {
        }

        public string RootDestinationFolder { get; set; }
        public string DestinationFile { get; set; }

        public override void GetText(System.IO.TextWriter writer, Dictionary<string, object> arguments, Scope context)
        {
            var filenameToReplace = arguments["filename"] as string;
            if (null != filenameToReplace)
            {
                var relativeFileUrl = DocSet.RelativePathToRootFromFile(
                    this.DestinationFile,
                    Path.Combine(this.RootDestinationFolder, filenameToReplace),
                    true);
                writer.Write(relativeFileUrl);
            }
        }

        protected override bool GetHasContent()
        {
            return false;
        }

        protected override IEnumerable<TagParameter> GetParameters()
        {
            return new TagParameter[] { new TagParameter("filename") { IsRequired = true } };
        }


        public override IEnumerable<TagParameter> GetChildContextParameters()
        {
            return new TagParameter[] { new TagParameter("filename") };
        }
    }
}
