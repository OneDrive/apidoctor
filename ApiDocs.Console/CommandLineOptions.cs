using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using CommandLine.Text;

namespace ApiDocs.ConsoleApp
{
    using System.Data.SqlTypes;
    using ApiDocs.ConsoleApp.Auth;
    using ApiDocs.Validation.Writers;

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
        
        [VerbOption(VerbPrint, HelpText="Print files, resources, and methods discovered in the documentation.")]
        public PrintOptions PrintVerbOptions { get; set; }

        [VerbOption(VerbCheckLinks, HelpText = "Verify links in the documentation aren't broken.")]
        public DocSetOptions CheckLinksVerb { get; set; }

        [VerbOption(VerbDocs, HelpText = "Check for errors in the documentation (resources + examples).")]
        public BasicCheckOptions CheckDocsVerb { get; set; }

        [VerbOption(VerbService, HelpText = "Check for errors between the documentation and service.")]
        public CheckServiceOptions CheckServiceVerb { get; set; }

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

        [Option("appveyor-url", HelpText="Specify the AppVeyor Build Worker API URL for output integration")]
        public string AppVeyorServiceUrl { get; set; }

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

    /// <summary>
    /// Command line options for any verbs that work with a documentation set.
    /// </summary>
    class DocSetOptions : BaseOptions
    {
        internal const string PathArgument = "path";
        internal const string VerboseArgument = "verbose";

        [Option('p', PathArgument, HelpText = "Path to the documentation set. Can set a default value using the set verb, otherwise defaults to the current working folder.")]
        public string DocumentationSetPath { get; set; }

        [Option(VerboseArgument, HelpText = "Output verbose messages.")]
        public bool EnableVerboseOutput { get; set; }

        /// <summary>
        /// Checks to see if this options block has all the required properties (or that we had values in the settings for them)
        /// </summary>
        /// <returns></returns>
        public override bool HasRequiredProperties(out string[] missingArguments)
        {
            string value = this.DocumentationSetPath;
            if (!MakePropertyValid(ref value, Program.DefaultSettings.DocumentationPath))
            {
                this.DocumentationSetPath = Environment.CurrentDirectory;
            }
            else
            {
                this.DocumentationSetPath = value;
            }

            FancyConsole.WriteLine("Documentation path: " + this.DocumentationSetPath);

            missingArguments = new string[0];
            return true;
        }
    }

    /// <summary>
    /// Command line options for the set verb, which allows storing default values for configuration settings.
    /// </summary>
    class SetCommandOptions : BaseOptions
    {
        [Option(DocSetOptions.PathArgument, HelpText="Set the default path for the documentation set location.")]
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
            if (string.IsNullOrEmpty(this.DocumentationPath) && string.IsNullOrEmpty(this.AccessToken) &&
                string.IsNullOrEmpty(this.ServiceUrl) && !this.ResetStoredValues && !this.PrintValues)
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


            if (!this.PrintFiles && !this.PrintResources && !this.PrintMethods)
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

    class BasicCheckOptions : DocSetOptions
    {
        [Option('m', "method", HelpText = "Name of the method to test. If omitted, all defined methods are tested.", MutuallyExclusiveSet="fileOrMethod")]
        public string MethodName { get; set; }

        [Option("file", HelpText="Name of the doc file to test. If missing, all methods are tested.", MutuallyExclusiveSet="fileOrMethod")]
        public string FileName { get; set; }

        [Option("force-all", HelpText="Force all defined scenarios to be executed, even if disabled.")]
        public bool ForceAllScenarios { get; set; }
    }

    class CheckServiceOptions : BasicCheckOptions
    {
        private const string AccessTokenArgument = "access-token";
        private const string ServiceUrlArgument = "url";

        public CheckServiceOptions()
        {
            this.FoundAccounts = new List<Account>();
        }

        // Three ways to "connect" to an account
        // 1. Using access-token and url
        [Option('t', AccessTokenArgument, HelpText = "OAuth access token. Required if not default value is set.")]
        public string AccessToken { get; set; }

