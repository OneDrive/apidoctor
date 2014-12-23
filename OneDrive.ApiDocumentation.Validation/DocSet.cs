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
        
        public Param.RequestParameters[] RequestParameters { get; private set;}

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
        public void ScanDocumentation()
        {
            var foundResources = new List<ResourceDefinition>();
            var foundMethods = new List<MethodDefinition>();

            m_ResourceCollection.Clear();
            foreach (var file in Files)
            {
                file.Scan();

                foundResources.AddRange(file.Resources);
                foundMethods.AddRange(file.Requests);
                m_ResourceCollection.RegisterJsonResources(foundResources);
            }

            Resources = foundResources.ToArray();
            Methods = foundMethods.ToArray();
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

                if (string.IsNullOrEmpty(method.ResponseMetadata.ResourceType))
                {
                    detectedErrors.Add(new ValidationError(null, "Missing resource type on method {0}", method.DisplayName));
                }
                else
                {
                    if (!m_ResourceCollection.ValidateJson(method.ResponseMetadata, response.Body, out schemaErrors))
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

        public bool TryReadRequestParameters(string relativePathToParamters)
        {
            var path = String.Concat(SourceFolderPath, relativePathToParamters);
            if (!File.Exists(path))
                return false;
            
            try
            {
                string rawJson = null;
                using (StreamReader reader = File.OpenText(path))
                {
                    rawJson = reader.ReadToEnd();
                }
                RequestParameters = Param.RequestParameters.ReadFromJson(rawJson);
                foreach (var request in RequestParameters)
                {
                    request.Method = ConvertPathSeparators(request.Method);
                }


                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error reading parameters: {0}", (object)ex.Message);
                return false;
            }
        }

        private static string ConvertPathSeparators(string p)
        {
            return p.Replace('/', Path.DirectorySeparatorChar);
        }

        public Param.RequestParameters RequestParamtersForMethod(MethodDefinition method)
        {
            if (null == RequestParameters) return null;

            var id = method.DisplayName;
            var query = from p in RequestParameters
                        where p.Method == id && p.Enabled == true
                        select p;

            return query.FirstOrDefault();
        }



    }
}
