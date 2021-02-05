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

namespace ApiDoctor.ConsoleApp
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using ApiDoctor.DocumentationGeneration;
    using ApiDoctor.Validation.Config;
    using ApiDoctor.Validation.OData.Transformation;
    using AppVeyor;
    using CommandLine;
    using Newtonsoft.Json;
    using Publishing.Html;
    using Publishing.Swagger;
    using Validation;
    using Validation.Error;
    using Validation.Http;
    using Validation.Json;
    using Validation.OData;
    using Validation.Params;
    using Validation.Writers;

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

            FancyConsole.WriteLine(ConsoleColor.Green, "API Doctor, version {0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            FancyConsole.WriteLine();
            if (args.Length > 0)
                FancyConsole.WriteLine("Command line: " + args.ComponentsJoinedByString(" "));

            try
            {
                Parser.Default.ParseArguments<PrintOptions, CheckLinkOptions, BasicCheckOptions, CheckAllLinkOptions, CheckServiceOptions, PublishOptions, PublishMetadataOptions, CheckMetadataOptions, FixDocsOptions, GenerateDocsOptions, GenerateSnippetsOptions, AboutOptions>(args)
                        .WithParsed<BaseOptions>((options) =>
                        {
                            IgnoreErrors = options.IgnoreErrors;
#if DEBUG
                            if (options.AttachDebugger == 1)
                            {
                                Debugger.Launch();
                            }
#endif

                            SetStateFromOptions(options);
                        })    
                        .MapResult(
                            (PrintOptions options) => RunInvokedMethodAsync(options),
                            (CheckLinkOptions options) => RunInvokedMethodAsync(options),
                            (BasicCheckOptions options) => RunInvokedMethodAsync(options),
                            (CheckAllLinkOptions options) => RunInvokedMethodAsync(options),
                            (CheckServiceOptions options) => RunInvokedMethodAsync(options),
                            (PublishOptions options) => RunInvokedMethodAsync(options),
                            (PublishMetadataOptions options) => RunInvokedMethodAsync(options),
                            (CheckMetadataOptions options) => RunInvokedMethodAsync(options),
                            (FixDocsOptions options) => RunInvokedMethodAsync(options),
                            (GenerateDocsOptions options) => RunInvokedMethodAsync(options),
                            (AboutOptions options) => RunInvokedMethodAsync(options),
                            (GenerateSnippetsOptions options) => RunInvokedMethodAsync(options),
                            (errors) =>
                                    {
                                        FancyConsole.WriteLine(ConsoleColor.Red, "COMMAND LINE PARSE ERROR");
                                        Exit(failure: true);
                                        return Task.FromResult(default(BaseOptions));
                                    })
                        .Wait();
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
                if ((!string.IsNullOrEmpty(checkOptions.FilesChangedFromOriginalBranch)) || (verbOptions is GenerateSnippetsOptions))
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
        private static async Task RunInvokedMethodAsync(BaseOptions options)
        {
            var issues = new IssueLogger()
            {
#if DEBUG
                DebugLine = options.AttachDebugger,
#endif
            };

            if (!options.HasRequiredProperties(out var missingProps))
            {
                issues.Error(ValidationErrorCode.MissingRequiredArguments, $"Command line is missing required arguments: {missingProps.ComponentsJoinedByString(", ")}");
                await WriteOutErrorsAndFinishTestAsync(issues, options.SilenceWarnings, printFailuresOnly: options.PrintFailuresOnly);
                Exit(failure: true);
            }

            LoadCurrentConfiguration(options as DocSetOptions);

            bool returnSuccess = true;

            switch (options)
            {
                case PrintOptions o:
                    await PrintDocInformationAsync(o, issues);
                    break;
                case CheckAllLinkOptions o:
                    returnSuccess = await CheckDocsAllAsync(o, issues);
                    break;
                case CheckServiceOptions o:
                    returnSuccess = await CheckServiceAsync(o, issues);
                    break;
                case GenerateDocsOptions o:
                    returnSuccess = await GenerateDocsAsync(o);
                    break;
                case FixDocsOptions o:
                    returnSuccess = await FixDocsAsync(o, issues);
                    break;
                case CheckLinkOptions o:
                    returnSuccess = await CheckLinksAsync(o, issues);
                    break;
                case GenerateSnippetsOptions o:
                    returnSuccess = await GenerateSnippetsAsync(o, issues);
                    break;
                case BasicCheckOptions o:
                    returnSuccess = await CheckDocsAsync(o, issues);
                    break;
                case PublishOptions o:
                    returnSuccess = await PublishDocumentationAsync(o, issues);
                    break;
                case PublishMetadataOptions o:
                    returnSuccess = await PublishMetadataAsync(o, issues);
                    break;
                case CheckMetadataOptions o:
                    await CheckServiceMetadataAsync(o, issues);
                    break;
                case AboutOptions o:
                    PrintAboutMessage();
                    Exit(failure: false);
                    break;
            }

            WriteMessages(
                issues,
                options.IgnoreWarnings,
                indent: "  ",
                printUnusedSuppressions: options is CheckAllLinkOptions);

            if (returnSuccess)
            {
                if ((issues.Errors.Any() && !options.IgnoreErrors) ||
                    (issues.Warnings.Any() && !options.IgnoreWarnings))
                {
                    returnSuccess = false;
                }
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
        private static async Task<bool> CheckDocsAllAsync(CheckLinkOptions options, IssueLogger issues)
        {
            var docset = await GetDocSetAsync(options, issues);

            if (null == docset)
                return false;

            var checkLinksResult = await CheckLinksAsync(options, issues, docset);
            var checkDocsResults = await CheckDocsAsync(options, issues, docset);

            var publishOptions = new PublishMetadataOptions();
            var publishResult = await PublishMetadataAsync(publishOptions, issues, docset);

            return checkLinksResult && checkDocsResults && publishResult;
        }

        private static void PrintAboutMessage()
        {
            FancyConsole.WriteLine();
            FancyConsole.WriteLine(ConsoleColor.Cyan, "apidoc.exe - API Documentation Test Tool");
            FancyConsole.WriteLine(ConsoleColor.Cyan, "Copyright (c) 2018 Microsoft Corporation");
            FancyConsole.WriteLine();
            FancyConsole.WriteLine(ConsoleColor.Cyan, "For more information see http://github.com/onedrive/apidoctor");
            FancyConsole.WriteLine();
        }


        /// <summary>
        /// Create and return a document set based on input options
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static Task<DocSet> GetDocSetAsync(DocSetOptions options, IssueLogger issues)
        {
            FancyConsole.VerboseWriteLine("Opening documentation from {0}", options.DocumentationSetPath);
            DocSet set = null;
            try
            {
                set = new DocSet(options.DocumentationSetPath, writeFixesBackToDisk: options is FixDocsOptions);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, ex.Message);
                Exit(failure: true);
                return Task.FromResult<DocSet>(null);
            }

            FancyConsole.VerboseWriteLine("Scanning documentation files...");

            string tagsToInclude;
            if (null == options.PageParameterDict || !options.PageParameterDict.TryGetValue("tags", out tagsToInclude))
            {
                tagsToInclude = String.Empty;
            }

            DateTimeOffset start = DateTimeOffset.Now;
            set.ScanDocumentation(tagsToInclude, issues);
            DateTimeOffset end = DateTimeOffset.Now;
            TimeSpan duration = end.Subtract(start);

            FancyConsole.WriteLine($"Took {duration.TotalSeconds} to parse {set.Files.Length} source files.");
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
        private static async Task PrintDocInformationAsync(PrintOptions options, IssueLogger issues, DocSet docset = null)
        {
            docset = docset ?? await GetDocSetAsync(options, issues);
            if (null == docset)
            {
                return;
            }

            if (options.PrintFiles)
            {
                await PrintFilesAsync(options, docset, issues);
            }
            if (options.PrintResources)
            {
                await PrintResourcesAsync(options, docset, issues);
            }
            if (options.PrintMethods)
            {
                await PrintMethodsAsync(options, docset, issues);
            }
            if (options.PrintAccounts)
            {
                PrintAccountsAsync(options, docset);
            }
        }

        #region Print verb commands
        /// <summary>
        /// Prints a list of the documentation files in a docset to the console.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="docset"></param>
        /// <returns></returns>
        private static async Task PrintFilesAsync(DocSetOptions options, DocSet docset, IssueLogger issues)
        {
            docset = docset ?? await GetDocSetAsync(options, issues);
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
        private static async Task PrintResourcesAsync(DocSetOptions options, DocSet docset, IssueLogger issues)
        {
            docset = docset ?? await GetDocSetAsync(options, issues);
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
        private static async Task PrintMethodsAsync(DocSetOptions options, DocSet docset, IssueLogger issues)
        {
            docset = docset ?? await GetDocSetAsync(options, issues);
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

        private static void PrintAccountsAsync(PrintOptions options, DocSet docset)
        {
            var accounts = Program.CurrentConfiguration.Accounts;
            foreach (var account in accounts)
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
        private static async Task<bool> CheckLinksAsync(CheckLinkOptions options, IssueLogger issues, DocSet docs = null)
        {
            const string testName = "Check-links";
            var docset = docs ?? await GetDocSetAsync(options, issues);

            if (null == docset)
                return false;


            string[] interestingFiles = null;
            if (!string.IsNullOrEmpty(options.FilesChangedFromOriginalBranch))
            {
                GitHelper helper = new GitHelper(options.GitExecutablePath, options.DocumentationSetPath);
                interestingFiles = helper.FilesChangedFromBranch(options.FilesChangedFromOriginalBranch);
            }

            TestReport.StartTest(testName);

            docset.ValidateLinks(options.EnableVerboseOutput, interestingFiles, issues, options.RequireFilenameCaseMatch, options.IncludeOrphanPageWarning);

            foreach (var error in issues.Issues)
            {
                MessageCategory category;
                if (error.IsWarning)
                {
                    category = MessageCategory.Warning;
                }
                else if (error.IsError)
                {
                    category = MessageCategory.Error;
                }
                else
                {
                    if (!FancyConsole.WriteVerboseOutput)
                    {
                        continue;
                    }

                    category = MessageCategory.Information;
                }

                string message = string.Format("{1}: {0}", error.Message.FirstLineOnly(), error.Code);
                await TestReport.LogMessageAsync(message, category);
            }

            return await WriteOutErrorsAndFinishTestAsync(issues, options.SilenceWarnings, successMessage: "No link errors detected.", testName: testName, printFailuresOnly: options.PrintFailuresOnly);
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
        private static async Task<bool> CheckDocsAsync(BasicCheckOptions options, IssueLogger issues, DocSet docs = null)
        {
            var docset = docs ?? await GetDocSetAsync(options, issues);
            if (null == docset)
                return false;

            FancyConsole.WriteLine();

            var resultStructure = await CheckDocStructure(options, docset, issues);

            var resultMethods = await CheckMethodsAsync(options, docset, issues);
            CheckResults resultExamples = new CheckResults();
            if (string.IsNullOrEmpty(options.MethodName))
            {
                resultExamples = await CheckExamplesAsync(options, docset, issues);
            }

            var combinedResults = resultMethods + resultExamples + resultStructure;

            if (options.IgnoreWarnings)
            {
                combinedResults.ConvertWarningsToSuccess();
            }

            combinedResults.PrintToConsole();

            return combinedResults.FailureCount == 0;
        }

        private static async Task<CheckResults> CheckDocStructure(BasicCheckOptions options, DocSet docset, IssueLogger issues)
        {
            var results = new CheckResults();

            TestReport.StartTest("Verify document structure");
            foreach (var doc in docset.Files)
            {
                doc.CheckDocumentStructure(issues.For(doc.DisplayName));
            }

            await WriteOutErrorsAndFinishTestAsync(issues, options.SilenceWarnings, "    ", "Passed.", false, "Verify document structure", "Warnings detected", "Errors detected", printFailuresOnly: options.PrintFailuresOnly);
            results.IncrementResultCount(issues.Issues);

            return results;
        }

        /// <summary>
        /// Perform an internal consistency check on the examples defined in the documentation. Prints
        /// the results of the tests to the console.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="docset"></param>
        /// <returns></returns>
        private static async Task<CheckResults> CheckExamplesAsync(BasicCheckOptions options, DocSet docset, IssueLogger issues)
        {
            var results = new CheckResults();

            foreach (var doc in docset.Files)
            {
                if (doc.Examples.Length == 0)
                {
                    continue;
                }

                var exampleIssues = issues.For(doc.DisplayName);
                FancyConsole.WriteLine(FancyConsole.ConsoleHeaderColor, "Checking examples in \"{0}\"...", doc.DisplayName);

                foreach (var example in doc.Examples)
                {
                    if (example.Metadata == null)
                        continue;
                    if (example.Language != CodeLanguage.Json)
                        continue;

                    var testName = string.Format("check-example: {0}", example.Metadata.MethodName?.FirstOrDefault(), example.Metadata.ResourceType);
                    TestReport.StartTest(testName, doc.DisplayName);

                    docset.ResourceCollection.ValidateJsonExample(example.Metadata, example.SourceExample, exampleIssues, new ValidationOptions { RelaxedStringValidation = options.RelaxStringTypeValidation ?? true });

                    await WriteOutErrorsAndFinishTestAsync(exampleIssues, options.SilenceWarnings, "   ", "Passed.", false, testName, "Warnings detected", "Errors detected", printFailuresOnly: options.PrintFailuresOnly);
                    results.IncrementResultCount(exampleIssues.Issues);
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
        private static async Task<CheckResults> CheckMethodsAsync(BasicCheckOptions options, DocSet docset, IssueLogger issues)
        {
            MethodDefinition[] methods = FindTestMethods(options, docset);
            CheckResults results = new CheckResults();

            foreach (var method in methods)
            {
                var methodIssues = issues.For(method.Identifier);
                var testName = "API Request: " + method.Identifier;

                TestReport.StartTest(testName, method.SourceFile.DisplayName, skipPrintingHeader: options.PrintFailuresOnly);

                if (method.ExpectedResponse == null)
                {
                    methodIssues.Error(ValidationErrorCode.UnpairedRequest, "Unable to locate the corresponding response for this method. Missing or incorrect code block annotation.");
                    await TestReport.FinishTestAsync(testName, TestOutcome.Failed, "No response was paired with this request.", printFailuresOnly: options.PrintFailuresOnly);
                    results.FailureCount++;
                    continue;
                }

                HttpResponse expectedResponse;
                HttpParser.TryParseHttpResponse(method.ExpectedResponse, out expectedResponse, methodIssues);
                if (expectedResponse != null)
                {
                    method.ValidateResponse(expectedResponse, null, null, methodIssues, new ValidationOptions { RelaxedStringValidation = options.RelaxStringTypeValidation ?? true });
                }
                await WriteOutErrorsAndFinishTestAsync(methodIssues, options.SilenceWarnings, "   ", "Passed.", false, testName, "Warnings detected", "Errors detected", printFailuresOnly: options.PrintFailuresOnly);
                results.IncrementResultCount(methodIssues.Issues);
            }

            return results;
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
        private static async Task<bool> WriteOutErrorsAndFinishTestAsync(IssueLogger issues, bool silenceWarnings, string indent = "", string successMessage = null, bool endLineBeforeWriting = false, string testName = null, string warningsMessage = null, string errorsMessage = null, bool printFailuresOnly = false)
        {
            string writeMessageHeader = null;
            if (printFailuresOnly)
            {
                writeMessageHeader = $"Test {testName} results:";
            }

            WriteMessages(issues, silenceWarnings, indent, endLineBeforeWriting, beforeWriteHeader: writeMessageHeader);

            TestOutcome outcome = TestOutcome.None;
            string outputMessage = null;

            var errorMessages = issues.Issues.Where(x => x.IsError);
            var warningMessages = issues.Issues.Where(x => x.IsWarning);

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
                var errorMessage = (from e in issues.Issues select e.ErrorText).ComponentsJoinedByString("\r\n");
                await TestReport.FinishTestAsync(testName, outcome, outputMessage, stdOut: errorMessage, printFailuresOnly: printFailuresOnly);
            }

            return outcome == TestOutcome.Passed;
        }

        /// <summary>
        /// Prints ValidationError messages to the console, optionally excluding warnings and messages.
        /// </summary>
        private static void WriteMessages(
            IssueLogger issues,
            bool errorsOnly = false,
            string indent = "",
            bool endLineBeforeWriting = false,
            string beforeWriteHeader = null,
            bool printUnusedSuppressions = false)
        {
            bool writtenHeader = false;
            foreach (var error in issues.Issues.OrderBy(err => err.IsWarningOrError).ThenBy(err => err.IsError).ThenBy(err => err.Source))
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

            var usedSuppressions = issues.UsedSuppressions;
            if (usedSuppressions.Count > 0)
            {
                FancyConsole.WriteLine(ConsoleColor.DarkYellow, $"{usedSuppressions.Count} issues were suppressed with manual overrides in the docs.");
            }

            if (printUnusedSuppressions)
            {
                var unusedSuppressions = issues.UnusedSuppressions;
                if (unusedSuppressions.Count > 0)
                {
                    FancyConsole.WriteLine(ConsoleColor.Cyan, "Validation suppressions found that weren't used. Please remove:");
                    foreach (var sup in unusedSuppressions)
                    {
                        FancyConsole.WriteLineIndented("  ", ConsoleColor.Cyan, sup);
                    }
                }
            }

            int warningCount = issues.Issues.Count(issue => issue.IsWarning);
            int errorCount = issues.Issues.Count(iss => iss.IsError);

            var color = ConsoleColor.Green;
            var status = "PASSED";
            if (warningCount > 0)
            {
                color = ConsoleColor.Yellow;
                status = "FAILED with warnings";
                FancyConsole.WriteLine(color, $"{warningCount} warnings");
            }

            if (errorCount > 0)
            {
                color = ConsoleColor.Red;
                status = "FAILED with errors";
                FancyConsole.WriteLine(color, $"{errorCount} errors");
            }

            FancyConsole.WriteLine(color, status);
        }

        private static void RecordUndocumentedProperties(ValidationError error)
        {
            if (error is UndocumentedPropertyWarning)
            {
                DiscoveredUndocumentedProperties.Add((UndocumentedPropertyWarning)error);
            }
            else if (error.InnerErrors != null && error.InnerErrors.Any())
            {
                foreach (var innerError in error.InnerErrors)
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
            FancyConsole.WriteLine();
        }

        /// <summary>
        /// Executes the remote service tests defined in the documentation. This is similar to CheckDocs, expect
        /// that the actual requests come from the service instead of the documentation. Prints the errors to 
        /// the console.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task<bool> CheckServiceAsync(CheckServiceOptions options, IssueLogger issues)
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

            var docset = await GetDocSetAsync(options, issues);
            if (null == docset)
            {
                return false;
            }

            FancyConsole.WriteLine();

            if (!string.IsNullOrEmpty(options.ODataMetadataLevel))
            {
                ValidationConfig.ODataMetadataLevel = options.ODataMetadataLevel;
            }

            if (options.FoundAccounts == null || !options.FoundAccounts.Any(x => x.Enabled))
            {
                RecordError("No account was found. Cannot connect to the service.");
                return false;
            }

            IServiceAccount secondaryAccount = null;
            if (options.SecondaryAccountName != null)
            {
                var secondaryAccounts = options.FoundAccounts.Where(
                    x => options.SecondaryAccountName.Equals(x.Name));
                secondaryAccount = secondaryAccounts.FirstOrDefault();
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
                // if the service root URL is also provided, override the account URL
                if (!string.IsNullOrEmpty(options.ServiceRootUrl))
                {
                    account.OverrideBaseUrl(options.ServiceRootUrl);
                }

                var accountResults = await CheckMethodsForAccountAsync(options, account, secondaryAccount, methods, docset);
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
        private static async Task<CheckResults> CheckMethodsForAccountAsync(CheckServiceOptions commandLineOptions, IServiceAccount primaryAccount, IServiceAccount secondaryAccount, MethodDefinition[] methods, DocSet docset)
        {
            ConfigureAdditionalHeadersForAccount(commandLineOptions, primaryAccount);

            FancyConsole.WriteLine(FancyConsole.ConsoleHeaderColor, "Testing account: {0}", primaryAccount.Name);
            FancyConsole.WriteLine(FancyConsole.ConsoleCodeColor, "Preparing authentication for requests...", primaryAccount.Name);

            try
            {
                await primaryAccount.PrepareForRequestAsync();
            }
            catch (Exception ex)
            {
                RecordError(ex.Message);
                return null;
            }

            if (secondaryAccount != null)
            {
                ConfigureAdditionalHeadersForAccount(commandLineOptions, secondaryAccount);

                try
                {
                    await secondaryAccount.PrepareForRequestAsync();
                }
                catch (Exception ex)
                {
                    RecordError(ex.Message);
                    return null;
                }
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
                ValidationResults results = await method.ValidateServiceResponseAsync(scenarios, primaryAccount, secondaryAccount,
                    new ValidationOptions
                    {
                        RelaxedStringValidation = commandLineOptions.RelaxStringTypeValidation ?? true,
                        IgnoreRequiredScopes = commandLineOptions.IgnoreRequiredScopes
                    });

                PrintResultsToConsole(method, primaryAccount, results, commandLineOptions);
                await TestReport.LogMethodTestResults(method, primaryAccount, results);
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

        public static async Task<bool> PublishMetadataAsync(PublishMetadataOptions options, IssueLogger issues, DocSet docSet = null)
        {
            var docs = docSet ?? await GetDocSetAsync(options, issues);
            if (null == docs)
            {
                return false;
            }

            var publisher = new Publishing.CSDL.CsdlWriter(docs, options.GetOptions());
            FancyConsole.WriteLine();

            FancyConsole.WriteLine("Publishing metadata...");
            publisher.NewMessage += publisher_NewMessage;

            try
            {
                var outputPath = options.OutputDirectory;
                await publisher.PublishToFolderAsync(outputPath, issues);
                FancyConsole.WriteLine(FancyConsole.ConsoleSuccessColor, "Finished publishing metadata.");
            }
            catch (Exception ex)
            {
                FancyConsole.WriteLine(
                    FancyConsole.ConsoleErrorColor,
                    "An error occured while publishing: {0}",
                    ex.ToString());
                Exit(failure: true, customExitCode: 99);
                return false;
            }

            if (!string.IsNullOrEmpty(options.CompareToMetadataPath))
            {
                SchemaConfigFile[] schemaConfigs = DocSet.TryLoadConfigurationFiles<SchemaConfigFile>(options.DocumentationSetPath);
                var schemaConfig = schemaConfigs.FirstOrDefault();
                SchemaDiffConfig config = null;
                if (schemaConfig?.SchemaDiffConfig != null)
                {
                    Console.WriteLine($"Using schemadiff config file: {schemaConfig.SourcePath}");
                    config = schemaConfig.SchemaDiffConfig;
                }

                var sorter = new Publishing.CSDL.XmlSorter(config) { KeepUnrecognizedObjects = options.KeepUnrecognizedObjects };
                sorter.Sort(Path.Combine(options.OutputDirectory, "metadata.xml"), options.CompareToMetadataPath);
            }

            return true;
        }

        private static async Task<bool> PublishDocumentationAsync(PublishOptions options, IssueLogger issues)
        {
            var outputPath = options.OutputDirectory;

            FancyConsole.WriteLine("Publishing documentation to {0}", outputPath);

            DocSet docs = await GetDocSetAsync(options, issues);
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
                await publisher.PublishToFolderAsync(outputPath, issues);
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

        private static async Task<bool> FixDocsAsync(FixDocsOptions options, IssueLogger issues)
        {
            var originalOut = Console.Out;
            Console.SetOut(new System.IO.StringWriter());

            // first read the reference metadata
            var inputSchemas = await TryGetMetadataSchemasAsync(options);
            if (inputSchemas == null)
            {
                return false;
            }

            // next read the doc set
            var docSetIssues = new IssueLogger() { DebugLine = issues.DebugLine };
            var docs = await GetDocSetAsync(options, docSetIssues);
            if (docs == null)
            {
                return false;
            }

            var csdlOptions = new PublishMetadataOptions();
            var publisher = new Publishing.CSDL.CsdlWriter(docs, csdlOptions.GetOptions());
            var edmx = publisher.CreateEntityFrameworkFromDocs(docSetIssues);

            Console.SetOut(originalOut);
            Console.WriteLine("checking for issues...");
            foreach (var inputSchema in inputSchemas)
            {
                var docSchema = edmx.DataServices?.Schemas?.FirstOrDefault(s => s.Namespace == inputSchema.Namespace);
                if (docSchema != null)
                {
                    // remove any superfluous 'keyProperty' declarations.
                    // this can happen when copy/pasting from an entity declaration, where the key property
                    // is inherited from a base type and not shown in the current type
                    foreach (var resource in docs.Resources)
                    {
                        if (!string.IsNullOrEmpty(resource.OriginalMetadata.KeyPropertyName) &&
                            !resource.Parameters.Any(p => p.Name == resource.KeyPropertyName))
                        {
                            if (resource.ResolvedBaseTypeReference != null &&
                                string.Equals(resource.OriginalMetadata.KeyPropertyName, resource.ResolvedBaseTypeReference.ExplicitOrInheritedKeyPropertyName))
                            {
                                if (!options.DryRun)
                                {
                                    resource.OriginalMetadata.KeyPropertyName = null;
                                    resource.OriginalMetadata.PatchSourceFile();
                                    Console.WriteLine($"Removed keyProperty from {resource.Name} because it was defined by an ancestor.");
                                }
                                else
                                {
                                    Console.WriteLine($"Want to remove keyProperty from {resource.Name} because it's defined by an ancestor.");
                                }
                            }

                            if (resource.BaseType == null)
                            {
                                if (!options.DryRun)
                                {
                                    resource.OriginalMetadata.KeyPropertyName = null;
                                    resource.OriginalMetadata.PatchSourceFile();
                                    Console.WriteLine($"Removed keyProperty from {resource.Name} because it's probably a complex type.");
                                }
                                else
                                {
                                    Console.WriteLine($"Want to remove keyProperty from {resource.Name} because it's probably a complex type.");
                                }
                            }
                        }
                    }

                    foreach (var inputEnum in inputSchema.Enumerations)
                    {
                        if (!docs.Enums.Any(e => e.TypeName.TypeOnly() == inputEnum.Name.TypeOnly()))
                        {
                            // found an enum that wasn't in the docs.
                            // see if we can find the resource it belongs to and stick a definition table in.
                            var enumMembers = inputEnum.Members.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);

                            foreach (var resource in docs.Resources)
                            {
                                if (options.Matches.Count > 0 && !options.Matches.Contains(resource.Name.TypeOnly()))
                                {
                                    continue;
                                }

                                bool found = false;
                                string tableDescriptionToAlter = null;
                                foreach (var param in resource.Parameters.Where(p => p.Type?.Type == SimpleDataType.String))
                                {
                                    if (param.Description != null &&
                                        (param.Description.IContains("possible values") ||
                                         param.Description.IContains("one of")))
                                    {
                                        var tokens = param.Description.TokenizedWords();
                                        if (tokens.Count(t => enumMembers.ContainsKey(t)) >= 3)
                                        {
                                            found = true;
                                            tableDescriptionToAlter = param.Description;
                                            break;
                                        }
                                    }

                                    if (param.PossibleEnumValues().Count(v => enumMembers.ContainsKey(v)) >= 2)
                                    {
                                        found = true;
                                        break;
                                    }
                                }

                                if (found)
                                {
                                    // we want to stick this after #properties but before the next one.
                                    int propertiesLineNumber = 0;
                                    int propertiesHNumber = 0;
                                    int nextWhiteSpaceLineNumber = 0;
                                    var lines = File.ReadAllLines(resource.SourceFile.FullPath);
                                    for (int i = 0; i < lines.Length; i++)
                                    {
                                        var line = lines[i];
                                        if (propertiesLineNumber == 0 &&
                                            line.StartsWith("#") &&
                                            line.Contains("Properties"))
                                        {
                                            propertiesLineNumber = i;
                                            propertiesHNumber = line.Count(c => c == '#');
                                            continue;
                                        }

                                        // ideally we wouldn't have to re-tokenize, but the place we did it earlier
                                        // had markdown syntax stripped out, and we don't have that luxury here...
                                        if (line.TokenizedWords().Count(t => enumMembers.ContainsKey(t)) >= 3)
                                        {
                                            // | paramName | string | description text ending with enum values
                                            var split = line.Split('|');
                                            if (split.Length > 3 && split[2].IContains("string"))
                                            {
                                                split[2] = split[2].IReplace("string", inputEnum.Name);
                                                lines[i] = string.Join("|", split);
                                            }
                                        }

                                        if (propertiesLineNumber > 0 && string.IsNullOrWhiteSpace(line))
                                        {
                                            nextWhiteSpaceLineNumber = i;
                                            break;
                                        }
                                    }

                                    if (propertiesLineNumber > 0)
                                    {
                                        // produce the table
                                        StringBuilder table = new StringBuilder($"{new string('#', propertiesHNumber + 1)} {inputEnum.Name} values\r\n\r\n");
                                        table.AppendLine("| Value\r\n|:-------------------------");
                                        foreach (var member in inputEnum.Members)
                                        {
                                            table.AppendLine($"| {member.Name}");
                                        }
                                        table.AppendLine();

                                        if (nextWhiteSpaceLineNumber == 0)
                                        {
                                            nextWhiteSpaceLineNumber = lines.Length - 1;
                                        }

                                        var final = FileSplicer(lines, nextWhiteSpaceLineNumber, table.ToString());

                                        if (options.DryRun)
                                        {
                                            Console.WriteLine($"Want to splice into L{nextWhiteSpaceLineNumber + 1} of {resource.SourceFile.DisplayName}\r\n{table}");
                                        }
                                        else
                                        {
                                            File.WriteAllLines(resource.SourceFile.FullPath, final);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"WARNING: Couldn't find where to insert enum table for {inputEnum.Name}");
                                    }
                                }
                            }
                        }
                    }

                    foreach (var inputComplex in inputSchema.ComplexTypes.Concat(inputSchema.EntityTypes))
                    {
                        if (options.Matches.Count > 0 && !options.Matches.Contains(inputComplex.Name))
                        {
                            continue;
                        }

                        var docComplex =
                            docSchema.ComplexTypes?.FirstOrDefault(c => c.Name.IEquals(inputComplex.Name)) ??
                            docSchema.EntityTypes?.FirstOrDefault(c => c.Name.IEquals(inputComplex.Name));
                        if (docComplex != null)
                        {
                            List<Action<CodeBlockAnnotation>> fixes = new List<Action<CodeBlockAnnotation>>();

                            if (inputComplex.BaseType != docComplex.BaseType)
                            {
                                Console.WriteLine($"Mismatched BaseTypes: '{docComplex.Name}:{docComplex.BaseType}' vs '{inputComplex.Name}:{inputComplex.BaseType}'.");
                                fixes.Add(code => code.BaseType = inputComplex.BaseType);
                            }

                            if (inputComplex.Abstract != docComplex.Abstract)
                            {
                                Console.WriteLine($"Mismatched Abstract: '{docComplex.Name}:{docComplex.Abstract}' vs '{inputComplex.Name}:{inputComplex.Abstract}'.");
                                fixes.Add(code => code.Abstract = inputComplex.Abstract);
                            }

                            if (inputComplex.OpenType != docComplex.OpenType)
                            {
                                Console.WriteLine($"Mismatched OpenType: '{docComplex.Name}:{docComplex.OpenType}' vs '{inputComplex.Name}:{inputComplex.OpenType}'.");
                                fixes.Add(code => code.IsOpenType = inputComplex.OpenType);
                            }

                            var inputEntity = inputComplex as EntityType;
                            var docEntity = docComplex as EntityType;
                            if (docEntity != null && inputEntity != null)
                            {
                                if (docEntity.Key?.PropertyRef?.Name != inputEntity.Key?.PropertyRef?.Name)
                                {
                                    Console.WriteLine($"Mismatched KeyProperty: '{docComplex.Name}:{docEntity.Key?.PropertyRef?.Name}' vs '{inputComplex.Name}:{inputEntity.Key?.PropertyRef?.Name}'.");
                                    fixes.Add(code => code.KeyPropertyName = inputEntity.Key?.PropertyRef?.Name);
                                }

                                if (!docEntity.HasStream && inputEntity.HasStream)
                                {
                                    Console.WriteLine($"Mismatched IsMediaEntity in {docComplex.Name}.");
                                    fixes.Add(code => code.IsMediaEntity = true);
                                }
                            }

                            if (fixes.Count > 0)
                            {
                                foreach (var resource in docComplex.Contributors)
                                {
                                    foreach (var fix in fixes)
                                    {
                                        fix(resource.OriginalMetadata);
                                        if (!options.DryRun)
                                        {
                                            resource.OriginalMetadata.PatchSourceFile();
                                            Console.WriteLine("\tFixed!");
                                        }
                                    }
                                }
                            }
                        }
                    }

                    issues.Message("Looking for composable functions that couldn't be inferred from the docs...");
                    foreach (var inputFunction in inputSchema.Functions.Where(fn => fn.IsComposable))
                    {
                        var pc = ParameterComparer.Instance;
                        var docFunctions = docSchema.Functions.
                            Where(fn =>
                                fn.Name == inputFunction.Name &&
                                !fn.IsComposable &&
                                fn.Parameters.OrderBy(p => p, pc).SequenceEqual(inputFunction.Parameters.OrderBy(p => p, pc), pc)).
                            ToList();

                        foreach (var docFunction in docFunctions)
                        {
                            var methods = docFunction.SourceMethods as List<MethodDefinition>;
                            if (methods != null)
                            {
                                foreach (var metadata in methods.Select(m => m.RequestMetadata).Where(md => !md.IsComposable))
                                {
                                    metadata.IsComposable = true;

                                    if (options.DryRun)
                                    {
                                        Console.WriteLine("Would fix " + metadata.MethodName.FirstOrDefault());
                                    }
                                    else
                                    {
                                        metadata.PatchSourceFile();
                                        Console.WriteLine("Fixed " + metadata.MethodName.FirstOrDefault());
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }


        public static IEnumerable<string> FileSplicer(string[] original, int offset, string valueToSplice)
        {
            for (int i = 0; i < original.Length; i++)
            {
                yield return original[i];

                if (i == offset)
                {
                    yield return valueToSplice;
                }
            }
        }

        /// <summary>
        /// Validate that the CSDL metadata defined for a service matches the documentation.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task<bool> CheckServiceMetadataAsync(CheckMetadataOptions options, IssueLogger issues)
        {
            List<Schema> schemas = await TryGetMetadataSchemasAsync(options);
            if (null == schemas)
                return false;

            FancyConsole.WriteLine(FancyConsole.ConsoleSuccessColor, "  found {0} schema definitions: {1}", schemas.Count, (from s in schemas select s.Namespace).ComponentsJoinedByString(", "));

            var docSet = await GetDocSetAsync(options, issues);
            if (null == docSet)
                return false;

            MetadataValidationConfigs metadataValidationConfigs = docSet.MetadataValidationConfigs;

            const string testname = "validate-service-metadata";
            TestReport.StartTest(testname);

            IEnumerable<ResourceDefinition> metadataResources = ODataParser.GenerateResourcesFromSchemas(schemas, issues, metadataValidationConfigs);
            CheckResults results = new CheckResults();

            foreach (var resource in metadataResources)
            {
                var resourceIssues = issues.For(resource.Name);
                FancyConsole.WriteLine();
                FancyConsole.Write(FancyConsole.ConsoleHeaderColor, "Checking metadata resource: {0}...", resource.Name);

                FancyConsole.VerboseWriteLine();
                FancyConsole.VerboseWriteLine(resource.ExampleText);
                FancyConsole.VerboseWriteLine();

                // Check if this resource exists in the documentation at all
                if (metadataValidationConfigs?.IgnorableModels?.Contains(resource.Name) == true)
                {
                    continue;
                }

                var modelConfigs = metadataValidationConfigs?.ModelConfigs;
                var documentedResources = docSet.Resources;

                ResourceDefinition resourceDocumentation = GetResoureDocumentation(resource, documentedResources, modelConfigs?.ValidateNamespace);

                if (null == resourceDocumentation)
                {
                    // Couldn't find this resource in the documentation!
                    resourceIssues.Error(
                        ValidationErrorCode.ResourceTypeNotFound,
                        $"Resource {resource.Name} is not in the documentation.");
                }

                else
                {
                    // Verify that this resource matches the documentation
                    docSet.ResourceCollection.ValidateJsonExample(
                        resource.OriginalMetadata,
                        resource.ExampleText,
                        resourceIssues,
                        new ValidationOptions
                        {
                            RelaxedStringValidation = true,
                            IgnorablePropertyTypes = metadataValidationConfigs?.IgnorableModels,
                            AllowTruncatedResponses = modelConfigs?.TruncatedProprtiesValidation ?? false
                        }
                    );
                }

                results.IncrementResultCount(resourceIssues.Issues);

                await WriteOutErrorsAndFinishTestAsync(resourceIssues, options.SilenceWarnings, successMessage: " passed.", printFailuresOnly: options.PrintFailuresOnly);
            }

            if (options.IgnoreWarnings)
            {
                results.ConvertWarningsToSuccess();
            }

            var output = (from e in issues.Issues select e.ErrorText).ComponentsJoinedByString("\r\n");

            await TestReport.FinishTestAsync(testname, results.WereFailures ? TestOutcome.Failed : TestOutcome.Passed, stdOut: output);

            results.PrintToConsole();
            return !results.WereFailures;
        }

        /// <summary>
        /// Generate snippets for the methods present in the documents by querying an existing snippet generation api
        /// </summary>
        /// <param name="options"></param>
        /// <param name="issues"></param>
        /// <param name="docs"></param>
        /// <returns>The success/failure of the task</returns>
        private static async Task<bool> GenerateSnippetsAsync(GenerateSnippetsOptions options, IssueLogger issues , DocSet docs = null)
        {
            if (!File.Exists(options.SnippetGeneratorPath))
            {
                FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, "Error with the provided snippet generator path: " + options.SnippetGeneratorPath);
                return false;
            }

            //we are not out to validate the documents in this context.
            options.IgnoreErrors = options.IgnoreWarnings = true;

            //scan the docset and find the methods present
            var docset = docs ?? await GetDocSetAsync(options, issues);
            if (null == docset)
            {
                return false;
            }
            var methods = FindTestMethods(options, docset);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            FancyConsole.WriteLine(FancyConsole.ConsoleSuccessColor, "Generating snippets from Snippets API..");

            var guid = Guid.NewGuid().ToString();
            var snippetsPath = Path.Combine(Path.GetTempPath(), guid);
            Directory.CreateDirectory(snippetsPath);

            WriteHttpSnippetsIntoFile(snippetsPath, methods, issues);

            if (string.IsNullOrWhiteSpace(options.CustomMetadataPath))
            {
                GenerateSnippets(options.SnippetGeneratorPath, // executable path
                    "--SnippetsPath", snippetsPath, "--Languages", options.Languages); // args
            }
            else
            {
                GenerateSnippets(options.SnippetGeneratorPath, // executable path
                    "--SnippetsPath", snippetsPath, "--Languages", options.Languages, "--CustomMetadataPath", options.CustomMetadataPath); // args
            }

            var languages = options.Languages.Split(',');
            foreach (var method in methods)
            {
                foreach (var lang in languages)
                {
                    string snippetPrefix;
                    try
                    {
                        snippetPrefix = GetSnippetPrefix(method);
                    }
                    catch (ArgumentException)
                    {
                        // we don't want to process snippets that don't belong to a version
                        continue;
                    }

                    var fileName = $"{snippetPrefix}---{lang}";
                    var fileFullPath = Path.Combine(snippetsPath, fileName);
                    FancyConsole.WriteLine(FancyConsole.ConsoleSuccessColor, $"Reading {fileFullPath}");

                    if (File.Exists(fileFullPath))
                    {
                        var codeSnippet = File.ReadAllText(fileFullPath);
                        InjectSnippetIntoFile(method, codeSnippet, lang);
                    }
                    else
                        FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, "Error: file does not exist");
                }
            }

            // clean up
            Directory.Delete(snippetsPath, true /* recursive */);

            return true;
        }

        /// <summary>
        /// Generate snippets using snippet generator command line tool
        /// </summary>
        /// <param name="executablePath">path to snippet generator command line tool</param>
        /// <param name="args">arguments to snippet generator</param>
        private static void GenerateSnippets(string executablePath, params string[] args)
        {
            var startInfo = new ProcessStartInfo(executablePath, string.Join(" ", args))
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            using var process = Process.Start(startInfo);
            using var outputWaitHandle = new AutoResetEvent(false);
            using var errorWaitHandle = new AutoResetEvent(false);
            process.OutputDataReceived += (sender, e) => {
                if (string.IsNullOrEmpty(e.Data))
                {
                    outputWaitHandle.Set();
                }
                else
                {
                    FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, "Error when generating code snippets!!!");
                    FancyConsole.Write(FancyConsole.ConsoleErrorColor, e.Data);
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                {
                    errorWaitHandle.Set();
                }
                else
                {
                    FancyConsole.Write(FancyConsole.ConsoleDefaultColor, e.Data);
                }
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }

        /// <summary>
        /// Gets snippet prefix with method name and version information to prevent collision between different API versions
        /// This method also eliminates http snippets that belong to a particular API version
        /// </summary>
        /// <param name="method">method definition</param>
        /// <returns>prefix representing method name and version</returns>
        private static string GetSnippetPrefix(MethodDefinition method)
        {
            var displayName = method.SourceFile.DisplayName;
            string version;
            if (displayName.Contains("beta"))
            {
                version = "-beta";
            }
            else if (displayName.Contains("v1.0"))
            {
                version = "-v1";
            }
            else
            {
                throw new ArgumentException("trying to parse a snippet which doesn't belong to a particular version", nameof(method));
            }

            var methodName = Regex.Replace(method.Identifier, @"[# .()\\/]", "").Replace("_", "-").ToLower();
            return methodName + version;
        }

        /// <summary>
        /// Writes http snippets to a temp directory so that snippet generation tool can parse them
        /// </summary>
        /// <param name="tempDir">Temporary directory where the http snippets are written</param>
        /// <param name="methods">methods to be written</param>
        /// <param name="issues">logging</param>
        private static void WriteHttpSnippetsIntoFile(string tempDir, MethodDefinition[] methods, IssueLogger issues)
        {
            foreach (var method in methods)
            {
                HttpRequest request;
                string snippetPrefix;
                try
                {
                    snippetPrefix = GetSnippetPrefix(method);
                    request = HttpParser.ParseHttpRequest(method.Request);
                }
                catch (Exception e)
                {
                    Console.WriteLine(method.Identifier);
                    Console.WriteLine(e.Message);
                    continue;
                }

                //cleanup any issues we might have with the url
                request = PreProcessUrlForSnippetGeneration(request, method, issues);

                var fileName = snippetPrefix + "-httpSnippet";
                var fileFullPath = Path.Combine(tempDir, fileName);
                FancyConsole.WriteLine(FancyConsole.ConsoleSuccessColor, $"Writing {fileFullPath}");

                File.WriteAllText(fileFullPath, request.FullHttpText(true));
            }
        }
        /// <summary>
        /// Replaces \ by / to keep output mardown consistent accross generation OSes
        /// </summary>
        /// <param name="original">The original path</param>
        /// <returns>The cleaned up result</returns>
        private static string ReplaceWindowsByLinuxPathSeparators(string original) => original.Replace("\\", "/");

        /// <summary>
        /// Finds the file the request is located and inserts the code snippet into the file.
        /// </summary>
        /// <param name="method">The <see cref="MethodDefinition"/> of the request being generated a snippet for</param>
        /// <param name="codeSnippet">The string of the code snippet</param>
        /// <param name="language">Language of programming to insert snippet into</param>
        private static void InjectSnippetIntoFile(MethodDefinition method, string codeSnippet, string language)
        {
            /* Useful variables */
            var originalFileContents = File.ReadAllLines(method.SourceFile.FullPath);
            var methodString = Regex.Replace(method.Identifier, @"[# .()\\/]", "").Replace("_", "-").ToLower();//cleanup the method name
            var httpRequestString = method.Request.Split(Environment.NewLine.ToCharArray()).First();

            /* Useful file indexes */
            var insertionLine = 0;
            var requestStartLine = 0;
            var parseStatus = "FindIdentifierLine";

            /* Useful File names and data*/
            var relativePathFolder = Path.Combine("includes", "snippets");
            const string includeSdkFileName = "snippets-sdk-documentation-link.md";
            const string firstTabText = "\r\n# [HTTP](#tab/http)";

            var codeFenceString = language.ToLower().Replace("#", "sharp").Replace("objective-c", "objc");
            var relativePathSnippetsFolder = Path.Combine(relativePathFolder, codeFenceString);

            var snippetFileName = methodString + $"-{codeFenceString}-snippets.md";

            var includeText = $"# [{language}](#tab/{codeFenceString})\r\n" +
                              $"[!INCLUDE [sample-code](../{ReplaceWindowsByLinuxPathSeparators(Path.Combine(relativePathSnippetsFolder, snippetFileName))})]\r\n" +
                              $"[!INCLUDE [sdk-documentation](../{ReplaceWindowsByLinuxPathSeparators(Path.Combine(relativePathFolder, includeSdkFileName))})]\r\n";

            const string includeSdkText = "<!-- markdownlint-disable MD041-->\r\n\r\n" +
                                          "> Read the [SDK documentation](https://docs.microsoft.com/graph/sdks/sdks-overview) " +
                                          "for details on how to [add the SDK](https://docs.microsoft.com/graph/sdks/sdk-installation) to your project and " +
                                          "[create an authProvider](https://docs.microsoft.com/graph/sdks/choose-authentication-providers) instance.";

            /*
                Scan through the file to find the right line to inject a snippet.
                We first look for the identifier then the request to save from the case where there are duplicates of the request
            */
            for (var currentIndex = 0; currentIndex < originalFileContents.Length; currentIndex++)
            {
                switch (parseStatus)
                {
                    case "FindIdentifierLine"://look for the identifier of the method
                        if (originalFileContents[currentIndex].Length >= method.Identifier.Length && originalFileContents[currentIndex].Contains(method.Identifier))
                        {
                            parseStatus = "FindRequestLine";
                        }
                        break;
                    case "FindRequestLine"://check if we have found the line with the request with the matching identifier
                        if (originalFileContents[currentIndex].Length >= httpRequestString.Length && originalFileContents[currentIndex].Equals(httpRequestString))
                        {
                            parseStatus = "FindRequestStartLine";
                        }
                        break;
                    case "FindRequestStartLine"://scan back to find the line where we can best place the http tab(start of request).
                        for (var identifierIndex = currentIndex; identifierIndex > 0; identifierIndex--)
                        {
                            if (originalFileContents[identifierIndex].Contains("<!-- {") 
                                || originalFileContents[identifierIndex].Contains("<!--{"))
                            {
                                requestStartLine = identifierIndex;
                                currentIndex--;
                                parseStatus = "FindEndOfCodeBlock";
                                break;
                            }
                            if (originalFileContents[identifierIndex].Contains("```http") 
                                && HttpParser.ParseHttpRequest(method.Request).Method.Equals("GET"))
                            {
                                originalFileContents[identifierIndex] = "```msgraph-interactive";
                            }
                        }
                        break;
                    case "FindEndOfCodeBlock"://Find the end of the code block
                        if (originalFileContents[currentIndex].Trim().Equals("```"))
                        {
                            insertionLine = currentIndex;
                            parseStatus = "FirstTabInsertion";
                        }
                        break;
                    case "FirstTabInsertion"://check if we ever inserted any code snippet tab.
                        if (originalFileContents[currentIndex].Contains("snippets") && originalFileContents[currentIndex].Contains("[sample-code]"))
                        {
                            parseStatus = "FindEndOfTabSection";
                            currentIndex -= 3;//backtrack a few lines so that we can scan the whole tab section
                        }
                        break;
                    case "FindEndOfTabSection"://we have inserted a code snippet tab before so look for end of tab section
                        if (originalFileContents[currentIndex].Contains("---"))
                        {
                            insertionLine = currentIndex - 1;//insert new language just before end of tab area
                            parseStatus = "AdditionalTabInsertion";//exit this parse mode.
                        }
                        if (originalFileContents[currentIndex].Contains($"(#tab/{codeFenceString})"))
                        {
                            originalFileContents[currentIndex] = $"# [{language}](#tab/{codeFenceString})";
                            originalFileContents[currentIndex + 1] = $"[!INCLUDE [sample-code](../{ReplaceWindowsByLinuxPathSeparators(Path.Combine(relativePathSnippetsFolder, snippetFileName))})]";//update include link. Just in case.
                            includeText = "";
                        }
                        break;
                    default:
                        //we've found it nothing to do here
                        break;
                }
            }

            IEnumerable<string> updatedFileContents;
            switch (parseStatus)
            {
                case "FirstTabInsertion":
                {
                    includeText = $"{includeText}\r\n---\r\n";//append end of tab section

                    /* Add the include link at the specified index together with the first tab */
                    updatedFileContents = FileSplicer(originalFileContents, insertionLine, includeText);//inject the include text
                    updatedFileContents = FileSplicer(updatedFileContents.ToArray(), requestStartLine-1, firstTabText);//inject the first tab section

                    /* DUMP THE SDK LINK FILE */
                    var sdkLinkDirectory = Path.Combine(Directory.GetParent(Path.GetDirectoryName(method.SourceFile.FullPath)).FullName, relativePathFolder);
                    Directory.CreateDirectory(sdkLinkDirectory);
                    // only dump a new file when it does not exist.
                    var fullFileName = Path.Combine(sdkLinkDirectory, includeSdkFileName);
                    if (!File.Exists(fullFileName))
                    {
                        File.WriteAllText(fullFileName, includeSdkText);
                    }
                    break;
                }
                case "AdditionalTabInsertion":
                    /* Add the include link at the specified index */
                    updatedFileContents = string.IsNullOrEmpty(includeText) ? originalFileContents : FileSplicer(originalFileContents, insertionLine, includeText);
                    break;
                default:
                    //Just return and do not insert a snippet if we can't find an proper place to inject the snippet
                    return;
            }

            /* DUMP THE INJECTIONS*/
            File.WriteAllLines(method.SourceFile.FullPath, updatedFileContents);

            /* DUMP THE CODE SNIPPET FILE */
            var snippetFileContents = "---\r\ndescription: \"Automatically generated file. DO NOT MODIFY\"\r\n---\r\n" +    //header
                                      $"\r\n```{codeFenceString}\r\n" +     //code fence
                                      $"\r\n{codeSnippet}\r\n" +            //generated snippet
                                      "\r\n```";                            //closing fence
            var directory = Path.Combine(Directory.GetParent(Path.GetDirectoryName(method.SourceFile.FullPath)).FullName, relativePathSnippetsFolder);
            Directory.CreateDirectory(directory);//Make sure snippet file directory exists
            var mdFilePath = Path.Combine(directory, snippetFileName);
            FancyConsole.WriteLine(FancyConsole.ConsoleSuccessColor, $"Writing snippet to {mdFilePath}");
            File.WriteAllText(mdFilePath, snippetFileContents);//write snippet to file

        }

        /// <summary>
        /// Finds the file the request is located and inserts the code snippet into the file.
        /// </summary>
        /// <param name="request">Request with url to be verified or corrected</param>
        /// <param name="method">The <see cref="MethodDefinition"/> of the request being generated a snippet for</param>
        /// <param name="issues">Issue logger to record any issues</param>
        private static HttpRequest PreProcessUrlForSnippetGeneration(HttpRequest request ,MethodDefinition method,IssueLogger issues)
        {  
            //Version 1.1 of HTTP protocol MUST specify the host header
            if (request.HttpVersion.Equals("HTTP/1.1"))
            {
                if ((request.Headers.Get("Host") == null) || (request.Headers.Get("host") == null))
                {
                    try
                    {
                        var testUri = new Uri(request.Url);
                        request.Url = testUri.PathAndQuery;
                        request.Headers.Add("Host", testUri.Host);
                    }
                    catch (UriFormatException)
                    {
                        //cant determine host. Relative url with no host header
                        request.Headers.Add("Host", "graph.microsoft.com");
                    }
                }
            }

            //make sure the url in the request begins with a valid api endpoint
            if (!(request.Url.Substring(0, 6).Equals("/beta/") || request.Url.Substring(0, 6).Equals("/v1.0/")))
            {
                //Log the error for the documentation to be fixed.
                issues.Warning(ValidationErrorCode.InvalidUrlString, $"The url: {request.Url} does not start a supported api version( /v1.0/ or /beta/ ). File: {method.SourceFile}");

                if (method.SourceFile.DisplayName.Contains("beta"))
                {
                    //try to force the url to a beta endpoint.
                    request.Url = "/beta" + request.Url;
                }
                else
                {
                    //try to force the url to a version 1 endpoint.
                    request.Url = "/v1.0" + request.Url;
                }
            }

            //replace instance of "<" with single quotes parameter to prevent api fails
            if (request.Url.Contains("%3C"))
            {
                request.Url = request.Url.Replace("%3C", "%27");
            }

            //replace instance ">" with single quotes parameter to prevent api fails
            if (request.Url.Contains("%3E"))
            {
                request.Url = request.Url.Replace("%3E", "%27");
            }

            //replace instance " " with single quotes parameter to prevent api fails
            if (request.Url.Contains(" "))
            {
                request.Url = request.Url.Replace(" ", "%20");
            }

            return request;
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

        private static ResourceDefinition GetResoureDocumentation(ResourceDefinition resource, ResourceDefinition[] documentedResources, bool? shouldValidateNamespace)
        {
            IEnumerable<ResourceDefinition> docResourceQuery = shouldValidateNamespace == true
                ? from dr in documentedResources
                  where dr.Name == resource.Name || (!string.IsNullOrEmpty(dr.SourceFile?.Namespace) && dr.Name == dr.SourceFile?.Namespace + "." + resource.Name.TypeOnly())
                  select dr

                : from dr in documentedResources
                  where dr.Name.TypeOnly() == resource.Name.TypeOnly()
                  select dr
                ;

            if (docResourceQuery.Count() > 1)
            {
                // Log an error about multiple documentation resource definitions of the metadata model.
                FancyConsole.WriteLine("Multiple resource definitions found for resource {0} in files:", resource.Name);
                foreach (var q in docResourceQuery)
                {
                    FancyConsole.WriteLine(q.SourceFile.DisplayName);
                }
            }

            return docResourceQuery.FirstOrDefault();
        }

        private class ParameterComparer : IEqualityComparer<Parameter>, IComparer<Parameter>
        {
            public static ParameterComparer Instance { get; } = new ParameterComparer();

            private static HashSet<string> equivalentBindingParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "bindParameter",
                "bindingParameter",
                "this",
            };

            private ParameterComparer()
            {
            }

            public bool Equals(Parameter x, Parameter y)
            {
                if (object.ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                return x.Name == y.Name ||
                    (equivalentBindingParameters.Contains(x.Name) && equivalentBindingParameters.Contains(y.Name));
            }

            public int GetHashCode(Parameter obj)
            {
                return obj?.Name?.GetHashCode() ?? 0;
            }

            public int Compare(Parameter x, Parameter y)
            {
                if (object.ReferenceEquals(x, y))
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                var xName = x.Name;
                var yName = y.Name;
                if (equivalentBindingParameters.Contains(xName))
                {
                    xName = "bindingParameter";
                }

                if (equivalentBindingParameters.Contains(yName))
                {
                    yName = "bindingParameter";
                }

                return xName.CompareTo(yName);
            }
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
