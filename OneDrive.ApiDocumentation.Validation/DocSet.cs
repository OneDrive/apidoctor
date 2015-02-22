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
        
        public Scenarios TestScenarios {get; set;}

        public IEnumerable<AuthScopeDefinition> AuthScopes
        {
            get
            {
                return ListFromFiles<AuthScopeDefinition>(x => x.AuthScopes);
            }
        }

        public IEnumerable<ErrorDefinition> ErrorCodes
        {
            get
            {
                return ListFromFiles<ErrorDefinition>(x => x.ErrorCodes);
            }
        }


        #endregion

        #region Constructors
        public DocSet(string sourceFolderPath, string optionalScenarioFileRelativePath = null)
        {
            sourceFolderPath = ResolvePathWithUserRoot(sourceFolderPath);
            if (sourceFolderPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                sourceFolderPath = sourceFolderPath.TrimEnd(new char[] { Path.DirectorySeparatorChar });
            }

            SourceFolderPath = sourceFolderPath;
            ReadDocumentationHierarchy(sourceFolderPath);

            if (!string.IsNullOrEmpty(optionalScenarioFileRelativePath))
                LoadTestScenarios(optionalScenarioFileRelativePath);
        }
        #endregion


        public void LoadTestScenarios(string relativePathName)
        {
            TestScenarios = new Scenarios(this, relativePathName);
        }

        public static string ResolvePathWithUserRoot(string path)
        {
            if (path.StartsWith(string.Concat("~", Path.DirectorySeparatorChar.ToString())))
            {
                var userFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(userFolderPath, path.Substring(2));
            }
            return path;
        }

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
            }
            m_ResourceCollection.RegisterJsonResources(foundResources);

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

            // Verify the HTTP request is valid
            method.VerifyHttpRequest(detectedErrors);

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
                detectedErrors.Add(new ValidationError(ValidationErrorCode.HttpBodyExpected, null, "Body missing from response (expected response includes a body)."));
            }
            else if (!string.IsNullOrEmpty(response.Body))
            {
                ValidationError[] schemaErrors;
                if (method.ExpectedResponseMetadata == null || (string.IsNullOrEmpty(method.ExpectedResponseMetadata.ResourceType) && (expectedResponse != null && !string.IsNullOrEmpty(expectedResponse.Body))))
                {
                    detectedErrors.Add(new ValidationError(ValidationErrorCode.ResponseResourceTypeMissing, null, "Expected a response, but resource type on method is missing: {0}", method.Identifier));
                }
                else
                {
                    if (!m_ResourceCollection.ValidateJsonExample(method.ExpectedResponseMetadata, response.Body, out schemaErrors))
                    {
                        detectedErrors.AddRange(schemaErrors);
                    }
                }
            }

            errors = detectedErrors.ToArray();
            return errors.Length == 0;
        }

        /// <summary>
        /// Scan through the document set looking for any broken links.
        /// </summary>
        /// <param name="errors"></param>
        /// <returns></returns>
        public bool ValidateLinks(bool includeWarnings, out ValidationError[] errors)
        {
            List<ValidationError> foundErrors = new List<ValidationError>();

            Dictionary<string, bool> orphanedPageIndex = Files.ToDictionary(x => RelativePathToFile(x, true), x => true);

            foreach (var file in Files)
            {
                ValidationError[] localErrors;
                string [] linkedPages;
                if (!file.ValidateNoBrokenLinks(includeWarnings, out localErrors, out linkedPages))
                {
                    foundErrors.AddRange(localErrors);
                }
                foreach (string pageName in linkedPages)
                {
                    orphanedPageIndex[pageName] = false;
                }
            }

            foundErrors.AddRange(from o in orphanedPageIndex
                                 where o.Value == true
                                 select new ValidationWarning(ValidationErrorCode.OrphanedDocumentPage, null, "Page {0} has no incoming links.", o.Key));

            errors = foundErrors.ToArray();
            return !errors.WereWarningsOrErrors();
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
                                    select new DocFile(SourceFolderPath, RelativePathToFile(fi.FullName), this);
            Files = relativeFilePaths.ToArray();
        }

        internal string RelativePathToFile(DocFile file, bool urlStyle = false)
        {
            return RelativePathToFile(file.FullPath, SourceFolderPath, urlStyle);
        }

        /// <summary>
        /// Generate a relative file path from the base path of the documentation
        /// </summary>
        /// <param name="fileFullName"></param>
        /// <returns></returns>
        internal string RelativePathToFile(string fileFullName, bool urlStyle = false)
        {
            return RelativePathToFile(fileFullName, SourceFolderPath, urlStyle);
        }

        internal static string RelativePathToFile(string fileFullName, string rootFolderPath, bool urlStyle = false)
        {
            System.Diagnostics.Debug.Assert(fileFullName.StartsWith(rootFolderPath), "fileFullName doesn't start with the source folder path");

            var path = fileFullName.Substring(rootFolderPath.Length);
            if (urlStyle)
            {
                // Should look like this "items/foobar.md" instead of @"\items\foobar.md"
                string[] components = path.Split(Path.DirectorySeparatorChar);
                path = components.ComponentsJoinedByString("/");
            }
            return path;
        }

        /// <summary>
        /// Generates a relative path from deepFilePath to shallowFilePath.
        /// </summary>
        /// <param name="fileFullName"></param>
        /// <param name="rootFolderPath"></param>
        /// <param name="urlStyle"></param>
        /// <returns></returns>
        internal static string RelativePathToRootFromFile(string deepFilePath, string shallowFilePath, bool urlStyle = false)
        {
            // example:
            // deep file path "/auth/auth_msa.md"
            // shallow file path "template/stylesheet.css"
            // returns "../template/stylesheet.css"

            List<string> deepPathComponents = new List<string>(deepFilePath.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries));
            List<string> shallowPathComponents = new List<string>(shallowFilePath.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries));

            while(deepPathComponents.Count > 0 && shallowPathComponents.Count > 0 && deepPathComponents[0].Equals(shallowPathComponents[0], StringComparison.OrdinalIgnoreCase))
            {
                deepPathComponents.RemoveAt(0);
                shallowPathComponents.RemoveAt(0);
            }
            
            int depth = Math.Max(0, deepPathComponents.Count - 1);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < depth; i++)
            {
                if (sb.Length > 0)
                {
                    sb.Append(urlStyle ? "/" : "\\");
                }
                sb.Append("..");
            }

            if (sb.Length > 0)
            {
                sb.Append(urlStyle ? "/" : "\\");
            }
            sb.Append(shallowPathComponents.ComponentsJoinedByString(urlStyle ? "/" : "\\"));

            return sb.ToString();
        }

        private IEnumerable<T> ListFromFiles<T>(Func<DocFile, IEnumerable<T>> perFileSource)
        {
            List<T> collected = new List<T>();
            foreach (var file in Files)
            {
                var data = perFileSource(file);
                if (null != data)
                    collected.AddRange(data);
            }
            return collected;
        }


        internal DocFile LookupFileForPath(string path)
        {
            // Translate path into what we're looking for
            string[] pathComponents = path.Split(new char[] { '/', '\\' });
            string displayName = @"\" + pathComponents.ComponentsJoinedByString(@"\");

            var query = from f in Files
                        where f.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase)
                        select f;

            return query.FirstOrDefault();
        }
        
        



    }
}
