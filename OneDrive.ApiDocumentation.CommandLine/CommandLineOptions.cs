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
        public const string VerbCheckLinks = "links";
        public const string VerbSet = "set";
        public const string VerbDocs = "check-docs";
        public const string VerbService = "check-service";

        public CommandLineOptions()
        {

        }

        [VerbOption(VerbPrint, HelpText="Print files, resources, and methods discovered in the documentation.")]
        public PrintOptions PrintVerbOptions { get; set; }

        [VerbOption(VerbCheckLinks, HelpText = "Verify links in the documentation aren't broken.")]
        public CommandOptions LinksVerb { get; set; }

        [VerbOption(VerbDocs, HelpText = "Check for errors in the documentation (resources + examples).")]
        public ConsistencyCheckOptions InternalConsistencyVerb { get; set; }

        [VerbOption(VerbService, HelpText = "Check for errors between the documentation and service.")]
        public ServiceConsistencyOptions ServiceConsistencyVerb { get; set; }

        [VerbOption(VerbSet, HelpText = "Save or reset default parameter values.")]
        public SetCommandOptions SetVerb { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }

    class BaseOptions
    {
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

    class CommandOptions : BaseOptions
    {
        private const string PathArgument = "path";
        private const string ShortArgument = "short";
        private const string VerboseArgument = "verbose";

        [Option('p', PathArgument, HelpText = "Path to the documentation set. Required if no default value set.")]
        public string PathToDocSet { get; set; }

        [Option(ShortArgument, HelpText = "Print output in single line format.")]
        public bool ShortForm { get; set; }

        [Option(VerboseArgument, HelpText = "Print verbose output.")]
        public bool Verbose { get; set; }

        /// <summary>
        /// Checks to see if this options block has all the required properties (or that we had values in the settings for them)
        /// </summary>
        /// <returns></returns>
        public override bool HasRequiredProperties(out string[] missingArguments)
        {
            List<string> props = new List<string>();

            string value = PathToDocSet;
            if (!MakePropertyValid(ref value, Properties.Settings.Default.DocumentationPath))
            {
                props.Add(PathArgument);
            }
            else
            {
                PathToDocSet = value;
            }

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

    class PrintOptions : CommandOptions
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

    class ConsistencyCheckOptions : CommandOptions
    {
        [Option('m', "method", HelpText = "Name of the method to test. If missing, all methods are tested.")]
        public string MethodName { get; set; }
    }

    class ServiceConsistencyOptions : ConsistencyCheckOptions
    {
        private const string AccessTokenArgument = "access-token";
        private const string ServiceUrlArgument = "url";
        private const string ParameterFileArgument = "parameter-file";

        [Option('t', AccessTokenArgument, HelpText = "OAuth access token. Required if not default value is set.")]
        public string AccessToken { get; set; }

        [Option('u', ServiceUrlArgument, HelpText = "Root URL for API calls, like https://api.onedrive.com/v1.0. Required if not default value is set.")]
        public string ServiceRootUrl { get; set; }

        [Option('f', ParameterFileArgument, DefaultValue = "/internal/api-automation.json", HelpText = "Source for parameter values in API calls")]
        public string ParameterSource { get; set; }

        [Option("pause", HelpText="Pause between method requests.")]
        public bool PauseBetweenRequests { get; set; }

        public override bool HasRequiredProperties(out string[] missingArguments)
        {
            bool result = base.HasRequiredProperties(out missingArguments);
            if (!result) return result;

            List<string> props = new List<string>(missingArguments);
            
            string checkValue = AccessToken;
            if (!MakePropertyValid(ref checkValue, Properties.Settings.Default.AccessToken))
                props.Add(AccessTokenArgument);
            else
                AccessToken = checkValue;

            checkValue = ServiceRootUrl;
            if (!MakePropertyValid(ref checkValue, Properties.Settings.Default.ServiceUrl))
                props.Add(ServiceUrlArgument);
            ServiceRootUrl = checkValue;

            missingArguments = props.ToArray();
            return missingArguments.Length == 0;
        }

    }
}
