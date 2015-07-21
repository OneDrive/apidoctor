using Mustache;
using ApiDocs.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ApiDocs.Publishing
{
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

            var relativeFileUrl = DocSet.RelativePathToRootFromFile(DestinationFile, Path.Combine(RootDestinationFolder, filenameToReplace), true);
            writer.Write(relativeFileUrl);
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
