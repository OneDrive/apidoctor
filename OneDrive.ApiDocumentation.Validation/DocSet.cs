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
        Json.JsonResourceCollection m_ResourceCollection = new Json.JsonResourceCollection();
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

        public Json.JsonResourceCollection ResourceCollection { get { return m_ResourceCollection; } }
        
        public RunMethodParameters RunParameters {get; set;}

        #endregion

        #region Constructors

        public static string ResolvePathWithUserRoot(string path)
        {
            if (path.StartsWith(string.Concat("~", Path.DirectorySeparatorChar.ToString())))
            {
                var userFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(userFolderPath, path.Substring(2));
            }
            return path;
        }

        public DocSet(string sourceFolderPath)
        {
            sourceFolderPath = ResolvePathWithUserRoot(sourceFolderPath);
            if (sourceFolderPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                sourceFolderPath = sourceFolderPath.TrimEnd(new char[] { Path.DirectorySeparatorChar });
            }

            SourceFolderPath = sourceFolderPath;
            ReadDocumentationHierarchy(sourceFolderPath);
            RunParameters = new RunMethodParameters();
        }
        #endregion

        /// <summary>
        /// Scan all files in the documentation set to load
        /// information about resources and methods defined in those files
        /// </summary>
        public bool ScanDocumentation(out ValidationError[] errors)
        {
            var foundResources = new List<ResourceDefinition>();
            var foundMethods = new List<MethodDefinition>();

            var detectedErrors = new List<ValidationError>();

            m_ResourceCollection.Clear();
            foreach (var file in Files)
            {
                ValidationError[] parseErrors;
                if (!file.Scan(out parseErrors))
                {
                    detectedErrors.AddRange(parseErrors);
                }

                foundResources.AddRange(file.Resources);
                foundMethods.AddRange(file.Requests);
                m_ResourceCollection.RegisterJsonResources(foundResources);
            }

            Resources = foundResources.ToArray();
            Methods = foundMethods.ToArray();

            errors = detectedErrors.ToArray();
            return detectedErrors.Count == 0;
        }

        /// <summary>
        /// Validates that a particular HttpResponse matches the method definition and optionally the expected response.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="response"></param>
        /// <param name="expectedResponse"></param>
        /// <returns></returns>
        public bool ValidateApiMethod(MethodDefinition method, Http.HttpResponse response, Http.HttpResponse expectedResponse, out ValidationError[] errors)
        {
            if (null == method) throw new ArgumentNullException("method");
            if (null == response) throw new ArgumentNullException("response");

            List<ValidationError> detectedErrors = new List<ValidationError>();

            if (null != expectedResponse)
            {
                // Verify that the HTTP portion of the expected response and the actual response are consistent
                ValidationError[] httpErrors;
                if (!expectedResponse.CompareToResponse(response, out httpErrors))
                {
                    detectedErrors.AddRange(httpErrors);
                }
            }

            if (string.IsNullOrEmpty(response.Body) && (expectedResponse != null && !string.IsNullOrEmpty(expectedResponse.Body)))
            {
                detectedErrors.Add(new ValidationError(null, "Body missing from response (expected response includes a body)."));
            }
            else if (!string.IsNullOrEmpty(response.Body))
            {
                ValidationError[] schemaErrors;
                if (method.ExpectedResponseMetadata == null || (string.IsNullOrEmpty(method.ExpectedResponseMetadata.ResourceType) && (expectedResponse != null && !string.IsNullOrEmpty(expectedResponse.Body))))
                {
                    detectedErrors.Add(new ValidationError(null, "Expected a response, but resource type on method is missing: {0}", method.DisplayName));
                }
                else
                {
                    if (!m_ResourceCollection.ValidateJson(method.ExpectedResponseMetadata, response.Body, out schemaErrors))
                    {
                        detectedErrors.AddRange(schemaErrors);
                    }
                }
            }

            errors = detectedErrors.ToArray();
            return errors.Length == 0;
        }

        /// <summary>
        /// Scan through the document set looking for any broken links
        /// </summary>
        /// <param name="errors"></param>
        /// <returns></returns>
        public bool ValidateLinks(bool includeWarnings, out ValidationError[] errors)
        {
            List<ValidationError> foundErrors = new List<ValidationError>();
            foreach (var file in Files)
            {
                ValidationError[] localErrors;
                if (!file.ValidateNoBrokenLinks(includeWarnings, out localErrors))
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
            if (!sourceFolder.Exists)
            {
                throw new FileNotFoundException(string.Format("Cannot find documentation. Directory doesn't exist: {0}", sourceFolder.FullName));
            }

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
