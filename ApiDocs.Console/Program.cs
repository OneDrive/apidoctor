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

namespace ApiDocs.ConsoleApp
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using AppVeyor;
    using ApiDocs.DocumentationGeneration;
    using Publishing.Html;
    using Publishing.Swagger;
    using Validation;
    using Validation.Error;
    using Validation.Http;
    using Validation.Json;
    using Validation.OData;
    using Validation.Params;
    using Validation.Writers;
    using CommandLine;
    using Newtonsoft.Json;


    class Program
    {
        private const int ExitCodeFailure = 1;
        private const int ExitCodeSuccess = 0;
        private const int ParallelTaskCount = 5;

        public static readonly BuildWorkerApi BuildWorker = new BuildWorkerApi();
        public static AppConfigFile CurrentConfiguration { get; private set; }

        // Set to true to disable returning an error code when the app exits.
        private static bool IgnoreErrors { get; set; }

        private static List<UndocumentedPropertyWarning> DiscoveredUndocumentedProperties = new List<UndocumentedPropertyWarning>();

        static void Main(string[] args)
        {
            Logging.ProviderLogger(new ConsoleAppLogger());

            FancyConsole.WriteLine(ConsoleColor.Green, "APIDocs tool, version {0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            FancyConsole.WriteLine();
            if (args.Length > 0)
                FancyConsole.WriteLine("Command line: " + args.ComponentsJoinedByString(" "));


            string verbName = null;
            BaseOptions verbOptions = null;

            var options = new CommandLineOptions();
            if (!Parser.Default.ParseArguments(args, options,
              (verb, subOptions) =>
              {
                  // if parsing succeeds the verb name and correct instance
                  // will be passed to onVerbCommand delegate (string,object)
                  verbName = verb;
                  verbOptions = (BaseOptions)subOptions;
              }))
            {
                Exit(failure: true);
            }

            IgnoreErrors = verbOptions.IgnoreErrors;
#if DEBUG
            if (verbOptions.AttachDebugger)
            {
                Debugger.Launch();
            }
#endif

            SetStateFromOptions(verbOptions);

            var task = Task.Run(() => RunInvokedMethodAsync(options, verbName, verbOptions));
            try
            {
                task.Wait();
            }
            catch (Exception ex)
            {
                FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, "Uncaught exception is causing a crash: {0}", ex);
                Exit(failure: true, customExitCode: 40);
            }
        }

        private static void SetStateFromOptions(BaseOptions verbOptions)
        {
            if (!string.IsNullOrEmpty(verbOptions.AppVeyorServiceUrl))
            {
                BuildWorker.UrlEndPoint = new Uri(verbOptions.AppVeyorServiceUrl);
            }

            var commandOptions = verbOptions as DocSetOptions;
            if (null != commandOptions)
            {
                FancyConsole.WriteVerboseOutput = commandOptions.EnableVerboseOutput;
            }

            var checkOptions = verbOptions as BasicCheckOptions;
            if (null != checkOptions)
            {
                if (!string.IsNullOrEmpty(checkOptions.FilesChangedFromOriginalBranch))
                {
                    if (string.IsNullOrEmpty(checkOptions.GitExecutablePath))
                    {
                        var foundPath = GitHelper.FindGitLocation();
                        if (null == foundPath)
                        {
                            FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, "To use changes-since-branch-only, git-path must be specified.");
                            Exit(failure: true, customExitCode: 41);
                        }
                        else
                        {
                            FancyConsole.WriteLine(FancyConsole.ConsoleDefaultColor, $"Using GIT executable: {foundPath}");
                            checkOptions.GitExecutablePath = foundPath;
                        }

                    }
                }
            }

            FancyConsole.LogFileName = verbOptions.LogFile;
        }

        public static void LoadCurrentConfiguration(DocSetOptions options)
        {
            if (CurrentConfiguration != null)
                return;

            if (null != options)
            {
                var configurationFiles = DocSet.TryLoadConfigurationFiles<AppConfigFile>(options.DocumentationSetPath);
                CurrentConfiguration = configurationFiles.FirstOrDefault();
                if (null != CurrentConfiguration)
                    Console.WriteLine("Using configuration file: {0}", CurrentConfiguration.SourcePath);
            }
        }

        private static async Task RunInvokedMethodAsync(CommandLineOptions origCommandLineOpts, string invokedVerb, BaseOptions options)
        {
            string[] missingProps;
            if (!options.HasRequiredProperties(out missingProps))
            {
                var error = new ValidationError(ValidationErrorCode.MissingRequiredArguments, null, "Command line is missing required arguments: {0}", missingProps.ComponentsJoinedByString(", "));
                FancyConsole.WriteLine(origCommandLineOpts.GetUsage(invokedVerb));
                await WriteOutErrorsAndFinishTestAsync(new ValidationError[] { error }, options.SilenceWarnings, printFailuresOnly: options.PrintFailuresOnly);
                Exit(failure: true);
            }

            LoadCurrentConfiguration(options as DocSetOptions);

            bool returnSuccess = true;

            switch (invokedVerb)
            {
                case CommandLineOptions.VerbPrint:
                    await PrintDocInformationAsync((PrintOptions)options);
                    break;
                case CommandLineOptions.VerbCheckLinks:
                    returnSuccess = await CheckLinksAsync((BasicCheckOptions)options);
                    break;
                case CommandLineOptions.VerbDocs:
                    returnSuccess = await CheckDocsAsync((BasicCheckOptions)options);
                    break;
                case CommandLineOptions.VerbCheckAll:
                    returnSuccess = await CheckDocsAllAsync((BasicCheckOptions)options);
                    break;
                case CommandLineOptions.VerbService:
                    returnSuccess = await CheckServiceAsync((CheckServiceOptions)options);
                    break;
                case CommandLineOptions.VerbPublish:
                    returnSuccess = await PublishDocumentationAsync((PublishOptions)options);
                    break;
                case CommandLineOptions.VerbPublishMetadata:
                    returnSuccess = await PublishMetadataAsync((PublishMetadataOptions)options);
                    break;
                case CommandLineOptions.VerbMetadata:
                    await CheckServiceMetadataAsync((CheckMetadataOptions)options);
                    break;
                case CommandLineOptions.VerbGenerateDocs:
                    returnSuccess = await GenerateDocsAsync((GenerateDocsOptions)options);
                    break;
                case CommandLineOptions.VerbAbout:
                    PrintAboutMessage();
                    Exit(failure: false);
                    break;
            }

            Exit(failure: !returnSuccess);
        }

        /// <summary>
        /// Perform all of the local documentation based checks. This is the "compile"
        /// command for the documentation that verifies that everything is clean inside the 
        /// documentation itself.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task<bool> CheckDocsAllAsync(BasicCheckOptions options)
        {
            var docset = await GetDocSetAsync(options);

            if (null == docset)
                return false;

            var checkLinksResult = await CheckLinksAsync(options, docset);
            var checkDocsResults = await CheckDocsAsync(options, docset);

            return checkLinksResult && checkDocsResults;
        }

        private static void PrintAboutMessage()
        {
            FancyConsole.WriteLine();
            FancyConsole.WriteLine(ConsoleColor.Cyan, "apidocs.exe - API Documentation Test Tool");
            FancyConsole.WriteLine(ConsoleColor.Cyan, "Copyright (c) 2015 Microsoft Corporation");
            FancyConsole.WriteLine();
            FancyConsole.WriteLine(ConsoleColor.Cyan, "For more information see http://github.com/onedrive/markdown-scanner/");
            FancyConsole.WriteLine();
        }


        /// <summary>
        /// Create and return a document set based on input options
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static Task<DocSet> GetDocSetAsync(DocSetOptions options)
        {
            FancyConsole.VerboseWriteLine("Opening documentation from {0}", options.DocumentationSetPath);
            DocSet set = null;
            try
            {
                set = new DocSet(options.DocumentationSetPath);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, ex.Message);
                Exit(failure: true);
                return Task.FromResult<DocSet>(null);
            }

            FancyConsole.VerboseWriteLine("Scanning documentation files...");
            ValidationError[] loadErrors;

            string tagsToInclude;
            if (null == options.PageParameterDict || !options.PageParameterDict.TryGetValue("tags", out tagsToInclude))
            {
                tagsToInclude = String.Empty;
            }

            DateTimeOffset start = DateTimeOffset.Now;
            set.ScanDocumentation(tagsToInclude, out loadErrors);
            DateTimeOffset end = DateTimeOffset.Now;
            TimeSpan duration = end.Subtract(start);

            FancyConsole.WriteLine($"Took {duration.TotalSeconds} to parse {set.Files.Length} source files.");

            if (loadErrors.Any())
            {
                FancyConsole.WriteLine();
                FancyConsole.WriteLine("Errors detected while parsing documentation:");
                WriteMessages(loadErrors, false, "  ", false);
            }

            return Task.FromResult<DocSet>(set);
        }

        public static void RecordWarning(string format, params object[] variables)
        {
            var message = string.Format(format, variables);
            FancyConsole.WriteLine(FancyConsole.ConsoleWarningColor, message);
            Task.Run(() => BuildWorker.AddMessageAsync(message, MessageCategory.Warning));
        }

        public static void RecordError(string format, params object[] variables)
        {
            var message = string.Format(format, variables);
            FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, message);
            Task.Run(() => BuildWorker.AddMessageAsync(message, MessageCategory.Error));
        }

        /// <summary>
        /// Output information about the document set to the console
        /// </summary>
        /// <param name="options"></param>
        /// <param name="docset"></param>
        /// <returns></returns>
        private static async Task PrintDocInformationAsync(PrintOptions options, DocSet docset = null)
        {
            docset = docset ?? await GetDocSetAsync(options);
            if (null == docset)
            {
                return;
            }

            if (options.PrintFiles)
            {
                await PrintFilesAsync(options, docset);
            }
            if (options.PrintResources)
            {
                await PrintResourcesAsync(options, docset);
            }
            if (options.PrintMethods)
            {
                await PrintMethodsAsync(options, docset);
            }
            if (options.PrintAccounts)
            {
                await PrintAccountsAsync(options, docset);
            }
        }

        #region Print verb commands
        /// <summary>
        /// Prints a list of the documentation files in a docset to the console.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="docset"></param>
        /// <returns></returns>
        private static async Task PrintFilesAsync(DocSetOptions options, DocSet docset)
        {
            docset = docset ?? await GetDocSetAsync(options);
            if (null == docset)
                return;


            if (null == docset)
                return;

            FancyConsole.WriteLine();
            FancyConsole.WriteLine(FancyConsole.ConsoleHeaderColor, "Documentation files");

            string format = null;
            if (options.EnableVerboseOutput)
            {
                format = "{1} (resources: {2}, methods: {3})";
            }
            else
            {
                format = "{0} (r:{2}, m:{3})";
            }

            foreach (var file in docset.Files)
            {
                ConsoleColor color = FancyConsole.ConsoleSuccessColor;
                if (file.Resources.Length == 0 && file.Requests.Length == 0)
                    color = FancyConsole.ConsoleWarningColor;

                FancyConsole.WriteLineIndented("  ", color, format, file.DisplayName, file.FullPath, file.Resources.Length, file.Requests.Length);
            }
        }

        /// <summary>
        /// Print a list of the resources detected in the documentation set to the console.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="docset"></param>
        /// <returns></returns>
        private static async Task PrintResourcesAsync(DocSetOptions options, DocSet docset)
        {
            docset = docset ?? await GetDocSetAsync(options);
            if (null == docset)
                return;

            FancyConsole.WriteLine();
            FancyConsole.WriteLine(FancyConsole.ConsoleHeaderColor, "Defined resources:");
            FancyConsole.WriteLine();

            var sortedResources = docset.Resources.OrderBy(x => x.Name);

            foreach (var resource in sortedResources)
            {
                if (options.EnableVerboseOutput)
                {
                    string metadata = JsonConvert.SerializeObject(resource.OriginalMetadata);
                    FancyConsole.Write("  ");
                    FancyConsole.Write(FancyConsole.ConsoleHeaderColor, resource.Name);
                    FancyConsole.WriteLine(" flags: {1}", resource.Name, metadata);
                }
                else
                {
                    FancyConsole.WriteLineIndented("  ", FancyConsole.ConsoleHeaderColor, resource.Name);
                }

                FancyConsole.WriteLineIndented("    ", FancyConsole.ConsoleCodeColor, resource.ExampleText);
                FancyConsole.WriteLine();
            }
        }

        /// <summary>
        /// Print a list of the methods (request/responses) discovered in the documentation to the console.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="docset"></param>
        /// <returns></returns>
        private static async Task PrintMethodsAsync(DocSetOptions options, DocSet docset)
        {
            docset = docset ?? await GetDocSetAsync(options);
            if (null == docset)
                return;

            FancyConsole.WriteLine();
            FancyConsole.WriteLine(FancyConsole.ConsoleHeaderColor, "Defined methods:");
            FancyConsole.WriteLine();

            foreach (var method in docset.Methods)
            {
                FancyConsole.WriteLine(FancyConsole.ConsoleHeaderColor, "Method '{0}' in file '{1}'", method.Identifier, method.SourceFile.DisplayName);

                var requestMetadata = options.EnableVerboseOutput ? JsonConvert.SerializeObject(method.RequestMetadata) : string.Empty;
                FancyConsole.WriteLineIndented("  ", FancyConsole.ConsoleSubheaderColor, "Request: {0}", requestMetadata);
                FancyConsole.WriteLineIndented("    ", FancyConsole.ConsoleCodeColor, method.Request);

                if (options.EnableVerboseOutput)
                {
                    FancyConsole.WriteLine();
                    var responseMetadata = JsonConvert.SerializeObject(method.ExpectedResponseMetadata);
                    FancyConsole.WriteLineIndented("  ", FancyConsole.ConsoleSubheaderColor, "Expected Response: {0}", responseMetadata);
                    FancyConsole.WriteLineIndented("    ", FancyConsole.ConsoleCodeColor, method.ExpectedResponse);
                    FancyConsole.WriteLine();
                }
                FancyConsole.WriteLine();
            }
        }

        private static async Task PrintAccountsAsync(PrintOptions options, DocSet docset)
        {
            var accounts = Program.CurrentConfiguration.Accounts;
            foreach(var account in accounts)
            {
                FancyConsole.WriteLine($"{account.Name} = {account.BaseUrl}");
            }
        }


        #endregion


        /// <summary>
        /// Verifies that all markdown links inside the documentation are valid. Prints warnings
        /// to the console for any invalid links. Also checks for orphaned documents.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="docs"></param>
        private static async Task<bool> CheckLinksAsync(BasicCheckOptions options, DocSet docs = null)
        {
            const string testName = "Check-links";
            var docset = docs ?? await GetDocSetAsync(options);

            if (null == docset)
                return false;


            string[] interestingFiles = null;
            if (!string.IsNullOrEmpty(options.FilesChangedFromOriginalBranch))
            {
                GitHelper helper = new GitHelper(options.GitExecutablePath, options.DocumentationSetPath);
                interestingFiles = helper.FilesChangedFromBranch(options.FilesChangedFromOriginalBranch);
            }

            TestReport.StartTest(testName);
            
            ValidationError[] errors;
            docset.ValidateLinks(options.EnableVerboseOutput, interestingFiles, out errors, options.RequireFilenameCaseMatch);

            foreach (var error in errors)
            {
                MessageCategory category;
                if (error.IsWarning)
                    category = MessageCategory.Warning;
                else if (error.IsError)
                    category = MessageCategory.Error;
                else
                    category = MessageCategory.Information;

                string message = string.Format("{1}: {0}", error.Message.FirstLineOnly(), error.Code);
                await TestReport.LogMessageAsync(message, category);
            }

            return await WriteOutErrorsAndFinishTestAsync(errors, options.SilenceWarnings, successMessage: "No link errors detected.", testName: testName, printFailuresOnly: options.PrintFailuresOnly);
        }

        /// <summary>
        /// Find the first instance of a method with a particular name in the docset.
        /// </summary>
        /// <param name="docset"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private static MethodDefinition LookUpMethod(DocSet docset, string methodName)
        {
            var query = from m in docset.Methods
                        where m.Identifier.Equals(methodName, StringComparison.OrdinalIgnoreCase)
                        select m;

            return query.FirstOrDefault();
        }

        /// <summary>
        /// Returns a collection of methods matching the string query. This can either be the
        /// literal name of the method of a wildcard match for the method name.
        /// </summary>
        /// <param name="docs"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        private static MethodDefinition[] FindMethods(DocSet docs, string wildcardPattern)
        {
            var query = from m in docs.Methods
                        where m.Identifier.IsWildcardMatch(wildcardPattern)
                        select m;
            return query.ToArray();
        }

        /// <summary>
        /// Perform internal consistency checks on the documentation, including verify that 
        /// code blocks have proper formatting, that resources are used properly, and that expected
        /// responses and examples conform to the resource definitions.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="docs"></param>
        /// <returns></returns>
        private static async Task<bool> CheckDocsAsync(BasicCheckOptions options, DocSet docs = null)
        {
            var docset = docs ?? await GetDocSetAsync(options);
            if (null == docset)
                return false;

            FancyConsole.WriteLine();

            var resultStructure = await CheckDocStructure(options, docset);

            var resultMethods = await CheckMethodsAsync(options, docset);
            CheckResults resultExamples = new CheckResults();
            if (string.IsNullOrEmpty(options.MethodName))
            {
                resultExamples = await CheckExamplesAsync(options, docset);
            }

            var combinedResults = resultMethods + resultExamples + resultStructure;

            if (options.IgnoreWarnings)
            {
                combinedResults.ConvertWarningsToSuccess();
            }

            combinedResults.PrintToConsole();

            return combinedResults.FailureCount == 0;
        }

        private static async Task<CheckResults> CheckDocStructure(BasicCheckOptions options, DocSet docset)
        {
            var results = new CheckResults();

            TestReport.StartTest("Verify document structure");
            List<ValidationError> detectedErrors = new List<ValidationError>();
            foreach (var doc in docset.Files)
            {
                detectedErrors.AddRange(doc.CheckDocumentStructure());
            }
            await WriteOutErrorsAndFinishTestAsync(detectedErrors, options.SilenceWarnings, "    ", "Passed.", false, "Verify document structure", "Warnings detected", "Errors detected", printFailuresOnly: options.PrintFailuresOnly);
            results.IncrementResultCount(detectedErrors);

            return results;
        }

        /// <summary>
        /// Perform an internal consistency check on the examples defined in the documentation. Prints
        /// the results of the tests to the console.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="docset"></param>
        /// <returns></returns>
        private static async Task<CheckResults> CheckExamplesAsync(BasicCheckOptions options, DocSet docset)
        {
            var results = new CheckResults();

            foreach (var doc in docset.Files)
            {
                if (doc.Examples.Length == 0)
                {
                    continue;
                }
                
                FancyConsole.WriteLine(FancyConsole.ConsoleHeaderColor, "Checking examples in \"{0}\"...", doc.DisplayName);

                foreach (var example in doc.Examples)
                {
                    if (example.Metadata == null)
                        continue;
                    if (example.Language != CodeLanguage.Json)
                        continue;

                    var testName = string.Format("check-example: {0}", example.Metadata.MethodName, example.Metadata.ResourceType);
                    TestReport.StartTest(testName, doc.DisplayName);

                    ValidationError[] errors;
                    docset.ResourceCollection.ValidateJsonExample(example.Metadata, example.SourceExample, out errors, new ValidationOptions { RelaxedStringValidation = options.RelaxStringTypeValidation });

                    await WriteOutErrorsAndFinishTestAsync(errors, options.SilenceWarnings, "   ", "Passed.", false, testName, "Warnings detected", "Errors detected", printFailuresOnly: options.PrintFailuresOnly);
                    results.IncrementResultCount(errors);
                }
            }

            return results;
        }

        /// <summary>
        /// Performs an internal consistency check on the methods (requests/responses) in the documentation.
        /// Prints the results of the tests to the console.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="docset"></param>
        /// <returns></returns>
        private static async Task<CheckResults> CheckMethodsAsync(BasicCheckOptions options, DocSet docset)
        {
            MethodDefinition[] methods = FindTestMethods(options, docset);
            CheckResults results = new CheckResults();

            foreach (var method in methods)
            {
                var testName = "API Request: " + method.Identifier;
                
                TestReport.StartTest(testName, method.SourceFile.DisplayName, skipPrintingHeader: options.PrintFailuresOnly);

                if (string.IsNullOrEmpty(method.ExpectedResponse))
                {
                    await TestReport.FinishTestAsync(testName, TestOutcome.Failed, "Null response where one was expected.", printFailuresOnly: options.PrintFailuresOnly);
                    results.FailureCount++;
                    continue;
                }

                var parser = new HttpParser();


                ValidationError[] errors;
                try
                {
                    var expectedResponse = parser.ParseHttpResponse(method.ExpectedResponse);
                    
                    method.ValidateResponse(expectedResponse, null, null, out errors, new ValidationOptions { RelaxedStringValidation = options.RelaxStringTypeValidation });
                }
                catch (Exception ex)
                {
                    errors = new ValidationError[] { new ValidationError(ValidationErrorCode.ExceptionWhileValidatingMethod, method.SourceFile.DisplayName, ex.Message) };
                }

                await WriteOutErrorsAndFinishTestAsync(errors, options.SilenceWarnings, "   ", "Passed.", false, testName, "Warnings detected", "Errors detected", printFailuresOnly: options.PrintFailuresOnly);
                results.IncrementResultCount(errors);
            }

            return results;
        }

        private static DocFile[] GetSelectedFiles(BasicCheckOptions options, DocSet docset)
        {
            List<DocFile> files = new List<DocFile>();
            if (!string.IsNullOrEmpty(options.FilesChangedFromOriginalBranch))
            {
                GitHelper helper = new GitHelper(options.GitExecutablePath, options.DocumentationSetPath);
                var changedFiles = helper.FilesChangedFromBranch(options.FilesChangedFromOriginalBranch);
                
                foreach (var filePath in changedFiles)
                {
                    var file = docset.LookupFileForPath(filePath);
                    if (null != file)
                        files.Add(file);
                }
            }
            else
            {
                files.AddRange(docset.Files);
            }
            return files.ToArray();
        }

        /// <summary>
        /// Parse the command line parameters into a set of methods that match the command line parameters.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="docset"></param>
        /// <returns></returns>
        private static MethodDefinition[] FindTestMethods(BasicCheckOptions options, DocSet docset)
        {
            MethodDefinition[] methods = null;
            if (!string.IsNullOrEmpty(options.FilesChangedFromOriginalBranch))
            {
                GitHelper helper = new GitHelper(options.GitExecutablePath, options.DocumentationSetPath);
                var changedFiles = helper.FilesChangedFromBranch(options.FilesChangedFromOriginalBranch);
                List<MethodDefinition> foundMethods = new List<MethodDefinition>();
                foreach (var filePath in changedFiles)
                {
                    var file = docset.LookupFileForPath(filePath);
                    if (null != file)
                        foundMethods.AddRange(file.Requests);
                }
                return foundMethods.ToArray();
            }
            else if (!string.IsNullOrEmpty(options.MethodName))
            {
                methods = FindMethods(docset, options.MethodName);
                if (null == methods || methods.Length == 0)
                {
                    FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, "Unable to locate method '{0}' in docset.", options.MethodName);
                    Exit(failure: true);
                }
            }
            else if (!string.IsNullOrEmpty(options.FileName))
            {
                var selectedFileQuery = FilesMatchingFilter(docset, options.FileName);
                if (!selectedFileQuery.Any())
                {
                    FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, "Unable to locate file matching '{0}' in docset.", options.FileName);
                    Exit(failure: true);
                }

                List<MethodDefinition> foundMethods = new List<MethodDefinition>();
                foreach (var docFile in selectedFileQuery)
                {
                    foundMethods.AddRange(docFile.Requests);
                }
                methods = foundMethods.ToArray();
            }
            else
            {
                methods = docset.Methods;
            }
            return methods;
        }

        /// <summary>
        /// Return a set of files matching a given wildcard filter
        /// </summary>
        /// <param name="docset"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        private static IEnumerable<DocFile> FilesMatchingFilter(DocSet docset, string filter)
        {
            return from f in docset.Files
                   where f.DisplayName.IsWildcardMatch(filter)
                   select f;
        }


        /// <summary>
        /// Print a collection of ValidationError objects to the console. Also shuts down the test runner
        /// for the current test.
        /// </summary>
        /// <param name="errors"></param>
        /// <param name="silenceWarnings"></param>
        /// <param name="indent"></param>
        /// <param name="successMessage"></param>
        /// <param name="endLineBeforeWriting"></param>
        /// <param name="testName"></param>
        /// <param name="warningsMessage"></param>
        /// <param name="errorsMessage"></param>
        /// <returns></returns>
        private static async Task<bool> WriteOutErrorsAndFinishTestAsync(IEnumerable<ValidationError> errors, bool silenceWarnings, string indent = "", string successMessage = null, bool endLineBeforeWriting = false, string testName = null, string warningsMessage = null, string errorsMessage = null, bool printFailuresOnly = false)
        {
            var validationErrors = errors as ValidationError[] ?? errors.ToArray();

            string writeMessageHeader = null;
            if (printFailuresOnly)
            {
                writeMessageHeader = $"Test {testName} results:";
            }

            WriteMessages(validationErrors, silenceWarnings, indent, endLineBeforeWriting, beforeWriteHeader: writeMessageHeader);

            TestOutcome outcome = TestOutcome.None;
            string outputMessage = null;

            var errorMessages = validationErrors.Where(x => x.IsError);
            var warningMessages = validationErrors.Where(x => x.IsWarning);
                        
            if (errorMessages.Any())
            {
                // Write failure message
                var singleError = errorMessages.First();
                if (errorMessages.Count() == 1)
                    outputMessage = singleError.Message.FirstLineOnly();
                else
                    outputMessage = "Multiple errors occured.";

                outcome = TestOutcome.Failed;
            }
            else if (!silenceWarnings && warningMessages.Any())
            {
                // Write warning message
                var singleWarning = warningMessages.First();
                if (warningMessages.Count() == 1)
                    outputMessage = singleWarning.Message.FirstLineOnly();
                else
                    outputMessage = "Multiple warnings occured.";
                outcome = TestOutcome.Passed;
            }
            else 
            {
                // write success message!
                outputMessage = successMessage;
                outcome = TestOutcome.Passed;
            }

            // Record this test's outcome in the build worker API
            if (null != testName)
            {
                var errorMessage = (from e in validationErrors select e.ErrorText).ComponentsJoinedByString("\r\n");
                await TestReport.FinishTestAsync(testName, outcome, outputMessage, stdOut: errorMessage, printFailuresOnly: printFailuresOnly);
            }

            return outcome == TestOutcome.Passed;
        }

        /// <summary>
        /// Prints ValidationError messages to the console, optionally excluding warnings and messages.
        /// </summary>
        /// <param name="validationErrors"></param>
        /// <param name="errorsOnly"></param>
        /// <param name="indent"></param>
        /// <param name="endLineBeforeWriting"></param>
        private static void WriteMessages(ValidationError[] validationErrors, bool errorsOnly = false, string indent = "", bool endLineBeforeWriting = false, string beforeWriteHeader = null)
        {
            bool writtenHeader = false;
            foreach (var error in validationErrors)
            {
                RecordUndocumentedProperties(error);

                // Skip messages if verbose output is off
                if (!error.IsWarning && !error.IsError && !FancyConsole.WriteVerboseOutput)
                {
                    continue;
                }

                // Skip warnings if silence warnings is enabled.
                if (errorsOnly && !error.IsError)
                {
                    continue;
                }

                if (endLineBeforeWriting)
                {
                    FancyConsole.WriteLine();
                }

                if (!writtenHeader && beforeWriteHeader != null)
                {
                    writtenHeader = true;
                    FancyConsole.WriteLine(beforeWriteHeader);
                }
                WriteValidationError(indent, error);
            }
        }

        private static void RecordUndocumentedProperties(ValidationError error)
        {
            if (error is UndocumentedPropertyWarning)
            {
                DiscoveredUndocumentedProperties.Add((UndocumentedPropertyWarning)error);
            }
            else if (error.InnerErrors != null && error.InnerErrors.Any())
            {
                foreach(var innerError in error.InnerErrors)
                {
                    RecordUndocumentedProperties(innerError);
                }
            }
        }

        /// <summary>
        /// Prints a formatted ValidationError object to the console.
        /// </summary>
        /// <param name="indent"></param>
        /// <param name="error"></param>
        internal static void WriteValidationError(string indent, ValidationError error)
        {
            ConsoleColor color;
            if (error.IsWarning)
            {
                color = FancyConsole.ConsoleWarningColor;
            }
            else if (error.IsError)
            {
                color = FancyConsole.ConsoleErrorColor;
            }
            else
            {
                color = FancyConsole.ConsoleDefaultColor;
            }

            FancyConsole.WriteLineIndented(indent, color, error.ErrorText);
        }

        /// <summary>
        /// Executes the remote service tests defined in the documentation. This is similar to CheckDocs, expect
        /// that the actual requests come from the service instead of the documentation. Prints the errors to 
        /// the console.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task<bool> CheckServiceAsync(CheckServiceOptions options)
        {
            // See if we're supposed to run check-service on this branch (assuming we know what branch we're running on)
            if (!string.IsNullOrEmpty(options.BranchName))
            {
                string[] validBranches = null;
                if (null != CurrentConfiguration)
                    validBranches = CurrentConfiguration.CheckServiceEnabledBranches;

                if (null != validBranches && !validBranches.Contains(options.BranchName))
                {
                    RecordWarning("Aborting check-service run. Branch \"{0}\" wasn't in the checkServiceEnabledBranches configuration list.", options.BranchName);
                    return true;
                }
            }

            // Configure HTTP request logging
            Validation.HttpLog.HttpLogGenerator httpLogging = null;
            if (!string.IsNullOrEmpty(options.HttpLoggerOutputPath))
            {
                httpLogging = new Validation.HttpLog.HttpLogGenerator(options.HttpLoggerOutputPath);
                httpLogging.InitializePackage();
                HttpRequest.HttpLogSession = httpLogging;
            }

            var docset = await GetDocSetAsync(options);
            if (null == docset)
            {
                return false;
            }

            FancyConsole.WriteLine();

            if (!string.IsNullOrEmpty(options.ODataMetadataLevel))
            {
                ValidationConfig.ODataMetadataLevel = options.ODataMetadataLevel;
            }

            if (options.FoundAccounts == null || !options.FoundAccounts.Any(x=>x.Enabled))
            {
                RecordError("No account was found. Cannot connect to the service.");
                return false;
            }

            var accountsToProcess =
                options.FoundAccounts.Where(
                    x => string.IsNullOrEmpty(options.AccountName)
                        ? x.Enabled
                        : options.AccountName.Equals(x.Name));

            if (!accountsToProcess.Any())
            {
                Console.WriteLine("No accounts were selected.");
            }

            var methods = FindTestMethods(options, docset);

            Dictionary<string, CheckResults> results = new Dictionary<string, CheckResults>();
            foreach (var account in accountsToProcess)
            {
                var accountResults = await CheckMethodsForAccountAsync(options, account, methods, docset);
                if (null != accountResults)
                {
                    results[account.Name] = accountResults;
                }
            }

            // Disable http logging
            if (null != httpLogging)
            {
                HttpRequest.HttpLogSession = null;
                httpLogging.ClosePackage();
            }

            // Print out account summary if multiple accounts were used
            foreach (var key in results.Keys)
            {
                FancyConsole.Write("Account {0}: ", key);
                results[key].PrintToConsole(false);
            }

            // Print out undocumented properties, if any where found.
            WriteUndocumentedProperties();

            return !results.Values.Any(x => x.WereFailures);
        }

        private static void WriteUndocumentedProperties()
        {
            // Collapse all the properties we've discovered down into an easy-to-digest list of properties and resources
            Dictionary<string, HashSet<string>> undocumentedProperties = new Dictionary<string, HashSet<string>>();
            foreach (var props in DiscoveredUndocumentedProperties)
            {
                HashSet<string> foundPropertiesOnResource;
                if (props.ResourceName != null)
                {
                    if (!undocumentedProperties.TryGetValue(props.ResourceName, out foundPropertiesOnResource))
                    {
                        foundPropertiesOnResource = new HashSet<string>();
                        undocumentedProperties.Add(props.ResourceName, foundPropertiesOnResource);
                    }
                    foundPropertiesOnResource.Add(props.PropertyName);
                }
            }

            string seperator = ", ";
            if (undocumentedProperties.Any())
            {
                FancyConsole.WriteLine(FancyConsole.ConsoleWarningColor, "The following undocumented properties were detected:");
                foreach (var resource in undocumentedProperties)
                {
                    Console.WriteLine($"Resource {resource.Key}: {resource.Value.ComponentsJoinedByString(seperator)}");
                }
            }
        }

        /// <summary>
        /// Execute the provided methods on the given account.
        /// </summary>
        /// <param name="commandLineOptions"></param>
        /// <param name="account"></param>
        /// <param name="methods"></param>
        /// <param name="docset"></param>
        /// <returns>A CheckResults instance that contains the details of the test run</returns>
        private static async Task<CheckResults> CheckMethodsForAccountAsync(CheckServiceOptions commandLineOptions, IServiceAccount account, MethodDefinition[] methods, DocSet docset)
        {
            ConfigureAdditionalHeadersForAccount(commandLineOptions, account);

            FancyConsole.WriteLine(FancyConsole.ConsoleHeaderColor, "Testing account: {0}", account.Name);
            FancyConsole.WriteLine(FancyConsole.ConsoleCodeColor, "Preparing authentication for requests...", account.Name);

            try
            {
                await account.PrepareForRequestAsync();
            }
            catch (Exception ex)
            {
                RecordError(ex.Message);
                return null;
            }
            
            int concurrentTasks = commandLineOptions.ParallelTests ? ParallelTaskCount : 1;

            CheckResults docSetResults = new CheckResults();

            await ForEachAsync(methods, concurrentTasks, async method =>
            {
                FancyConsole.WriteLine(
                    FancyConsole.ConsoleCodeColor,
                    "Running validation for method: {0}",
                    method.Identifier);

                // List out the scenarios defined for this method
                ScenarioDefinition[] scenarios = docset.TestScenarios.ScenariosForMethod(method);

                // Test these scenarios and validate responses
                ValidationResults results = await method.ValidateServiceResponseAsync(scenarios, account, 
                    new ValidationOptions {
                        RelaxedStringValidation = commandLineOptions.RelaxStringTypeValidation,
                        IgnoreRequiredScopes = commandLineOptions.IgnoreRequiredScopes
                    });

                PrintResultsToConsole(method, account, results, commandLineOptions);
                await TestReport.LogMethodTestResults(method, account, results);
                docSetResults.RecordResults(results, commandLineOptions);
                
                if (concurrentTasks == 1)
                {
                    AddPause(commandLineOptions);
                }
            });

            if (commandLineOptions.IgnoreWarnings || commandLineOptions.SilenceWarnings)
            {
                // Remove the warning flag from the outcomes
                docSetResults.ConvertWarningsToSuccess();
            }

            docSetResults.PrintToConsole();

            return docSetResults;
        }

        

        /// <summary>
        /// Parallel enabled for each processor that supports async lambdas. Copied from 
        /// http://blogs.msdn.com/b/pfxteam/archive/2012/03/05/10278165.aspx
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">Collection of items to iterate over</param>
        /// <param name="dop">Degree of parallelism to execute.</param>
        /// <param name="body">Lambda expression executed for each operation</param>
        /// <returns></returns>
        public static Task ForEachAsync<T>(IEnumerable<T> source, int dop, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in System.Collections.Concurrent.Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate
                {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);
                }));
        }

        /// <summary>
        /// Write the results of a test to the output console.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="account"></param>
        /// <param name="results"></param>
        /// <param name="options"></param>
        private static void PrintResultsToConsole(MethodDefinition method, IServiceAccount account, ValidationResults output, CheckServiceOptions options)
        {
            // Only allow one thread at a time to write to the console so we don't interleave our results.
            lock (typeof(Program))
            {
                FancyConsole.WriteLine(
                    FancyConsole.ConsoleHeaderColor,
                    "Testing method {0} with account {1}",
                    method.Identifier,
                    account.Name);

                foreach (var scenario in output.Results)
                {
                    if (scenario.Errors.Count > 0)
                    {
                        FancyConsole.WriteLineIndented(
                            "  ",
                            FancyConsole.ConsoleSubheaderColor,
                            "Scenario: {0}",
                            scenario.Name);

                        foreach (var message in scenario.Errors)
                        {
                            if (options.EnableVerboseOutput || message.IsWarningOrError)
                                FancyConsole.WriteLineIndented(
                                    "    ",
                                    FancyConsole.ConsoleDefaultColor,
                                    message.ErrorText);
                            RecordUndocumentedProperties(message);
                        }

                        if (options.SilenceWarnings && scenario.Outcome == ValidationOutcome.Warning)
                        {
                            scenario.Outcome = ValidationOutcome.Passed;
                        }

                        FancyConsole.WriteLineIndented(
                            "    ",
                            scenario.Outcome.ConsoleColor(),
                            "Scenario finished with outcome: {0}. Duration: {1}",
                            scenario.Outcome,
                            scenario.Duration);
                    }
                }

                FancyConsole.WriteLineIndented(
                    "  ",
                    output.OverallOutcome.ConsoleColor(),
                    "Method testing finished with overall outcome: {0}",
                    output.OverallOutcome);
                FancyConsole.WriteLine();
            }
        }




        private static void ConfigureAdditionalHeadersForAccount(CheckServiceOptions options, IServiceAccount account)
        {
            if (account.AdditionalHeaders != null && account.AdditionalHeaders.Length > 0)
            {
                // If the account needs additional headers, merge them in.
                List<string> headers = new List<string>(account.AdditionalHeaders);
                if (options.AdditionalHeaders != null)
                {
                    headers.AddRange(options.AdditionalHeaders.Split('|'));
                }
                ValidationConfig.AdditionalHttpHeaders = headers.ToArray();
            }
            else if (options.AdditionalHeaders != null)
            {
                var headers = options.AdditionalHeaders.Split('|');
                ValidationConfig.AdditionalHttpHeaders = headers.ToArray();
            }
        }

        private static void Exit(bool failure, int? customExitCode = null)
        {
            int exitCode = failure ? ExitCodeFailure : ExitCodeSuccess;
            if (customExitCode.HasValue)
            {
                exitCode = customExitCode.Value;
            }

            if (IgnoreErrors)
            {
                FancyConsole.WriteLine("Ignoring errors and returning a successful exit code.");
                exitCode = ExitCodeSuccess;
            }

#if DEBUG
            Console.WriteLine("Exit code: " + exitCode);
            if (Debugger.IsAttached)
            {
                Console.WriteLine();
                Console.Write("Press any key to exit.");
                Console.ReadKey();
            }
#endif

            Environment.Exit(exitCode);
        }


       

        private static void AddPause(CheckServiceOptions options)
        {
            if (options.PauseBetweenRequests)
            {
                FancyConsole.WriteLine("Press any key to continue");
                Console.ReadKey();
                FancyConsole.WriteLine();
            }
        }

        public static async Task<bool> PublishMetadataAsync(PublishMetadataOptions options)
        {
            DocSet docs = await GetDocSetAsync(options);
            if (null == docs)
                return false;

            var publisher = new Publishing.CSDL.CsdlWriter(docs, options.GetOptions());
            FancyConsole.WriteLine();

            FancyConsole.WriteLine("Publishing metadata...");
            publisher.NewMessage += publisher_NewMessage;

            try
            {
                var outputPath = options.OutputDirectory;
                await publisher.PublishToFolderAsync(outputPath);
                FancyConsole.WriteLine(FancyConsole.ConsoleSuccessColor, "Finished publishing metadata.");
            }
            catch (Exception ex)
            {
                FancyConsole.WriteLine(
                    FancyConsole.ConsoleErrorColor,
                    "An error occured while publishing: {0}",
                    ex.Message);
                FancyConsole.VerboseWriteLine(ex.ToString());
                Exit(failure: true, customExitCode: 99);
                return false;
            }

            return true;

        }

        private static async Task<bool> PublishDocumentationAsync(PublishOptions options)
        {
            var outputPath = options.OutputDirectory;

            FancyConsole.WriteLine("Publishing documentation to {0}", outputPath);

            DocSet docs = await GetDocSetAsync(options);
            if (null == docs)
                return false;

            DocumentPublisher publisher = null;
            switch (options.Format)
            {
                case PublishOptions.PublishFormat.Markdown:
                    publisher = new MarkdownPublisher(docs);
                    break;
                case PublishOptions.PublishFormat.Html:
                    publisher = new DocumentPublisherHtml(docs, options);
                    break;
                case PublishOptions.PublishFormat.Mustache:
                    publisher = new HtmlMustacheWriter(docs, options);
                    break;
                case PublishOptions.PublishFormat.JsonToc:
                    publisher = new HtmlMustacheWriter(docs, options) { TocOnly = true };
                    break;
                case PublishOptions.PublishFormat.Swagger2:
                    publisher = new SwaggerWriter(docs, "https://service.org")  // TODO: Plumb in the base URL.
                    {
                        Title = options.Title,
                        Description = options.Description,
                        Version = options.Version
                    };
                    break;
                case PublishOptions.PublishFormat.Outline:
                    publisher = new OutlinePublisher(docs);
                    break;
                default:
                    FancyConsole.WriteLine(
                        FancyConsole.ConsoleErrorColor,
                        "Unsupported publishing format: {0}",
                        options.Format);
                    return false;
            }

            FancyConsole.WriteLineIndented("  ", "Format: {0}", publisher.GetType().Name);
            publisher.VerboseLogging = options.EnableVerboseOutput;
            FancyConsole.WriteLine();
            
            FancyConsole.WriteLine("Publishing content...");
            publisher.NewMessage += publisher_NewMessage;

            try
            {
                await publisher.PublishToFolderAsync(outputPath);
                FancyConsole.WriteLine(FancyConsole.ConsoleSuccessColor, "Finished publishing documentation to: {0}", outputPath);
            }
            catch (Exception ex)
            {
                FancyConsole.WriteLine(
                    FancyConsole.ConsoleErrorColor,
                    "An error occured while publishing: {0}",
                    ex.Message);
                FancyConsole.VerboseWriteLine(ex.ToString());
                Exit(failure: true, customExitCode: 99);
                return false;
            }

            return true;
        }

        static void publisher_NewMessage(object sender, ValidationMessageEventArgs e)
        {
            var msg = e.Message;
            if (!FancyConsole.WriteVerboseOutput && !msg.IsError && !msg.IsWarning)
                return;

            WriteValidationError("", msg);
        }


        private static async Task<EntityFramework> TryGetMetadataEntityFrameworkAsync(CheckMetadataOptions options)
        {
            if (string.IsNullOrEmpty(options.ServiceMetadataLocation))
            {
                RecordError("No service metadata file location specified.");
                return null;
            }

            FancyConsole.WriteLine(FancyConsole.ConsoleHeaderColor, "Loading service metadata from '{0}'...", options.ServiceMetadataLocation);

            EntityFramework edmx = null;
            try
            {
                Uri metadataUrl;
                if (Uri.IsWellFormedUriString(options.ServiceMetadataLocation, UriKind.Absolute) && Uri.TryCreate(options.ServiceMetadataLocation, UriKind.Absolute, out metadataUrl))
                {
                    edmx = await ODataParser.ParseEntityFrameworkFromUrlAsync(metadataUrl);
                }
                else
                {
                    edmx = await ODataParser.ParseEntityFrameworkFromFileAsync(options.ServiceMetadataLocation);
                }
            }
            catch (Exception ex)
            {
                RecordError("Error parsing service metadata: {0}", ex.Message);
                return null;
            }

            return edmx;
        }

        private static async Task<List<Schema>> TryGetMetadataSchemasAsync(CheckMetadataOptions options)
        {
            EntityFramework edmx = await TryGetMetadataEntityFrameworkAsync(options);

            return edmx.DataServices.Schemas;
        }

        /// <summary>
        /// Validate that the CSDL metadata defined for a service matches the documentation.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task<bool> CheckServiceMetadataAsync(CheckMetadataOptions options)
        {
            List<Schema> schemas = await TryGetMetadataSchemasAsync(options);
            if (null == schemas)
                return false;

            FancyConsole.WriteLine(FancyConsole.ConsoleSuccessColor, "  found {0} schema definitions: {1}", schemas.Count, (from s in schemas select s.Namespace).ComponentsJoinedByString(", "));

            var docSet = await GetDocSetAsync(options);
            if (null == docSet)
                return false;

            const string testname = "validate-service-metadata";
            TestReport.StartTest(testname);

            List<ResourceDefinition> foundResources = ODataParser.GenerateResourcesFromSchemas(schemas);
            CheckResults results = new CheckResults();

            List<ValidationError> collectedErrors = new List<ValidationError>();

            foreach (var resource in foundResources)
            {
                FancyConsole.WriteLine();
                FancyConsole.Write(FancyConsole.ConsoleHeaderColor, "Checking metadata resource: {0}...", resource.Name);

                FancyConsole.VerboseWriteLine();
                FancyConsole.VerboseWriteLine(resource.ExampleText);
                FancyConsole.VerboseWriteLine();

                // Check if this resource exists in the documentation at all
                ResourceDefinition matchingDocumentationResource = null;
                    var docResourceQuery =
                        from r in docSet.Resources
                        where r.Name == resource.Name
                        select r;
                    matchingDocumentationResource = docResourceQuery.FirstOrDefault();
                if (docResourceQuery.Count() > 1)
                {
                    // Log an error about multiple resource definitions
                    FancyConsole.WriteLine("Multiple resource definitions for resource {0} in files:", resource.Name);
                    foreach (var q in docResourceQuery)
                    {
                        FancyConsole.WriteLine(q.SourceFile.DisplayName);
                    }
                }


                ValidationError[] errors;
                if (null == matchingDocumentationResource)
                {
                    // Couldn't find this resource in the documentation!
                    errors = new ValidationError[]
                    {
                        new ValidationError(
                            ValidationErrorCode.ResourceTypeNotFound,
                            null,
                            "Resource {0} is not in the documentation.",
                            resource.Name)
                    };
                }
                else
                {
                    // Verify that this resource matches the documentation
                    docSet.ResourceCollection.ValidateJsonExample(resource.OriginalMetadata, resource.ExampleText, out errors, new ValidationOptions { RelaxedStringValidation = true });
                }
                
                results.IncrementResultCount(errors);
                collectedErrors.AddRange(errors);

                await WriteOutErrorsAndFinishTestAsync(errors, options.SilenceWarnings, successMessage: " passed.", printFailuresOnly: options.PrintFailuresOnly);
            }

            if (options.IgnoreWarnings)
            {
                results.ConvertWarningsToSuccess();
            }

            var output = (from e in collectedErrors select e.ErrorText).ComponentsJoinedByString("\r\n");

            await TestReport.FinishTestAsync(testname, results.WereFailures ? TestOutcome.Failed : TestOutcome.Passed, stdOut:output);

            results.PrintToConsole();
            return !results.WereFailures;
        }

        private static async Task<bool> GenerateDocsAsync(GenerateDocsOptions options)
        {
            EntityFramework ef = await TryGetMetadataEntityFrameworkAsync(options);
            if (null == ef)
                return false;

            FancyConsole.WriteLine(FancyConsole.ConsoleSuccessColor, "  found {0} schema definitions: {1}", ef.DataServices.Schemas.Count, (from s in ef.DataServices.Schemas select s.Namespace).ComponentsJoinedByString(", "));

            DocumentationGenerator docGenerator = new DocumentationGenerator(options.ResourceTemplateFile);
            docGenerator.GenerateDocumentationFromEntityFrameworkAsync(ef, options.DocumentationSetPath);

            return true;
        }
    }

    class ConsoleAppLogger : ILogHelper
    {
        public void RecordError(ValidationError error)
        {
            Program.WriteValidationError(string.Empty, error);
        }
    }

    internal static class OutcomeExtensionMethods
    {

        public static ConsoleColor ConsoleColor(this ValidationOutcome outcome)
        {
            if ((outcome & ValidationOutcome.Error) > 0)
                return FancyConsole.ConsoleErrorColor;
            if ((outcome & ValidationOutcome.Warning) > 0)
                return FancyConsole.ConsoleWarningColor;
            if ((outcome & ValidationOutcome.Passed) > 0)
                return FancyConsole.ConsoleSuccessColor;
            if ((outcome & ValidationOutcome.Skipped) > 0)
                return FancyConsole.ConsoleWarningColor;

            return FancyConsole.ConsoleDefaultColor;

        }

    }

}
