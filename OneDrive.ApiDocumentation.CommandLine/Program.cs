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
                    throw new NotImplementedException();
                    break;
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
            set.ScanDocumentation();

            return set;
        }

        private static void PrintFiles(CommonOptions options)
        {
            var docset = GetDocSet(options);

            string format = options.Verbose ? "{0} => {1} (r: {2}, m: {3})" : "{0} (r: {2}, m:{3})";
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

                if (options.Verbose)
                {
                    string metadata = JsonConvert.SerializeObject(resource.Metadata);
                    Console.WriteLine(string.Concat("Metadata: ", metadata));
                    Console.WriteLine();
                }

                Console.WriteLine(resource.JsonExample);
                Console.WriteLine();
            }
        }

        private static void PrintMethods(CommonOptions options)
        {
            var docset = GetDocSet(options);
            Console.WriteLine();

            foreach (var method in docset.Methods)
            {
                var requestMetadata = options.Verbose ? JsonConvert.SerializeObject(method.RequestMetadata) : string.Empty;
                var responseMetadata = options.Verbose ? JsonConvert.SerializeObject(method.ResponseMetadata) : string.Empty;

                Console.WriteLine("Method \"{0}\"", method.DisplayName);
                Console.WriteLine("Request: {0}", requestMetadata);
                Console.WriteLine(method.Request);
                Console.WriteLine();
                Console.WriteLine("Response: {0}", responseMetadata );
                Console.WriteLine(method.Response);
                Console.WriteLine();
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

            MethodDefinition[] methods = null;
            if (!string.IsNullOrEmpty(options.MethodName))
            {
                var foundMethod = LookUpMethod(docset, options.MethodName);
                if (null == foundMethod)
                {
                    Console.WriteLine("Unable to locate method '{0}' in docset.", options.MethodName);
                    Environment.Exit(ExitCodeFailure);
                    return;
                }
                methods = new MethodDefinition[] { LookUpMethod(docset, options.MethodName) };
            }
            else
            {
                methods = docset.Methods;
            }

            bool result = true;
            foreach (var method in methods)
            {
                Console.WriteLine();
                Console.Write("Checking \"{0}\"...", method.DisplayName);

                var parser = new HttpParser();
                var expectedResponse = parser.ParseHttpResponse(method.Response);
                result &= ValidateHttpResponse(docset, method, expectedResponse);
            }

            if (!result)
                Environment.Exit(ExitCodeFailure);
            else
                Environment.Exit(ExitCodeSuccess);
        }

        private static void WriteOutErrors(ValidationError[] errors)
        {
            foreach (var error in errors)
            {
                Console.WriteLine(error.ErrorText);
            }
        }

        private static bool ValidateHttpResponse(DocSet docset, MethodDefinition method, HttpResponse response, HttpResponse expectedResponse = null)
        {
            ValidationError[] errors;
            if (!docset.ValidateApiMethod(method, response, expectedResponse, out errors))
            {
                Console.WriteLine();
                WriteOutErrors(errors);
                return false;
            }
            else
            {
                Console.WriteLine(" no errors");
                return true;
            }
        }
        

    }
}
