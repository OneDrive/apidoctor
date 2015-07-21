using OneDrive.ApiDocumentation.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.UnitTests.ApiDocumentation.Validation
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