        [Option('u', ServiceUrlArgument, HelpText = "Root URL for API calls, like https://api.example.org/v1.0. Required if not default value is set.")]
        public string ServiceRootUrl { get; set; }

        // 2. Using environment variables for oauth properties - auto-detected
        // 3. Using an accounts file - auto-detected
        [Option("account", HelpText="Specify the name of an account in the account configuration file. If omitted all enabled accounts will be used.")]
        public string AccountName { get; set; }

        [Option("pause", HelpText="Pause between method requests.")]
        public bool PauseBetweenRequests { get; set; }

        [Option("headers", HelpText = "Additional headers to add to requests to the service. For example If-Match: *")]
        public string AdditionalHeaders { get; set; }

        [Option("odata-metadata", HelpText="Set the odata.metadata level in the accept header.", DefaultValue=null)]
        public string ODataMetadataLevel { get; set; }

        public List<Account> FoundAccounts { get; set; }

        [Option("branch-name")]
        public string BranchName { get; set; }


        public override bool HasRequiredProperties(out string[] missingArguments)
        {
            var result = base.HasRequiredProperties(out missingArguments);
            if (!result) return result;

            List<string> props = new List<string>(missingArguments);
            Program.LoadCurrentConfiguration(this);

            if (string.IsNullOrEmpty(this.AccessToken))
            {
                // Try to add accounts from configuration file in the documentation
                Account[] accounts = null;
                if (Program.CurrentConfiguration != null)
                    accounts = Program.CurrentConfiguration.Accounts;
                if (null != accounts)
                {
                    this.FoundAccounts.AddRange(accounts);
                }

                // Try to add an account from environment variables
                try
                {
                    var account = Account.CreateAccountFromEnvironmentVariables();
                    account.ServiceUrl = this.ServiceRootUrl;
                    this.FoundAccounts.Add(account);
                }
                catch (InvalidOperationException) { }
            }

            var hasAccount = (this.FoundAccounts.Select(x => x.Enabled).Any());

            if (!hasAccount)
            {
                string checkValue = this.AccessToken;
                if (!MakePropertyValid(ref checkValue, Program.DefaultSettings.AccessToken))
                {
                    props.Add(AccessTokenArgument);
                }
                else
                {
                    this.AccessToken = checkValue;
                }

                checkValue = this.ServiceRootUrl;
                if (!MakePropertyValid(ref checkValue, Program.DefaultSettings.ServiceUrl))
                {
                    props.Add(ServiceUrlArgument);
                }
                this.ServiceRootUrl = checkValue;

                if (!string.IsNullOrEmpty(this.AccessToken) && !string.IsNullOrEmpty(this.ServiceRootUrl))
                {
                    this.FoundAccounts.Add(new Account { Name = "CommandLineAccount", Enabled = true, AccessToken = this.AccessToken, ServiceUrl = this.ServiceRootUrl });
                }
            }

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

        [Option("line-ending",
            DefaultValue=LineEndings.Default,
            HelpText="Change the line endings for output files. Values: default, windows, unix, or macintosh")]
        public LineEndings LineEndings { get; set; } 


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

        public override bool HasRequiredProperties(out string[] missingArguments)
        {
            string[] baseResults;
            if (!base.HasRequiredProperties(out baseResults))
            {
                missingArguments = baseResults;
                return false;
            }

            List<string> missingArgs = new List<string>();
            if (this.Format == PublishFormat.Mustache)
            {
                if (string.IsNullOrEmpty(this.TemplateFolder))
                    missingArgs.Add("template");
            }
            if (this.Format == PublishFormat.Swagger2)
            {
                if (string.IsNullOrEmpty(this.Title))
                    missingArgs.Add("swagger-title");
                if (string.IsNullOrEmpty(this.Description))
                    missingArgs.Add("swagger-description");
                if (string.IsNullOrEmpty(this.Version))
                    missingArgs.Add("swagger-version");
            }

            missingArguments = missingArgs.ToArray();
            return missingArgs.Count == 0;
        }

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
