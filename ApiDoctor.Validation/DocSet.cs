/*
 * API Doctor
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

namespace ApiDoctor.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using ApiDoctor.Validation.Config;
    using ApiDoctor.Validation.Error;
    using ApiDoctor.Validation.Http;
    using ApiDoctor.Validation.Json;
    using ApiDoctor.Validation.OData.Transformation;
    using ApiDoctor.Validation.Params;
    using ApiDoctor.Validation.TableSpec;
    using Newtonsoft.Json;

    public class DocSet
    {
        private const string DocumentationFileExtension = "*.md";
        private static readonly string[] XmlFileExtensions = new[] { "*.xml", "*.html", "*.htm" };

        private static List<string> foldersToSkip;

        private static List<string> filesToSkip;

        private bool writeFixesBackToDisk;

        #region Properties

        /// <summary>
        /// Static schema config for those hard-to-reach places
        /// </summary>
        public static SchemaConfig SchemaConfig { get; private set; }

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
        public EnumerationDefinition[] Enums { get; private set; }

        public JsonResourceCollection ResourceCollection { get; } = new JsonResourceCollection();

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

        public MetadataValidationConfigs MetadataValidationConfigs { get; internal set; }

        public DocumentOutlineFile DocumentStructure { get; internal set; }

        public LinkValidationConfigFile LinkValidationConfig { get; private set; }

        public TableParserConfigFile TableParserConfig { get; private set; }

        internal TableSpecConverter TableParser { get; private set; }
        #endregion

        #region Constructors
        public DocSet(string sourceFolderPath, bool writeFixesBackToDisk = false)
        {
            this.writeFixesBackToDisk = writeFixesBackToDisk;
            sourceFolderPath = ResolvePathWithUserRoot(sourceFolderPath);
            if (sourceFolderPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                sourceFolderPath = sourceFolderPath.TrimEnd(Path.DirectorySeparatorChar);
            }
            this.SourceFolderPath = sourceFolderPath;

            this.LoadRequirements();
            this.LoadTestScenarios();
            this.LoadTableParser();

            this.ReadDocumentationHierarchy(sourceFolderPath);
        }



        public DocSet()
        {
            this.SourceFolderPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName().Split(".", StringSplitOptions.RemoveEmptyEntries).First());
            Directory.CreateDirectory(this.SourceFolderPath);
            this.LoadRequirements();
            this.LoadTestScenarios();
            this.LoadTableParser();
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

            MetadataValidationConfigFile[] documentsValidationConfigs = TryLoadConfigurationFiles<MetadataValidationConfigFile>(this.SourceFolderPath);
            var foundDocsconfigs = documentsValidationConfigs.FirstOrDefault();
            if (null != foundDocsconfigs)
            {
                Console.WriteLine("Using Documents validation config file: {0}", foundDocsconfigs.SourcePath);
                this.MetadataValidationConfigs = foundDocsconfigs.MetadataValidationConfigs;
            }

            DocumentOutlineFile[] outlines = TryLoadConfigurationFiles<DocumentOutlineFile>(this.SourceFolderPath);
            var foundOutlines = outlines.FirstOrDefault();
            if (null != foundOutlines)
            {
                Console.WriteLine("Using document structure file: {0}", foundOutlines.SourcePath);
                this.DocumentStructure = foundOutlines;
            }

            LinkValidationConfigFile[] linkConfigs = TryLoadConfigurationFiles<LinkValidationConfigFile>(this.SourceFolderPath);
            var foundLinkConfig = linkConfigs.FirstOrDefault();
            if (foundLinkConfig != null)
            {
                Console.WriteLine($"Using link validation config file: {foundLinkConfig.SourcePath}");
                this.LinkValidationConfig = foundLinkConfig;
            }

            SchemaConfigFile[] schemaConfigs = TryLoadConfigurationFiles<SchemaConfigFile>(this.SourceFolderPath);
            var schemaConfig = schemaConfigs.FirstOrDefault();
            if (schemaConfig != null)
            {
                Console.WriteLine($"Using schema config file: {schemaConfig.SourcePath}");
                SchemaConfig = schemaConfig.SchemaConfig;
            }
            else
            {
                SchemaConfig = new SchemaConfig();
            }

            string indent = "".PadLeft(4);
            string[] requiredYamlHeaders = SchemaConfig.RequiredYamlHeaders;
            if (requiredYamlHeaders.Any())
            {
                Console.WriteLine($"{indent}Required YAML headers: {requiredYamlHeaders.ComponentsJoinedByString(", ")}");
            }
            else
            {
                Console.WriteLine($"{indent}Required YAML headers have not been set.");
            }

            List<string> treatErrorsAsWarningsWorkloads = SchemaConfig.TreatErrorsAsWarningsWorkloads;
            if (treatErrorsAsWarningsWorkloads.Any())
            {
                Console.WriteLine($"{indent}Treating errors as warnings for: {treatErrorsAsWarningsWorkloads.ComponentsJoinedByString(", ")}");
            }

            foldersToSkip = SchemaConfig.FoldersToSkip;
            filesToSkip = SchemaConfig.FilesToSkip;
        }

        private void LoadTableParser()
        {
            TableSpec.TableParserConfigFile[] configurations = TryLoadConfigurationFiles<TableSpec.TableParserConfigFile>(this.SourceFolderPath);
            var tableParserConfig = configurations.FirstOrDefault();
            if (null != tableParserConfig)
            {
                Console.WriteLine("Using table definitions from: {0}", tableParserConfig.SourcePath);
                this.TableParserConfig = tableParserConfig;
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
        public bool ScanDocumentation(string tags, IssueLogger issues)
        {
            var foundResources = new List<ResourceDefinition>();
            var foundMethods = new List<MethodDefinition>();
            var foundEnums = new List<EnumerationDefinition>();

            this.ResourceCollection.Clear();

            if (!this.Files.Any())
            {
                issues.Error(ValidationErrorCode.NoDocumentsFound,
                    "No markdown documentation was found in the current path.");
            }

            foreach (var file in this.Files)
            {
                file.Scan(tags, issues.For(file.DisplayName));
                foundResources.AddRange(file.Resources);
                foundMethods.AddRange(file.Requests);
                foundEnums.AddRange(file.Enums);
            }

            // do a topological sort so that base types come first.
            // also resolve and link up parent references.
            // do a topological sort of the resources so that we process base types first.
            var sortedResources = new List<ResourceDefinition>();
            var processed = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var resourceMap = new Dictionary<string, List<ResourceDefinition>>(StringComparer.OrdinalIgnoreCase);
            foreach (var resource in foundResources)
            {
                List<ResourceDefinition> resources;
                if (!resourceMap.TryGetValue(resource.Name, out resources))
                {
                    resources = new List<ResourceDefinition>();
                    resourceMap.Add(resource.Name, resources);
                }

                resources.Add(resource);

                if (resources.Count > 1)
                {
                    var firstResource = resources[0];
                    if (!string.Equals(firstResource.Name, resource.Name))
                    {
                        issues.Warning(ValidationErrorCode.Unknown,
                            $"Inconsistent name casing found for {resource.Name} from {resource.SourceFile.DisplayName}" +
                            $" vs duplicate resource in {firstResource.SourceFile.DisplayName}");
                    }

                    if (!string.Equals(firstResource.BaseType, resource.BaseType))
                    {
                        if (string.IsNullOrEmpty(resource.BaseType))
                        {
                            resource.BaseType = firstResource.BaseType;
                        }
                        else if (string.IsNullOrEmpty(firstResource.BaseType))
                        {
                            foreach (var duplicateResource in resources)
                            {
                                duplicateResource.BaseType = resource.BaseType;
                            }
                        }
                        else
                        {
                            issues.Error(ValidationErrorCode.ExpectedTypeDifferent,
                                $"Inconsistent base types for {resource.Name} : {resource.BaseType} from {resource.SourceFile.DisplayName}" +
                                $" vs duplicate resource in {firstResource.SourceFile.DisplayName} with base type {firstResource.BaseType} ");
                        }
                    }
                }
            }

            foreach (var kvp in resourceMap)
            {
                if (kvp.Value.Select(r => r.SourceFile.DisplayName).Distinct().Count() > 1)
                {
                    issues.Warning(ValidationErrorCode.Unknown,
                        $"Resource {kvp.Key} is defined in multiple files: {string.Join(", ", kvp.Value.Select(r => r.SourceFile.DisplayName))}");
                }

                // if we've already processed a node, we don't need to do it again.
                if (!processed.ContainsKey(kvp.Key))
                {
                    TopologicalSortNode(kvp.Value, resourceMap, sortedResources, processed);
                }
            }

            this.ResourceCollection.RegisterJsonResources(sortedResources);

            this.Resources = sortedResources.ToArray();
            this.Methods = foundMethods.ToArray();
            this.Enums = foundEnums.ToArray();

            // suppressing until we have a better way that's less collission-prone
            ////CheckForDuplicatesMethods(foundMethods, issues);

            var definedTypes = new HashSet<string>(
                this.Resources.Select(r => r.Name).
                Concat(this.Enums.Select(e => (e.Namespace + "." + e.TypeName).Trim('.'))).
                Concat(new[] { "odata.error" }));

            foreach (var resource in this.Resources)
            {
                var resourceIssues = issues.For(resource.Name);
                if (!string.IsNullOrWhiteSpace(resource.BaseType) && !definedTypes.Contains(resource.BaseType))
                {
                    resourceIssues.Error(ValidationErrorCode.ResourceTypeNotFound,
                        $"Referenced base type {resource.BaseType} in resource {resource.Name} is not defined in the doc set!");
                }

                if (resource.BaseType != null && resource.BaseType.Trim().Length == 0)
                {
                    resourceIssues.Error(ValidationErrorCode.EmptyResourceBaseType,
                        $"Missing value for referenced base type in resource {resource.Name}");
                }

                foreach (var param in resource.Parameters)
                {
                    if (param.Type.CustomTypeName != null)
                    {
                        EnsureDefinedInDocs(param.Type.CustomTypeName, definedTypes, resource.SourceFile, resourceIssues.For(param.Name));
                    }
                }

                if (!string.IsNullOrEmpty(resource.KeyPropertyName) &&
                    !resource.Parameters.Any(p => p.Name == resource.KeyPropertyName))
                {
                    resourceIssues.Warning(ValidationErrorCode.RequiredPropertiesMissing,
                        $"Could not find key property {resource.KeyPropertyName} on {resource.Name}. Assuming doc bug, and ignoring.");
                    resource.KeyPropertyName = null;
                }
            }

            foreach (var method in this.Methods)
            {
                foreach (var param in method.Parameters.Concat(method.RequestBodyParameters))
                {
                    if (param.Type?.CustomTypeName != null)
                    {
                        EnsureDefinedInDocs(param.Type.CustomTypeName, definedTypes, method.SourceFile, issues.For(method.Identifier + "/" + param.Name));
                    }
                }
            }

            return issues.Issues.All(issue => !issue.IsWarningOrError);
        }

        private static void TopologicalSortNode(
            List<ResourceDefinition> duplicateResources,
            Dictionary<string, List<ResourceDefinition>> allResources,
            List<ResourceDefinition> sortedResources,
            Dictionary<string, bool> processed)
        {
            var resourceName = duplicateResources[0].Name;

            bool sorted;
            if (processed.TryGetValue(resourceName, out sorted) && !sorted)
            {
                throw new ArgumentException($"Circular dependency in {duplicateResources[0].SourceFile.DisplayName}. Depends on {duplicateResources[0].BaseType}.");
            }

            if (!sorted)
            {
                // mark it processed but not yet sorted
                processed[resourceName] = false;

                foreach (var baseTypeName in duplicateResources.Select(r => r.BaseType).Where(bt => !string.IsNullOrEmpty(bt)).Distinct())
                {
                    List<ResourceDefinition> baseTypes;
                    if (allResources.TryGetValue(baseTypeName, out baseTypes))
                    {
                        // annoying how there are duplicates...
                        foreach (var resource in duplicateResources)
                        {
                            if (resource.ResolvedBaseTypeReference == null)
                            {
                                resource.ResolvedBaseTypeReference = baseTypes[0];
                            }
                        }

                        TopologicalSortNode(baseTypes, allResources, sortedResources, processed);
                    }
                }

                // we're done, so mark it sorted
                sortedResources.AddRange(duplicateResources);
                processed[resourceName] = true;
            }
        }

        private void EnsureDefinedInDocs(string type, HashSet<string> definedTypes, DocFile sourceFile, IssueLogger issues)
        {
            if (!string.IsNullOrEmpty(type))
            {
                if (!definedTypes.Contains(type))
                {
                    var nameOnly = type.Substring(type.LastIndexOf('.') + 1);
                    var suggestion =
                            definedTypes.FirstOrDefault(t => t.IContains(nameOnly)) ??
                            definedTypes.FirstOrDefault(t => nameOnly.IContains(t.Substring(t.LastIndexOf('.') + 1))) ??
                            "UNKNOWN";

                    issues.Error(ValidationErrorCode.ResourceTypeNotFound, $"Referenced type {type} is not defined in the doc set! Potential suggestion: {suggestion}");
                }
            }
        }

        /// <summary>
        /// Report errors if there are any methods with duplicate identifiers discovered
        /// </summary>
        private void CheckForDuplicatesMethods(List<MethodDefinition> foundMethods, IssueLogger issues)
        {
            Dictionary<string, DocFile> identifiers = new Dictionary<string, DocFile>();
            foreach (var method in foundMethods)
            {
                if (identifiers.ContainsKey(method.Identifier))
                {
                    issues.Warning(ValidationErrorCode.DuplicateMethodIdentifier, $"Duplicate method identifier detected: {method.Identifier} in {identifiers[method.Identifier].DisplayName} and {method.SourceFile.DisplayName}.");
                }
                else
                {
                    identifiers.Add(method.Identifier, method.SourceFile);
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
        /// <param name="issues">A collection of errors, warnings, and verbose messages generated by this process.</param>
        public static void ValidateApiMethod(MethodDefinition method, HttpResponse actualResponse, HttpResponse expectedResponse, ScenarioDefinition scenario, IssueLogger issues)
        {
            if (null == method) throw new ArgumentNullException("method");
            if (null == actualResponse) throw new ArgumentNullException("actualResponse");

            // Verify the request is valid (headers, request body)
            method.VerifyRequestFormat(issues);

            // Verify that the expected response headers match the actual response headers
            if (null != expectedResponse)
            {
                expectedResponse.ValidateResponseHeaders(actualResponse, issues);
            }

            // Verify the actual response body is correct according to the schema defined for the response
            VerifyResponseBody(method, actualResponse, expectedResponse, issues);

            // Verify any expectations in the scenario are met
            if (null != scenario)
            {
                scenario.ValidateExpectations(actualResponse, issues);
            }
        }

        /// <summary>
        /// Verify that the body of the actual response is consistent with the method definition and expected response parameters
        /// </summary>
        /// <param name="method">The MethodDefinition that generated the response.</param>
        /// <param name="actualResponse">The actual response from the service to validate.</param>
        /// <param name="expectedResponse">The prototype expected response from the service.</param>
        /// <param name="issues">A collection of errors that will be appended with any detected errors</param>
        private static void VerifyResponseBody(MethodDefinition method, HttpResponse actualResponse, HttpResponse expectedResponse, IssueLogger issues)
        {
            if (string.IsNullOrEmpty(actualResponse.Body) &&
                (expectedResponse != null && !string.IsNullOrEmpty(expectedResponse.Body)))
            {
                issues.Error(ValidationErrorCode.HttpBodyExpected, "Body missing from response (expected response includes a body or a response type was provided).");
            }
            else if (!string.IsNullOrEmpty(actualResponse.Body))
            {
                if (method.ExpectedResponseMetadata == null ||
                    (string.IsNullOrEmpty(method.ExpectedResponseMetadata.ResourceType) &&
                     (expectedResponse != null && !string.IsNullOrEmpty(expectedResponse.Body))))
                {
                    issues.Error(ValidationErrorCode.ResponseResourceTypeMissing, $"Expected a response, but resource type on method is missing: {method.Identifier}");
                }
                else
                {
                    var otherResources = method.SourceFile.Parent.ResourceCollection;
                    otherResources.ValidateResponseMatchesSchema(
                        method,
                        actualResponse,
                        expectedResponse,
                        issues);
                }

                var responseValidation = actualResponse.IsResponseValid(
                    method.SourceFile.DisplayName,
                    method.SourceFile.Parent.Requirements,
                    issues);
            }
        }

        /// <summary>
        /// Scan through the document set looking for any broken links.
        /// </summary>
        /// <returns></returns>
        public bool ValidateLinks(bool includeWarnings, string[] relativePathForFiles, IssueLogger issues, bool requireFilenameCaseMatch, bool printOrphanedFiles)
        {
            Dictionary<string, bool> orphanedPageIndex = this.Files.ToDictionary(x => this.RelativePathToFile(x, true), x => true);

            List<DocFile> filesToCheck = new List<Validation.DocFile>();
            if (null == relativePathForFiles)
            {
                filesToCheck.AddRange(this.Files);
            }
            else
            {
                foreach (var relativePath in relativePathForFiles)
                {
                    var file = this.LookupFileForPath(relativePath);
                    if (null != file)
                        filesToCheck.Add(file);
                }
            }

            foreach (var file in filesToCheck) //.Where(f => f.LinkDestinations.Any()))
            {
                var issuesForFile = issues.For(file.DisplayName);
                try
                {
                    // skip files without links
                    if (file.LinkDestinations?.Length == 0)
                    {
                        continue;
                    }

                    string[] linkedPages;
                    file.ValidateNoBrokenLinks(includeWarnings, issuesForFile, out linkedPages, requireFilenameCaseMatch);

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
                catch (Exception ex)
                {
                    issuesForFile.Error(ValidationErrorCode.Unknown, "Exception processing links.", ex);
                }
            }

            if (relativePathForFiles == null && printOrphanedFiles)
            {
                // We only report orphan pages when scanning the whole docset.
                foreach (var o in orphanedPageIndex.Where(o => o.Value))
                {
                    issues.Warning(ValidationErrorCode.OrphanedDocumentPage, $"Page {o.Key} has no incoming links.");
                }
            }

            return !issues.Issues.WereWarningsOrErrors();
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

            var markdownFileInfos = sourceFolder.GetFiles(DocumentationFileExtension, SearchOption.AllDirectories);
            var markdownFiles = markdownFileInfos
                .Where(fi => foldersToSkip.All(folderToSkip => !fi.DirectoryName.Contains(folderToSkip)) && !filesToSkip.Contains(fi.Name))
                .Select(fi => new DocFile(this.SourceFolderPath, this.RelativePathToFile(fi.FullName), this) { WriteFixesBackToDisk = this.writeFixesBackToDisk });

            var supplementalFileInfos = XmlFileExtensions.SelectMany(x => sourceFolder.GetFiles(x, SearchOption.AllDirectories)).ToArray();
            var supplementalFiles = supplementalFileInfos
                .Where(fi => foldersToSkip.All(folderToSkip => !fi.DirectoryName.Contains(folderToSkip)) && !filesToSkip.Contains(fi.Name))
                .Select(fi => new SupplementalFile(this.SourceFolderPath, this.RelativePathToFile(fi.FullName), this));

            this.Files = markdownFiles.Concat(supplementalFiles).ToArray();
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

            while (deepPathComponents.Count > 0 && shallowPathComponents.Count > 0 && deepPathComponents[0].Equals(shallowPathComponents[0], StringComparison.OrdinalIgnoreCase))
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
        public string Title { get; private set; }

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
