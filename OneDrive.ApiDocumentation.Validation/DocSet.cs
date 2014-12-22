namespace OneDrive.ApiDocumentation.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.IO;

    public class DocSet
    {
        #region Constants
        private const string DocumentationFileExtension = "*.md";
        #endregion

        #region Instance Variables
        Json.JsonValidator m_Validator = new Json.JsonValidator();
        #endregion

        #region Properties
        /// <summary>
        /// Location of the documentation set
        /// </summary>
        public string SourceFolderPath { get; private set; }

        /// <summary>
        /// Documentation files found in the source folder
        /// </summary>
        public DocFile[] Files { get; private set; }

        public ResourceDefinition[] Resources { get; private set; }
        public MethodDefinition[] Methods { get; private set; }

        
        #endregion

        #region Constructors
        public DocSet(string sourceFolderPath)
        {
            SourceFolderPath = sourceFolderPath;
            ReadDocumentationHierarchy(sourceFolderPath);
        }
        #endregion

        /// <summary>
        /// Scan all files in the documentation set to load
        /// information about resources and methods defined in those files
        /// </summary>
        public void ReadResourcesAndMethods()
        {
            var foundResources = new List<ResourceDefinition>();
            var foundMethods = new List<MethodDefinition>();

            foreach (var file in Files)
            {
                file.Scan();

                foundResources.AddRange(file.Resources);
                foundMethods.AddRange(file.Requests);
            }

            Resources = foundResources.ToArray();
            Methods = foundMethods.ToArray();
        }

        /// <summary>
        /// Scan through the document set looking for any broken links
        /// </summary>
        /// <param name="errors"></param>
        /// <returns></returns>
        public bool ValidateLinks(out ValidationError[] errors)
        {
            List<ValidationError> foundErrors = new List<ValidationError>();
            foreach (var file in Files)
            {
                ValidationError[] localErrors;
                if (!file.ValidateNoBrokenLinks(out localErrors))
                {
                    foundErrors.AddRange(localErrors);
                }
            }

            errors = foundErrors.ToArray();
            return errors.Length == 0;
        }

        /// <summary>
        /// Pull the file and folder information from SourceFolderPath into the object
        /// </summary>
        private void ReadDocumentationHierarchy(string path)
        {
            DirectoryInfo sourceFolder = new DirectoryInfo(path);
            var fileInfos = sourceFolder.GetFiles(DocumentationFileExtension, SearchOption.AllDirectories);

            var relativeFilePaths = from fi in fileInfos
                                    select new DocFile(SourceFolderPath, RelativePathToFile(fi.FullName));
            Files = relativeFilePaths.ToArray();
        }

        /// <summary>
        /// Generate a relative URL from the base path of the documentation
        /// </summary>
        /// <param name="fileFullName"></param>
        /// <returns></returns>
        private string RelativePathToFile(string fileFullName)
        {
            System.Diagnostics.Debug.Assert(fileFullName.StartsWith(SourceFolderPath), "fileFullName doesn't start with the source folder path");

            var relativePath = fileFullName.Substring(SourceFolderPath.Length);
            return relativePath;
        }





    }
}
