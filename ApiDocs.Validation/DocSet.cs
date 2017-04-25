/*
 * Markdown Scanner
 * Copyright (c) Microsoft Corporation
 * All rights reserved. 
 * 
 * MIT License
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of 
 * this software and associated documentation files (the ""Software""), to deal in 
 * the Software without restriction, including without limitation the rights to use, 
 * copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
 * Software, and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all 
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace ApiDocs.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using ApiDocs.Validation.Config;
    using ApiDocs.Validation.Error;
    using ApiDocs.Validation.Http;
    using ApiDocs.Validation.Json;
    using ApiDocs.Validation.Params;
    using Newtonsoft.Json;

    public class DocSet
    {
        #region Constants
        private const string DocumentationFileExtension = "*.md";
        #endregion

        #region Instance Variables
        readonly JsonResourceCollection resourceCollection = new JsonResourceCollection();
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

        public JsonResourceCollection ResourceCollection { get { return this.resourceCollection; } }

        public List<ScenarioDefinition> TestScenarios { get; internal set; }
        public List<CannedRequestDefinition> CannedRequests { get; internal set; }

        public IEnumerable<AuthScopeDefinition> AuthScopes
        {
            get
            {
                return this.ListFromFiles(x => x.AuthScopes);
            }
        }

        public IEnumerable<ErrorDefinition> ErrorCodes
        {
            get
            {
                return this.ListFromFiles(x => x.ErrorCodes);
            }
        }

        public ApiRequirements Requirements { get; internal set; }

        public DocumentOutlineFile DocumentStructure { get; internal set;}

        internal TableSpec.TableSpecConverter TableParser { get; private set; }
        #endregion

        #region Constructors
        public DocSet(string sourceFolderPath)
        {
            sourceFolderPath = ResolvePathWithUserRoot(sourceFolderPath);
            if (sourceFolderPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                sourceFolderPath = sourceFolderPath.TrimEnd(Path.DirectorySeparatorChar);
            }

            this.SourceFolderPath = sourceFolderPath;
            this.ReadDocumentationHierarchy(sourceFolderPath);

            this.LoadRequirements();
            this.LoadTestScenarios();
            this.LoadTableParser();
        }



        public DocSet()
        {
            
        }
        #endregion

        private void LoadRequirements()
        {
            ApiRequirementsFile[] requirements = TryLoadConfigurationFiles<ApiRequirementsFile>(this.SourceFolderPath);
            var foundRequirements = requirements.FirstOrDefault();
            if (null != foundRequirements)
            {
                Console.WriteLine("Using API requirements file: {0}", foundRequirements.SourcePath);
                this.Requirements = foundRequirements.ApiRequirements;
            }

            DocumentOutlineFile[] outlines = TryLoadConfigurationFiles<DocumentOutlineFile>(this.SourceFolderPath);
            var foundOutlines = outlines.FirstOrDefault();
            if (null != foundOutlines)
            {
                Console.WriteLine("Using document structure file: {0}", foundOutlines.SourcePath);
                this.DocumentStructure = foundOutlines;
            }
        }

        private void LoadTableParser()
        {
            TableSpec.TableParserConfigFile[] configurations = TryLoadConfigurationFiles<TableSpec.TableParserConfigFile>(this.SourceFolderPath);
            var tableParserConfig = configurations.FirstOrDefault();
            if (null != tableParserConfig)
            {
                Console.WriteLine("Using table definitions from: {0}", tableParserConfig.SourcePath);
                this.TableParser = new TableSpec.TableSpecConverter(tableParserConfig.TableDefinitions);
            }
            else
            {
                Console.WriteLine("Using default table parser definitions");
                this.TableParser = TableSpec.TableSpecConverter.FromDefaultConfiguration();
            }
        }

        private void LoadTestScenarios()
        {
            this.TestScenarios = new List<ScenarioDefinition>();
            this.CannedRequests = new List<CannedRequestDefinition>();

            ScenarioFile[] files = TryLoadConfigurationFiles<ScenarioFile>(this.SourceFolderPath);
            foreach (var file in files)
            {
                Console.WriteLine("Found test scenario file: {0}", file.SourcePath);
                this.TestScenarios.AddRange(file.Scenarios);
                if (null != file.CannedRequests)
                {
                    this.CannedRequests.AddRange(file.CannedRequests);
                }
            }
        }

        /// <summary>
        /// Looks for JSON files that are a member of the doc set and parses them
        /// to look for files of type T that can be loaded.
        /// </summary>
        /// <returns>The loaded configuration files.</returns>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static T[] TryLoadConfigurationFiles<T>(string path) where T : ConfigFile
        {
            List<T> validConfigurationFiles = new List<T>();

            DirectoryInfo docSetDir = new DirectoryInfo(path);
            if (!docSetDir.Exists)
                return new T[0];

            var jsonFiles = docSetDir.GetFiles("*.json", SearchOption.AllDirectories);
            foreach (var file in jsonFiles)
            {
                try
                {
                    using (var reader = file.OpenText())
                    {
                        var config = JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
                        if (null != config && config.IsValid)
                        {
                            config.LoadComplete();
                            validConfigurationFiles.Add(config);
                            config.SourcePath = file.FullName;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Logging.LogMessage(new ValidationWarning(ValidationErrorCode.JsonParserException, file.FullName, "JSON parser error: {0}", ex.Message));
                }
                catch (Exception ex)
                {
                    Logging.LogMessage(new ValidationWarning(ValidationErrorCode.JsonParserException, file.FullName, "Exception reading file: {0}", ex.Message));
                }

            }

            return validConfigurationFiles.ToArray();
        }

        public static string ResolvePathWithUserRoot(string path)
        {
            if (path.StartsWith(string.Concat("~", Path.DirectorySeparatorChar.ToString())))
            {
                var userFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(userFolderPath, path.Substring(2));
            }
            // Return absolute path
            return Path.GetFullPath(path);
        }

        /// <summary>
        /// Scan all files in the documentation set to load
        /// information about resources and methods defined in those files
        /// </summary>
        public bool ScanDocumentation(string tags, out ValidationError[] errors)
        {
            var foundResources = new List<ResourceDefinition>();
            var foundMethods = new List<MethodDefinition>();

            var detectedErrors = new List<ValidationError>();

            this.resourceCollection.Clear();

            if (!this.Files.Any())
            {
                detectedErrors.Add(
                    new ValidationError(
                        ValidationErrorCode.NoDocumentsFound,
                        null,
                        "No markdown documentation was found in the current path."));
            }

            foreach (var file in this.Files)
            {
                ValidationError[] parseErrors;
                file.Scan(tags, out parseErrors);
                if (parseErrors != null)
                {
                    detectedErrors.AddRange(parseErrors);
                }

                foundResources.AddRange(file.Resources);
                foundMethods.AddRange(file.Requests);
            }
            this.resourceCollection.RegisterJsonResources(foundResources);

            this.Resources = foundResources.ToArray();
            this.Methods = foundMethods.ToArray();

            //CheckForDuplicatesResources(foundResources, detectedErrors);
            //CheckForDuplicatesMethods(foundMethods, detectedErrors);

            errors = detectedErrors.ToArray();
            return detectedErrors.Count == 0;
        }

        /// <summary>
        /// Report errors if there are any methods with duplicate identifiers discovered
        /// </summary>
        /// <param name="foundMethods"></param>
        /// <param name="detectedErrors"></param>
        private void CheckForDuplicatesMethods(List<MethodDefinition> foundMethods, List<ValidationError> detectedErrors)
        {
            Dictionary<string, DocFile> identifiers = new Dictionary<string, DocFile>();
            foreach (var method in foundMethods)
            {
                if (identifiers.ContainsKey(method.Identifier))
                {
                    detectedErrors.Add(new ValidationError(ValidationErrorCode.DuplicateMethodIdentifier, null, "Duplicate method identifier detected: {0} in {1} and {2}.", method.Identifier, identifiers[method.Identifier].DisplayName, method.SourceFile.DisplayName));
                }
                else
                {
                    identifiers.Add(method.Identifier, method.SourceFile);
                }
            }
        }

        private void CheckForDuplicatesResources(List<ResourceDefinition> foundResources, List<ValidationError> detectedErrors)
        {
            Dictionary<string, DocFile> identifiers = new Dictionary<string, DocFile>();
            foreach (var resource in foundResources)
            {
                if (identifiers.ContainsKey(resource.Name))
                {
                    detectedErrors.Add(new ValidationError(ValidationErrorCode.DuplicateMethodIdentifier, null, "Duplicate method identifier detected: {0} in {1} and {2}.", resource.Name, identifiers[resource.Name].DisplayName, resource.SourceFile.DisplayName));
                }
                else
                {
                    identifiers.Add(resource.Name, resource.SourceFile);
                }
            }
        }

        /// <summary>
        /// Validates that a particular HttpResponse matches the method definition and optionally the expected response.
        /// </summary>
        /// <param name="method">Method definition that was used to generate a request.</param>
        /// <param name="actualResponse">Actual response from the service (this is what we validate).</param>
        /// <param name="expectedResponse">Prototype response (expected) that shows what a valid response should look like.</param>
        /// <param name="scenario">A test scenario used to generate the response, which may include additional parameters to verify.</param>
        /// <param name="errors">A collection of errors, warnings, and verbose messages generated by this process.</param>
        public static void ValidateApiMethod(MethodDefinition method, HttpResponse actualResponse, HttpResponse expectedResponse, ScenarioDefinition scenario, out ValidationError[] errors)
        {
            if (null == method) throw new ArgumentNullException("method");
            if (null == actualResponse) throw new ArgumentNullException("actualResponse");

            List<ValidationError> detectedErrors = new List<ValidationError>();

            // Verify the request is valid (headers, request body)
            method.VerifyRequestFormat(detectedErrors);

            // Verify that the expected response headers match the actual response headers
            ValidationError[] httpErrors;
            if (null != expectedResponse && !expectedResponse.ValidateResponseHeaders(actualResponse, out httpErrors))
            {
                detectedErrors.AddRange(httpErrors);
            }

            // Verify the actual response body is correct according to the schema defined for the response
            ValidationError[] bodyErrors;
            VerifyResponseBody(method, actualResponse, expectedResponse, out bodyErrors);
            detectedErrors.AddRange(bodyErrors);

            // Verify any expectations in the scenario are met
            if (null != scenario)
            {
                scenario.ValidateExpectations(actualResponse, detectedErrors);
            }

            errors = detectedErrors.ToArray();
        }

        /// <summary>
        /// Verify that the body of the actual response is consistent with the method definition and expected response parameters
        /// </summary>
        /// <param name="method">The MethodDefinition that generated the response.</param>
        /// <param name="actualResponse">The actual response from the service to validate.</param>
        /// <param name="expectedResponse">The prototype expected response from the service.</param>
        /// <param name="detectedErrors">A collection of errors that will be appended with any detected errors</param>
        private static void VerifyResponseBody(MethodDefinition method, HttpResponse actualResponse, HttpResponse expectedResponse, out ValidationError[] errors)
        {
            List<ValidationError> detectedErrors = new List<ValidationError>();

            if (string.IsNullOrEmpty(actualResponse.Body) &&
                (expectedResponse != null && !string.IsNullOrEmpty(expectedResponse.Body)))
            {
                detectedErrors.Add(new ValidationError(ValidationErrorCode.HttpBodyExpected, null, "Body missing from response (expected response includes a body or a response type was provided)."));
            }
            else if (!string.IsNullOrEmpty(actualResponse.Body))
            {
                ValidationError[] schemaErrors;
                if (method.ExpectedResponseMetadata == null ||
                    (string.IsNullOrEmpty(method.ExpectedResponseMetadata.ResourceType) &&
                     (expectedResponse != null && !string.IsNullOrEmpty(expectedResponse.Body))))
                {
                    detectedErrors.Add(new ValidationError(ValidationErrorCode.ResponseResourceTypeMissing, null, "Expected a response, but resource type on method is missing: {0}", method.Identifier));
                }
                else
                {
                    var otherResources = method.SourceFile.Parent.ResourceCollection;
                    if (
                        !otherResources.ValidateResponseMatchesSchema(
                            method,
                            actualResponse,
                            expectedResponse,
                            out schemaErrors))
                    {
                        detectedErrors.AddRange(schemaErrors);
                    }
                }

                var responseValidation = actualResponse.IsResponseValid(
                    method.SourceFile.DisplayName,
                    method.SourceFile.Parent.Requirements);
                detectedErrors.AddRange(responseValidation.Messages);
            }

            errors = detectedErrors.ToArray();
        }

        /// <summary>
        /// Scan through the document set looking for any broken links.
        /// </summary>
        /// <param name="includeWarnings"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public bool ValidateLinks(bool includeWarnings, string[] relativePathForFiles, out ValidationError[] errors, bool requireFilenameCaseMatch)
        {
            List<ValidationError> foundErrors = new List<ValidationError>();

            Dictionary<string, bool> orphanedPageIndex = this.Files.ToDictionary(x => this.RelativePathToFile(x, true), x => true);

            List<DocFile> filesToCheck = new List<Validation.DocFile>();
            if (null == relativePathForFiles)
            {
                filesToCheck.AddRange(this.Files);
            }
            else
            {
                foreach(var relativePath in relativePathForFiles)
                {
                    var file = this.LookupFileForPath(relativePath);
                    if (null != file)
                        filesToCheck.Add(file);
                }
            }

            foreach (var file in filesToCheck)
            {
                ValidationError[] localErrors;
                string [] linkedPages;
                if (!file.ValidateNoBrokenLinks(includeWarnings, out localErrors, out linkedPages, requireFilenameCaseMatch))
                {
                    foundErrors.AddRange(localErrors);
                }
                foreach (string pageName in linkedPages)
                {
                    orphanedPageIndex[pageName] = false;
                }

                if (null != file.Annotation && file.Annotation.TocPath != null)
                {
                    var pageName = this.CleanUpDisplayName(file.DisplayName);
                    orphanedPageIndex[pageName] = false;
                }
            }

            if (relativePathForFiles == null)
            {
                // We only report orphan pages when scanning the whole docset.
                foundErrors.AddRange(from o in orphanedPageIndex
                                     where o.Value
                                     select new ValidationWarning(ValidationErrorCode.OrphanedDocumentPage, null, "Page {0} has no incoming links.", o.Key));
            }

            errors = foundErrors.ToArray();
            return !errors.WereWarningsOrErrors();
        }

        /// <summary>
        /// Remove the initial path separator and make sure all of the remaining ones are forward-slash
        /// </summary>
        /// <param name="displayName"></param>
        /// <returns></returns>
        private string CleanUpDisplayName(string displayName)
        {
            if (displayName.StartsWith(Path.DirectorySeparatorChar.ToString()))
                displayName = displayName.Substring(1);
            return displayName.Replace(Path.DirectorySeparatorChar, '/');
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
                                    select new DocFile(this.SourceFolderPath, this.RelativePathToFile(fi.FullName), this);
            this.Files = relativeFilePaths.ToArray();
        }

        internal string RelativePathToFile(DocFile file, bool urlStyle = false)
        {
            return RelativePathToFile(file.FullPath, this.SourceFolderPath, urlStyle);
        }

        /// <summary>
        /// Generate a relative file path from the base path of the documentation
        /// </summary>
        /// <param name="fileFullName"></param>
        /// <param name="urlStyle"></param>
        /// <returns></returns>
        internal string RelativePathToFile(string fileFullName, bool urlStyle = false)
        {
            return RelativePathToFile(fileFullName, this.SourceFolderPath, urlStyle);
        }

        internal static string RelativePathToFile(string fileFullName, string rootFolderPath, bool urlStyle = false)
        {
            Debug.Assert(fileFullName.StartsWith(rootFolderPath), "fileFullName doesn't start with the source folder path");

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
        /// <param name="shallowFilePath"></param>
        /// <param name="urlStyle"></param>
        /// <param name="deepFilePath"></param>
        /// <returns></returns>
        public static string RelativePathToRootFromFile(string deepFilePath, string shallowFilePath, bool urlStyle = false)
        {
            if (deepFilePath == null) throw new ArgumentNullException("deepFilePath");
            if (shallowFilePath == null) throw new ArgumentNullException("shallowFilePath");

            // example:
            // deep file path "/auth/auth_msa.md"
            // shallow file path "template/stylesheet.css"
            // returns "../template/stylesheet.css"

            if (deepFilePath.Equals(shallowFilePath, StringComparison.OrdinalIgnoreCase))
            {
                return urlStyle ? "#" : Path.GetFileName(shallowFilePath);
            }

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

            if (sb.Length == 0)
                Console.WriteLine("Failed to resolve link for {0}", shallowFilePath);

            return sb.ToString();
        }

        private IEnumerable<T> ListFromFiles<T>(Func<DocFile, IEnumerable<T>> perFileSource)
        {
            List<T> collected = new List<T>();
            foreach (var file in this.Files)
            {
                var data = perFileSource(file);
                if (null != data)
                    collected.AddRange(data);
            }
            return collected;
        }


        public DocFile LookupFileForPath(string path)
        {
            // Translate path into what we're looking for
            string[] pathComponents = path.Split('/', '\\');
            string pathSeperator = Path.DirectorySeparatorChar.ToString();
            string displayName = pathSeperator + pathComponents.ComponentsJoinedByString(pathSeperator);

            var query = from f in this.Files
                        where f.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase)
                        select f;

            return query.FirstOrDefault();
        }

    }

    public class DocSetEventArgs : EventArgs
    {
        public string Message { get; private set; }
        public bool Verbose { get; private set; }
        public string Title {get; private set;}

        public DocSetEventArgs(string message, bool verbose = false)
        {
            this.Message = message;
            this.Verbose = verbose;
        }

        public DocSetEventArgs(bool verbose, string title, string format, params object[] parameters)
        {
            this.Title = title;
            this.Message = string.Format(format, parameters);
            this.Verbose = verbose;
        }
    }
}
