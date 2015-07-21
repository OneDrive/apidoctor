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
        private string _contentsOfFile;
        public DocFileForTesting(string contentsOfFile, string fullPath, string displayName, DocSet parent)
            : base()
        {
            _contentsOfFile = contentsOfFile;
            FullPath = fullPath;
            DisplayName = displayName;
            Parent = parent;
        }

        protected override string GetContentsOfFile()
        {
            return _contentsOfFile;
        }

    }
}
