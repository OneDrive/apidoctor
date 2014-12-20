using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ApiDocumentationTester
{
    public class DocSet
    {

        public string SourceFolderPath { get; private set; }

        public DocFile[] Files { get; private set; }

        public DocSet(string sourceFolderPath)
        {
            this.SourceFolderPath = sourceFolderPath;
            LoadDocumentationHierarchy();
        }

        private void LoadDocumentationHierarchy()
        {
            DirectoryInfo sourceFolder = new DirectoryInfo(SourceFolderPath);
            var fileInfos = sourceFolder.GetFiles("*.md", SearchOption.AllDirectories);

            var relativeFilePaths = from fi in fileInfos
                                    select new DocFile(SourceFolderPath, RelativePathToFile(fi.FullName));


            Files = relativeFilePaths.ToArray();
        }

        private string RelativePathToFile(string fileFullName)
        {
            System.Diagnostics.Debug.Assert(fileFullName.StartsWith(SourceFolderPath), "fileFullName doesn't start with the source folder path");

            var relativePath = fileFullName.Substring(SourceFolderPath.Length);
            return relativePath;
        }



    }
}
