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

        static void Main(string[] args)
        {
            string invokedVerb = null;
            CommonOptions invokedVerbInstance = null;

            var options = new CommandLineOptions();
            if (!CommandLine.Parser.Default.ParseArguments(args, options,
              (verb, subOptions) =>
              {
                  // if parsing succeeds the verb name and correct instance
                  // will be passed to onVerbCommand delegate (string,object)
                  invokedVerb = verb;
                  invokedVerbInstance = (CommonOptions)subOptions;
              }))
            {
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }

            VerboseEnabled = invokedVerbInstance.Verbose;

            Nito.AsyncEx.AsyncContext.Run(() => RunInvokedMethodAsync(invokedVerb, invokedVerbInstance));
        }

        private static async Task RunInvokedMethodAsync(string invokedVerb, CommonOptions invokedVerbInstance)
        {
            switch (invokedVerb)
            {
                case "files":
                    PrintFiles(invokedVerbInstance);
                    break;
                case "links":
                    VerifyLinks(invokedVerbInstance);
                    break;
                case "resources":
                    PrintResources(invokedVerbInstance);
                    break;
                case "methods":
                    PrintMethods(invokedVerbInstance);
                    break;
                case "docs":
                    MethodExampleValidation((ConsistencyCheckOptions)invokedVerbInstance);
                    break;
                case "service":
                    await ServiceCallValidation((ServiceConsistencyOptions)invokedVerbInstance);
                    break;
            }
        }

        private static void VerbosePrint(string output)
        {
            if (VerboseEnabled)
            {
                Console.WriteLine(output);
            }
        }
        private static void VerbosePrint(string format, params object[] parameters)
        {
            if (VerboseEnabled)
            {
                Console.WriteLine(format, parameters);
            }
        }

        private static DocSet GetDocSet(CommonOptions options)
        {
            VerbosePrint("Opening documentation from {0}", options.PathToDocSet);
            DocSet set = new DocSet(options.PathToDocSet);

            VerbosePrint("Scanning documentation files...");
            ValidationError[] loadErrors;
            if (!set.ScanDocumentation(out loadErrors) && options.Verbose)
            {
                WriteOutErrors(loadErrors);
            }

            var serviceOptions = options as ServiceConsistencyOptions;
            if (null != serviceOptions)
            {
                VerbosePrint("Reading configuration parameters...");
                bool success = set.TryReadRequestParameters(serviceOptions.ParameterSource);
                if (!success)
                {
                    Console.WriteLine("WARNING: Unable to read request parameter configuration file: {0}", serviceOptions.ParameterSource);
                }
            }

            return set;
        }

        private static void PrintFiles(CommonOptions options)
        {
            var docset = GetDocSet(options);

            string format = options.Verbose ? "{0} => {1} (resources: {2}, methods: {3})" : "{0} (r:{2}, m:{3})";
            foreach (var file in docset.Files)
            {
                Console.WriteLine(format, file.DisplayName, file.FullPath, file.Resources.Length, file.Requests.Length);
            }
        }

        private static void VerifyLinks(CommonOptions options)
        {
            var docset = GetDocSet(options);
            ValidationError[] errors;
            if (!docset.ValidateLinks(options.Verbose, out errors))
            {
                foreach (var error in errors)
                {
                    Console.WriteLine(error.ErrorText);
                }
                Environment.Exit(ExitCodeFailure);
            }
            else
            {
                Console.WriteLine("No link errors detected.");
                Environment.Exit(ExitCodeSuccess);
            }
        }

        private static void PrintResources(CommonOptions options)
        {
            var docset = GetDocSet(options);
            Console.WriteLine();

            foreach (var resource in docset.Resources)
            {
                Console.WriteLine(resource.ResourceType);

                if (!options.ShortForm && options.Verbose)
                {
                    string metadata = JsonConvert.SerializeObject(resource.Metadata);
                    Console.WriteLine(string.Concat("Metadata: ", metadata));
                    Console.WriteLine();
                }

                if (!options.ShortForm)
                {
                    Console.WriteLine(resource.JsonExample);
                    Console.WriteLine();
                }
            }
        }

        private static void PrintMethods(CommonOptions options)
        {
            var docset = GetDocSet(options);
            Console.WriteLine();

            foreach (var method in docset.Methods)
            {
                var requestMetadata = options.Verbose ? JsonConvert.SerializeObject(method.RequestMetadata) : string.Empty;
                var responseMetadata = options.Verbose ? JsonConvert.SerializeObject(method.ExpectedResponseMetadata) : string.Empty;

                string headerFormatText = options.Verbose ? "{0}: {1}" : "{0}";

                Console.WriteLine("Method \"{0}\"", method.DisplayName);
                Console.WriteLine(headerFormatText, "Request", requestMetadata);
                Console.WriteLine(options.ShortForm ? method.Request.TopLineOnly() : method.Request);
                Console.WriteLine();
                if (!options.ShortForm)
                {
                    Console.WriteLine(headerFormatText, "Response", responseMetadata);
                    Console.WriteLine(method.ExpectedResponse);
                    Console.WriteLine();
                }
                Console.WriteLine();
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
            Console.WriteLine();

            MethodDefinition[] methods = TestMethods(options, docset);

            bool result = true;
            foreach (var method in methods)
            {
                Console.WriteLine();
                Console.Write("Checking \"{0}\"...", method.DisplayName);

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
                    Console.WriteLine("Unable to locate method '{0}' in docset.", options.MethodName);
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

        private static void WriteOutErrors(ValidationError[] errors, string indent = "")
        {
            foreach (var error in errors)
            {
                Console.ForegroundColor = error.IsWarning ? ConsoleColor.Yellow : ConsoleColor.Red;
                Console.WriteLine(string.Concat(indent, error.ErrorText));
                Console.ResetColor();
            }
        }

        private static bool ValidateHttpResponse(DocSet docset, MethodDefinition method, HttpResponse response, HttpResponse expectedResponse = null)
        {
            ValidationError[] errors;
            if (!docset.ValidateApiMethod(method, response, expectedResponse, out errors))
            {
                Console.WriteLine();
                WriteOutErrors(errors, "  ");
                return false;
            }
            else
            {
                Console.WriteLine(" no errors");
                return true;
            }
        }

        private static async Task ServiceCallValidation(ServiceConsistencyOptions options)
        {
            var docset = GetDocSet(options);
            Console.WriteLine();

            var methods = TestMethods(options, docset);

            bool result = true;
            foreach (var method in methods)
            {
                Console.WriteLine();
                Console.Write("Calling method \"{0}\"...", method.DisplayName);

                var requestParams = docset.RequestParamtersForMethod(method);
                VerbosePrint("");
                VerbosePrint("Request:");
                VerbosePrint(method.PreviewRequest(requestParams).FullHttpText());

                var parser = new HttpParser();
                var expectedResponse = parser.ParseHttpResponse(method.ExpectedResponse);
                
                var actualResponse = await method.ApiResponseForMethod(options.ServiceRootUrl, options.AccessToken, requestParams);

                VerbosePrint("Response:");
                VerbosePrint(actualResponse.FullHttpText());

                VerbosePrint("Validation results:");
                result &= ValidateHttpResponse(docset, method, actualResponse, expectedResponse);

                if (options.PauseBetweenRequests)
                {
                    Console.Write("Press any key to continue");
                    Console.ReadKey();
                    Console.WriteLine();
                }
            }

            if (!result)
                Environment.Exit(ExitCodeFailure);
            else
                Environment.Exit(ExitCodeSuccess);
        }
        

    }
}
