using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace OneDrive.ApiDocumentation.ConsoleApp
{
    class CommandLineOptions
    {
        public const string VerbPrint = "print";
        public const string VerbCheckLinks = "check-links";
        public const string VerbSet = "set";
        public const string VerbDocs = "check-docs";
        public const string VerbService = "check-service";
        public const string VerbPublish = "publish";
        public const string VerbMetadata = "check-metadata";
        public const string VerbAbout = "about";
        

        public CommandLineOptions()
        {

        }

        [VerbOption(VerbPrint, HelpText="Print files, resources, and methods discovered in the documentation.")]
        public PrintOptions PrintVerbOptions { get; set; }

        [VerbOption(VerbCheckLinks, HelpText = "Verify links in the documentation aren't broken.")]
        public DocSetOptions LinksVerb { get; set; }

        [VerbOption(VerbDocs, HelpText = "Check for errors in the documentation (resources + examples).")]
        public ConsistencyCheckOptions InternalConsistencyVerb { get; set; }

        [VerbOption(VerbService, HelpText = "Check for errors between the documentation and service.")]
        public ServiceConsistencyOptions ServiceConsistencyVerb { get; set; }

        [VerbOption(VerbSet, HelpText = "Save or reset default parameter values.")]
        public SetCommandOptions SetVerb { get; set; }

        [VerbOption(VerbPublish, HelpText="Publish a version of the documentation, optionally converting it into other formats.")]
        public PublishOptions PublishVerb { get; set; }

        [VerbOption(VerbMetadata, HelpText="Check service CSDL metadata against documentation.")]
        public CheckMetadataOptions CheckMetadataVerb { get; set; }

        [VerbOption(VerbAbout, HelpText="Print about information for this application.")]
        public BaseOptions AboutVerb { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }

    class BaseOptions
    {

        [Option("log", HelpText="Write the console output to file.")]
        public string LogFile { get; set; }

        [Option("ignore-warnings", HelpText = "Ignore warnings as errors for pass rate.")]
        public bool IgnoreWarnings { get; set; }

        [Option("silence-warnings", HelpText = "Don't print warnings to the screen or consider them errors")]
        public bool SilenceWarnings { get; set; }


#if DEBUG
        [Option("debug", HelpText="Launch the debugger before doing anything interesting")]
#endif
        public bool AttachDebugger { get; set; }


        public virtual bool HasRequiredProperties(out string[] missingArguments)
        {
            missingArguments = new string[0];
            return true;
        }

        protected static bool MakePropertyValid(ref string value, string storedValue)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (!string.IsNullOrEmpty(storedValue))
                    value = storedValue;
                else
                    return false;
            }
            return true;
        }

    }

    class DocSetOptions : BaseOptions
    {
        private const string PathArgument = "path";
        private const string ShortArgument = "short";
        private const string VerboseArgument = "verbose";
        private const string LoadErrorArguments = "show-load-warnings";

        [Option('p', PathArgument, HelpText = "Path to the documentation set. Required if no default value set.")]
        public string PathToDocSet { get; set; }

        [Option(ShortArgument, HelpText = "Print output in single line format.")]
        public bool ShortForm { get; set; }

        [Option(VerboseArgument, HelpText = "Print verbose output.")]
        public bool Verbose { get; set; }

        [Option(LoadErrorArguments, HelpText="Show document set load messages and warnings")]
        public bool ShowLoadWarnings { get; set; }

        /// <summary>
        /// Checks to see if this options block has all the required properties (or that we had values in the settings for them)
        /// </summary>
        /// <returns></returns>
        public override bool HasRequiredProperties(out string[] missingArguments)
        {
            List<string> props = new List<string>();

            string value = PathToDocSet;
            if (!MakePropertyValid(ref value, Program.DefaultSettings.DocumentationPath))
            {
                
                PathToDocSet = Environment.CurrentDirectory;
            }
            else
            {
                PathToDocSet = value;
            }

            FancyConsole.WriteLine("Documentation path: " + PathToDocSet);

            missingArguments = props.ToArray();
            return missingArguments.Length == 0;
        }
    }

    class SetCommandOptions : BaseOptions
    {
        [Option("path", HelpText="Set the default documentation folder.")]
        public string DocumentationPath { get; set; }

        [Option("access-token", HelpText="Set the default access token.")]
        public string AccessToken { get; set; }

        [Option("url", HelpText="Set the default service URL.")]
        public string ServiceUrl { get; set; }

        [Option("reset", HelpText="Clear any stored values.")]
        public bool ResetStoredValues {get;set;}

        [Option("print", HelpText="Print out the currently stored values.")]
        public bool PrintValues { get; set; }

        public override bool HasRequiredProperties(out string[] missingArguments)
        {
            if (string.IsNullOrEmpty(DocumentationPath) && string.IsNullOrEmpty(AccessToken) &&
                string.IsNullOrEmpty(ServiceUrl) && !ResetStoredValues && !PrintValues)
            {
                missingArguments = new string[] { "One or more arguments are required. Check the usage of this command for more information." };
            }
            else
            {
                missingArguments = new string[0];
            }
            return missingArguments.Length == 0;
        }
    }

    class CheckMetadataOptions : DocSetOptions
    {
        [Option("metadata", HelpText="Path or URL for the service metadata CSDL")]
        public string ServiceMetadataLocation { get; set; }
    }

    class PrintOptions : DocSetOptions
    {
        [Option("files", HelpText="Print the files discovered as part of the documentation")]
        public bool PrintFiles { get; set; }
        
        [Option("resources", HelpText="Print the resources discovered in the documentation")]
        public bool PrintResources { get; set; }
        
        [Option("methods", HelpText="Print the methods discovered in the documentation.")]
        public bool PrintMethods { get; set; }

        public override bool HasRequiredProperties(out string[] missingArguments)
        {
            if (!base.HasRequiredProperties(out missingArguments))
                return false;


            if (!PrintFiles && !PrintResources && !PrintMethods)
            {
                missingArguments = new string[] { "One or more arguments to specify what to display are required." };
            }
            else
            {
                missingArguments = new string[0];
            }
            return missingArguments.Length == 0;
        }
    }

    class ConsistencyCheckOptions : DocSetOptions
    {
        [Option('m', "method", HelpText = "Name of the method to test. If missing, all methods are tested.", MutuallyExclusiveSet="fileOrMethod")]
        public string MethodName { get; set; }

        [Option("file", HelpText="Name of the doc file to test. If missing, all methods are tested.", MutuallyExclusiveSet="fileOrMethod")]
        public string FileName { get; set; }

    }

    class ServiceConsistencyOptions : ConsistencyCheckOptions
    {
        private const string AccessTokenArgument = "access-token";
        private const string ServiceUrlArgument = "url";
        private const string ParameterFileArgument = "scenarios";

        [Option('t', AccessTokenArgument, HelpText = "OAuth access token. Required if not default value is set.")]
        public string AccessToken { get; set; }

        [Option('u', ServiceUrlArgument, HelpText = "Root URL for API calls, like https://api.example.org/v1.0. Required if not default value is set.")]
        public string ServiceRootUrl { get; set; }

        [Option('s', ParameterFileArgument, DefaultValue = "/tests/test-scenarios.json", HelpText = "Test scenarios configuration file")]
        public string ScenarioFilePath { get; set; }

        [Option("pause", HelpText="Pause between method requests.")]
        public bool PauseBetweenRequests { get; set; }


        // OAuth 2 Token Generator Properties
        [Option("refresh-token", HelpText="Refresh token for authentication.")]
        public string RefreshToken { get; set; }
        [Option("token-service", HelpText = "OAuth 2 token service to exchange refresh token for access token")]
        public string TokenServiceUrl { get; set; }
        [Option("client-id", HelpText = "Client ID used in token generation")]
        public string ClientId { get; set; }
        [Option("client-secret", HelpText = "Client secret for token generation")]
        public string ClientSecret { get; set; }
        [Option("redirect-uri", HelpText = "Redirect URI used with refresh token")]
        public string RedirectUri { get; set; }

        [Option("use-environment-variables", HelpText="Read OAuth values from environment variables")]
        public bool UseEnvironmentVariables { get; set; }


        public override bool HasRequiredProperties(out string[] missingArguments)
        {
            bool result = base.HasRequiredProperties(out missingArguments);
            if (!result) return result;

            List<string> props = new List<string>(missingArguments);

            string checkValue = null;

            if (!UseEnvironmentVariables)
            {
                checkValue = AccessToken;
                if (!MakePropertyValid(ref checkValue, Program.DefaultSettings.AccessToken))
                    props.Add(AccessTokenArgument);
                else
                    AccessToken = checkValue;
            }

            checkValue = ServiceRootUrl;
            if (!MakePropertyValid(ref checkValue, Program.DefaultSettings.ServiceUrl))
                props.Add(ServiceUrlArgument);
            ServiceRootUrl = checkValue;

            missingArguments = props.ToArray();
            return missingArguments.Length == 0;
        }

    }

    class PublishOptions : DocSetOptions
    {
        [Option("output", Required=true, HelpText="Output directory for sanitized documentation.")]
        public string OutputDirectory { get; set; }

        [Option("format", DefaultValue=PublishFormat.Markdown, HelpText="Format of the output documentation.")]
        public PublishFormat Format { get; set; }

        [Option("template", HelpText = "Specify the folder where output template files are located.")]
        public string TemplateFolder { get; set; }



        #region Swagger2 output controls

        [Option("swagger-title", DefaultValue=null, HelpText="Title to include in the published documentation")]
        public string Title { get; set; }
        
        [Option("swagger-description", DefaultValue = null, HelpText = "Description to include in the published documentation")]
        public string Description { get; set; }
        
        [Option("swagger-version", DefaultValue=null, HelpText="Api Version information to include in documentation")]
        public string Version { get; set; }

        [Option("swagger-auth-scope", HelpText = "Override the auth scope detection with a default auth scope on every method")]
        public string AuthScopeDefault { get; set; }


        #endregion


        public enum PublishFormat
        {
            Markdown,
            Html,
            Swagger2,
            Outline,
            Mustache
        }
    }
}
