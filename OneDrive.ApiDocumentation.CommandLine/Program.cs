<<<<<<< HEAD
﻿using Newtonsoft.Json;
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

        static void Main(string[] args)
        {
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

            var commandOptions = verbOptions as DocSetOptions;
            if (null != commandOptions)
            {
                FancyConsole.WriteVerboseOutput = commandOptions.Verbose;
            }

            FancyConsole.LogFileName = verbOptions.LogFile;

#if DEBUG
//            System.Diagnostics.Debugger.Launch();
#endif


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
                    WriteSavedValues(SavedSettings.Default);
                    Exit(failure: true);
                }
                var error = new ValidationError(ValidationErrorCode.MissingRequiredArguments, null, "Command line is missing required arguments: {0}", missingProps.ComponentsJoinedByString(", "));
                FancyConsole.WriteLine(origCommandLineOpts.GetUsage(invokedVerb));
                WriteOutErrors(new ValidationError[] { error });
                Exit(failure: true);
            }

            switch (invokedVerb)
            {
                case CommandLineOptions.VerbPrint:
                    PrintDocInformation((PrintOptions)options);
                    break;
                case CommandLineOptions.VerbCheckLinks:
                    VerifyLinks((DocSetOptions)options);
                    break;
                case CommandLineOptions.VerbDocs:
                    CheckMethodExamples((ConsistencyCheckOptions)options);
                    break;
                case CommandLineOptions.VerbService:
                    await CheckMethodsAgainstService((ServiceConsistencyOptions)options);
                    break;
                case CommandLineOptions.VerbSet:
                    SetDefaultValues((SetCommandOptions)options);
                    break;
                case CommandLineOptions.VerbClean:
                    await PublishDocumentationAsync((PublishOptions)options);
                    break;
                case CommandLineOptions.VerbMetadata:
                    await CheckServiceMetadata((CheckMetadataOptions)options);
                    break;
            }
        }


        private static void SetDefaultValues(SetCommandOptions setCommandOptions)
        {
            var settings = SavedSettings.Default;
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
                WriteOutErrors(loadErrors);
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

        private static void VerifyLinks(DocSetOptions options)
        {
            var docset = GetDocSet(options);
            ValidationError[] errors;
            docset.ValidateLinks(options.Verbose, out errors);

            if (null != errors && errors.Length > 0)
            {
                WriteOutErrors(errors);
                if (errors.All(x => !x.IsWarning && !x.IsError))
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
                    FancyConsole.WriteLineIndented("  ", resource.ResourceType);
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

        private static void CheckMethodExamples(ConsistencyCheckOptions options)
        {
            var docset = GetDocSet(options);
            FancyConsole.WriteLine();

            MethodDefinition[] methods = FindTestMethods(options, docset);

            bool result = true;
            int successCount = 0, errorCount = 0, warningCount = 0;
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
                ValidationError[] errors = ValidateHttpResponse(docset, method, expectedResponse);
                result &= errors.WereErrors();

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
            }

            if (options.IgnoreWarnings)
            {
                successCount += warningCount;
                warningCount = 0;
            }

            PrintStatusMessage(successCount, warningCount, errorCount);

            Exit(failure: !result);
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

        private static void WriteOutErrors(IEnumerable<ValidationError> errors, string indent = "")
        {
            foreach (var error in errors)
            {
                if (!error.IsWarning && !error.IsError && !FancyConsole.WriteVerboseOutput)
                    continue;
                WriteValidationError(indent, error);
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

        private static ValidationError[] ValidateHttpResponse(DocSet docset, MethodDefinition method, HttpResponse response, HttpResponse expectedResponse = null, string indentLevel = "")
        {
            ValidationError[] errors;
            if (!docset.ValidateApiMethod(method, response, expectedResponse, out errors))
            {
                FancyConsole.WriteLine();
                WriteOutErrors(errors, indentLevel + "  ");
                FancyConsole.WriteLine();
            }
            else
            {
                FancyConsole.WriteLine(ConsoleSuccessColor, " no errors");
            }
            return errors;
        }

        /// <summary>
        /// Make requests against the service. Uses DocSet.RunParameter information to alter requests.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task CheckMethodsAgainstService(ServiceConsistencyOptions options)
        {
            var docset = GetDocSet(options);
            FancyConsole.WriteLine();

            var methods = FindTestMethods(options, docset);
            int successCount = 0, warningCount = 0, errorCount = 0;
            bool result = true;
            foreach (var method in methods)
            {
                ValidationError[] errors = null;

                FancyConsole.Write(ConsoleHeaderColor, "Calling method \"{0}\"...", method.Identifier);
                if (method.RequestMetadata.Disabled)
                {
                    errors = new ValidationError[] { new ValidationWarning(ValidationErrorCode.MethodDisabled, null, "Method was disabled: {0}", method.Identifier) };
                    FancyConsole.WriteLine();
                    WriteOutErrors(errors, "  ");
                    warningCount++;
                    continue;
                }
                
                AuthenicationCredentials credentials = AuthenicationCredentials.CreateAutoCredentials(options.AccessToken);
                var setsOfParameters = docset.TestScenarios.ScenariosForMethod(method);
                if (setsOfParameters.Length == 0)
                {
                    // If there are no parameters defined, we still try to call the request as-is.
                    errors = await TestMethodWithParameters(docset, method, null, options.ServiceRootUrl, credentials);
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
                    foreach (var requestSettings in setsOfParameters.Where(s => s.Enabled))
                    {
                        errors = await TestMethodWithParameters(docset, method, requestSettings, options.ServiceRootUrl, credentials);
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

                FancyConsole.WriteLine();
            }

            if (options.IgnoreWarnings)
            {
                successCount += warningCount;
                warningCount = 0;
            }

            PrintStatusMessage(successCount, warningCount, errorCount);
            result = (errorCount > 0) || (warningCount > 0);
            Exit(!result);
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

        private static async Task<ValidationError[]> TestMethodWithParameters(DocSet docset, MethodDefinition method, ScenarioDefinition requestSettings, string rootUrl, AuthenicationCredentials credentials)
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
            var requestPreviewResult = await method.PreviewRequestAsync(requestSettings, rootUrl, credentials);
            if (requestPreviewResult.IsWarningOrError)
            {
                WriteOutErrors(requestPreviewResult.Messages, indentLevel + "  ");
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
            return ValidateHttpResponse(docset, method, actualResponse, expectedResponse, indentLevel);
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
                case PublishOptions.SanitizedFormat.Markdown:
                    publisher = new DocumentPublisher(docs);
                    break;
                case PublishOptions.SanitizedFormat.Html:
                    publisher = new DocumentPublisherHtml(docs);
                    break;
                case PublishOptions.SanitizedFormat.Swagger2:
                    publisher = new OneDrive.ApiDocumentation.Validation.Writers.SwaggerWriter(docs);
                    break;
                default:
                    throw new NotSupportedException("Unsupported format: " + options.Format.ToString());
            }

            publisher.VerboseLogging = options.Verbose;
            publisher.SourceFileExtensions = options.TextFileExtensions;
            FancyConsole.WriteLineIndented("  ", "File extensions: {0}", publisher.SourceFileExtensions);

            if (!string.IsNullOrEmpty(options.IgnorePaths))
                publisher.SkipPaths = options.IgnorePaths;
            FancyConsole.WriteLineIndented("  ", "Ignored Paths: {0}", publisher.SkipPaths);
            publisher.PublishAllFiles = options.PublishAllFiles;
            FancyConsole.WriteLineIndented("  ", "Include all files: {0}", publisher.PublishAllFiles);
            FancyConsole.WriteLine();
            
            FancyConsole.WriteLine("Publishing content...");
            publisher.NewMessage += publisher_NewMessage;
            await publisher.PublishToFolderAsync(outputPath);

            FancyConsole.WriteLine(ConsoleSuccessColor, "Finished publishing documentation to: {0}", outputPath);
        }

        static void publisher_NewMessage(object sender, ValidationError e)
        {
            if (!FancyConsole.WriteVerboseOutput && !e.IsError && !e.IsWarning)
                return;

            WriteValidationError("", e);
        }

        private static async Task CheckServiceMetadata(CheckMetadataOptions options)
        {
            if (string.IsNullOrEmpty(options.ServiceMetadataLocation))
            {
                if (!string.IsNullOrEmpty(SavedSettings.Default.ServiceUrl))
                {
                    options.ServiceMetadataLocation = SavedSettings.Default.ServiceUrl + "$metadata";
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

                if (!errors.WereErrors() && !errors.WereWarnings())
                {
                    FancyConsole.WriteLine(ConsoleSuccessColor, "  no errors.");
                    successCount++;
                }
                else
                {
                    if (errors.WereWarnings() && !errors.WereErrors())
                        warningCount++;
                    else
                        errorCount++;
                    
                    FancyConsole.WriteLine();
                    WriteOutErrors(errors, "  ");
                }
                FancyConsole.WriteLine();
            }

            PrintStatusMessage(successCount, warningCount, errorCount);

            Exit(failure: false);
        }
    }
}
=======
﻿using Newtonsoft.Json;
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

        static void Main(string[] args)
        {
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

            var commandOptions = verbOptions as DocSetOptions;
            if (null != commandOptions)
            {
                FancyConsole.WriteVerboseOutput = commandOptions.Verbose;
            }

            FancyConsole.LogFileName = verbOptions.LogFile;

#if DEBUG
            System.Diagnostics.Debugger.Launch();
#endif


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
                    WriteSavedValues(SavedSettings.Default);
                    Exit(failure: true);
                }
                var error = new ValidationError(ValidationErrorCode.MissingRequiredArguments, null, "Command line is missing required arguments: {0}", missingProps.ComponentsJoinedByString(", "));
                FancyConsole.WriteLine(origCommandLineOpts.GetUsage(invokedVerb));
                WriteOutErrors(new ValidationError[] { error });
                Exit(failure: true);
            }

            switch (invokedVerb)
            {
                case CommandLineOptions.VerbPrint:
                    PrintDocInformation((PrintOptions)options);
                    break;
                case CommandLineOptions.VerbCheckLinks:
                    VerifyLinks((DocSetOptions)options);
                    break;
                case CommandLineOptions.VerbDocs:
                    CheckMethodExamples((ConsistencyCheckOptions)options);
                    break;
                case CommandLineOptions.VerbService:
                    await CheckMethodsAgainstService((ServiceConsistencyOptions)options);
                    break;
                case CommandLineOptions.VerbSet:
                    SetDefaultValues((SetCommandOptions)options);
                    break;
                case CommandLineOptions.VerbClean:
                    await PublishDocumentationAsync((PublishOptions)options);
                    break;
                case CommandLineOptions.VerbMetadata:
                    await CheckServiceMetadata((CheckMetadataOptions)options);
                    break;
            }
        }


        private static void SetDefaultValues(SetCommandOptions setCommandOptions)
        {
            var settings = SavedSettings.Default;
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
                WriteOutErrors(loadErrors);
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

        private static void VerifyLinks(DocSetOptions options)
        {
            var docset = GetDocSet(options);
            ValidationError[] errors;
            docset.ValidateLinks(options.Verbose, out errors);

            if (null != errors && errors.Length > 0)
            {
                WriteOutErrors(errors);
                if (errors.All(x => !x.IsWarning && !x.IsError))
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
                    FancyConsole.WriteLineIndented("  ", resource.ResourceType);
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
                FancyConsole.WriteLine(ConsoleHeaderColor, "Method '{0}' in file '{1}'", method.DisplayName, method.SourceFile.DisplayName);

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
                        where m.DisplayName.Equals(methodName, StringComparison.OrdinalIgnoreCase)
                        select m;

            return query.FirstOrDefault();
        }

        private static void CheckMethodExamples(ConsistencyCheckOptions options)
        {
            var docset = GetDocSet(options);
            FancyConsole.WriteLine();

            MethodDefinition[] methods = FindTestMethods(options, docset);

            bool result = true;
            int successCount = 0, errorCount = 0, warningCount = 0;
            foreach (var method in methods)
            {
                FancyConsole.Write(ConsoleHeaderColor, "Checking \"{0}\" in {1}...", method.DisplayName, method.SourceFile.DisplayName);

                if (string.IsNullOrEmpty(method.ExpectedResponse))
                {
                    FancyConsole.WriteLine();
                    FancyConsole.WriteLine(ConsoleErrorColor, "  Error: response was null.");
                    errorCount++;
                    continue;
                }

                var parser = new HttpParser();
                var expectedResponse = parser.ParseHttpResponse(method.ExpectedResponse);
                ValidationError[] errors = ValidateHttpResponse(docset, method, expectedResponse);
                result &= errors.WereErrors();

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
            }

            if (options.IgnoreWarnings)
            {
                successCount += warningCount;
                warningCount = 0;
            }

            PrintStatusMessage(successCount, warningCount, errorCount);

            Exit(failure: !result);
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

        private static void WriteOutErrors(IEnumerable<ValidationError> errors, string indent = "")
        {
            foreach (var error in errors)
            {
                if (!error.IsWarning && !error.IsError && !FancyConsole.WriteVerboseOutput)
                    continue;
                WriteValidationError(indent, error);
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

        private static ValidationError[] ValidateHttpResponse(DocSet docset, MethodDefinition method, HttpResponse response, HttpResponse expectedResponse = null, string indentLevel = "")
        {
            ValidationError[] errors;
            if (!docset.ValidateApiMethod(method, response, expectedResponse, out errors))
            {
                FancyConsole.WriteLine();
                WriteOutErrors(errors, indentLevel + "  ");
                FancyConsole.WriteLine();
            }
            else
            {
                FancyConsole.WriteLine(ConsoleSuccessColor, " no errors");
            }
            return errors;
        }

        /// <summary>
        /// Make requests against the service. Uses DocSet.RunParameter information to alter requests.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static async Task CheckMethodsAgainstService(ServiceConsistencyOptions options)
        {
            var docset = GetDocSet(options);
            FancyConsole.WriteLine();

            var methods = FindTestMethods(options, docset);
            int successCount = 0, warningCount = 0, errorCount = 0;
            bool result = true;
            foreach (var method in methods)
            {
                ValidationError[] errors = null;

                FancyConsole.Write(ConsoleHeaderColor, "Calling method \"{0}\"...", method.DisplayName);
                if (method.RequestMetadata.Disabled)
                {
                    errors = new ValidationError[] { new ValidationWarning(ValidationErrorCode.MethodDisabled, null, "Method was disabled: {0}", method.DisplayName) };
                    FancyConsole.WriteLine();
                    WriteOutErrors(errors, "  ");
                    warningCount++;
                    continue;
                }
                
                AuthenicationCredentials credentials = AuthenicationCredentials.CreateAutoCredentials(options.AccessToken);
                var setsOfParameters = docset.TestScenarios.ScenariosForMethod(method);
                if (setsOfParameters.Length == 0)
                {
                    // If there are no parameters defined, we still try to call the request as-is.
                    errors = await TestMethodWithParameters(docset, method, null, options.ServiceRootUrl, credentials);
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
                    foreach (var requestSettings in setsOfParameters.Where(s => s.Enabled))
                    {
                        errors = await TestMethodWithParameters(docset, method, requestSettings, options.ServiceRootUrl, credentials);
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

                FancyConsole.WriteLine();
            }

            if (options.IgnoreWarnings)
            {
                successCount += warningCount;
                warningCount = 0;
            }

            PrintStatusMessage(successCount, warningCount, errorCount);
            result = (errorCount > 0) || (warningCount > 0);
            Exit(!result);
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

        private static async Task<ValidationError[]> TestMethodWithParameters(DocSet docset, MethodDefinition method, ScenarioDefinition requestSettings, string rootUrl, AuthenicationCredentials credentials)
        {
            string indentLevel = "";
            if (requestSettings != null)
            {
                FancyConsole.WriteLine();
                FancyConsole.Write(ConsoleHeaderColor, "  With scenario \"{1}\"...", method.DisplayName, requestSettings.Description);
                indentLevel = "  ";
            }

            FancyConsole.VerboseWriteLine("");
            FancyConsole.VerboseWriteLineIndented(indentLevel, "Request:");
            var requestPreviewResult = await method.PreviewRequestAsync(requestSettings, rootUrl, credentials);
            if (requestPreviewResult.IsWarningOrError)
            {
                WriteOutErrors(requestPreviewResult.Messages, indentLevel + "  ");
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
            return ValidateHttpResponse(docset, method, actualResponse, expectedResponse, indentLevel);
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
                case PublishOptions.SanitizedFormat.Markdown:
                    publisher = new DocumentPublisher(docs);
                    break;
                case PublishOptions.SanitizedFormat.Html:
                    publisher = new DocumentPublisherHtml(docs);
                    break;
                case PublishOptions.SanitizedFormat.Swagger2:
                    publisher = new OneDrive.ApiDocumentation.Validation.Writers.SwaggerWriter(docs);
                    break;
                default:
                    throw new NotSupportedException("Unsupported format: " + options.Format.ToString());
            }

            publisher.VerboseLogging = options.Verbose;
            publisher.SourceFileExtensions = options.TextFileExtensions;
            FancyConsole.WriteLineIndented("  ", "File extensions: {0}", publisher.SourceFileExtensions);

            if (!string.IsNullOrEmpty(options.IgnorePaths))
                publisher.SkipPaths = options.IgnorePaths;
            FancyConsole.WriteLineIndented("  ", "Ignored Paths: {0}", publisher.SkipPaths);
            publisher.PublishAllFiles = options.PublishAllFiles;
            FancyConsole.WriteLineIndented("  ", "Include all files: {0}", publisher.PublishAllFiles);
            FancyConsole.WriteLine();
            
            FancyConsole.WriteLine("Publishing content...");
            publisher.NewMessage += publisher_NewMessage;
            await publisher.PublishToFolderAsync(outputPath);

            FancyConsole.WriteLine(ConsoleSuccessColor, "Finished publishing documentation to: {0}", outputPath);
        }

        static void publisher_NewMessage(object sender, ValidationError e)
        {
            if (!FancyConsole.WriteVerboseOutput && !e.IsError && !e.IsWarning)
                return;

            WriteValidationError("", e);
        }

        private static async Task CheckServiceMetadata(CheckMetadataOptions options)
        {
            if (string.IsNullOrEmpty(options.ServiceMetadataLocation))
            {
                if (!string.IsNullOrEmpty(SavedSettings.Default.ServiceUrl))
                {
                    options.ServiceMetadataLocation = SavedSettings.Default.ServiceUrl + "$metadata";
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

                if (!errors.WereErrors() && !errors.WereWarnings())
                {
                    FancyConsole.WriteLine(ConsoleSuccessColor, "  no errors.");
                    successCount++;
                }
                else
                {
                    if (errors.WereWarnings() && !errors.WereErrors())
                        warningCount++;
                    else
                        errorCount++;
                    
                    FancyConsole.WriteLine();
                    WriteOutErrors(errors, "  ");
                }
                FancyConsole.WriteLine();
            }

            PrintStatusMessage(successCount, warningCount, errorCount);

            Exit(failure: false);
        }
    }
}
>>>>>>> 09c6d6ee641014e5069c880444258173ac17cc8b
