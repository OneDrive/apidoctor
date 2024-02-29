﻿/*
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
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using ApiDoctor.DocumentationGeneration;
    using ApiDoctor.Validation.Config;
    using ApiDoctor.Validation.OData.Transformation;
    using AppVeyor;
    using CommandLine;
    using Kibali;
    using Microsoft.OpenApi.Writers;
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
        public static AppConfigFile CurrentConfiguration
        {
            get; private set;
        }

        // Set to true to disable returning an error code when the app exits.
        private static bool IgnoreErrors
        {
            get; set;
        }

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
                Parser.Default.ParseArguments<PrintOptions, CheckLinkOptions, BasicCheckOptions, CheckAllLinkOptions, CheckServiceOptions, PublishOptions, PublishMetadataOptions, CheckMetadataOptions, FixDocsOptions, GenerateDocsOptions, GenerateSnippetsOptions, AboutOptions, DeduplicateExampleNamesOptions, GeneratePermissionFilesOptions>(args)
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
                            (DeduplicateExampleNamesOptions options) => RunInvokedMethodAsync(options),
                            (GeneratePermissionFilesOptions options) => RunInvokedMethodAsync(options),
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
                case DeduplicateExampleNamesOptions o:
                    returnSuccess = await DeduplicateExampleNamesAsync(o, issues);
                    break;
                case GeneratePermissionFilesOptions o:
                    returnSuccess = await GeneratePermissionFilesAsync(o, issues);
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
                            AllowTruncatedResponses = modelConfigs?.TruncatedPropertiesValidation ?? false
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
        /// Detects duplicate names within a version (beta and V1) and deduplicates them by postfixing a counter at the end
        /// </summary>
        /// <param name="options">command line options</param>
        /// <param name="issues">issue logger</param>
        /// <param name="docs">doc set</param>
        /// <returns>whether the attempt was successful</returns>
        private static async Task<bool> DeduplicateExampleNamesAsync(DeduplicateExampleNamesOptions options, IssueLogger issues, DocSet docs = null)
        {
            //we are not out to validate the documents in this context.
            options.IgnoreErrors = options.IgnoreWarnings = true;

            //scan the docset and find the methods present
            var docset = docs ?? await GetDocSetAsync(options, issues).ConfigureAwait(false);
            if (null == docset)
            {
                return false;
            }
            var methods = FindTestMethods(options, docset);
            var hadDuplicates = !await ReportDuplicateMethodNamesAndDeduplicateAsync(methods).ConfigureAwait(false);
            if (hadDuplicates)
            {
                FancyConsole.WriteLine(FancyConsole.ConsoleWarningColor, "======================================================================================");
                FancyConsole.WriteLine(FancyConsole.ConsoleWarningColor, "Parsing the documentation again to make sure that deduplication attempt was successful");
                FancyConsole.WriteLine(FancyConsole.ConsoleWarningColor, "======================================================================================");

                docset = await GetDocSetAsync(options, issues).ConfigureAwait(false);
                if (null == docset)
                {
                    return false;
                }

                methods = FindTestMethods(options, docset);
                var hadDuplicatesInSecondRun = !await ReportDuplicateMethodNamesAndDeduplicateAsync(methods).ConfigureAwait(false);
                if (hadDuplicatesInSecondRun)
                {
                    issues.Error(ValidationErrorCode.DeduplicationWasUnsuccessful, "Please revisit the deduplication logic as one replacement attempt was not enough to clear all duplicates.");
                    return false;
                }
            }

            return true;
        }

        #region generate snippets
        /// <summary>
        /// Generate snippets for the methods present in the documents by querying an existing snippet generation api
        /// </summary>
        /// <param name="options"></param>
        /// <param name="issues"></param>
        /// <param name="docs"></param>
        /// <returns>The success/failure of the task</returns>
        private static async Task<bool> GenerateSnippetsAsync(GenerateSnippetsOptions options, IssueLogger issues, DocSet docs = null)
        {
            if (!File.Exists(options.SnippetGeneratorPath))
            {
                FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, "Error with the provided snippet generator path: " + options.SnippetGeneratorPath);
                return false;
            }

            //we are not out to validate the documents in this context.
            options.IgnoreErrors = options.IgnoreWarnings = true;

            var languageOptions = options.Languages.Split(',');

            //scan the docset and find the methods present
            var docset = docs ?? await GetDocSetAsync(options, issues);
            if (null == docset)
            {
                return false;
            }
            var methods = FindTestMethods(options, docset);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            FancyConsole.WriteLine(FancyConsole.ConsoleSuccessColor, "Generating snippets from Snippets API..");

            var snippetsPath = string.IsNullOrEmpty(options.TempOutputPath) ?
                Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()) :
                options.TempOutputPath;
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

            var docFiles = methods.Select(method => method.SourceFile).DistinctBy(x => x.DisplayName);
            var snippetsLanguageSetBySourceFile = GetSnippetsLanguageSetForDocSet(docFiles, languageOptions, snippetsPath);

            foreach (var method in methods)
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

                if (!snippetsLanguageSetBySourceFile.TryGetValue($"{method.SourceFile.DisplayName}/{method.Identifier}", out var languagesToIncludeForMethod))
                {
                    snippetsLanguageSetBySourceFile.TryGetValue(method.SourceFile.DisplayName, out languagesToIncludeForMethod);
                }

                // Generate snippets section for method to inject to document
                var codeSnippetsText = GenerateSnippetsTabSectionForMethod(method, languagesToIncludeForMethod, snippetsPath, snippetPrefix);
                InjectSnippetsIntoFile(method, codeSnippetsText);
            }

            return true;
        }

        private static string GenerateSnippetsTabSectionForMethod(MethodDefinition method, HashSet<string> languages, string snippetsPath, string snippetPrefix)
        {
            if (languages == null || !languages.Any())
                return string.Empty;

            var methodString = Regex.Replace(method.Identifier, @"[# .()\\/]", "").Replace("_", "-").ToLower(); // cleanup the method name
            var relativePathFolder = Path.Combine("includes", "snippets");
            var docsSnippetsDirectory = Path.Combine(Directory.GetParent(Path.GetDirectoryName(method.SourceFile.FullPath)).FullName, relativePathFolder);

            const string includeSdkFileName = "snippets-sdk-documentation-link.md";
            const string includeSnippetsNotAvailableFileName = "snippet-not-available.md";

            var sdkIncludeText = $"[!INCLUDE [sdk-documentation](../{ReplaceWindowsByLinuxPathSeparators(Path.Combine(relativePathFolder, includeSdkFileName))})]";
            var snippetNotAvailableIncludeText = $"[!INCLUDE [snippet-not-available](../{ReplaceWindowsByLinuxPathSeparators(Path.Combine(relativePathFolder, includeSnippetsNotAvailableFileName))})]";

            var isConceptsDirectory = method.SourceFile.FullPath.Contains("concepts");
            var versionString = snippetPrefix.Contains("beta") ? "beta" : "v1";
            var snippetsTabSectionForMethod = new StringBuilder();
            foreach (var language in languages)
            {
                var codeFenceString = language.ToLower().Replace("#", "sharp");
                var snippetFileName = methodString + $"-{codeFenceString}-snippets.md";

                var codeSnippet = GetSnippetContentForMethodByLanguage(language, snippetPrefix, snippetsPath);
                var sampleCodeIncludeText = string.Empty;
                if (isConceptsDirectory)
                {
                    sampleCodeIncludeText = $"[!INCLUDE [sample-code](../{ReplaceWindowsByLinuxPathSeparators(Path.Combine(relativePathFolder, codeFenceString, versionString, snippetFileName))})]";
                }
                else{
                    sampleCodeIncludeText = $"[!INCLUDE [sample-code](../{ReplaceWindowsByLinuxPathSeparators(Path.Combine(relativePathFolder, codeFenceString, snippetFileName))})]";
                }
                var tabText = $"# [{language}](#tab/{codeFenceString})\r\n" +
                              $"{(!string.IsNullOrWhiteSpace(codeSnippet) ? sampleCodeIncludeText : snippetNotAvailableIncludeText)}\r\n" +
                              $"{sdkIncludeText}\r\n\r\n";

                snippetsTabSectionForMethod.Append(tabText);

                // Dump the code snippet file
                if (codeSnippet != null)
                {
                    var snippetFileContents = "---\r\ndescription: \"Automatically generated file. DO NOT MODIFY\"\r\n---\r\n\r\n" +    //header
                        $"```{codeFenceString.Replace("cli","bash")}\r\n\r\n" + // code fence
                        $"{codeSnippet}\r\n\r\n" +       // generated code snippet
                        "```";                           // closing fence

                    var docsSnippetLanguageDirectory = Path.Combine(docsSnippetsDirectory, codeFenceString);
                    if (isConceptsDirectory)
                    {
                        // If the method is in the concepts directory, we further need to separate the snippets by version
                        docsSnippetLanguageDirectory = Path.Combine(docsSnippetLanguageDirectory, versionString);
                    }
                    Directory.CreateDirectory(docsSnippetLanguageDirectory); // make sure snippet file directory exists

                    var snippetMarkdownFilePath = Path.Combine(docsSnippetLanguageDirectory, snippetFileName);
                    FancyConsole.WriteLine(FancyConsole.ConsoleSuccessColor, $"Writing snippet to {snippetMarkdownFilePath}");
                    File.WriteAllText(snippetMarkdownFilePath, snippetFileContents); // write snippet to file
                }
            }

            if (snippetsTabSectionForMethod.Length > 0)
                snippetsTabSectionForMethod.Append("---"); // append end of tab section

            // Dump the SDK link file if doesn't exist
            var sdkFileFullName = Path.Combine(docsSnippetsDirectory, includeSdkFileName);
            if (!File.Exists(sdkFileFullName))
            {
                const string includeSdkText = "<!-- markdownlint-disable MD041-->\r\n\r\n" +
                    "> Read the [SDK documentation](https://docs.microsoft.com/graph/sdks/sdks-overview) " +
                    "for details on how to [add the SDK](https://docs.microsoft.com/graph/sdks/sdk-installation) to your project and " +
                    "[create an authProvider](https://docs.microsoft.com/graph/sdks/choose-authentication-providers) instance.";
                File.WriteAllText(sdkFileFullName, includeSdkText);
            }

            // Dump the snippet not available file if it does not exist
            var snippetNotAvailableFileFullName = Path.Combine(docsSnippetsDirectory, includeSnippetsNotAvailableFileName);
            if (!File.Exists(snippetNotAvailableFileFullName))
            {
                const string includeSnippetNotAvailableText = "```\r\nSnippet not available\r\n```";
                File.WriteAllText(snippetNotAvailableFileFullName, includeSnippetNotAvailableText);
            }
            return snippetsTabSectionForMethod.ToString();
        }

        private static string GetSnippetContentForMethodByLanguage(string language, string snippetPrefix, string snippetsPath)
        {
            var fileName = $"{snippetPrefix}---{language.ToLowerInvariant()}";
            var fileFullPath = Path.Combine(snippetsPath, fileName);

            FancyConsole.WriteLine(FancyConsole.ConsoleSuccessColor, $"Reading {fileFullPath}");

            string codeSnippet = null;
            if (File.Exists(fileFullPath))
                codeSnippet = File.ReadAllText(fileFullPath);
            else
                FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, $"Error: file '{fileName}' does not exist");

            return codeSnippet;
        }

        private static Dictionary<string, HashSet<string>> GetSnippetsLanguageSetForDocSet(IEnumerable<DocFile> docFiles, string[] languageOptions, string snippetsPath)
        {
            var snippetsLanguageSetBySourceFile = new Dictionary<string, HashSet<string>>();
            var snippetTempFiles = Directory.EnumerateFiles(snippetsPath);
            foreach (DocFile docFile in docFiles)
            {
                var languages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var request in docFile.Requests)
                {
                    string snippetPrefix;
                    try { snippetPrefix = GetSnippetPrefix(request); } catch (ArgumentException) { continue; }

                    var expectedFileName = $"{Path.Combine(snippetsPath, snippetPrefix)}---";
                    var snippetLanguagesForMethod = snippetTempFiles
                        .Where(x => !x.EndsWith("-error", StringComparison.OrdinalIgnoreCase))
                        .Where(x => x.StartsWith(expectedFileName))
                        .Select(x => x.Substring(expectedFileName.Length))
                        .ToHashSet();
                    if (!snippetLanguagesForMethod.Any())
                    {
                        snippetsLanguageSetBySourceFile.TryAdd($"{request.SourceFile.DisplayName}/{request.Identifier}", snippetLanguagesForMethod);
                        continue;
                    }

                    languages.UnionWith(snippetLanguagesForMethod);
                }

                // just so that we have the correct casing displayed as tab names in docs e.g. PowerShell and not powershell
                languages = languageOptions.Where(x => languages.Contains(x)).ToHashSet();

                snippetsLanguageSetBySourceFile.Add(docFile.DisplayName, languages);
            }
            return snippetsLanguageSetBySourceFile;
        }

        /// <summary>
        /// Snippet generation has an assumption that request block names are unique within a version
        /// This method reports duplicate names and deduplicates them
        /// </summary>
        /// <param name="methods">request method definitions</param>
        private static async Task<bool> ReportDuplicateMethodNamesAndDeduplicateAsync(MethodDefinition[] methods)
        {
            var identifierFileMapping = new Dictionary<string, List<(string, string)>>();
            var duplicates = new HashSet<string>();
            var allIdentifiers = new HashSet<string>();
            var fileCount = 0;
            foreach (var method in methods)
            {
                string snippetPrefix;
                try
                {
                    snippetPrefix = GetSnippetPrefix(method);
                }
                catch
                {
                    snippetPrefix = method.Identifier;
                }
                allIdentifiers.Add(snippetPrefix);

                if (identifierFileMapping.ContainsKey(snippetPrefix))
                {
                    identifierFileMapping[snippetPrefix].Add((method.Identifier, method.SourceFile.FullPath));
                    fileCount++;
                    duplicates.Add(snippetPrefix);
                }
                else
                {
                    identifierFileMapping[snippetPrefix] = new List<(string, string)> { (method.Identifier, method.SourceFile.FullPath) };
                }
            }

            if (duplicates.Count > 0)
            {
                var totalFileCount = fileCount + duplicates.Count; // add number of initial occurences as well
                FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, $"==================================================================");
                FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, $"found {duplicates.Count} duplicate names in {totalFileCount} files");
                FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, $"==================================================================");

                foreach (var duplicate in duplicates)
                {
                    FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, $"{duplicate}");
                    var counter = 0;
                    foreach ((var identifier, var fileName) in identifierFileMapping[duplicate])
                    {
                        string version;
                        try
                        {
                            version = GetSnippetVersion(fileName);
                        }
                        catch
                        {
                            version = string.Empty;
                        }

                        do
                        {
                            // make sure we don't collide with existing identifiers while replacing
                            // continue incrementing until we don't have collision
                            counter++;
                        } while (allIdentifiers.Contains(GetSnippetPrefix($"{identifier}_{counter}", version)));

                        // deduplicate by appending a counter at the end.
                        await ReplaceFirstOccurence(fileName, identifier, $"{identifier}_{counter}").ConfigureAwait(false);

                        // report duplicate
                        FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, $"  {identifier} in {fileName}");
                    }
                }

                FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, $"==================================================================");
                FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, $"End of duplicates reporting...");
                FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, $"==================================================================");
                return false;
            }
            else
            {
                FancyConsole.WriteLine(FancyConsole.ConsoleSuccessColor, $"==================================================================");
                FancyConsole.WriteLine(FancyConsole.ConsoleSuccessColor, $"No duplicates found");
                FancyConsole.WriteLine(FancyConsole.ConsoleSuccessColor, $"==================================================================");
            }

            return true;
        }

        /// <summary>
        /// Generate snippets using snippet generator command line tool
        /// </summary>
        /// <param name="executablePath">path to snippet generator command line tool</param>
        /// <param name="args">arguments to snippet generator</param>
        private static void GenerateSnippets(string executablePath, params string[] args)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo(executablePath, string.Join(" ", args))
                {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                }
            };
            using var outputWaitHandle = new AutoResetEvent(false);
            using var errorWaitHandle = new AutoResetEvent(false);
            process.OutputDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                {
                    outputWaitHandle.Set();
                }
                else
                {
                    FancyConsole.Write(FancyConsole.ConsoleDefaultColor, e.Data);
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
                    FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, "Error when generating code snippets!!!");
                    FancyConsole.Write(FancyConsole.ConsoleErrorColor, e.Data);
                }
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }

        /// Gets snippet prefix with method name and version information to prevent collision between different API versions
        /// <param name="method">method definition</param>
        /// <returns>prefix representing method name and version</returns>
        private static string GetSnippetPrefix(MethodDefinition method)
        {
            string version;
            try
            {
                // try to get the version from the url
                version = GetSnippetVersion(method.Request);
            }
            catch (ArgumentException)
            {
                // default to using the file location. Error/Warning will be logged
                version = GetSnippetVersion(method.SourceFile.DisplayName);
            }

            return GetSnippetPrefix(method.Identifier, version);
        }

        /// <summary>
        /// determines the version postfix in snippet file name
        /// </summary>
        /// <param name="request">the url snippet calls</param>
        /// <returns>version postfix for snippet file name</returns>
        /// <exception cref="ArgumentException">if file doesn't belong to a version</exception>
        private static string GetSnippetVersion(string request)
        {
            if (request.Contains("beta", StringComparison.OrdinalIgnoreCase))
            {
                return "-beta";
            }
            else if (request.Contains("v1.0", StringComparison.OrdinalIgnoreCase))
            {
                return "-v1";
            }
            else
            {
                throw new ArgumentException("trying to parse a snippet which doesn't belong to a particular version", nameof(request));
            }
        }

        /// <summary>
        /// Calculates canonical name from method identifier and version.
        /// </summary>
        /// <param name="identifier">identifier as it appears in request definition block</param>
        /// <param name="version">version postfix</param>
        /// <returns>canonical name from method identifier and version</returns>
        private static string GetSnippetPrefix(string identifier, string version)
        {
            var methodName = Regex.Replace(identifier, @"[$# .()\\/]", "").Replace("_", "-").ToLower();
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
            var duplicates = new Dictionary<string, (string OriginalSourceFile, string OriginalIdentifier, int Count)>();
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

                // if there is a duplicate in http snippet naming, write error message into a file with -duplicate<count> suffix
                if (duplicates.ContainsKey(fileName))
                {
                    (var originalSourceFile, var originalIdentifier, var count) = duplicates[fileName];
                    var docsUrlFileSegment = Path.GetFileNameWithoutExtension(method.SourceFile.FullPath);
                    var docsUrlVersionSegment = snippetPrefix.Contains("beta") ? "beta" : "1.0";
                    var documentationLink = $"https://learn.microsoft.com/en-us/graph/api/{docsUrlFileSegment}?view=graph-rest-{docsUrlVersionSegment}&tabs=http";
                    duplicates[fileName] = (originalSourceFile, originalIdentifier, count + 1);
                    var errorMessage = "OriginalFile: " + originalSourceFile + Environment.NewLine
                                    + "OriginalIdentifier: " + originalIdentifier + Environment.NewLine
                                    + "DuplicateFile: " + method.SourceFile.DisplayName + Environment.NewLine
                                    + "DuplicateIdentifier: " + method.Identifier + Environment.NewLine
                                    + "DocsLink: " + documentationLink + Environment.NewLine;

                    var duplicateFileFullPath = fileFullPath + "-duplicate" + count;
                    FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, $"Writing {duplicateFileFullPath}");
                    File.WriteAllText(duplicateFileFullPath, errorMessage);
                }
                else
                {
                    duplicates[fileName] = (method.SourceFile.DisplayName, method.Identifier, 1);
                    FancyConsole.WriteLine(FancyConsole.ConsoleSuccessColor, $"Writing {fileFullPath}");
                    File.WriteAllText(fileFullPath, ReplaceLFbyCRLF(request.FullHttpText(true)));
                }
            }
        }
        private static string ReplaceLFbyCRLF(string original)
        {
            return original.Replace("\r\n", "\n").Replace("\n", "\r\n"); // we first replace CRLF by LF and then all LF by CRLF to avoid breaking any CRLF or missing any LF
        }
        /// <summary>
        /// Replaces \ by / to keep output mardown consistent accross generation OSes
        /// </summary>
        /// <param name="original">The original path</param>
        /// <returns>The cleaned up result</returns>
        private static string ReplaceWindowsByLinuxPathSeparators(string original) => original.Replace("\\", "/");

        /// <summary>
        /// Finds the file the request is located and inserts the code snippet into the file
        /// </summary>
        /// <param name="method">The <see cref="MethodDefinition"/> of the request being generated a snippet for</param>
        /// <param name="codeSnippets">The string of the code snippets tab section</param>
        private static void InjectSnippetsIntoFile(MethodDefinition method, string codeSnippets)
        {
            var originalFileContents = File.ReadAllLines(method.SourceFile.FullPath);
            var httpRequestString = method.Request.Split(Environment.NewLine.ToCharArray()).First();

            var insertionLine = 0;
            var snippetsTabSectionEndLine = 0;
            var requestStartLine = 0;

            var parseStatus = CodeSnippetInsertionState.FindMethodIdentifierLine;
            var finishedParsing = false;
            for (var currentIndex = 0; currentIndex < originalFileContents.Length && !finishedParsing; currentIndex++)
            {
                var currentLine = originalFileContents[currentIndex];
                switch (parseStatus)
                {
                    case CodeSnippetInsertionState.FindMethodIdentifierLine:
                        if (currentLine.Contains(method.Identifier))
                        {
                            parseStatus = CodeSnippetInsertionState.FindHttpRequestLine;
                        }
                        break;
                    case CodeSnippetInsertionState.FindHttpRequestLine: // check if we have found the line with HTTP request for method
                        if (currentLine == httpRequestString)
                        {
                            parseStatus = CodeSnippetInsertionState.FindHttpRequestStartLine;
                        }
                        break;
                    case CodeSnippetInsertionState.FindHttpRequestStartLine: // scan back to find the line where we can best place the HTTP tab
                        for (var index = currentIndex; index > 0; index--)
                        {
                            if (originalFileContents[index].Contains("<!-- {")
                              || originalFileContents[index].Contains("<!--{"))
                            {
                                requestStartLine = index;
                                currentIndex--;
                                parseStatus = CodeSnippetInsertionState.FindEndOfCodeBlock;
                                break;
                            }
                            if (originalFileContents[index].Contains("```http")
                                && HttpParser.TryParseHttpRequest(method.Request, out var request)
                                && request.Method.Equals("GET"))
                            {
                                originalFileContents[index] = "```msgraph-interactive";
                            }
                        }
                        break;
                    case CodeSnippetInsertionState.FindEndOfCodeBlock:
                        if (currentLine.Trim().Equals("```"))
                        {
                            insertionLine = currentIndex + 1;
                            parseStatus = CodeSnippetInsertionState.FirstTabInsertion;
                        }
                        break;
                    case CodeSnippetInsertionState.FirstTabInsertion:
                        // stop if we get to a header, HTTP tab, or HTML comment
                        if (currentLine.Trim().StartsWith("##") || currentLine.Contains("#tab/http") || currentLine.Trim().StartsWith("<!--"))
                        {
                            finishedParsing = true;
                        }
                        else if (currentLine.Contains("#tab"))
                        {
                            parseStatus = CodeSnippetInsertionState.FindEndOfTabSection;
                        }
                        break;
                    case CodeSnippetInsertionState.FindEndOfTabSection:
                        if (currentLine.Contains("---"))
                        {
                            snippetsTabSectionEndLine = currentIndex;
                            if (string.IsNullOrWhiteSpace(originalFileContents[currentIndex + 1]))
                                snippetsTabSectionEndLine++;
                            parseStatus = CodeSnippetInsertionState.InsertSnippets;
                            finishedParsing = true;
                        }
                        break;
                    default:
                        break;
                }
            }

            IEnumerable<string> updatedFileContents = originalFileContents;
            if (parseStatus == CodeSnippetInsertionState.FirstTabInsertion && !string.IsNullOrWhiteSpace(codeSnippets))
            {
                // inject the first tab section
                if (!originalFileContents[requestStartLine - 1].Contains("#tab/http"))
                {
                    const string httpTabText = "# [HTTP](#tab/http)";
                    updatedFileContents = FileSplicer(updatedFileContents.ToArray(), requestStartLine - 1, httpTabText);
                    codeSnippets = $"\r\n{codeSnippets}";
                }

                snippetsTabSectionEndLine = insertionLine;
                parseStatus = CodeSnippetInsertionState.InsertSnippets;
            }

            if (parseStatus == CodeSnippetInsertionState.InsertSnippets)
            {
                // Remove HTTP tab if it exists and there are no snippets to add
                if (string.IsNullOrWhiteSpace(codeSnippets) && originalFileContents[requestStartLine - 1].Contains("#tab/http"))
                {
                    updatedFileContents = updatedFileContents.Splice(requestStartLine - 1, 1);
                    insertionLine--;
                    snippetsTabSectionEndLine--;
                }
                updatedFileContents = updatedFileContents.Splice(insertionLine, snippetsTabSectionEndLine - insertionLine);
                if (!string.IsNullOrWhiteSpace(codeSnippets))
                {
                    if (!string.IsNullOrWhiteSpace(updatedFileContents.ElementAt(insertionLine + 1)))
                        codeSnippets = $"{codeSnippets}\r\n";
                    updatedFileContents = FileSplicer(updatedFileContents.ToArray(), insertionLine, codeSnippets);
                }
                // dump the injections
                File.WriteAllLines(method.SourceFile.FullPath, updatedFileContents);
            }
        }

        private enum CodeSnippetInsertionState
        {
            FindMethodIdentifierLine,
            FindHttpRequestLine,
            FindHttpRequestStartLine,
            FindEndOfCodeBlock,
            FirstTabInsertion,
            FindEndOfTabSection,
            InsertSnippets
        }

        private const string graphHostName = "graph.microsoft.com";
        private const string hostHeaderKey = "Host";
        /// <summary>
        /// Finds the file the request is located and inserts the code snippet into the file.
        /// </summary>
        /// <param name="request">Request with url to be verified or corrected</param>
        /// <param name="method">The <see cref="MethodDefinition"/> of the request being generated a snippet for</param>
        /// <param name="issues">Issue logger to record any issues</param>
        private static HttpRequest PreProcessUrlForSnippetGeneration(HttpRequest request, MethodDefinition method, IssueLogger issues)
        {
            //Version 1.1 of HTTP protocol MUST specify the host header
            if (request.HttpVersion.Equals("HTTP/1.1"))
            {
                if (!request.Headers.AllKeys.Contains(hostHeaderKey) || string.IsNullOrEmpty(request.Headers[hostHeaderKey]))
                {
                    try
                    {
                        var testUri = new Uri(request.Url);
                        request.Url = WebUtility.UrlDecode(testUri.PathAndQuery);
                        var hostName = string.IsNullOrEmpty(testUri.Host) ? graphHostName : testUri.Host;

                        if (request.Headers.AllKeys.Contains(hostHeaderKey))
                            request.Headers[hostHeaderKey] = hostName;
                        else
                            request.Headers.Add(hostHeaderKey, hostName);
                    }
                    catch (UriFormatException)
                    {
                        //cant determine host. Relative url with no host header
                        if (request.Headers.AllKeys.Contains(hostHeaderKey))
                            request.Headers[hostHeaderKey] = graphHostName;
                        else
                            request.Headers.Add(hostHeaderKey, graphHostName);
                    }
                }
            }

            //make sure the url in the request begins with a valid api endpoint
            if (!(request.Url.Substring(0, 6).Equals("/beta/") || request.Url.Substring(0, 6).Equals("/v1.0/")))
            {
                //Log the error for the documentation to be fixed.
                issues.Warning(ValidationErrorCode.InvalidUrlString, $"The url: {request.Url} does not start with a supported api version( /v1.0/ or /beta/ ). File: {method.SourceFile}");

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
        #endregion

        #region generate permission files
        /// <summary>
        /// Generate/update permission files for operation topics
        /// </summary>
        /// <param name="options"></param>
        /// <param name="issues"></param>
        /// <returns></returns>
        private static async Task<bool> GeneratePermissionFilesAsync(GeneratePermissionFilesOptions options, IssueLogger issues, DocSet docs = null)
        {
            // we don't care about validating documents in this context
            options.IgnoreErrors = options.IgnoreWarnings = true;

            var docSet = docs ?? await GetDocSetAsync(options, issues);
            if (null == docSet)
                return false;

            // we only expect to have permission definitions in documents of ApiPageType
            var docFiles = docSet.Files.Where(x => x.DocumentPageType == DocFile.PageType.ApiPageType);

            // skip generation for workloads specified in config
            var workloadsToSkip = DocSet.SchemaConfig?.SkipPermissionTableUpdateForWorkloads ?? new List<string>();
            docFiles = docFiles.Where(f => !workloadsToSkip.Any(w => !string.IsNullOrWhiteSpace(f.DisplayName) && f.DisplayName.IContains(w)));

            // generate permissions document
            var permissionsDocument = new PermissionsDocument();
            if (docFiles.Any() && !options.BootstrappingOnly)
            {
                permissionsDocument = await GetPermissionsDocumentAsync(options.PermissionsSourceFile);
                if (permissionsDocument == null)
                    return false;
            }

            var listOfFiles = new List<string>()
            {
                "/api-reference/v1.0/api/privilegedaccessgroupassignmentschedule-filterbycurrentuser.md",
                "/api-reference/v1.0/api/user-retryserviceprovisioning.md",
                "/api-reference/v1.0/api/accesspackagecatalog-list-resources.md",
                "/api-reference/v1.0/api/educationassignment-post-gradingcategory.md",
                "/api-reference/v1.0/api/payloaddetail-get.md",
                "/api-reference/v1.0/api/worksheet-cell.md",
                "/api-reference/v1.0/api/serviceprincipal-delete-remotedesktopsecurityconfiguration.md",
                "/api-reference/v1.0/api/domain-list-federationconfiguration.md",
                "/api-reference/v1.0/api/entitlementmanagement-list-resourceenvironments.md",
                "/api-reference/v1.0/api/educationmodule-post-resources.md",
                "/api-reference/v1.0/api/accesspackageassignmentworkflowextension-delete.md",
                "/api-reference/v1.0/api/targetdevicegroup-update.md",
                "/api-reference/v1.0/api/simulationreportoverview-get.md",
                "/api-reference/v1.0/api/virtualeventwebinar-get.md",
                "/api-reference/v1.0/api/privilegedaccessgroup-post-assignmentschedulerequests.md",
                "/api-reference/v1.0/api/subjectrightsrequest-getfinalreport.md",
                "/api-reference/v1.0/api/security-hostport-get.md",
                "/api-reference/v1.0/api/accesspackagecatalog-list-resourceroles.md",
                "/api-reference/v1.0/api/security-host-list-sslcertificates.md",
                "/api-reference/v1.0/api/entitlementmanagement-post-resourcerequests.md",
                "/api-reference/v1.0/api/accesspackageassignmentworkflowextension-get.md",
                "/api-reference/v1.0/api/security-threatintelligence-list-sslcertificates.md",
                "/api-reference/v1.0/api/remotedesktopsecurityconfiguration-list-targetdevicegroups.md",
                "/api-reference/v1.0/api/educationclass-post-module.md",
                "/api-reference/v1.0/api/call-senddtmftones.md",
                "/api-reference/v1.0/api/samlorwsfedexternaldomainfederation-update.md",
                "/api-reference/v1.0/api/orgcontact-retryserviceprovisioning.md",
                "/api-reference/v1.0/api/privilegedaccessgroupassignmentscheduleinstance-get.md",
                "/api-reference/v1.0/api/identitygovernance-userprocessingresult-summary.md",
                "/api-reference/v1.0/api/applicationtemplate-get.md",
                "/api-reference/v1.0/api/educationmodule-get.md",
                "/api-reference/v1.0/api/remotedesktopsecurityconfiguration-update.md",
                "/api-reference/v1.0/api/profilecardproperty-delete.md",
                "/api-reference/v1.0/api/educationassignment-delete-gradingcategory.md",
                "/api-reference/v1.0/api/privilegedaccessgroupassignmentschedulerequest-filterbycurrentuser.md",
                "/api-reference/v1.0/api/user-reminderview.md",
                "/api-reference/v1.0/api/educationmodule-pin.md",
                "/api-reference/v1.0/api/privilegedaccessgroup-post-eligibilityschedulerequests.md",
                "/api-reference/v1.0/api/accesspackageassignmentrequestworkflowextension-get.md",
                "/api-reference/v1.0/api/privilegedaccessgroupassignmentschedulerequest-cancel.md",
                "/api-reference/v1.0/api/educationassignment-activate.md",
                "/api-reference/v1.0/api/identitygovernance-taskreport-summary.md",
                "/api-reference/v1.0/api/applicationtemplate-list.md",
                "/api-reference/v1.0/api/tablerowcollection-itemat.md",
                "/api-reference/v1.0/api/privilegedaccessgroup-list-assignmentschedules.md",
                "/api-reference/v1.0/api/peopleadminsettings-post-profilecardproperties.md",
                "/api-reference/v1.0/api/educationgradingcategory-update.md",
                "/api-reference/v1.0/api/learningcourseactivity-update.md",
                "/api-reference/v1.0/api/remotedesktopsecurityconfiguration-get.md",
                "/api-reference/v1.0/api/remotedesktopsecurityconfiguration-post-targetdevicegroups.md",
                "/api-reference/v1.0/api/accesspackageassignmentrequestworkflowextension-delete.md",
                "/api-reference/v1.0/api/educationmoduleresource-delete.md",
                "/api-reference/v1.0/api/privilegedaccessgroupassignmentschedule-get.md",
                "/api-reference/v1.0/api/educationmodule-delete.md",
                "/api-reference/v1.0/api/remotedesktopsecurityconfiguration-delete-targetdevicegroups.md",
                "/api-reference/v1.0/api/targetdevicegroup-get.md",
                "/api-reference/v1.0/api/accesspackagecatalog-post-accesspackagecustomworkflowextensions.md",
                "/api-reference/v1.0/api/educationmoduleresource-get.md",
                "/api-reference/v1.0/api/fido2authenticationmethod-get.md",
                "/api-reference/v1.0/api/fido2authenticationmethod-get.md",
                "/api-reference/v1.0/api/security-host-list-parenthostpairs.md",
                "/api-reference/v1.0/api/itemactivitystat-getactivitybyinterval.md",
                "/api-reference/v1.0/api/devicelocalcredentialinfo-get.md",
                "/api-reference/v1.0/api/workbookrangeview-itemat.md",
                "/api-reference/v1.0/api/security-whoisrecord-list-history.md",
                "/api-reference/v1.0/api/privilegedaccessgroupeligibilityschedulerequest-get.md",
                "/api-reference/v1.0/api/security-host-list-childhostpairs.md",
                "/api-reference/v1.0/api/privilegedaccessgroup-list-assignmentschedulerequests.md",
                "/api-reference/v1.0/api/security-intelligenceprofileindicator-get.md",
                "/api-reference/v1.0/api/identitygovernance-userprocessingresult-get.md",
                "/api-reference/v1.0/api/call-reject.md",
                "/api-reference/v1.0/api/virtualeventwebinar-getbyuseridandrole.md",
                "/api-reference/v1.0/api/security-hostsslcertificate-get.md",
                "/api-reference/v1.0/api/security-host-list-ports.md",
                "/api-reference/v1.0/api/privilegedaccessgroup-list-eligibilityschedules.md",
                "/api-reference/v1.0/api/virtualeventwebinar-list-registrations.md",
                "/api-reference/v1.0/api/educationclass-list-modules.md",
                "/api-reference/v1.0/api/agreementfile-list-localizations.md",
                "/api-reference/v1.0/api/accesspackage-post-resourcerolescopes.md",
                "/api-reference/v1.0/api/privilegedaccessgroupassignmentschedulerequest-get.md",
                "/api-reference/v1.0/api/virtualeventwebinar-getbyuserrole.md",
                "/api-reference/v1.0/api/educationmoduleresource-update.md",
                "/api-reference/v1.0/api/accesspackageassignmentrequestworkflowextension-update.md",
                "/api-reference/v1.0/api/privilegedaccessgroupeligibilityschedule-get.md",
                "/api-reference/v1.0/api/educationmodule-list-resources.md",
                "/api-reference/v1.0/api/security-whoisrecord-get.md",
                "/api-reference/v1.0/api/reports-getgrouparchivedprintjobs.md",
                "/api-reference/v1.0/api/privilegedaccessgroupassignmentscheduleinstance-filterbycurrentuser.md",
                "/api-reference/v1.0/api/reports-getuserarchivedprintjobs.md",
                "/api-reference/v1.0/api/addlargegalleryviewoperation-get.md",
                "/api-reference/v1.0/api/educationassignment-deactivate.md",
                "/api-reference/v1.0/api/accesspackagecatalog-list-accesspackagecustomworkflowextensions.md",
                "/api-reference/v1.0/api/reports-getprinterarchivedprintjobs.md",
                "/api-reference/v1.0/api/accesspackage-delete-resourcerolescopes.md",
                "/api-reference/v1.0/api/virtualeventsession-get.md",
                "/api-reference/v1.0/api/entitlementmanagement-list-resourcerequests.md",
                "/api-reference/v1.0/api/educationmodule-publish.md",
                "/api-reference/v1.0/api/channel-doesuserhaveaccess.md",
                "/api-reference/v1.0/api/privilegedaccessgroupeligibilityscheduleinstance-filterbycurrentuser.md",
                "/api-reference/v1.0/api/educationmodule-unpin.md",
                "/api-reference/v1.0/api/samlorwsfedexternaldomainfederation-post.md",
                "/api-reference/v1.0/api/security-host-list-subdomains.md",
                "/api-reference/v1.0/api/privilegedaccessgroupeligibilityschedulerequest-filterbycurrentuser.md",
                "/api-reference/v1.0/api/security-sslcertificate-get.md",
                "/api-reference/v1.0/api/educationmodule-update.md",
                "/api-reference/v1.0/api/identitygovernance-workflow-list-userprocessingresults.md",
                "/api-reference/v1.0/api/virtualevent-list-sessions.md",
                "/api-reference/v1.0/api/usersimulationdetails-list.md",
                "/api-reference/v1.0/api/directory-list-devicelocalcredentials.md",
                "/api-reference/v1.0/api/serviceprincipal-post-remotedesktopsecurityconfiguration.md",
                "/api-reference/v1.0/api/educationmodule-setupresourcesfolder.md",
                "/api-reference/v1.0/api/workbookrange-resizedrange.md",
                "/api-reference/v1.0/api/privilegedaccessgroup-list-assignmentscheduleinstances.md",
                "/api-reference/v1.0/api/accesspackageassignmentworkflowextension-update.md",
                "/api-reference/v1.0/api/privilegedaccessgroupeligibilityscheduleinstance-get.md",
                "/api-reference/v1.0/api/virtualeventregistration-get.md",
                "/api-reference/v1.0/api/partners-billing-billedusage-export.md",
                "/api-reference/v1.0/api/privilegedaccessgroup-list-eligibilityscheduleinstances.md",
                "/api-reference/v1.0/api/driveitem-permanentdelete.md",
                "/api-reference/v1.0/api/virtualeventsroot-list-webinars.md",
                "/api-reference/v1.0/api/privilegedaccessgroupeligibilityschedule-filterbycurrentuser.md",
                "/api-reference/v1.0/api/security-subdomain-get.md",
                "/api-reference/v1.0/api/group-retryserviceprovisioning.md",
                "/api-reference/v1.0/api/security-threatintelligence-list-whoisrecords.md",
                "/api-reference/v1.0/api/profilecardproperty-update.md",
                "/api-reference/v1.0/api/privilegedaccessgroupeligibilityschedulerequest-cancel.md",
                "/api-reference/v1.0/api/accesspackageassignmentrequest-resume.md",
                "/api-reference/v1.0/api/security-whoishistoryrecord-get.md",
                "/api-reference/v1.0/api/security-hostpair-get.md",
                "/api-reference/v1.0/api/privilegedaccessgroup-list-eligibilityschedulerequests.md",
                "/api-reference/v1.0/api/security-host-list-hostpairs.md",
                "/api-reference/v1.0/api/profilecardproperty-get.md",
                "/api-reference/beta/api/azureauthorizationsystem-list-services.md",
                "/api-reference/beta/api/hardwareoathauthenticationmethodconfiguration-get.md",
                "/api-reference/beta/api/awsauthorizationsystem-list.md",
                "/api-reference/beta/api/hardwareoathauthenticationmethodconfiguration-update.md",
                "/api-reference/beta/api/awspolicy-get.md",
                "/api-reference/beta/api/security-labelsroot-delete-citations.md",
                "/api-reference/beta/api/admindynamics-update.md",
                "/api-reference/beta/api/pendingexternaluserprofile-update.md",
                "/api-reference/beta/api/openawssecuritygroupfinding-get.md",
                "/api-reference/beta/api/dailyuserinsightmetricsroot-list-requests.md",
                "/api-reference/beta/api/privilegedaccessgroupassignmentschedule-filterbycurrentuser.md",
                "/api-reference/beta/api/inactiveawsrolefinding-get.md",
                "/api-reference/beta/api/user-retryserviceprovisioning.md",
                "/api-reference/beta/api/networkaccess-branchsite-get.md",
                "/api-reference/beta/api/directory-list-pendingexternaluserprofile.md",
                "/api-reference/beta/api/networkaccess-enrichedauditlogs-update.md",
                "/api-reference/beta/api/filestoragecontainer-get.md",
                "/api-reference/beta/api/educationassignment-post-gradingcategory.md",
                "/api-reference/beta/api/message-markasjunk.md",
                "/api-reference/beta/api/secretinformationaccessawsresourcefinding-get.md",
                "/api-reference/beta/api/azureassociatedidentities-list-serviceprincipals.md",
                "/api-reference/beta/api/networkaccess-filteringpolicylink-list.md",
                "/api-reference/beta/api/encryptedazurestorageaccountfinding-get.md",
                "/api-reference/beta/api/payloaddetail-get.md",
                "/api-reference/beta/api/worksheet-cell.md",
                "/api-reference/beta/api/gcpauthorizationsystem-list-services.md",
                "/api-reference/beta/api/synchronization-synchronizationjob-post-bulkupload.md",
                "/api-reference/beta/api/serviceprincipal-delete-remotedesktopsecurityconfiguration.md",
                "/api-reference/beta/api/supergcpserviceaccountfinding-get.md",
                "/api-reference/beta/api/networkaccess-branchsite-list-devicelinks.md",
                "/api-reference/beta/api/domain-list-federationconfiguration.md",
                "/api-reference/beta/api/securitytoolawsuseradministratorfinding-get.md",
                "/api-reference/beta/api/educationmodule-post-resources.md",
                "/api-reference/beta/api/azureadauthentication-get.md",
                "/api-reference/beta/api/networkaccess-reports-transactionsummaries.md",
                "/api-reference/beta/api/superserverlessfunctionfinding-aggregatedsummary.md",
                "/api-reference/beta/api/targetdevicegroup-update.md",
                "/api-reference/beta/api/networkaccess-filteringpolicy-post-policyrules.md",
                "/api-reference/beta/api/verticalsection-update.md",
                "/api-reference/beta/api/networkaccess-connectivity-post-branches.md",
                "/api-reference/beta/api/regionalandlanguagesettings-get.md",
                "/api-reference/beta/api/dynamics-create-customerpaymentsjournal.md",
                "/api-reference/beta/api/verticalsection-delete.md",
                "/api-reference/beta/api/approval-list-steps.md",
                "/api-reference/beta/api/approval-list-steps.md",
                "/api-reference/beta/api/simulationreportoverview-get.md",
                "/api-reference/beta/api/privilegeescalationawsresourcefinding-get.md",
                "/api-reference/beta/api/gcpauthorizationsystem-list-resources.md",
                "/api-reference/beta/api/networkaccess-branchsite-update.md",
                "/api-reference/beta/api/security-labelsroot-post-categories.md",
                "/api-reference/beta/api/horizontalsection-delete.md",
                "/api-reference/beta/api/b2cidentityuserflow-userflowidentityproviders-update.md",
                "/api-reference/beta/api/filestoragecontainer-post-permissions.md",
                "/api-reference/beta/api/virtualeventwebinar-get.md",
                "/api-reference/beta/api/deletedchat-undodelete.md",
                "/api-reference/beta/api/security-labelsroot-post-departments.md",
                "/api-reference/beta/api/secretinformationaccessawsrolefinding-get.md",
                "/api-reference/beta/api/networkaccess-filteringpolicylink-delete-policy.md",
                "/api-reference/beta/api/privilegedaccessgroup-post-assignmentschedulerequests.md",
                "/api-reference/beta/api/accessreviewinstance-stopapplydecisions.md",
                "/api-reference/beta/api/unifiedroleassignmentmultiple-update.md",
                "/api-reference/beta/api/unifiedroleassignmentmultiple-update.md",
                "/api-reference/beta/api/security-subcategorytemplate-get.md",
                "/api-reference/beta/api/gcpauthorizationsystemtypeaction-get.md",
                "/api-reference/beta/api/subjectrightsrequest-getfinalreport.md",
                "/api-reference/beta/api/awsuser-get.md",
                "/api-reference/beta/api/serviceactivity-getmetricsformfasigninsuccess.md",
                "/api-reference/beta/api/permissionscreepindexdistribution-get.md",
                "/api-reference/beta/api/security-hostport-get.md",
                "/api-reference/beta/api/externallyaccessibleazureblobcontainerfinding-get.md",
                "/api-reference/beta/api/networkaccess-conditionalaccesssettings-get.md",
                "/api-reference/beta/api/security-labelsroot-list-citations.md",
                "/api-reference/beta/api/appcredentialsigninactivity-get.md",
                "/api-reference/beta/api/security-labelsroot-list-authorities.md",
                "/api-reference/beta/api/inactivegroupfinding-get.md",
                "/api-reference/beta/api/gcpuser-get.md",
                "/api-reference/beta/api/multitenantorganizationidentitysyncpolicytemplate-resettodefaultsettings.md",
                "/api-reference/beta/api/security-host-list-sslcertificates.md",
                "/api-reference/beta/api/filestoragecontainer-list-permissions.md",
                "/api-reference/beta/api/virtualmachinewithawsstoragebucketaccessfinding-get.md",
                "/api-reference/beta/api/dailyuserinsightmetricsroot-list-activeusers.md",
                "/api-reference/beta/api/unifiedrolemanagementalertincident-get.md",
                "/api-reference/beta/api/networkaccess-policylink-list-policy.md",
                "/api-reference/beta/api/onpremisesagentgroup-post.md",
                "/api-reference/beta/api/networkaccess-branchsite-post-devicelinks.md",
                "/api-reference/beta/api/user-get-transitivereports.md",
                "/api-reference/beta/api/awsidentityaccessmanagementkeyusagefinding-get.md",
                "/api-reference/beta/api/security-labelsroot-list-categories.md",
                "",
                "/api-reference/beta/api/filestorage-list-containers.md",
                "/api-reference/beta/api/workbookdocumenttask-get.md",
                "/api-reference/beta/api/networkaccess-remotenetworkhealthstatusevent-list.md",
                "/api-reference/beta/api/networkaccess-branchconnectivityconfiguration-get.md",
                "/api-reference/beta/api/security-threatintelligence-list-sslcertificates.md",
                "/api-reference/beta/api/superuserfinding-aggregatedsummary.md",
                "/api-reference/beta/api/onattributecollectionexternalusersselfservicesignup-post-attributes.md",
                "/api-reference/beta/api/attacksimulationroot-get-includedaccounttarget.md",
                "/api-reference/beta/api/gcpassociatedidentities-list-users.md",
                "/api-reference/beta/api/remotedesktopsecurityconfiguration-list-targetdevicegroups.md",
                "/api-reference/beta/api/approvalstep-get.md",
                "/api-reference/beta/api/approvalstep-get.md",
                "/api-reference/beta/api/awsidentity-get.md",
                "/api-reference/beta/api/networkaccess-forwardingpolicylink-delete.md",
                "/api-reference/beta/api/webpart-delete.md",
                "/api-reference/beta/api/certificateauthorityasentity-get.md",
                "/api-reference/beta/api/inactiveazureserviceprincipalfinding-get.md",
                "/api-reference/beta/api/onpremisesagentgroup-delete.md",
                "/api-reference/beta/api/message-markasnotjunk.md",
                "/api-reference/beta/api/platformcredentialauthenticationmethod-list.md",
                "/api-reference/beta/api/platformcredentialauthenticationmethod-list.md",
                "/api-reference/beta/api/networkaccess-reports-getdestinationsummaries.md",
                "/api-reference/beta/api/networkaccess-filteringpolicy-get.md",
                "/api-reference/beta/api/unifiedrolemanagementalert-list-alertincidents.md",
                "/api-reference/beta/api/security-labelsroot-list-fileplanreferences.md",
                "/api-reference/beta/api/openawssecuritygroupfinding-list-assignedcomputeinstancesdetails.md",
                "/api-reference/beta/api/networkaccess-reports-userreport.md",
                "/api-reference/beta/api/search-qna-update.md",
                "/api-reference/beta/api/awsassociatedidentities-list-users.md",
                "/api-reference/beta/api/security-labelsroot-delete-authorities.md",
                "/api-reference/beta/api/educationclass-post-module.md",
                "/api-reference/beta/api/filestoragecontainer-list-customproperty.md",
                "",
                "/api-reference/beta/api/sitepage-update.md",
                "/api-reference/beta/api/call-senddtmftones.md",
                "/api-reference/beta/api/networkaccess-forwardingpolicy-updatepolicyrules.md",
                "/api-reference/beta/api/networkaccess-reports-devicereport.md",
                "/api-reference/beta/api/gcpserviceaccount-get.md",
                "/api-reference/beta/api/changetrackedentity-stagefordeletion.md",
                "/api-reference/beta/api/changetrackedentity-stagefordeletion.md",
                "/api-reference/beta/api/changetrackedentity-stagefordeletion.md",
                "/api-reference/beta/api/filestoragecontainer-get-drive.md",
                "/api-reference/beta/api/auditlogroot-list-customsecurityattributeaudits.md",
                "/api-reference/beta/api/awsidentityaccessmanagementkeyagefinding-get.md",
                "/api-reference/beta/api/samlorwsfedexternaldomainfederation-update.md",
                "/api-reference/beta/api/orgcontact-retryserviceprovisioning.md",
                "/api-reference/beta/api/privilegedaccessgroupassignmentscheduleinstance-get.md",
                "/api-reference/beta/api/gcpidentity-get.md",
                "/api-reference/beta/api/azureauthorizationsystem-list-resources.md",
                "/api-reference/beta/api/attacksimulationroot-get-excludedaccounttarget.md",
                "/api-reference/beta/api/reportroot-list-serviceprincipalsigninactivities.md",
                "/api-reference/beta/api/windowsupdates-deploymentaudience-updateaudience.md",
                "/api-reference/beta/api/identitygovernance-userprocessingresult-summary.md",
                "/api-reference/beta/api/assignedcomputeinstancedetails-get.md",
                "/api-reference/beta/api/inactiveawsresourcefinding-aggregatedsummary.md",
                "/api-reference/beta/api/security-categorytemplate-delete-subcategories.md",
                "/api-reference/beta/api/applicationtemplate-get.md",
                "/api-reference/beta/api/externaluserprofile-get.md",
                "/api-reference/beta/api/awsauthorizationsystem-list-policies.md",
                "/api-reference/beta/api/azureauthorizationsystem-list.md",
                "/api-reference/beta/api/monthlyuserinsightmetricsroot-list-authentications.md",
                "/api-reference/beta/api/security-labelsroot-post-authorities.md",
                "/api-reference/beta/api/awsuser-list-assumableroles.md",
                "/api-reference/beta/api/educationmodule-get.md",
                "/api-reference/beta/api/inactiveuserfinding-aggregatedsummary.md",
                "/api-reference/beta/api/remotedesktopsecurityconfiguration-update.md",
                "/api-reference/beta/api/profilecardproperty-delete.md",
                "/api-reference/beta/api/networkaccess-forwardingpolicylink-get.md",
                "/api-reference/beta/api/educationassignment-delete-gradingcategory.md",
                "/api-reference/beta/api/azureassociatedidentities-list-managedidentities.md",
                "/api-reference/beta/api/networkaccess-branchsite-delete.md",
                "/api-reference/beta/api/networkaccess-crosstenantaccesssettings-update.md",
                "/api-reference/beta/api/onauthenticationmethodloadstartexternalusersselfservicesignup-post-identityproviders.md",
                "/api-reference/beta/api/rolemanagementalert-list-alertconfigurations.md",
                "/api-reference/beta/api/privilegedaccessgroupassignmentschedulerequest-filterbycurrentuser.md",
                "/api-reference/beta/api/superserverlessfunctionfinding-get.md",
                "/api-reference/beta/api/user-reminderview.md",
                "/api-reference/beta/api/externalconnectors-external-list-authorizationsystems.md",
                "/api-reference/beta/api/certificatebasedapplicationconfiguration-post-trustedcertificateauthorities.md",
                "/api-reference/beta/api/authorizationsystem-get.md",
                "/api-reference/beta/api/educationmodule-pin.md",
                "/api-reference/beta/api/privilegeescalationgcpserviceaccountfinding-get.md",
                "/api-reference/beta/api/multitenantorganizationpartnerconfigurationtemplate-update.md",
                "/api-reference/beta/api/gcprole-get.md",
                "/api-reference/beta/api/authentication-get.md",
                "/api-reference/beta/api/certificateauthoritypath-list-certificatebasedapplicationconfigurations.md",
                "/api-reference/beta/api/overprovisioneduserfinding-get.md",
                "/api-reference/beta/api/dailyuserinsightmetricsroot-list-usercount.md",
                "/api-reference/beta/api/filestoragecontainer-post-customproperty.md",
                "/api-reference/beta/api/horizontalsection-get.md",
                "/api-reference/beta/api/privilegedaccessgroup-post-eligibilityschedulerequests.md",
                "/api-reference/beta/api/privilegedaccessgroupassignmentschedulerequest-cancel.md",
                "/api-reference/beta/api/educationassignment-activate.md",
                "/api-reference/beta/api/monthlyuserinsightmetricsroot-list-signups.md",
                "/api-reference/beta/api/identitygovernance-taskreport-summary.md",
                "/api-reference/beta/api/admindynamics-get.md",
                "/api-reference/beta/api/applicationtemplate-list.md",
                "/api-reference/beta/api/awsassociatedidentities-list-roles.md",
                "/api-reference/beta/api/workbookdocumenttaskchange-get.md",
                "/api-reference/beta/api/horizontalsection-list.md",
                "/api-reference/beta/api/encryptedgcpstoragebucketfinding-get.md",
                "/api-reference/beta/api/certificateauthorityasentity-update.md",
                "/api-reference/beta/api/awsauthorizationsystemresource-get.md",
                "/api-reference/beta/api/networkaccess-policy-list-policyrules.md",
                "/api-reference/beta/api/multitenantorganizationpartnerconfigurationtemplate-get.md",
                "/api-reference/beta/api/reportroot-getrelyingpartydetailedsummary.md",
                "/api-reference/beta/api/directory-delete-pendingexternaluserprofiles.md",
                "/api-reference/beta/api/dailyuserinsightmetricsroot-list-signups.md",
                "/api-reference/beta/api/informationprotectionlabel-evaluateclassificationresults.md",
                "/api-reference/beta/api/security-categorytemplate-get.md",
                "/api-reference/beta/api/unifiedrolemanagementalertconfiguration-update.md",
                "/api-reference/beta/api/dailyuserinsightmetricsroot-list-authentications.md",
                "/api-reference/beta/api/networkaccess-forwardingprofile-get.md",
                "/api-reference/beta/api/networkaccess-filteringrule-update.md",
                "/api-reference/beta/api/accesspackage-delete-accesspackageresourcerolescopes.md",
                "/api-reference/beta/api/inactiveuserfinding-get.md",
                "/api-reference/beta/api/cloudpcreports-getfrontlinereport.md",
                "/api-reference/beta/api/awsauthorizationsystem-list-services.md",
                "/api-reference/beta/api/security-departmenttemplate-get.md",
                "/api-reference/beta/api/security-emailthreatsubmission-review.md",
                "/api-reference/beta/api/networkaccess-reports-crosstenantaccessreport.md",
                "/api-reference/beta/api/multitenantorganizationpartnerconfigurationtemplate-resettodefaultsettings.md",
                "/api-reference/beta/api/networkaccess-networkaccessroot-list-forwardingprofiles.md",
                "/api-reference/beta/api/cloudpcreports-getrawremoteconnectionreports.md",
                "/api-reference/beta/api/gcpauthorizationsystem-list-actions.md",
                "/api-reference/beta/api/scheduledpermissionsrequest-cancelall.md",
                "/api-reference/beta/api/onauthenticationmethodloadstartexternalusersselfservicesignup-delete-identityproviders.md",
                "/api-reference/beta/api/tablerowcollection-itemat.md",
                "/api-reference/beta/api/networkaccess-reports-getcrosstenantsummary.md",
                "/api-reference/beta/api/platformcredentialauthenticationmethod-delete.md",
                "/api-reference/beta/api/platformcredentialauthenticationmethod-delete.md",
                "/api-reference/beta/api/networkaccess-filteringrule-list.md",
                "/api-reference/beta/api/networkaccess-conditionalaccesssettings-update.md",
                "/api-reference/beta/api/networkaccess-filteringprofile-delete-policies.md",
                "/api-reference/beta/api/directory-list-subscriptions.md",
                "/api-reference/beta/api/security-emailthreatsubmissionpolicy-delete.md",
                "/api-reference/beta/api/security-citationtemplate-get.md",
                "/api-reference/beta/api/networkaccess-connectivity-list-branches.md",
                "/api-reference/beta/api/dailyuserinsightmetricsroot-list-summary.md",
                "/api-reference/beta/api/privilegedaccessgroup-list-assignmentschedules.md",
                "/api-reference/beta/api/privilegeescalationawsrolefinding-get.md",
                "/api-reference/beta/api/peopleadminsettings-post-profilecardproperties.md",
                "/api-reference/beta/api/educationgradingcategory-update.md",
                "/api-reference/beta/api/awsauthorizationsystem-list-resources.md",
                "/api-reference/beta/api/monthlyuserinsightmetricsroot-list-mfacompletions.md",
                "/api-reference/beta/api/cloudpcreports-getactionstatusreports.md",
                "/api-reference/beta/api/unifiedrolemanagementalertconfiguration-get.md",
                "/api-reference/beta/api/learningcourseactivity-update.md",
                "/api-reference/beta/api/remotedesktopsecurityconfiguration-get.md",
                "/api-reference/beta/api/remotedesktopsecurityconfiguration-post-targetdevicegroups.md",
                "/api-reference/beta/api/networkaccess-policyrule-get.md",
                "/api-reference/beta/api/azureauthorizationsystemtypeaction-get.md",
                "/api-reference/beta/api/security-categorytemplate-list-subcategories.md",
                "/api-reference/beta/api/callrecords-cloudcommunications-list-callrecords.md",
                "/api-reference/beta/api/certificateauthoritypath-post-certificatebasedapplicationconfigurations.md",
                "/api-reference/beta/api/managedtenants-managedtenant-list-auditevents.md",
                "/api-reference/beta/api/orgcontact-get-transitivereports.md",
                "/api-reference/beta/api/permissionsmanagement-list-permissionsrequestchanges.md",
                "/api-reference/beta/api/educationmoduleresource-delete.md",
                "/api-reference/beta/api/privilegedaccessgroupassignmentschedule-get.md",
                "/api-reference/beta/api/networkaccess-crosstenantaccesssettings-get.md",
                "/api-reference/beta/api/educationmodule-delete.md",
                "/api-reference/beta/api/remotedesktopsecurityconfiguration-delete-targetdevicegroups.md",
                "/api-reference/beta/api/authorizationsystemtypeservice-get.md",
                "/api-reference/beta/api/targetdevicegroup-get.md",
                "/api-reference/beta/api/cloudpc-validatebulkresize.md",
                "/api-reference/beta/api/serviceprincipal-list-permissiongrantpreapprovalpolicies.md",
                "/api-reference/beta/api/sitepage-create-webpart.md",
                "/api-reference/beta/api/plannerplan-movetocontainer.md",
                "/api-reference/beta/api/m365appsinstallationoptions-update.md",
                "/api-reference/beta/api/networkaccess-filteringrule-get.md",
                "/api-reference/beta/api/tenantrelationship-put-multitenantorganization.md",
                "/api-reference/beta/api/educationmoduleresource-get.md",
                "/api-reference/beta/api/fido2authenticationmethod-get.md",
                "/api-reference/beta/api/fido2authenticationmethod-get.md",
                "/api-reference/beta/api/security-host-list-parenthostpairs.md",
                "/api-reference/beta/api/dynamics-create-taxarea.md",
                "/api-reference/beta/api/sitepage-getwebpartsbyposition.md",
                "/api-reference/beta/api/approvalstep-update.md",
                "/api-reference/beta/api/approvalstep-update.md",
                "/api-reference/beta/api/reportroot-list-appcredentialsigninactivities.md",
                "/api-reference/beta/api/awsassociatedidentities-list-all.md",
                "/api-reference/beta/api/pendingexternaluserprofile-get.md",
                "/api-reference/beta/api/externaluserprofile-update.md",
                "/api-reference/beta/api/security-labelsroot-delete-categories.md",
                "/api-reference/beta/api/monthlyuserinsightmetricsroot-list-activeusersbreakdown.md",
                "/api-reference/beta/api/networkaccess-forwardingoptions-update.md",
                "/api-reference/beta/api/m365appsinstallationoptions-get.md",
                "/api-reference/beta/api/devicelocalcredentialinfo-get.md",
                "/api-reference/beta/api/cloudpcreports-getconnectionqualityreports.md",
                "/api-reference/beta/api/supergcpserviceaccountfinding-aggregatedsummary.md",
                "/api-reference/beta/api/workbookrangeview-itemat.md",
                "/api-reference/beta/api/security-whoisrecord-list-history.md",
                "/api-reference/beta/api/privilegedaccessgroupeligibilityschedulerequest-get.md",
                "/api-reference/beta/api/security-labelsroot-post-citations.md",
                "/api-reference/beta/api/security-host-list-childhostpairs.md",
                "/api-reference/beta/api/security-labelsroot-delete-departments.md",
                "/api-reference/beta/api/networkaccess-devicelink-get.md",
                "/api-reference/beta/api/privilegedaccessgroup-list-assignmentschedulerequests.md",
                "/api-reference/beta/api/security-intelligenceprofileindicator-get.md",
                "/api-reference/beta/api/networkaccess-forwardingoptions-get.md",
                "/api-reference/beta/api/filestoragecontainer-delete-permissions.md",
                "/api-reference/beta/api/gcpauthorizationsystem-list-roles.md",
                "/api-reference/beta/api/azureidentity-get.md",
                "/api-reference/beta/api/identitygovernance-userprocessingresult-get.md",
                "/api-reference/beta/api/azureauthorizationsystem-list-roledefinitions.md",
                "/api-reference/beta/api/secretinformationaccessawsuserfinding-get.md",
                "/api-reference/beta/api/cloudpc-getfrontlinecloudpcaccessstate.md",
                "/api-reference/beta/api/awsrole-get.md",
                "/api-reference/beta/api/networkaccess-devicelink-update.md",
                "/api-reference/beta/api/call-reject.md",
                "/api-reference/beta/api/serviceprincipal-delete-permissiongrantpreapprovalpolicies.md",
                "/api-reference/beta/api/networkaccess-reports-destinationreport.md",
                "/api-reference/beta/api/networkaccess-branchsite-delete-devicelinks.md",
                "/api-reference/beta/api/networkaccess-reports-entitiessummaries.md",
                "/api-reference/beta/api/monthlyuserinsightmetricsroot-list-activeusers.md",
                "/api-reference/beta/api/filestoragecontainer-update.md",
                "/api-reference/beta/api/callrecording-delta.md",
                "/api-reference/beta/api/webpart-list.md",
                "/api-reference/beta/api/networkaccess-settings-list-enrichedauditlogs.md",
                "/api-reference/beta/api/superawsrolefinding-get.md",
                "/api-reference/beta/api/azuremanagedidentity-get.md",
                "/api-reference/beta/api/virtualeventwebinar-getbyuseridandrole.md",
                "/api-reference/beta/api/security-hostsslcertificate-get.md",
                "/api-reference/beta/api/unifiedrolemanagementalertincident-remediate.md",
                "/api-reference/beta/api/security-host-list-ports.md",
                "/api-reference/beta/api/privilegedaccessgroup-list-eligibilityschedules.md",
                "/api-reference/beta/api/overprovisionedazureserviceprincipalfinding-get.md",
                "/api-reference/beta/api/filestoragecontainer-activate.md",
                "/api-reference/beta/api/security-fileplanreferencetemplate-get.md",
                "/api-reference/beta/api/policyroot-list-permissiongrantpreapprovalpolicies.md",
                "/api-reference/beta/api/networkaccess-filteringprofile-update.md",
                "/api-reference/beta/api/encryptedawsstoragebucketfinding-get.md",
                "/api-reference/beta/api/onattributecollectionsubmitcustomextension-update.md",
                "/api-reference/beta/api/superazureserviceprincipalfinding-get.md",
                "/api-reference/beta/api/adminappsandservices-update.md",
                "/api-reference/beta/api/unenforcedmfaawsuserfinding-get.md",
                "/api-reference/beta/api/educationclass-list-modules.md",
                "/api-reference/beta/api/team-gettimesoff.md",
                "/api-reference/beta/api/agreementfile-list-localizations.md",
                "/api-reference/beta/api/superuserfinding-get.md",
                "/api-reference/beta/api/workbookdocumenttask-list-changes.md",
                "/api-reference/beta/api/horizontalsectioncolumn-list.md",
                "/api-reference/beta/api/externallyaccessiblegcpstoragebucketfinding-get.md",
                "/api-reference/beta/api/superawsresourcefinding-get.md",
                "/api-reference/beta/api/networkaccess-forwardingprofile-update.md",
                "/api-reference/beta/api/certificateauthorityasentity-delete.md",
                "/api-reference/beta/api/opennetworkazuresecuritygroupfinding-get.md",
                "/api-reference/beta/api/privilegedaccessgroupassignmentschedulerequest-get.md",
                "/api-reference/beta/api/networkaccess-filteringprofile-list.md",
                "/api-reference/beta/api/virtualeventwebinar-getbyuserrole.md",
                "/api-reference/beta/api/callrecords-callrecord-list-participants_v2.md",
                "/api-reference/beta/api/secretinformationaccessawsserverlessfunctionfinding-get.md",
                "/api-reference/beta/api/awsauthorizationsystemtypeaction-get.md",
                "/api-reference/beta/api/hardwareoathauthenticationmethodconfiguration-delete.md",
                "/api-reference/beta/api/educationmoduleresource-update.md",
                "/api-reference/beta/api/networkaccess-policyrule-update.md",
                "/api-reference/beta/api/overprovisionedawsrolefinding-get.md",
                "/api-reference/beta/api/horizontalsectioncolumn-get.md",
                "/api-reference/beta/api/inactivegcpserviceaccountfinding-aggregatedsummary.md",
                "/api-reference/beta/api/filestoragecontainer-delete-customproperty.md",
                "/api-reference/beta/api/privilegedaccessgroupeligibilityschedule-get.md",
                "/api-reference/beta/api/dynamics-create-journalline.md",
                "/api-reference/beta/api/certificatebasedapplicationconfiguration-get.md",
                "/api-reference/beta/api/unifiedrolemanagementalertdefinition-get.md",
                "/api-reference/beta/api/educationmodule-list-resources.md",
                "/api-reference/beta/api/permissionsanalytics-list-permissionscreepindexdistributions.md",
                "/api-reference/beta/api/networkaccess-forwardingpolicylink-update.md",
                "/api-reference/beta/api/security-whoisrecord-get.md",
                "/api-reference/beta/api/reports-getgrouparchivedprintjobs.md",
                "/api-reference/beta/api/virtualendpoint-retrievescopedpermissions.md",
                "/api-reference/beta/api/webpart-update.md",
                "/api-reference/beta/api/chat-delete.md",
                "/api-reference/beta/api/privilegedaccessgroupassignmentscheduleinstance-filterbycurrentuser.md",
                "/api-reference/beta/api/reports-getuserarchivedprintjobs.md",
                "/api-reference/beta/api/addlargegalleryviewoperation-get.md",
                "/api-reference/beta/api/adminforms-get.md",
                "/api-reference/beta/api/networkaccess-forwardingpolicy-get.md",
                "/api-reference/beta/api/educationassignment-deactivate.md",
                "/api-reference/beta/api/privilegeescalationuserfinding-get.md",
                "/api-reference/beta/api/directory-list-externaluserprofiles.md",
                "/api-reference/beta/api/serviceactivity-getmetricsforconditionalaccesscompliantdevicessigninsuccess.md",
                "/api-reference/beta/api/workbookworksheet-list-tasks.md",
                "/api-reference/beta/api/reports-getprinterarchivedprintjobs.md",
                "/api-reference/beta/api/longrunningoperation-get.md",
                "/api-reference/beta/api/inactiveserverlessfunctionfinding-aggregatedsummary.md",
                "/api-reference/beta/api/filestorage-delete-containers.md",
                "/api-reference/beta/api/educationsubmission-excuse.md",
                "/api-reference/beta/api/deletedchat-get.md",
                "/api-reference/beta/api/virtualeventsession-get.md",
                "/api-reference/beta/api/adminappsandservices-get.md",
                "/api-reference/beta/api/customsecurityattributeaudit-get.md",
                "/api-reference/beta/api/networkaccess-branchsite-post-forwardingprofiles.md",
                "/api-reference/beta/api/sitepage-post-verticalsection.md",
                "/api-reference/beta/api/security-categorytemplate-post-subcategories.md",
                "/api-reference/beta/api/accesspackagesubject-update.md",
                "/api-reference/beta/api/directory-delete-externaluserprofiles.md",
                "/api-reference/beta/api/inactiveawsrolefinding-aggregatedsummary.md",
                "/api-reference/beta/api/securitytoolawsroleadministratorfinding-get.md",
                "/api-reference/beta/api/inactiveawsresourcefinding-get.md",
                "/api-reference/beta/api/networkaccess-reports-webcategoryreport.md",
                "/api-reference/beta/api/gcpauthorizationsystem-list.md",
                "/api-reference/beta/api/inactiveazureserviceprincipalfinding-aggregatedsummary.md",
                "/api-reference/beta/api/onattributecollectionstartcustomextension-update.md",
                "/api-reference/beta/api/educationmodule-publish.md",
                "/api-reference/beta/api/azureserviceprincipal-get.md",
                "/api-reference/beta/api/rangeborder-get.md",
                "/api-reference/beta/api/certificatebasedapplicationconfiguration-delete.md",
                "/api-reference/beta/api/channel-doesuserhaveaccess.md",
                "/api-reference/beta/api/virtualendpoint-list-frontlineserviceplans.md",
                "/api-reference/beta/api/onpremisesagentgroup-update.md",
                "/api-reference/beta/api/multitenantorganizationidentitysyncpolicytemplate-get.md",
                "/api-reference/beta/api/privilegedaccessgroupeligibilityscheduleinstance-filterbycurrentuser.md",
                "/api-reference/beta/api/filestoragecontainer-update-permissions.md",
                "/api-reference/beta/api/educationmodule-unpin.md",
                "/api-reference/beta/api/accesspackageassignment-additionalaccess.md",
                "/api-reference/beta/api/sitepage-publish.md",
                "/api-reference/beta/api/accesspackagesubject-get.md",
                "/api-reference/beta/api/sitepage-get.md",
                "/api-reference/beta/api/certificatebasedapplicationconfiguration-update.md",
                "/api-reference/beta/api/samlorwsfedexternaldomainfederation-post.md",
                "/api-reference/beta/api/regionalandlanguagesettings-update.md",
                "/api-reference/beta/api/gcpassociatedidentities-list-serviceaccounts.md",
                "/api-reference/beta/api/networkaccess-tenantstatus-get.md",
                "/api-reference/beta/api/security-host-list-subdomains.md",
                "/api-reference/beta/api/authentication-update.md",
                "/api-reference/beta/api/monthlyuserinsightmetricsroot-list-summary.md",
                "/api-reference/beta/api/networkaccess-filteringrule-delete.md",
                "/api-reference/beta/api/permissiongrantpreapprovalpolicy-delete.md",
                "/api-reference/beta/api/privilegedaccessgroupeligibilityschedulerequest-filterbycurrentuser.md",
                "/api-reference/beta/api/gcpauthorizationsystemresource-get.md",
                "/api-reference/beta/api/networkaccess-networkaccessroot-list-forwardingpolicies.md",
                "/api-reference/beta/api/serviceactivity-getmetricsformfasigninfailure.md",
                "/api-reference/beta/api/directory-post-pendingexternaluserprofile.md",
                "/api-reference/beta/api/networkaccess-networkaccessroot-onboard.md",
                "/api-reference/beta/api/security-sslcertificate-get.md",
                "/api-reference/beta/api/security-labelsroot-post-fileplanreferences.md",
                "/api-reference/beta/api/serviceprincipal-post-permissiongrantpreapprovalpolicies.md",
                "/api-reference/beta/api/externallyaccessibleawsstoragebucketfinding-get.md",
                "/api-reference/beta/api/certificatebasedapplicationconfiguration-list-trustedcertificateauthorities.md",
                "/api-reference/beta/api/educationmodule-update.md",
                "/api-reference/beta/api/permissiongrantpreapprovalpolicy-update.md",
                "/api-reference/beta/api/verticalsection-get.md",
                "/api-reference/beta/api/identitygovernance-workflow-list-userprocessingresults.md",
                "/api-reference/beta/api/inactivegcpserviceaccountfinding-get.md",
                "/api-reference/beta/api/ediscovery-unifiedgroupsource-delete.md",
                "/api-reference/beta/api/cloudpcusersetting-update.md",
                "/api-reference/beta/api/adminforms-update.md",
                "/api-reference/beta/api/virtualevent-list-sessions.md",
                "/api-reference/beta/api/usersimulationdetails-list.md",
                "/api-reference/beta/api/serviceactivity-getmetricsforconditionalaccessmanageddevicessigninsuccess.md",
                "/api-reference/beta/api/directory-list-devicelocalcredentials.md",
                "/api-reference/beta/api/security-labelsroot-delete-fileplanreferences.md",
                "/api-reference/beta/api/networkaccess-branchsite-list-forwardingprofiles.md",
                "/api-reference/beta/api/serviceprincipal-post-remotedesktopsecurityconfiguration.md",
                "/api-reference/beta/api/networkaccess-filteringprofile-get.md",
                "/api-reference/beta/api/educationmodule-setupresourcesfolder.md",
                "/api-reference/beta/api/azureuser-get.md",
                "/api-reference/beta/api/networkaccess-forwardingprofile-list-policies.md",
                "/api-reference/beta/api/policyroot-post-permissiongrantpreapprovalpolicies.md",
                "/api-reference/beta/api/webpart-getposition.md",
                "/api-reference/beta/api/cloudpcreports-getinaccessiblecloudpcreports.md",
                "/api-reference/beta/api/rolemanagementalert-list-alerts.md",
                "/api-reference/beta/api/azureauthorizationsystemresource-get.md",
                "/api-reference/beta/api/azureroledefinition-get.md",
                "/api-reference/beta/api/workbookrange-resizedrange.md",
                "/api-reference/beta/api/horizontalsection-update.md",
                "/api-reference/beta/api/privilegedaccessgroup-list-assignmentscheduleinstances.md",
                "/api-reference/beta/api/webpart-get.md",
                "/api-reference/beta/api/privilegedaccessgroupeligibilityscheduleinstance-get.md",
                "/api-reference/beta/api/sitepage-post-horizontalsection.md",
                "/api-reference/beta/api/team-getopenshifts.md",
                "/api-reference/beta/api/networkaccess-filteringpolicylink-get.md",
                "/api-reference/beta/api/virtualeventregistration-get.md",
                "/api-reference/beta/api/networkaccess-logs-list-traffic.md",
                "/api-reference/beta/api/networkaccess-filteringpolicylink-update.md",
                "/api-reference/beta/api/networkaccess-networkaccessroot-list-filteringpolicies.md",
                "/api-reference/beta/api/security-labelsroot-list-departments.md",
                "/api-reference/beta/api/multitenantorganization-update.md",
                "/api-reference/beta/api/serviceprincipalsigninactivity-get.md",
                "/api-reference/beta/api/unifiedrolemanagementalert-refresh.md",
                "/api-reference/beta/api/b2xidentityuserflow-delete-userflowidentityproviders.md",
                "/api-reference/beta/api/partners-billing-billedusage-export.md",
                "/api-reference/beta/api/privilegedaccessgroup-list-eligibilityscheduleinstances.md",
                "/api-reference/beta/api/overprovisionedgcpserviceaccountfinding-get.md",
                "/api-reference/beta/api/overprovisionedawsresourcefinding-get.md",
                "/api-reference/beta/api/serviceactivity-getmetricsforsamlsigninsuccess.md",
                "/api-reference/beta/api/driveitem-permanentdelete.md",
                "/api-reference/beta/api/permissiongrantpreapprovalpolicy-get.md",
                "/api-reference/beta/api/ediscovery-custodian-post-unifiedgroupsources.md",
                "/api-reference/beta/api/superawsrolefinding-aggregatedsummary.md",
                "/api-reference/beta/api/dailyuserinsightmetricsroot-list-mfacompletions.md",
                "/api-reference/beta/api/unifiedrolemanagementalert-update.md",
                "/api-reference/beta/api/virtualeventregistration-list.md",
                "/api-reference/beta/api/virtualeventsroot-list-webinars.md",
                "/api-reference/beta/api/inactiveserverlessfunctionfinding-get.md",
                "/api-reference/beta/api/cloudpcreports-getcloudpcrecommendationreports.md",
                "/api-reference/beta/api/dailyuserinsightmetricsroot-list-activeusersbreakdown.md",
                "/api-reference/beta/api/cloudpc-bulkresize.md",
                "/api-reference/beta/api/awsexternalsystemaccessfinding-get.md",
                "/api-reference/beta/api/privilegedaccessgroupeligibilityschedule-filterbycurrentuser.md",
                "/api-reference/beta/api/azureassociatedidentities-list-users.md",
                "/api-reference/beta/api/unifiedrolemanagementalert-get.md",
                "/api-reference/beta/api/superawsresourcefinding-aggregatedsummary.md",
                "/api-reference/beta/api/team-getshifts.md",
                "/api-reference/beta/api/unifiedroledefinition-assignedprincipals.md",
                "/api-reference/beta/api/security-subdomain-get.md",
                "/api-reference/beta/api/filestoragecontainer-update-customproperty.md",
                "/api-reference/beta/api/admintodo-get.md",
                "/api-reference/beta/api/dynamics-create-journal.md",
                "/api-reference/beta/api/group-retryserviceprovisioning.md",
                "/api-reference/beta/api/gcpassociatedidentities-list-all.md",
                "/api-reference/beta/api/azureauthorizationsystem-list-actions.md",
                "/api-reference/beta/api/awsexternalsystemaccessrolefinding-get.md",
                "/api-reference/beta/api/overprovisionedserverlessfunctionfinding-get.md",
                "/api-reference/beta/api/cloudpcfrontlineserviceplan-get.md",
                "/api-reference/beta/api/rolemanagementalert-list-alertdefinitions.md",
                "/api-reference/beta/api/partners-billing-billedreconciliation-export.md",
                "/api-reference/beta/api/filestoragecontainer-post.md",
                "/api-reference/beta/api/itemactivity-getbyinterval.md",
                "/api-reference/beta/api/awsauthorizationsystem-list-actions.md",
                "/api-reference/beta/api/permissionsrequestchange-get.md",
                "/api-reference/beta/api/networkaccess-reports-getdeviceusagesummary.md",
                "/api-reference/beta/api/security-threatintelligence-list-whoisrecords.md",
                "/api-reference/beta/api/dynamics-create-customerpayment.md",
                "/api-reference/beta/api/multitenantorganizationidentitysyncpolicytemplate-update.md",
                "/api-reference/beta/api/profilecardproperty-update.md",
                "/api-reference/beta/api/platformcredentialauthenticationmethod-get.md",
                "/api-reference/beta/api/platformcredentialauthenticationmethod-get.md",
                "/api-reference/beta/api/calltranscript-delta.md",
                "/api-reference/beta/api/privilegedaccessgroupeligibilityschedulerequest-cancel.md",
                "/api-reference/beta/api/accesspackageassignmentrequest-resume.md",
                "/api-reference/beta/api/monthlyuserinsightmetricsroot-list-requests.md",
                "/api-reference/beta/api/security-whoishistoryrecord-get.md",
                "/api-reference/beta/api/azureassociatedidentities-list-all.md",
                "/api-reference/beta/api/security-hostpair-get.md",
                "/api-reference/beta/api/security-authoritytemplate-get.md",
                "/api-reference/beta/api/privilegedaccessgroup-list-eligibilityschedulerequests.md",
                "/api-reference/beta/api/security-host-list-hostpairs.md",
                "/api-reference/beta/api/companysubscription-get.md",
                "/api-reference/beta/api/onattributecollectionexternalusersselfservicesignup-delete-attributes.md",
                "/api-reference/beta/api/rangeborder-update.md",
                "/api-reference/beta/api/recyclebin-list-items.md",
                "/api-reference/beta/api/networkaccess-filteringpolicylink-delete.md",
                "/api-reference/beta/api/networkaccess-filteringrule-post.md",
                "/api-reference/beta/api/profilecardproperty-get.md"
            };

            List<(string requestUrl, string fileName, string scopeType, string leastPrivilegedPermissions, string higherPrivilegedPermissions)> allPermissions = [];

            foreach (var docFile in docFiles)
            {
                if (!listOfFiles.Contains(docFile.DisplayName))
                    continue;
                var originalFileContents = await File.ReadAllLinesAsync(docFile.FullPath);
                var parseStatus = PermissionsInsertionState.FindPermissionsHeader;
                int foundPermissionTablesOrBlocks = 0, foundHttpRequestBlocks = 0;
                bool finishedParsing = false, isBootstrapped = false, ignorePermissionTableUpdate = false;
                int insertionStartLine = -1, insertionEndLine = -1, httpRequestStartLine = -1, httpRequestEndLine = -1,
                    boilerplateStartLine = -1, boilerplateEndLine = -1, permissionsHeaderIndex = -1;
                string includeLine = "";


                for (var currentIndex = 0; currentIndex < originalFileContents.Length && !finishedParsing; currentIndex++)
                {
                    var currentLine = originalFileContents[currentIndex].Trim();
                    switch (parseStatus)
                    {
                        case PermissionsInsertionState.FindPermissionsHeader:
                            if (currentLine.Equals("## Permissions", StringComparison.OrdinalIgnoreCase) || currentLine.Equals("## Prerequisites", StringComparison.OrdinalIgnoreCase))
                            {
                                permissionsHeaderIndex = currentIndex;
                                parseStatus = PermissionsInsertionState.FindInsertionStartLine;
                            }
                            break;
                        case PermissionsInsertionState.FindInsertionStartLine:
                            if (currentLine.Contains("blockType", StringComparison.OrdinalIgnoreCase) && currentLine.Contains("\"ignored\""))
                                ignorePermissionTableUpdate = true;

                            if (currentLine.Contains("[!INCLUDE [permissions-table](")) // bootstrapping already took place
                            {
                                includeLine = currentLine;
                                foundPermissionTablesOrBlocks++;
                                if (ignorePermissionTableUpdate)
                                {
                                    FancyConsole.WriteLine(ConsoleColor.Yellow, $"Skipping update of permissions table ({foundPermissionTablesOrBlocks}) in {docFile.DisplayName}");
                                    parseStatus = PermissionsInsertionState.FindNextPermissionBlock;
                                    break;
                                }

                                isBootstrapped = true;
                                if (!options.BootstrappingOnly)
                                {
                                    // find the permissions block start line
                                    for (var i = currentIndex; i > 0; i--)
                                    {
                                        if (originalFileContents[i].Contains("<!-- {") || originalFileContents[i].Contains("<!--{"))
                                        {
                                            insertionStartLine = i;
                                            insertionEndLine = currentIndex; // [!INCLUDE [permissions-table]... is the end of the insertion block
                                            parseStatus = PermissionsInsertionState.FindHttpRequestHeading;
                                            break;
                                        }
                                    }
                                }
                            }
                            else if (currentLine.Contains('|') && currentLine.Trim().Contains("Permission type", StringComparison.OrdinalIgnoreCase)) // found the permissions table
                            {
                                foundPermissionTablesOrBlocks++;
                                if (ignorePermissionTableUpdate)
                                {
                                    FancyConsole.WriteLine(ConsoleColor.Yellow, $"Skipping update of permissions table ({foundPermissionTablesOrBlocks}) in {docFile.DisplayName}");
                                    parseStatus = PermissionsInsertionState.FindNextPermissionBlock;
                                    break;
                                }

                                insertionStartLine = currentIndex;
                                parseStatus = PermissionsInsertionState.FindInsertionEndLine;

                                // Find position to add boileplate text
                                if (foundPermissionTablesOrBlocks == 1)
                                {
                                    boilerplateStartLine = permissionsHeaderIndex;
                                    for (int index = permissionsHeaderIndex + 1; index < currentIndex; index++)
                                    {
                                        // if the line is not empty and is not a sub header, this is the boilerplate start line
                                       if (!string.IsNullOrWhiteSpace(originalFileContents[index]) && !originalFileContents[index].StartsWith('#'))
                                       {
                                            if (boilerplateStartLine == permissionsHeaderIndex)
                                                boilerplateStartLine = index;
                                            boilerplateEndLine = index;
                                       }
                                    }
                                }
                            }
                            break;
                        case PermissionsInsertionState.FindInsertionEndLine: // if we are here, we need to find the end of the permissions table
                            int numberOfRows = 1;
                            for (int index = currentIndex; index < originalFileContents.Length; index++)
                            {
                                if (originalFileContents[index].Contains('|'))
                                {
                                    numberOfRows++;
                                    if (numberOfRows > 5)
                                    {
                                        FancyConsole.WriteLine(ConsoleColor.Yellow, $"Permissions table ({foundPermissionTablesOrBlocks}) in {docFile.DisplayName} was not updated because extra rows were found");
                                        parseStatus = PermissionsInsertionState.FindNextPermissionBlock;
                                        break;
                                    }

                                    if (originalFileContents[index].Contains("<sup>") || originalFileContents[index].Contains("*"))
                                    {
                                        FancyConsole.WriteLine(ConsoleColor.Yellow, $"Permissions table ({foundPermissionTablesOrBlocks}) in {docFile.DisplayName} was not updated because an astrerisk or superscript was found");
                                        parseStatus = PermissionsInsertionState.FindNextPermissionBlock;
                                        break;
                                    }
                                }
                                else
                                {
                                    currentIndex = index - 1;
                                    insertionEndLine = currentIndex;
                                    parseStatus = options.BootstrappingOnly
                                        ? PermissionsInsertionState.InsertPermissionBlock
                                        : PermissionsInsertionState.FindHttpRequestHeading;
                                    break;
                                }
                            }
                            break;
                        case PermissionsInsertionState.FindHttpRequestHeading:
                            if (currentLine.Trim().Equals("## HTTP request", StringComparison.OrdinalIgnoreCase) || foundHttpRequestBlocks > 0)
                                parseStatus = PermissionsInsertionState.FindHttpRequestStartLine;
                            break;
                        case PermissionsInsertionState.FindHttpRequestStartLine:
                            if (currentLine.Trim().StartsWith("## ") && !currentLine.Trim().Equals("## HTTP request", StringComparison.OrdinalIgnoreCase)) // if we get to another header 2, break
                            {
                                if (!options.BootstrappingOnly)
                                {
                                    FancyConsole.WriteLine(ConsoleColor.Yellow, $"The number of permission tables does not match the HTTP request blocks in {docFile.DisplayName}");
                                    finishedParsing = true;
                                }
                                parseStatus = PermissionsInsertionState.InsertPermissionBlock;
                                break;
                            }

                            if (currentLine.Contains("```"))
                            {
                                foundHttpRequestBlocks++;
                                httpRequestStartLine = currentIndex;
                                parseStatus = PermissionsInsertionState.FindHttpRequestEndLine;
                            }
                            break;
                        case PermissionsInsertionState.FindHttpRequestEndLine:
                            if (currentLine.Contains("```"))
                            {
                                if (foundHttpRequestBlocks == foundPermissionTablesOrBlocks)
                                {
                                    httpRequestEndLine = currentIndex;
                                    parseStatus = PermissionsInsertionState.InsertPermissionBlock;
                                }
                                else
                                {
                                    parseStatus = PermissionsInsertionState.FindHttpRequestStartLine;
                                }
                            }
                            break;
                        case PermissionsInsertionState.InsertPermissionBlock:
                            var permissionsFileContents = string.Empty;
                            if (!isBootstrapped)
                            {
                                var existingPermissionsTable = originalFileContents.Skip(insertionStartLine + 2).Take(insertionEndLine - insertionStartLine - 1);
                                permissionsFileContents =  $"{ConvertToThreeColumnPermissionsTable(existingPermissionsTable)}";
                            }

                            if (httpRequestStartLine == -1)
                            {
                                finishedParsing = true;
                                break;
                            }

                            var httpRequests = new List<string>(originalFileContents.Skip(httpRequestStartLine + 1).Take(httpRequestEndLine - httpRequestStartLine - 1));

                            // create folder and file names
                            var permissionsFileRelativePath = Path.Combine("includes", "permissions");
                            var indOf = includeLine.IndexOf("../includes/permissions/") + 24;
                            var indOfEnd = includeLine.IndexOf(".md");
                            var permissionsFileName = includeLine.Substring(indOf, indOfEnd - indOf + 3);

                            // write permissions to separate Markdown file
                            var permissionsDirectory = Path.Combine(Directory.GetParent(Path.GetDirectoryName(docFile.FullPath)).FullName, permissionsFileRelativePath);
                            var permissionsMarkdownFilePath = Path.Combine(permissionsDirectory, permissionsFileName);

                            if (string.IsNullOrEmpty(permissionsFileContents))
                            {
                                permissionsFileContents = await File.ReadAllTextAsync(permissionsMarkdownFilePath);
                            }
                            var foundPermissions = GetPermissionsFromNewTable(permissionsFileContents);
                            foreach (var httpRequest in httpRequests)
                            {
                                if (string.IsNullOrWhiteSpace(httpRequest))
                                    continue;

                                foreach (var (scopeType, leastPrivilegePermissions, higherPrivilegedPermissions) in foundPermissions)
                                {
                                    var existingPermissions = allPermissions.Where(x => x.scopeType == scopeType && x.requestUrl == httpRequest).FirstOrDefault();
                                    if (existingPermissions != default)
                                    {
                                        existingPermissions.leastPrivilegedPermissions = string.IsNullOrEmpty(existingPermissions.leastPrivilegedPermissions) && !string.IsNullOrEmpty(leastPrivilegePermissions)
                                            ? leastPrivilegePermissions
                                            : $"{existingPermissions.leastPrivilegedPermissions}, {leastPrivilegePermissions}";

                                        existingPermissions.higherPrivilegedPermissions = string.IsNullOrEmpty(existingPermissions.higherPrivilegedPermissions) && !string.IsNullOrEmpty(higherPrivilegedPermissions)
                                            ? higherPrivilegedPermissions
                                            : $"{existingPermissions.higherPrivilegedPermissions}, {higherPrivilegedPermissions}";
                                    }
                                    else
                                    {
                                        allPermissions.Add((httpRequest, docFile.DisplayName, scopeType, leastPrivilegePermissions, higherPrivilegedPermissions));
                                    }
                                }
                            }
                            parseStatus = PermissionsInsertionState.FindNextPermissionBlock;
                            break;
                        case PermissionsInsertionState.FindNextPermissionBlock:
                            if (!ignorePermissionTableUpdate)
                            {
                                foundHttpRequestBlocks = 0;
                                currentIndex = insertionStartLine + 2;
                                originalFileContents = await File.ReadAllLinesAsync(docFile.FullPath);
                                insertionStartLine = insertionEndLine = httpRequestStartLine = httpRequestEndLine = -1;
                            }
                            isBootstrapped = false;
                            ignorePermissionTableUpdate = false;
                            parseStatus = PermissionsInsertionState.FindInsertionStartLine;
                            break;
                        default:
                            break;
                    }
                }

                if (foundPermissionTablesOrBlocks == 0)
                    FancyConsole.WriteLine(ConsoleColor.Yellow, $"Could not locate permissions table for {docFile.DisplayName}");
            }

            //var filePath = "C:\\Users\\miachien\\Downloads\\file.txt";
            //using StreamWriter writer = new(filePath);
            foreach (var (requestUrl, fileName, scopeType, leastPrivilegedPermissions, higherPrivilegedPermissions) in allPermissions)
            {
                // writer.WriteLine($"{requestUrl},{scopeType},{leastPrivilegedPermissions},{higherPrivilegedPermissions}");
                Console.WriteLine($"{requestUrl},{fileName},{scopeType},{leastPrivilegedPermissions},{higherPrivilegedPermissions}");
            }

            return true;
        }

        private static HashSet<string> PermissionKeywordsToIgnore = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "None",
            "None.",
            "Not supported.",
            "Not supported",
            "Not available.",
            "Not available",
            "Not applicable",
            "Not applicable."
        };

        private static string ConvertToThreeColumnPermissionsTable(IEnumerable<string> tableRows)
        {
            var tableString = new StringBuilder("|Permission type|Least privileged permissions|Higher privileged permissions|");
            tableString.Append("\r\n|:---|:---|:---|");
            foreach (string row in tableRows)
            {
                string[] cells = Regex.Split(row.Trim(), @"\s*\|\s*").Where(static x => !string.IsNullOrWhiteSpace(x)).ToArray();
                var allPermissions = cells[1].Trim().Split(',', StringSplitOptions.TrimEntries)
                    .Where(x => !string.IsNullOrWhiteSpace(x) && !PermissionKeywordsToIgnore.Contains(x))
                    .ToList();

                var permissionType = cells[0];
                var leastPrivilegePermission = allPermissions.Any() ? allPermissions.First().Trim() : "Not supported.";
                var higherPrivilegePermissions = !allPermissions.Any()
                    ? "Not supported."
                    : allPermissions.Count() == 1
                        ? "Not available."
                        : string.Join(", ", allPermissions.Skip(1).Select(x => x.Trim()).ToList());
                tableString.Append($"\r\n|{permissionType}|{leastPrivilegePermission}|{higherPrivilegePermissions}|");
            }
            return tableString.ToString();
        }

        private static string GetPermissionsMarkdownTableForHttpRequestBlock(List<string> httpRequests, PermissionsDocument permissionsDoc)
        {
            // use the first HTTP Request, we are assuming the group of URLs will have the same set of permissions
            var request = httpRequests.Where(x => !string.IsNullOrWhiteSpace(x)).FirstOrDefault();
            if (HttpParser.TryParseHttpRequest(request, out var parsedRequest))
            {
                try
                {
                    // remove $ref, $count, $value segments from paths
                    parsedRequest.Url = Constants.QueryOptionSegementRegex.Replace(parsedRequest.Url, string.Empty).TrimEnd('/').ToLowerInvariant();

                    // normalize function parameters
                    parsedRequest.Url = Constants.FunctionParameterRegex.Replace(parsedRequest.Url, "{value}");

                    var generator = new PermissionsStubGenerator(permissionsDoc, parsedRequest.Url, parsedRequest.Method, false, true);
                    return generator.GenerateTable();
                }
                catch (Exception ex)
                {
                    FancyConsole.WriteLine($"Could not fetch permissions for '{parsedRequest.Method} {parsedRequest.Url}': {ex.Message}");
                }
            }
            return null;
        }

        public static List<(string scopeType, string leastPrivilegePermissions, string higherPrivilegedPermissions)> GetPermissionsFromNewTable(string markdownTable)
        {
            List<(string scopeType, string leastPrivilegePermissions, string higherPrivilegedPermissions)> permissionsTuple = [];

            string[] rows = markdownTable.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            foreach (string row in rows.Skip(1))
            {
                if (!row.StartsWith('|'))
                    continue;

                string[] cells = Regex.Split(row.Trim(), @"\s*\|\s*").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

                var leastPrivilegePermissions = cells[1].Trim();
                var higherPrivilegePermissions = cells[2].Trim();
                if (cells[0].Trim().StartsWith("Delegated", StringComparison.OrdinalIgnoreCase) && cells[0].Contains("work", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(leastPrivilegePermissions) && PermissionKeywordsToIgnore.Contains(leastPrivilegePermissions, StringComparer.OrdinalIgnoreCase))
                    {
                        leastPrivilegePermissions = "";
                    }
                    if (!string.IsNullOrWhiteSpace(higherPrivilegePermissions) && PermissionKeywordsToIgnore.Contains(higherPrivilegePermissions, StringComparer.OrdinalIgnoreCase))
                    {
                        higherPrivilegePermissions = "";
                    }

                    if (string.IsNullOrEmpty(leastPrivilegePermissions) && string.IsNullOrEmpty(higherPrivilegePermissions))
                    {
                        continue;
                    }

                    permissionsTuple.Add(("DelegatedWork", leastPrivilegePermissions, higherPrivilegePermissions));
                }
                else if (cells[0].Trim().StartsWith("Delegated", StringComparison.OrdinalIgnoreCase) && cells[0].Contains("personal", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(leastPrivilegePermissions) && PermissionKeywordsToIgnore.Contains(leastPrivilegePermissions, StringComparer.OrdinalIgnoreCase))
                    {
                        leastPrivilegePermissions = "";
                    }
                    if (!string.IsNullOrWhiteSpace(higherPrivilegePermissions) && PermissionKeywordsToIgnore.Contains(higherPrivilegePermissions, StringComparer.OrdinalIgnoreCase))
                    {
                        higherPrivilegePermissions = "";
                    }

                    if (string.IsNullOrEmpty(leastPrivilegePermissions) && string.IsNullOrEmpty(higherPrivilegePermissions))
                    {
                        continue;
                    }

                    permissionsTuple.Add(("DelegatedPersonal", leastPrivilegePermissions, higherPrivilegePermissions));
                }

                else if (cells[0].Trim().Equals("Application", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(leastPrivilegePermissions) && PermissionKeywordsToIgnore.Contains(leastPrivilegePermissions, StringComparer.OrdinalIgnoreCase))
                    {
                        leastPrivilegePermissions = "";
                    }
                    if (!string.IsNullOrWhiteSpace(higherPrivilegePermissions) && PermissionKeywordsToIgnore.Contains(higherPrivilegePermissions, StringComparer.OrdinalIgnoreCase))
                    {
                        higherPrivilegePermissions = "";
                    }

                    if (string.IsNullOrEmpty(leastPrivilegePermissions) && string.IsNullOrEmpty(higherPrivilegePermissions))
                    {
                        continue;
                    }

                    permissionsTuple.Add(("Application", leastPrivilegePermissions, higherPrivilegePermissions));
                }
            }
            return permissionsTuple;
        }

        private static async Task<PermissionsDocument> GetPermissionsDocumentAsync(string filePathOrUrl)
        {
            var permissionsDocument = new PermissionsDocument();
            try
            {
                if (Uri.IsWellFormedUriString(filePathOrUrl, UriKind.Absolute))
                {
                    using var client = new HttpClient();
                    using var stream = await client.GetStreamAsync(filePathOrUrl);
                    permissionsDocument = PermissionsDocument.Load(stream);
                }
                else
                {
                    using var fileStream = new FileStream(filePathOrUrl, FileMode.Open);
                    permissionsDocument = PermissionsDocument.Load(fileStream);
                }
                return permissionsDocument;
            }
            catch (Exception ex)
            {
                FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, $"Failed to parse permission source file {ex.Message}");
                return null;
            }
        }

        private enum PermissionsInsertionState
        {
            FindPermissionsHeader,
            FindInsertionStartLine,
            FindInsertionEndLine,
            InsertPermissionBlock,
            FindHttpRequestHeading,
            FindHttpRequestStartLine,
            FindHttpRequestEndLine,
            FindNextPermissionBlock
        }

        #endregion

        /// <summary>
        /// Replaces first occurence of a string
        /// </summary>
        /// <param name="fileName">file to search for the string to be replaced</param>
        /// <param name="original">string to be replaced</param>
        /// <param name="replacement">replacement string</param>
        /// <returns></returns>
        private static async Task ReplaceFirstOccurence(string fileName, string original, string replacement)
        {
            // put strings in quotes as method names are always in quotes in the JSON blob
            // this makes sure that there is no substring collisions in the search
            original = $"\"{original}\"";
            replacement = $"\"{replacement}\"";
            var fileContents = await File.ReadAllTextAsync(fileName).ConfigureAwait(false);
            int position = fileContents.IndexOf(original);
            if (position < 0)
            {
                return;
            }
            var updatedFileContentes = fileContents[..position] + replacement + fileContents[(position + original.Length)..];
            await File.WriteAllTextAsync(fileName, updatedFileContentes).ConfigureAwait(false);
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
