using ApiDocs.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation.UnitTests
{
    public class DocFileForTesting : DocFile
    {
        private readonly string contentsOfFile;
        public DocFileForTesting(string contentsOfFile, string fullPath, string displayName, DocSet parent)
            : base()
        {
            this.contentsOfFile = contentsOfFile;
            this.FullPath = fullPath;
            this.DisplayName = displayName;
            this.Parent = parent;
        }

        protected override string GetContentsOfFile()
        {
            return this.contentsOfFile;
        }

    }
}
