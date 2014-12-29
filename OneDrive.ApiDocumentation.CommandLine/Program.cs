using Newtonsoft.Json;
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
        static bool VerboseEnabled { get; set; }

        private const int ExitCodeFailure = 1;
        private const int ExitCodeSuccess = 0;

        private const ConsoleColor ConsoleDefaultColor = ConsoleColor.Gray;
        private const ConsoleColor ConsoleHeaderColor = ConsoleColor.Cyan;
        private const ConsoleColor ConsoleSubheaderColor = ConsoleColor.DarkCyan;
        private const ConsoleColor ConsoleCodeColor = ConsoleColor.DarkGray;
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
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }

            var commandOptions = verbOptions as DocSetOptions;
            if (null != commandOptions)
            {
                VerboseEnabled = commandOptions.Verbose;
            }

            Nito.AsyncEx.AsyncContext.Run(() => RunInvokedMethodAsync(options, verbName, verbOptions));
        }

        private static async Task RunInvokedMethodAsync(CommandLineOptions origCommandLineOpts, string invokedVerb, BaseOptions options)
        {
            string[] missingProps;
            if (!options.HasRequiredProperties(out missingProps))
            {
                var error = new ValidationError(null, "Command line is missing required arguments: {0}", missingProps.ComponentsJoinedByString(", "));
                FancyConsole.WriteLine(origCommandLineOpts.GetUsage(invokedVerb));
                WriteOutErrors(new ValidationError[] { error });
                Environment.Exit(ExitCodeFailure);
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
                    MethodExampleValidation((ConsistencyCheckOptions)options);
                    break;
                case CommandLineOptions.VerbService:
                    await ServiceCallValidation((ServiceConsistencyOptions)options);
                    break;
                case CommandLineOptions.VerbSet:
                    SetDefaultValues((SetCommandOptions)options);
                    break;
                case CommandLineOptions.VerbClean:
                    await PublishDocumentationAsync((PublishOptions)options);
                    break;
            }
        }


        private static void SetDefaultValues(SetCommandOptions setCommandOptions)
        {
            var settings = Properties.Settings.Default;
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
                FancyConsole.WriteLine(ConsoleHeaderColor, "Stored settings:");
                FancyConsole.WriteLineIndented("  ", "{0}: {1}", "AccessToken", settings.AccessToken);
                FancyConsole.WriteLineIndented("  ", "{0}: {1}", "DocumentationPath", settings.DocumentationPath);
                FancyConsole.WriteLineIndented("  ", "{0}: {1}", "ServiceUrl", settings.ServiceUrl);
            }
            
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
            if (!set.ScanDocumentation(out loadErrors) && options.Verbose)
            {
                WriteOutErrors(loadErrors);
            }

            var serviceOptions = options as ServiceConsistencyOptions;
            if (null != serviceOptions)
            {
                FancyConsole.VerboseWriteLine("Reading configuration parameters...");

                set.RunParameters = new RunMethodParameters(set, serviceOptions.ParameterSource);
                if (!set.RunParameters.Loaded)
                {
                    FancyConsole.WriteLine(ConsoleWarningColor, "WARNING: Unable to read request parameter configuration file: {0}", serviceOptions.ParameterSource);
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
            if (!docset.ValidateLinks(options.Verbose, out errors))
            {
                WriteOutErrors(errors);
                Environment.Exit(ExitCodeFailure);
            }
            else
            {
                FancyConsole.WriteLine(ConsoleSuccessColor, "No link errors detected.");
                Environment.Exit(ExitCodeSuccess);
            }
        }

        private static void PrintResources(DocSetOptions options, DocSet docset)
        {
            if (null == docset)
                docset = GetDocSet(options);

            FancyConsole.WriteLine();
            FancyConsole.WriteLine(ConsoleHeaderColor, "Defined resources:");
            FancyConsole.WriteLine();

            foreach (var resource in docset.Resources)
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

        private static void MethodExampleValidation(ConsistencyCheckOptions options)
        {
            var docset = GetDocSet(options);
            FancyConsole.WriteLine();

            MethodDefinition[] methods = TestMethods(options, docset);

            bool result = true;
            foreach (var method in methods)
            {
                FancyConsole.Write(ConsoleHeaderColor, "Checking \"{0}\" in {1}...", method.DisplayName, method.SourceFile.DisplayName);

                var parser = new HttpParser();
                var expectedResponse = parser.ParseHttpResponse(method.ExpectedResponse);
                result &= ValidateHttpResponse(docset, method, expectedResponse);
            }

            if (!result)
                Environment.Exit(ExitCodeFailure);
            else
                Environment.Exit(ExitCodeSuccess);
        }

        private static MethodDefinition[] TestMethods(ConsistencyCheckOptions options, DocSet docset)
        {
            MethodDefinition[] methods = null;
            if (!string.IsNullOrEmpty(options.MethodName))
            {
                var foundMethod = LookUpMethod(docset, options.MethodName);
                if (null == foundMethod)
                {
                    FancyConsole.WriteLine(ConsoleErrorColor, "Unable to locate method '{0}' in docset.", options.MethodName);
                    Environment.Exit(ExitCodeFailure);
                }
                methods = new MethodDefinition[] { LookUpMethod(docset, options.MethodName) };
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
                if (!error.IsWarning && !error.IsError && !VerboseEnabled)
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

        private static bool ValidateHttpResponse(DocSet docset, MethodDefinition method, HttpResponse response, HttpResponse expectedResponse = null)
        {
            ValidationError[] errors;
            if (!docset.ValidateApiMethod(method, response, expectedResponse, out errors))
            {
                FancyConsole.WriteLine();
                WriteOutErrors(errors, "  ");
                FancyConsole.WriteLine();
                return false;
            }
            else
            {
                FancyConsole.WriteLine(ConsoleSuccessColor, " no errors");
                return true;
            }
        }

        private static async Task ServiceCallValidation(ServiceConsistencyOptions options)
        {
            var docset = GetDocSet(options);
            FancyConsole.WriteLine();

            var methods = TestMethods(options, docset);

            bool result = true;
            foreach (var method in methods)
            {
                FancyConsole.WriteLine();
                FancyConsole.Write(ConsoleHeaderColor, "Calling method \"{0}\"...", method.DisplayName);

                var requestParams = docset.RunParameters.RunParamtersForMethod(method);
                FancyConsole.VerboseWriteLine("");
                FancyConsole.VerboseWriteLine("Request:");
                FancyConsole.VerboseWriteLineIndented("  ", method.PreviewRequest(requestParams).FullHttpText());

                var parser = new HttpParser();
                var expectedResponse = parser.ParseHttpResponse(method.ExpectedResponse);
                
                var actualResponse = await method.ApiResponseForMethod(options.ServiceRootUrl, options.AccessToken, requestParams);

                FancyConsole.VerboseWriteLine("Response:");
                FancyConsole.VerboseWriteLineIndented("  ", actualResponse.FullHttpText());

                FancyConsole.VerboseWriteLine("Validation results:");
                result &= ValidateHttpResponse(docset, method, actualResponse, expectedResponse);

                if (options.PauseBetweenRequests)
                {
                    FancyConsole.Write("Press any key to continue");
                    Console.ReadKey();
                    FancyConsole.WriteLine();
                }
            }

            if (!result)
                Environment.Exit(ExitCodeFailure);
            else
                Environment.Exit(ExitCodeSuccess);
        }

        private static async Task PublishDocumentationAsync(PublishOptions options)
        {
            if (options.Format != PublishOptions.SanitizedFormat.Markdown)
            {
                FancyConsole.WriteLine(ConsoleErrorColor, "Publish format not yet implemented: {0}", options.Format);
                return;
            }

            var outputPath = options.OutputDirectory;
            var inputPath = options.PathToDocSet;

            FancyConsole.WriteLine("Publishing documentation to {0}", outputPath);

            var publisher = new OneDrive.ApiDocumentation.Validation.Publish.DocumentPublisher(inputPath);
            
            publisher.VerboseLogging = options.Verbose;
            publisher.TextFileExtensions = options.TextFileExtensions;
            FancyConsole.WriteLineIndented("  ", "File extensions: {0}", publisher.TextFileExtensions);

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
            if (!VerboseEnabled && !e.IsError && !e.IsWarning)
                return;

            WriteValidationError("", e);
        }
    }
}
