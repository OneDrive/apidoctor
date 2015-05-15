using Newtonsoft.Json;
using OneDrive.ApiDocumentation.Publishing;
using OneDrive.ApiDocumentation.Validation;
using OneDrive.ApiDocumentation.Validation.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.ConsoleApp
{
    class Program
    {
        private const int ExitCodeFailure = 1;
        private const int ExitCodeSuccess = 0;

        public static readonly SavedSettings DefaultSettings = new SavedSettings("ApiTestTool", "settings.json");
        public static readonly AppVeyor.BuildWorkerApi BuildWorker = new AppVeyor.BuildWorkerApi();

        static void Main(string[] args)
        {
            OneDrive.ApiDocumentation.Validation.LogHelper.ProvideLogHelper(new LogRecorder());

            FancyConsole.WriteLine(ConsoleColor.Green, "apidocs.exe Copyright (c) 2015 Microsoft Corporation.");
            FancyConsole.WriteLine();
            if (args.Length > 0)
                FancyConsole.WriteLine("Command line: " + args.ComponentsJoinedByString(" "));
            

            string verbName = null;
            BaseOptions verbOptions = null;

            var options = new CommandLineOptions();
            if (!CommandLine.Parser.Default.ParseArguments(args, options,
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

            if (verbOptions.AttachDebugger)
            {
                System.Diagnostics.Debugger.Launch();
            }

            if (!string.IsNullOrEmpty(verbOptions.AppVeyorServiceUrl))
            {
                BuildWorker.UrlEndPoint = new Uri(verbOptions.AppVeyorServiceUrl);
            }

            var commandOptions = verbOptions as DocSetOptions;
            if (null != commandOptions)
            {
                FancyConsole.WriteVerboseOutput = commandOptions.EnableVerboseOutput;
            }

            FancyConsole.LogFileName = verbOptions.LogFile;

            Nito.AsyncEx.AsyncContext.Run(() => RunInvokedMethodAsync(options, verbName, verbOptions));
        }

        private static async Task RunInvokedMethodAsync(CommandLineOptions origCommandLineOpts, string invokedVerb, BaseOptions options)
        {
            string[] missingProps;
            if (!options.HasRequiredProperties(out missingProps))
            {
                if (options is SetCommandOptions)
                {
                    // Just print out the current values of the set parameters
                    FancyConsole.WriteLine(origCommandLineOpts.GetUsage(invokedVerb));
                    FancyConsole.WriteLine();
                    WriteSavedValues(Program.DefaultSettings);
                    Exit(failure: true);
                }
                var error = new ValidationError(ValidationErrorCode.MissingRequiredArguments, null, "Command line is missing required arguments: {0}", missingProps.ComponentsJoinedByString(", "));
                FancyConsole.WriteLine(origCommandLineOpts.GetUsage(invokedVerb));
                await WriteOutErrors(new ValidationError[] { error }, options.SilenceWarnings);
                Exit(failure: true);
            }

            bool returnSuccess = true;

            switch (invokedVerb)
            {
                case CommandLineOptions.VerbPrint:
                    await PrintDocInformationAsync((PrintOptions)options);
                    break;
                case CommandLineOptions.VerbCheckLinks:
                    returnSuccess = await CheckLinksAsync((DocSetOptions)options);
                    break;
                case CommandLineOptions.VerbDocs:
                    returnSuccess = await CheckDocsAsync((BasicCheckOptions)options);
                    break;
                case CommandLineOptions.VerbService:
                    returnSuccess = await CheckServiceAsync((CheckServiceOptions)options);
                    break;
                case CommandLineOptions.VerbSet:
                    SetDefaultValues((SetCommandOptions)options);
                    break;
                case CommandLineOptions.VerbPublish:
                    await PublishDocumentationAsync((PublishOptions)options);
                    break;
                case CommandLineOptions.VerbMetadata:
                    await CheckServiceMetadata((CheckMetadataOptions)options);
                    break;
                case CommandLineOptions.VerbAbout:
                    PrintAboutMessage();
                    Exit(failure: false);
                    break;
            }

            Exit(failure: !returnSuccess);
        }

        private static void PrintAboutMessage()
        {
            FancyConsole.WriteLine();
            FancyConsole.WriteLine(ConsoleColor.Cyan, "apidocs.exe - API Documentation Test Tool");
            FancyConsole.WriteLine(ConsoleColor.Cyan, "Copyright (c) 2015 Microsoft Corporation");
            FancyConsole.WriteLine();
            FancyConsole.WriteLine(ConsoleColor.Cyan, "For more information see http://github.com/onedrive/markdown-scanner/");
            FancyConsole.WriteLine();
            FancyConsole.WriteLine(ConsoleColor.Cyan, "Includes or links with OSS code from:");
            FancyConsole.WriteLine();
            FancyConsole.WriteLine(ConsoleColor.Cyan, "MarkdownDeep - http://www.toptensoftware.com/markdowndeep");
            FancyConsole.WriteLine(ConsoleColor.Cyan, "Copyright (C) 2010-2011 Topten Software");
            FancyConsole.WriteLine();
            FancyConsole.WriteLine(ConsoleColor.Cyan, "Nito.AsyncEx - https://github.com/StephenCleary/AsyncEx");
            FancyConsole.WriteLine(ConsoleColor.Cyan, "Copyright (c) 2014 StephenCleary");
            FancyConsole.WriteLine();
        }


        private static void SetDefaultValues(SetCommandOptions setCommandOptions)
        {
            var settings = Program.DefaultSettings;
            if (setCommandOptions.ResetStoredValues)
            {
                settings.AccessToken = null;
                settings.DocumentationPath = null;
                settings.ServiceUrl = null;
            }

            bool setValues = false;

            if (!string.IsNullOrEmpty(setCommandOptions.AccessToken))
            {
                settings.AccessToken = setCommandOptions.AccessToken;
                setValues = true;
            }

            if (!string.IsNullOrEmpty(setCommandOptions.DocumentationPath))
            {
                settings.DocumentationPath = setCommandOptions.DocumentationPath;
                setValues = true;
            }

            if (!string.IsNullOrEmpty(setCommandOptions.ServiceUrl))
            {
                settings.ServiceUrl = setCommandOptions.ServiceUrl;
                setValues = true;
            }

            settings.Save();

            if (setCommandOptions.PrintValues || setValues)
            {
                WriteSavedValues(settings);
            }
            
        }

        private static void WriteSavedValues(SavedSettings settings)
        {
            FancyConsole.WriteLine(FancyConsole.ConsoleHeaderColor, "Stored settings:");
            FancyConsole.WriteLineIndented("  ", "{0}: {1}", "AccessToken", settings.AccessToken);
            FancyConsole.WriteLineIndented("  ", "{0}: {1}", "DocumentationPath", settings.DocumentationPath);
            FancyConsole.WriteLineIndented("  ", "{0}: {1}", "ServiceUrl", settings.ServiceUrl);
        }


        /// <summary>
        /// Create a document set based on input options
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task<DocSet> GetDocSetAsync(DocSetOptions options)
        {
            FancyConsole.VerboseWriteLine("Opening documentation from {0}", options.DocumentationSetPath);
            DocSet set = new DocSet(options.DocumentationSetPath);

            FancyConsole.VerboseWriteLine("Scanning documentation files...");
            ValidationError[] loadErrors;
            if (!set.ScanDocumentation(out loadErrors) && options.EnableVerboseOutput)
            {
                await WriteOutErrors(loadErrors, options.SilenceWarnings);
            }

            var serviceOptions = options as CheckServiceOptions;
            if (null != serviceOptions)
            {
                FancyConsole.VerboseWriteLine("Reading configuration parameters...");
                set.LoadTestScenarios();
            }

            return set;
        }

        public static void RecordWarning(string format, params object[] variables)
        {
            var message = string.Format(format, variables);
            FancyConsole.WriteLine(FancyConsole.ConsoleWarningColor, message);
            Task t = BuildWorker.AddMessageAsync(message, AppVeyor.MessageCategory.Warning);
        }

        public static void RecordError(string format, params object[] variables)
        {
            var message = string.Format(format, variables);
            FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, message);
            Task t = BuildWorker.AddMessageAsync(message, AppVeyor.MessageCategory.Error);
        }

        private static async Task PrintDocInformationAsync(PrintOptions options)
        {
            DocSet docset = await GetDocSetAsync(options);
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
        }

        private static async Task PrintFilesAsync(DocSetOptions options, DocSet docset)
        {
            if (null == docset)
            {
                docset = await GetDocSetAsync(options);
            }

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
        /// Validate that all links in the documentation are unbroken.
        /// </summary>
        /// <param name="options"></param>
        private static async Task<bool> CheckLinksAsync(DocSetOptions options)
        {
            const string testName = "Check-links";
            var docset = await GetDocSetAsync(options);

            await TestReport.StartTestAsync(testName);

            ValidationError[] errors;
            docset.ValidateLinks(options.EnableVerboseOutput, out errors);

            return await WriteOutErrors(errors, options.SilenceWarnings, successMessage: "No link errors detected.", testName: testName);
        }




        private static async Task PrintResourcesAsync(DocSetOptions options, DocSet docset)
        {
            if (null == docset)
                docset = await GetDocSetAsync(options);

            FancyConsole.WriteLine();
            FancyConsole.WriteLine(FancyConsole.ConsoleHeaderColor, "Defined resources:");
            FancyConsole.WriteLine();

            var sortedResources = docset.Resources.OrderBy(x => x.ResourceType);

            foreach (var resource in sortedResources)
            {


                if (options.EnableVerboseOutput)
                {
                    string metadata = JsonConvert.SerializeObject(resource.Metadata);
                    FancyConsole.Write("  ");
                    FancyConsole.Write(FancyConsole.ConsoleHeaderColor, resource.ResourceType);
                    FancyConsole.WriteLine(" flags: {1}", resource.ResourceType, metadata);
                }
                else
                {
                    FancyConsole.WriteLineIndented("  ", FancyConsole.ConsoleHeaderColor, resource.ResourceType);
                }

                FancyConsole.WriteLineIndented("    ", FancyConsole.ConsoleCodeColor, resource.JsonExample);
                FancyConsole.WriteLine();
            }
        }

        private static async Task PrintMethodsAsync(DocSetOptions options, DocSet docset)
        {
            if (null == docset)
                docset = await GetDocSetAsync(options);

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

        private static MethodDefinition LookUpMethod(DocSet docset, string methodName)
        {
            var query = from m in docset.Methods
                        where m.Identifier.Equals(methodName, StringComparison.OrdinalIgnoreCase)
                        select m;

            return query.FirstOrDefault();
        }

        private static async Task<bool> CheckDocsAsync(BasicCheckOptions options)
        {
            var docset = await GetDocSetAsync(options);
            FancyConsole.WriteLine();

            var resultMethods = await CheckMethodsAsync(options, docset);
            var resultExamples = await CheckExamplesAsync(options, docset);

            var combinedResults = resultMethods + resultExamples;

            if (options.IgnoreWarnings)
            {
                combinedResults.ConvertWarningsToSuccess();
            }

            combinedResults.PrintToConsole();

            return combinedResults.FailureCount == 0;
        }

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

                    var testName = string.Format("check-example: {0}", example.Metadata.MethodName, example.Metadata.ResourceType);
                    await TestReport.StartTestAsync(testName, doc.DisplayName);

                    var resourceType = example.Metadata.ResourceType;

                    ValidationError[] errors;
                    docset.ResourceCollection.ValidateJsonExample(example.Metadata, example.OriginalExample, out errors);

                    await WriteOutErrors(errors, options.SilenceWarnings, "   ", "No errors.", false, testName, "Warnings detected", "Errors detected");
                    results.IncrementResultCount(errors);
                }
            }

            return results;
        }

        private static async Task<CheckResults> CheckMethodsAsync(BasicCheckOptions options, DocSet docset)
        {
            MethodDefinition[] methods = FindTestMethods(options, docset);
            CheckResults results = new CheckResults();

            foreach (var method in methods)
            {
                var testName = "check-method-syntax: " + method.Identifier;
                await TestReport.StartTestAsync(testName, method.SourceFile.DisplayName);

                if (string.IsNullOrEmpty(method.ExpectedResponse))
                {
                    await TestReport.FinishTestAsync(testName, AppVeyor.TestOutcome.Failed, "Null response where one was expected.");
                    results.FailureCount++;
                    continue;
                }

                var parser = new HttpParser();
                var expectedResponse = parser.ParseHttpResponse(method.ExpectedResponse);
                ValidationError[] errors = await ValidateHttpResponse(docset, method, expectedResponse, options.SilenceWarnings);

                await WriteOutErrors(errors, options.SilenceWarnings, "   ", "No errors.", false, testName, "Warnings detected", "Errors detected");
                results.IncrementResultCount(errors);
            }

            return results;
        }

        private static MethodDefinition[] FindTestMethods(BasicCheckOptions options, DocSet docset)
        {
            MethodDefinition[] methods = null;
            if (!string.IsNullOrEmpty(options.MethodName))
            {
                var foundMethod = LookUpMethod(docset, options.MethodName);
                if (null == foundMethod)
                {
                    FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, "Unable to locate method '{0}' in docset.", options.MethodName);
                    Exit(failure: true);
                }
                methods = new MethodDefinition[] { LookUpMethod(docset, options.MethodName) };
            }
            else if (!string.IsNullOrEmpty(options.FileName))
            {
                var selectedFileQuery = from f in docset.Files where f.DisplayName == options.FileName select f;
                var selectedFile = selectedFileQuery.SingleOrDefault();
                if (selectedFile == null)
                {
                    FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, "Unable to locate file '{0}' in docset.", options.FileName);
                    Exit(failure: true);
                }
                methods = selectedFile.Requests;
            }
            else
            {
                methods = docset.Methods;
            }
            return methods;
        }


        private static async Task<bool> WriteOutErrors(IEnumerable<ValidationError> errors, bool silenceWarnings, string indent = "", string successMessage = null, bool endLineBeforeWriting = false, string testName = null, string warningMessage = null, string failureMessage = null)
        {
            foreach (var error in errors)
            {
                // Skip messages if verbose output is off
                if (!error.IsWarning && !error.IsError && !FancyConsole.WriteVerboseOutput)
                    continue;

                // Skip warnings if silence warnings is enabled.
                if (silenceWarnings && error.IsWarning)
                    continue;

                if (endLineBeforeWriting)
                {
                    FancyConsole.WriteLine();
                }

                WriteValidationError(indent, error);
            }

            AppVeyor.TestOutcome outcome = AppVeyor.TestOutcome.None;
            string outputMessage = null;
            if (errors.WereErrors())
            {
                // Write failure message
                //if (null != failureMessage)
                //    FancyConsole.WriteLine(FancyConsole.ConsoleErrorColor, failureMessage);
                outputMessage = errors.First().Message.FirstLineOnly();
                outcome = AppVeyor.TestOutcome.Failed;
            }
            else if (!silenceWarnings && errors.WereWarnings())
            {
                // Write warning message
                //if (null != warningMessage)
                //    FancyConsole.WriteLine(FancyConsole.ConsoleWarningColor, warningMessage);
                outputMessage = errors.First().Message.FirstLineOnly();
                outcome = AppVeyor.TestOutcome.Passed;
            }
            else
            {
                // write success message!
                //if (null != successMessage)
                //    FancyConsole.WriteLine(FancyConsole.ConsoleSuccessColor, successMessage);
                outputMessage = successMessage;
                outcome = AppVeyor.TestOutcome.Passed;
            }

            // Record this test's outcome in the build worker API
            if (null != testName)
            {
                var errorMessage = (from e in errors select e.ErrorText).ComponentsJoinedByString("\r\n");
                await TestReport.FinishTestAsync(testName, outcome, outputMessage, stdOut: errorMessage);
            }

            return outcome == AppVeyor.TestOutcome.Passed;
        }

        private static void WriteValidationError(string indent, ValidationError error)
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

        private static async Task<ValidationError[]> ValidateHttpResponse(DocSet docset, MethodDefinition method, HttpResponse response, bool silenceWarnings, HttpResponse expectedResponse = null, string indentLevel = "")
        {
            ValidationError[] errors;
            bool errorsOccured = !docset.ValidateApiMethod(method, response, expectedResponse, out errors, silenceWarnings);
            return errors;
        }

        /// <summary>
        /// Make requests against the service. Uses DocSet.RunParameter information to alter requests.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task<bool> CheckServiceAsync(CheckServiceOptions options)
        {
            var docset = await GetDocSetAsync(options);
            FancyConsole.WriteLine();

            if (!string.IsNullOrEmpty(options.ODataMetadataLevel))
            {
                ValidationConfig.ODataMetadataLevel = options.ODataMetadataLevel;
            }

            if (options.FoundAccounts == null || options.FoundAccounts.Count == 0)
            {
                RecordError("No account was found. Cannot connect to the service.");
                return false;
            }

            var methods = FindTestMethods(options, docset);
            bool wereFailures = false;

            foreach (var account in options.FoundAccounts.Where(x => x.Enabled))
            {
                CheckResults results = new CheckResults();

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

                string testNamePrefix = "";
                if (options.FoundAccounts.Count > 1)
                {
                    FancyConsole.WriteLine(FancyConsole.ConsoleHeaderColor, "Testing with account: {0}", account.Name);
                    testNamePrefix = account.Name.ToLower() + "-";
                }
                // Make sure we have an access token
                if (string.IsNullOrEmpty(account.AccessToken))
                {
                    var tokens = await OAuthTokenGenerator.RedeemRefreshToken(account);
                    if (null != tokens)
                    {
                        account.AccessToken = tokens.AccessToken;
                    }
                    else
                    {
                        RecordError("Failed to retrieve access token for account: {0}", account.Name);
                        continue;
                    }
                }
                
                AuthenicationCredentials credentials = AuthenicationCredentials.CreateAutoCredentials(account.AccessToken);
                
                foreach (var method in methods)
                {
                    var testScenarios = docset.TestScenarios.ScenariosForMethod(method);

                    if (testScenarios.Length == 0)
                    {
                        // If there are no parameters defined, we still try to call the request as-is.
                        FancyConsole.WriteLine(FancyConsole.ConsoleCodeColor, "\r\n  Method {0} has no scenario defined. Running as-is from docs.", method.RequestMetadata.MethodName);
                        var errors = await TestMethodWithParameters(docset, method, null, account.ServiceUrl, credentials, options.SilenceWarnings, testNamePrefix);
                        results.IncrementResultCount(errors);
                        AddPause(options);
                    }
                    else
                    {
                        // Otherwise, if there are parameter sets, we call each of them and check the result.
                        var enabledScenarios = testScenarios.Where(s => s.Enabled);
                        if (enabledScenarios.FirstOrDefault() == null)
                        {
                            await TestReport.StartTestAsync(method.Identifier, method.SourceFile.DisplayName);
                            await TestReport.FinishTestAsync(method.Identifier, AppVeyor.TestOutcome.Skipped, "All scenarios for this method were disabled.");
                            FancyConsole.Write(FancyConsole.ConsoleHeaderColor, "Skipped test: {0}.", method.Identifier);
                            FancyConsole.WriteLine(FancyConsole.ConsoleWarningColor, " All scenarios for test were disabled.", method.Identifier);
                        }
                        else
                        {
                            foreach (var requestSettings in testScenarios.Where(s => s.Enabled))
                            {
                                var errors = await TestMethodWithParameters(docset, method, requestSettings, account.ServiceUrl, credentials, options.SilenceWarnings, testNamePrefix);
                                results.IncrementResultCount(errors);
                                AddPause(options);
                            }
                        }
                    }

                    FancyConsole.WriteLine();
                }

                if (options.IgnoreWarnings || options.SilenceWarnings)
                {
                    results.ConvertWarningsToSuccess();
                }

                results.PrintToConsole();
                wereFailures |= (results.FailureCount + results.WarningCount) > 0;
            }

            return !wereFailures;
        }

        private static void Exit(bool failure)
        {
            var exitCode = failure ? ExitCodeFailure : ExitCodeSuccess;
#if DEBUG
            Console.WriteLine("Exit code: " + exitCode);
            if (System.Diagnostics.Debugger.IsAttached)
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
                FancyConsole.Write("Press any key to continue");
                Console.ReadKey();
                FancyConsole.WriteLine();
            }
        }

        private static async Task<ValidationError[]> TestMethodWithParameters(DocSet docset, MethodDefinition method, ScenarioDefinition requestSettings, string rootUrl, AuthenicationCredentials credentials, bool silenceWarnings, string testNamePrefix)
        {
            string testName = testNamePrefix + method.Identifier;
            string indentLevel = "";
            if (requestSettings != null)
            {
                if (!string.IsNullOrEmpty(requestSettings.Description))
                {
                    testName = testNamePrefix + string.Format("{0} [{1}]", method.Identifier, requestSettings.Description);
                }
                indentLevel = indentLevel + "  ";
            }

            FancyConsole.VerboseWriteLine("");
            FancyConsole.VerboseWriteLineIndented(indentLevel, "Request:");

            await TestReport.StartTestAsync(testName, method.SourceFile.DisplayName);

            var requestPreviewResult = await method.PreviewRequestAsync(requestSettings, rootUrl, credentials, docset);
            
            // Check to see if an error occured building the request, and abort if so.
            if (requestPreviewResult.IsWarningOrError)
            {
                await WriteOutErrors(requestPreviewResult.Messages, silenceWarnings, indentLevel + "  ", testName: testName);
                return requestPreviewResult.Messages;
            }
            
            var requestPreview = requestPreviewResult.Value;
            FancyConsole.VerboseWriteLineIndented(indentLevel + "  ", requestPreview.FullHttpText());

            var parser = new HttpParser();
            HttpResponse expectedResponse = null;
            if (!string.IsNullOrEmpty(method.ExpectedResponse))
            {
                expectedResponse = parser.ParseHttpResponse(method.ExpectedResponse);
            }

            var actualResponse = await requestPreview.GetResponseAsync(rootUrl);

            FancyConsole.VerboseWriteLineIndented(indentLevel, "Response:");
            FancyConsole.VerboseWriteLineIndented(indentLevel + "  ", actualResponse.FullHttpText());
            FancyConsole.VerboseWriteLine();
            
            FancyConsole.VerboseWriteLineIndented(indentLevel, "Validation results:");
            var validateResponse = await ValidateHttpResponse(docset, method, actualResponse, silenceWarnings, expectedResponse, indentLevel);
            await WriteOutErrors(validateResponse, silenceWarnings, indentLevel, "No errors.", false, testName);
            
            return validateResponse;
        }

        private static async Task PublishDocumentationAsync(PublishOptions options)
        {
            var outputPath = options.OutputDirectory;
            var inputPath = options.DocumentationSetPath;

            FancyConsole.WriteLine("Publishing documentation to {0}", outputPath);

            DocSet docs = await GetDocSetAsync(options);
            DocumentPublisher publisher = null;
            switch (options.Format)
            {
                case PublishOptions.PublishFormat.Markdown:
                    publisher = new MarkdownPublisher(docs);
                    break;
                case PublishOptions.PublishFormat.Html:
                    publisher = new DocumentPublisherHtml(docs, options.TemplateFolder);
                    break;
                case PublishOptions.PublishFormat.Mustache:
                    publisher = new HtmlMustacheWriter(docs, options.TemplateFolder);
                    break;
                case PublishOptions.PublishFormat.Swagger2:
                    publisher = new OneDrive.ApiDocumentation.Validation.Writers.SwaggerWriter(docs, Program.DefaultSettings.ServiceUrl)
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
                    throw new NotSupportedException("Unsupported format: " + options.Format.ToString());
            }

            FancyConsole.WriteLineIndented("  ", "Format: {0}", publisher.GetType().Name);
            publisher.VerboseLogging = options.EnableVerboseOutput;
            FancyConsole.WriteLine();
            
            FancyConsole.WriteLine("Publishing content...");
            publisher.NewMessage += publisher_NewMessage;
            await publisher.PublishToFolderAsync(outputPath);

            FancyConsole.WriteLine(FancyConsole.ConsoleSuccessColor, "Finished publishing documentation to: {0}", outputPath);

            Exit(failure: false);
        }

        static void publisher_NewMessage(object sender, ValidationMessageEventArgs e)
        {
            var msg = e.Message;
            if (!FancyConsole.WriteVerboseOutput && !msg.IsError && !msg.IsWarning)
                return;

            WriteValidationError("", msg);
        }


        private static async Task<List<OneDrive.ApiDocumentation.Validation.OData.Schema>> TryGetMetadataSchemas(CheckMetadataOptions options)
        {
            if (string.IsNullOrEmpty(options.ServiceMetadataLocation))
            {
                if (!string.IsNullOrEmpty(Program.DefaultSettings.ServiceUrl))
                {
                    options.ServiceMetadataLocation = Program.DefaultSettings.ServiceUrl + "/$metadata";
                }
                else
                {
                    RecordError("No service metadata file location specified.");
                    return null;
                }
            }

            FancyConsole.WriteLine(FancyConsole.ConsoleHeaderColor, "Loading service metadata from '{0}'...", options.ServiceMetadataLocation);

            Uri metadataUrl;
            List<OneDrive.ApiDocumentation.Validation.OData.Schema> schemas = new List<Validation.OData.Schema>();
            try
            {
                if (Uri.TryCreate(options.ServiceMetadataLocation, UriKind.Absolute, out metadataUrl))
                {
                    schemas = await OneDrive.ApiDocumentation.Validation.OData.ODataParser.ReadSchemaFromMetadataUrl(metadataUrl);
                }
                else
                {
                    schemas = await OneDrive.ApiDocumentation.Validation.OData.ODataParser.ReadSchemaFromFile(options.ServiceMetadataLocation);
                }
            }
            catch (Exception ex)
            {
                RecordError("Error parsing service metadata: {0}", ex.Message);
                return null;
            }

            return schemas;
        }

        private static async Task<bool> CheckServiceMetadata(CheckMetadataOptions options)
        {

            List<OneDrive.ApiDocumentation.Validation.OData.Schema> schemas = await TryGetMetadataSchemas(options);
            if (null == schemas)
                return false;

            FancyConsole.WriteLine(FancyConsole.ConsoleSuccessColor, "  found {0} schema definitions: {1}", schemas.Count, (from s in schemas select s.Namespace).ComponentsJoinedByString(", "));

            var docSet = await GetDocSetAsync(options);

            const string testname = "validate-service-metadata";
            await TestReport.StartTestAsync(testname);

            List<ResourceDefinition> foundResources = OneDrive.ApiDocumentation.Validation.OData.ODataParser.GenerateResourcesFromSchemas(schemas);
            CheckResults results = new CheckResults();

            List<ValidationError> collectedErrors = new List<ValidationError>();

            foreach (var resource in foundResources)
            {
                FancyConsole.WriteLine();
                FancyConsole.Write(FancyConsole.ConsoleHeaderColor, "Checking resource: {0}...", resource.Metadata.ResourceType);

                FancyConsole.VerboseWriteLine();
                FancyConsole.VerboseWriteLine(resource.JsonExample);
                FancyConsole.VerboseWriteLine();

                // Verify that this resource matches the documentation
                ValidationError[] errors;
                docSet.ResourceCollection.ValidateJsonExample(resource.Metadata, resource.JsonExample, out errors);
                results.IncrementResultCount(errors);

                collectedErrors.AddRange(errors);

                await WriteOutErrors(errors, options.SilenceWarnings, successMessage: " no errors.");
            }

            if (options.IgnoreWarnings)
            {
                results.ConvertWarningsToSuccess();
            }

            var output = (from e in collectedErrors select e.ErrorText).ComponentsJoinedByString("\r\n");

            await TestReport.FinishTestAsync(testname, results.WereFailures ? AppVeyor.TestOutcome.Failed : AppVeyor.TestOutcome.Passed, stdOut:output);

            results.PrintToConsole();
            return !results.WereFailures;
        }
    }

    class LogRecorder : OneDrive.ApiDocumentation.Validation.ILogHelper
    {
        public void RecordFailure(string message)
        {
            Program.RecordError(message);
        }

        public void RecordWarning(string message)
        {
            Program.RecordWarning(message);
        }
    }
   
}
