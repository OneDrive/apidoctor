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

        private const ConsoleColor ConsoleDefaultColor = ConsoleColor.White;
        private const ConsoleColor ConsoleHeaderColor = ConsoleColor.Cyan;
        private const ConsoleColor ConsoleSubheaderColor = ConsoleColor.DarkCyan;
        private const ConsoleColor ConsoleCodeColor = ConsoleColor.Gray;
        private const ConsoleColor ConsoleErrorColor = ConsoleColor.Red;
        private const ConsoleColor ConsoleWarningColor = ConsoleColor.Yellow;
        private const ConsoleColor ConsoleSuccessColor = ConsoleColor.Green;

        public static readonly SavedSettings DefaultSettings = new SavedSettings("ApiTestTool", "settings.json");

        static void Main(string[] args)
        {
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

            var commandOptions = verbOptions as DocSetOptions;
            if (null != commandOptions)
            {
                FancyConsole.WriteVerboseOutput = commandOptions.Verbose;
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
                WriteOutErrors(new ValidationError[] { error }, options.SilenceWarnings);
                Exit(failure: true);
            }

            switch (invokedVerb)
            {
                case CommandLineOptions.VerbPrint:
                    PrintDocInformation((PrintOptions)options);
                    break;
                case CommandLineOptions.VerbCheckLinks:
                    CheckLinks((DocSetOptions)options);
                    break;
                case CommandLineOptions.VerbDocs:
                    CheckDocs((ConsistencyCheckOptions)options);
                    break;
                case CommandLineOptions.VerbService:
                    await CheckService((ServiceConsistencyOptions)options);
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
            FancyConsole.WriteLine(ConsoleHeaderColor, "Stored settings:");
            FancyConsole.WriteLineIndented("  ", "{0}: {1}", "AccessToken", settings.AccessToken);
            FancyConsole.WriteLineIndented("  ", "{0}: {1}", "DocumentationPath", settings.DocumentationPath);
            FancyConsole.WriteLineIndented("  ", "{0}: {1}", "ServiceUrl", settings.ServiceUrl);
        }


        /// <summary>
        /// Create a document set based on input options
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static DocSet GetDocSet(DocSetOptions options)
        {
            FancyConsole.VerboseWriteLine("Opening documentation from {0}", options.PathToDocSet);
            DocSet set = new DocSet(options.PathToDocSet);

            FancyConsole.VerboseWriteLine("Scanning documentation files...");
            ValidationError[] loadErrors;
            if (!set.ScanDocumentation(out loadErrors) && options.ShowLoadWarnings)
            {
                WriteOutErrors(loadErrors, options.SilenceWarnings);
            }

            var serviceOptions = options as ServiceConsistencyOptions;
            if (null != serviceOptions)
            {
                FancyConsole.VerboseWriteLine("Reading configuration parameters...");
                set.LoadTestScenarios(serviceOptions.ScenarioFilePath);
                if (!set.TestScenarios.Loaded)
                {
                    FancyConsole.WriteLine(ConsoleWarningColor, "Unable to read request parameter configuration file: {0}", serviceOptions.ScenarioFilePath);
                }
            }

            return set;
        }

        private static void PrintDocInformation(PrintOptions options)
        {
            DocSet docset = GetDocSet(options);
            if (options.PrintFiles)
            {
                PrintFiles(options, docset);
            }
            if (options.PrintResources)
            {
                PrintResources(options, docset);
            }
            if (options.PrintMethods)
            {
                PrintMethods(options, docset);
            }
        }

        private static void PrintFiles(DocSetOptions options, DocSet docset)
        {
            if (null == docset)
                docset = GetDocSet(options);

            FancyConsole.WriteLine();
            FancyConsole.WriteLine(ConsoleHeaderColor, "Documentation files");

            string format = null;
            if (options.Verbose)
                format = "{1} (resources: {2}, methods: {3})";
            else if (options.ShortForm)
                format = "{0}";
            else
                format = "{0} (r:{2}, m:{3})";

            foreach (var file in docset.Files)
            {
                ConsoleColor color = ConsoleSuccessColor;
                if (file.Resources.Length == 0 && file.Requests.Length == 0)
                    color = ConsoleWarningColor;

                FancyConsole.WriteLineIndented("  ", color, format, file.DisplayName, file.FullPath, file.Resources.Length, file.Requests.Length);
            }
        }

        /// <summary>
        /// Validate that all links in the documentation are unbroken.
        /// </summary>
        /// <param name="options"></param>
        private static void CheckLinks(DocSetOptions options)
        {
            var docset = GetDocSet(options);
            ValidationError[] errors;
            docset.ValidateLinks(options.Verbose, out errors);

            if (null != errors && errors.Length > 0)
            {
                WriteOutErrors(errors, options.SilenceWarnings);
                if (!errors.WereWarningsOrErrors())
                {
                    FancyConsole.WriteLine(ConsoleSuccessColor, "No link errors detected.");
                    Exit(failure: false);
                }
            }
            else
            {
                FancyConsole.WriteLine(ConsoleSuccessColor, "No link errors detected.");
                Exit(failure: false);
            }
        }

        private static void PrintResources(DocSetOptions options, DocSet docset)
        {
            if (null == docset)
                docset = GetDocSet(options);

            FancyConsole.WriteLine();
            FancyConsole.WriteLine(ConsoleHeaderColor, "Defined resources:");
            FancyConsole.WriteLine();

            var sortedResources = docset.Resources.OrderBy(x => x.ResourceType);

            foreach (var resource in sortedResources)
            {


                if (!options.ShortForm && options.Verbose)
                {
                    string metadata = JsonConvert.SerializeObject(resource.Metadata);
                    FancyConsole.Write("  ");
                    FancyConsole.Write(ConsoleHeaderColor, resource.ResourceType);
                    FancyConsole.WriteLine(" flags: {1}", resource.ResourceType, metadata);
                }
                else
                {
                    FancyConsole.WriteLineIndented("  ", ConsoleHeaderColor, resource.ResourceType);
                }

                if (!options.ShortForm)
                {
                    FancyConsole.WriteLineIndented("    ", ConsoleCodeColor, resource.JsonExample);
                    FancyConsole.WriteLine();
                }
            }
        }

        private static void PrintMethods(DocSetOptions options, DocSet docset)
        {
            if (null == docset)
                docset = GetDocSet(options);

            FancyConsole.WriteLine();
            FancyConsole.WriteLine(ConsoleHeaderColor, "Defined methods:");
            FancyConsole.WriteLine();

            foreach (var method in docset.Methods)
            {
                FancyConsole.WriteLine(ConsoleHeaderColor, "Method '{0}' in file '{1}'", method.Identifier, method.SourceFile.DisplayName);

                if (!options.ShortForm)
                {
                    var requestMetadata = options.Verbose ? JsonConvert.SerializeObject(method.RequestMetadata) : string.Empty;
                    FancyConsole.WriteLineIndented("  ", ConsoleSubheaderColor, "Request: {0}", requestMetadata);
                    FancyConsole.WriteLineIndented("    ", ConsoleCodeColor, method.Request);
                }

                if (options.Verbose)
                {
                    FancyConsole.WriteLine();
                    var responseMetadata = JsonConvert.SerializeObject(method.ExpectedResponseMetadata);
                    if (options.ShortForm)
                        FancyConsole.WriteLineIndented("  ", ConsoleHeaderColor, "Expected Response: {0}", method.ExpectedResponse.TopLineOnly());
                    else
                    {
                        FancyConsole.WriteLineIndented("  ", ConsoleSubheaderColor, "Expected Response: {0}", responseMetadata);
                        FancyConsole.WriteLineIndented("    ", ConsoleCodeColor, method.ExpectedResponse);
                    }
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

        private static void CheckDocs(ConsistencyCheckOptions options)
        {


            var docset = GetDocSet(options);
            FancyConsole.WriteLine();

            int successCount = 0, errorCount = 0, warningCount = 0;

            CheckMethods(options, docset, ref successCount, ref errorCount, ref warningCount);
            CheckExamples(options, docset, ref successCount, ref errorCount, ref warningCount);

            if (options.IgnoreWarnings)
            {
                successCount += warningCount;
                warningCount = 0;
            }

            PrintStatusMessage(successCount, warningCount, errorCount);

            bool wasFailure = errorCount > 0;
            Exit(failure: wasFailure);
        }

        private static void CheckExamples(ConsistencyCheckOptions options, DocSet docset, ref int successCount, ref int errorCount, ref int warningCount)
        {
            foreach (var doc in docset.Files)
            {
                if (doc.Examples.Length == 0)
                    continue;

                FancyConsole.WriteLine(ConsoleHeaderColor, "Checking examples in \"{0}\"...", doc.DisplayName);

                foreach (var example in doc.Examples)
                {
                    if (example.Metadata == null)
                        continue;

                    FancyConsole.Write("  Example: {0} [{1}]", example.Metadata.MethodName, example.Metadata.ResourceType);
                    var resourceType = example.Metadata.ResourceType;
                    ValidationError[] errors;

                    docset.ResourceCollection.ValidateJsonExample(example.Metadata, example.OriginalExample, out errors);
                    if (errors.WereErrors())
                        errorCount++;
                    else if (errors.WereWarnings())
                        warningCount++;
                    else
                        successCount++;

                    WriteOutErrors(errors, options.SilenceWarnings, "    ", " no errors.", true);
                }
            }
        }

        private static void CheckMethods(ConsistencyCheckOptions options, DocSet docset, ref int successCount, ref int errorCount, ref int warningCount)
        {
            MethodDefinition[] methods = FindTestMethods(options, docset);
            foreach (var method in methods)
            {
                FancyConsole.Write(ConsoleHeaderColor, "Checking \"{0}\" in {1}...", method.Identifier, method.SourceFile.DisplayName);
                if (string.IsNullOrEmpty(method.ExpectedResponse))
                {
                    FancyConsole.WriteLine();
                    FancyConsole.WriteLine(ConsoleErrorColor, "  Error: response was null.");
                    errorCount++;
                    continue;
                }
                var parser = new HttpParser();
                var expectedResponse = parser.ParseHttpResponse(method.ExpectedResponse);
                ValidationError[] errors = ValidateHttpResponse(docset, method, expectedResponse, options.SilenceWarnings);
                if (errors.WereErrors())
                {
                    errorCount++;
                }
                else
                    if (errors.WereWarnings())
                    {
                        warningCount++;
                    }
                    else
                    {
                        successCount++;
                    }
            }
        }

        private static MethodDefinition[] FindTestMethods(ConsistencyCheckOptions options, DocSet docset)
        {
            MethodDefinition[] methods = null;
            if (!string.IsNullOrEmpty(options.MethodName))
            {
                var foundMethod = LookUpMethod(docset, options.MethodName);
                if (null == foundMethod)
                {
                    FancyConsole.WriteLine(ConsoleErrorColor, "Unable to locate method '{0}' in docset.", options.MethodName);
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
                    FancyConsole.WriteLine(ConsoleErrorColor, "Unable to locate file '{0}' in docset.", options.FileName);
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

        private static void WriteOutErrors(IEnumerable<ValidationError> errors, bool silenceWarnings, string indent = "", string successMessage = null, bool endLine = false)
        {
            bool writeSuccessMessage = true;
            foreach (var error in errors)
            {
                // Skip messages if verbose output is off
                if (!error.IsWarning && !error.IsError && !FancyConsole.WriteVerboseOutput)
                    continue;

                // Skip warnings if silence warnings is enabled.
                if (silenceWarnings && error.IsWarning)
                    continue;

                writeSuccessMessage = false;
                if (endLine)
                    FancyConsole.WriteLine();
                WriteValidationError(indent, error);
            }

            if (writeSuccessMessage && successMessage != null)
            {
                FancyConsole.WriteLine(ConsoleSuccessColor, successMessage);
            }
        }

        private static void WriteValidationError(string indent, ValidationError error)
        {
            ConsoleColor color;
            if (error.IsWarning)
                color = ConsoleWarningColor;
            else if (error.IsError)
                color = ConsoleErrorColor;
            else
                color = ConsoleDefaultColor;

            FancyConsole.WriteLineIndented(indent, color, error.ErrorText);
        }

        private static ValidationError[] ValidateHttpResponse(DocSet docset, MethodDefinition method, HttpResponse response, bool silenceWarnings, HttpResponse expectedResponse = null, string indentLevel = "")
        {
            ValidationError[] errors;
            bool errorsOccured = !docset.ValidateApiMethod(method, response, expectedResponse, out errors, silenceWarnings);
            if (errorsOccured)
            {
                FancyConsole.WriteLine();
                WriteOutErrors(errors, silenceWarnings, indentLevel + "  ");
                FancyConsole.WriteLine();
            }
            else
            {
                if (response.CallDuration > TimeSpan.Zero)
                {
                    FancyConsole.Write(ConsoleSuccessColor, " no errors ");
                    FancyConsole.WriteLine(ConsoleSuccessColor, " ({0} ms)", response.CallDuration.TotalMilliseconds);
                }
                else
                {
                    FancyConsole.WriteLine(ConsoleSuccessColor, " no errors.");
                }
            }
            return errors;
        }

        /// <summary>
        /// Make requests against the service. Uses DocSet.RunParameter information to alter requests.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task CheckService(ServiceConsistencyOptions options)
        {
            var docset = GetDocSet(options);
            FancyConsole.WriteLine();

            if (options.UseEnvironmentVariables)
            {
                // Generate a new auth token from environment variables
                var token = await OAuthTokenGenerator.RedeemRefreshTokenFromEnvironment();
                if (token == null)
                {
                    FancyConsole.WriteLine(ConsoleErrorColor, "Unable to retrieve access token from environment variables");
                    Exit(failure: true);
                    return;
                }
                options.AccessToken = token.AccessToken;
            }

            var methods = FindTestMethods(options, docset);
            int successCount = 0, warningCount = 0, errorCount = 0;
            foreach (var method in methods)
            {
                FancyConsole.Write(ConsoleHeaderColor, "Calling method \"{0}\"...", method.Identifier);

                AuthenicationCredentials credentials = AuthenicationCredentials.CreateAutoCredentials(options.AccessToken);
                var testScenarios = docset.TestScenarios.ScenariosForMethod(method);
                if (testScenarios.Length == 0)
                {
                    // If there are no parameters defined, we still try to call the request as-is.
                    FancyConsole.WriteLine(ConsoleCodeColor, "\r\n  Method {0} has no scenario defined. Running as-is from docs.", method.RequestMetadata.MethodName);
                    var errors = await TestMethodWithParameters(docset, method, null, options.ServiceRootUrl, credentials, options.SilenceWarnings);
                    if (errors.WereErrors())
                    {
                        errorCount++;
                    }
                    else if (errors.WereWarnings())
                    {
                        warningCount++;
                    }
                    else
                    {
                        successCount++;
                    }
                    AddPause(options);
                }
                else
                {
                    // Otherwise, if there are parameter sets, we call each of them and check the result.
                    var enabledScenarios = testScenarios.Where(s => s.Enabled);
                    if (enabledScenarios.FirstOrDefault() == null)
                    {
                        FancyConsole.WriteLine(ConsoleWarningColor, " skipped.");
                    }
                    else
                    {
                        foreach (var requestSettings in testScenarios.Where(s => s.Enabled))
                        {
                            var errors = await TestMethodWithParameters(docset, method, requestSettings, options.ServiceRootUrl, credentials, options.SilenceWarnings);
                            if (errors.WereErrors())
                            {
                                errorCount++;
                            }
                            else if (errors.WereWarnings())
                            {
                                warningCount++;
                            }
                            else
                            {
                                successCount++;
                            }
                            AddPause(options);
                        }
                    }
                }

                FancyConsole.WriteLine();
            }

            if (options.IgnoreWarnings || options.SilenceWarnings)
            {
                successCount += warningCount;
                warningCount = 0;
            }

            PrintStatusMessage(successCount, warningCount, errorCount);
            bool wereFailures = (errorCount > 0) || (warningCount > 0);
            Exit(failure: wereFailures);
        }

        private static void Exit(bool failure)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine();
                Console.Write("Press any key to exit.");
                Console.ReadKey();
            }
#endif

            Environment.Exit(failure ? ExitCodeFailure : ExitCodeSuccess);
        }


        private static void PrintStatusMessage(int successCount, int warningCount, int errorCount)
        {
            FancyConsole.WriteLine();
            FancyConsole.Write("Runs completed. ");
            var totalCount = successCount + warningCount + errorCount;
            double percentSuccessful = 100 * (successCount / (double)totalCount);

            const string percentCompleteFormat = "{0:0.00}% passed";
            if (percentSuccessful == 100.0)
                FancyConsole.Write(ConsoleSuccessColor, percentCompleteFormat, percentSuccessful);
            else
                FancyConsole.Write(ConsoleWarningColor, percentCompleteFormat, percentSuccessful);

            if (errorCount > 0 || warningCount > 0)
            {
                FancyConsole.Write(" (");
                if (errorCount > 0)
                    FancyConsole.Write(ConsoleErrorColor, "{0} errors", errorCount);
                if (warningCount > 0 && errorCount > 0)
                    FancyConsole.Write(", ");
                if (warningCount > 0)
                    FancyConsole.Write(ConsoleWarningColor, "{0} warnings", warningCount);
                if (warningCount > 0 || errorCount > 0 && successCount > 0)
                    FancyConsole.Write(", ");
                if (successCount > 0)
                    FancyConsole.Write(ConsoleSuccessColor, "{0} successful", successCount);
                FancyConsole.Write(")");
            }
            FancyConsole.WriteLine();
        }

        private static void AddPause(ServiceConsistencyOptions options)
        {
            if (options.PauseBetweenRequests)
            {
                FancyConsole.Write("Press any key to continue");
                Console.ReadKey();
                FancyConsole.WriteLine();
            }
        }

        private static async Task<ValidationError[]> TestMethodWithParameters(DocSet docset, MethodDefinition method, ScenarioDefinition requestSettings, string rootUrl, AuthenicationCredentials credentials, bool silenceWarnings)
        {
            string indentLevel = "";
            if (requestSettings != null)
            {
                FancyConsole.WriteLine();
                FancyConsole.Write(ConsoleHeaderColor, "  With scenario \"{1}\"...", method.Identifier, requestSettings.Description);
                indentLevel = "  ";
            }

            FancyConsole.VerboseWriteLine("");
            FancyConsole.VerboseWriteLineIndented(indentLevel, "Request:");
            var requestPreviewResult = await method.PreviewRequestAsync(requestSettings, rootUrl, credentials, docset);
            if (requestPreviewResult.IsWarningOrError)
            {
                WriteOutErrors(requestPreviewResult.Messages, silenceWarnings, indentLevel + "  ");
                return requestPreviewResult.Messages;
            }
            
            var requestPreview = requestPreviewResult.Value;
            FancyConsole.VerboseWriteLineIndented(indentLevel + "  ", requestPreview.FullHttpText());

            var parser = new HttpParser();
            var expectedResponse = parser.ParseHttpResponse(method.ExpectedResponse);

            var request = requestPreview.PrepareHttpWebRequest(rootUrl);
            var actualResponse = await HttpResponse.ResponseFromHttpWebResponseAsync(request);

            FancyConsole.VerboseWriteLineIndented(indentLevel, "Response:");
            FancyConsole.VerboseWriteLineIndented(indentLevel + "  ", actualResponse.FullHttpText());
            FancyConsole.VerboseWriteLine();
            
            FancyConsole.VerboseWriteLineIndented(indentLevel, "Validation results:");
            return ValidateHttpResponse(docset, method, actualResponse, silenceWarnings, expectedResponse, indentLevel);
        }

        private static async Task PublishDocumentationAsync(PublishOptions options)
        {
            var outputPath = options.OutputDirectory;
            var inputPath = options.PathToDocSet;

            FancyConsole.WriteLine("Publishing documentation to {0}", outputPath);

            DocSet docs = GetDocSet(options);
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
            publisher.VerboseLogging = options.Verbose;
            FancyConsole.WriteLine();
            
            FancyConsole.WriteLine("Publishing content...");
            publisher.NewMessage += publisher_NewMessage;
            await publisher.PublishToFolderAsync(outputPath);

            FancyConsole.WriteLine(ConsoleSuccessColor, "Finished publishing documentation to: {0}", outputPath);

            Exit(failure: false);
        }

        static void publisher_NewMessage(object sender, ValidationMessageEventArgs e)
        {
            var msg = e.Message;
            if (!FancyConsole.WriteVerboseOutput && !msg.IsError && !msg.IsWarning)
                return;

            WriteValidationError("", msg);
        }

        private static async Task CheckServiceMetadata(CheckMetadataOptions options)
        {
            if (string.IsNullOrEmpty(options.ServiceMetadataLocation))
            {
                if (!string.IsNullOrEmpty(Program.DefaultSettings.ServiceUrl))
                {
                    options.ServiceMetadataLocation = Program.DefaultSettings.ServiceUrl + "/$metadata";
                }
                else
                {
                    FancyConsole.WriteLine(ConsoleErrorColor, "No service metadata file location specified. Cannot continue.");
                    Exit(failure: true);
                }
            }
            
            FancyConsole.WriteLine(ConsoleHeaderColor, "Loading service metadata from '{0}'...", options.ServiceMetadataLocation);

            Uri metadataUrl;
            List<OneDrive.ApiDocumentation.Validation.OData.Schema> schemas = null;
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
                FancyConsole.WriteLine(ConsoleErrorColor, "Error parsing metadata: {0}", ex.Message);
                return;
            }

            FancyConsole.WriteLine(ConsoleSuccessColor, "  found {0} schema definitions: {1}", schemas.Count, (from s in schemas select s.Namespace).ComponentsJoinedByString(", "));

            var docSet = GetDocSet(options);
            
            List<ResourceDefinition> foundResources = OneDrive.ApiDocumentation.Validation.OData.ODataParser.GenerateResourcesFromSchemas(schemas);
            int successCount = 0, errorCount = 0, warningCount = 0;

            foreach (var resource in foundResources)
            {
                FancyConsole.Write(ConsoleHeaderColor, "Checking resource: {0}...", resource.Metadata.ResourceType);

                FancyConsole.VerboseWriteLine();
                FancyConsole.VerboseWriteLine(resource.JsonExample);
                FancyConsole.VerboseWriteLine();

                // Verify that this resource matches the documentation
                ValidationError[] errors;
                docSet.ResourceCollection.ValidateJsonExample(resource.Metadata, resource.JsonExample, out errors);

                var wereErrors = errors.WereErrors() || (!options.SilenceWarnings && errors.WereWarnings());
                if (!wereErrors)
                {
                    FancyConsole.WriteLine(ConsoleSuccessColor, "  no errors.");
                    successCount++;
                }
                else
                {
                    if (errors.WereErrors())
                        errorCount++;
                    else if ((!options.IgnoreWarnings && !options.SilenceWarnings) && errors.WereWarnings())
                        warningCount++;
                    else
                        successCount++;
                    
                    FancyConsole.WriteLine();
                    WriteOutErrors(errors, options.SilenceWarnings, "  ");
                }
                FancyConsole.WriteLine();
            }

            PrintStatusMessage(successCount, warningCount, errorCount);

            Exit(failure: false);
        }
    }
}
