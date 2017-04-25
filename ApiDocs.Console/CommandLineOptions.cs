/*
 * Markdown Scanner
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

namespace ApiDocs.ConsoleApp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ApiDocs.ConsoleApp.Auth;
    using ApiDocs.Validation;
    using ApiDocs.Validation.Writers;
    using CommandLine;
    using CommandLine.Text;
    using Publishing.CSDL;

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
        public const string VerbCheckAll = "check-all";
        public const string VerbPublishMetadata = "publish-edmx";
        
        [VerbOption(VerbPrint, HelpText="Print files, resources, and methods discovered in the documentation.")]
        public PrintOptions PrintVerbOptions { get; set; }

        [VerbOption(VerbCheckLinks, HelpText = "Verify links in the documentation aren't broken.")]
        public BasicCheckOptions CheckLinksVerb { get; set; }

        [VerbOption(VerbDocs, HelpText = "Check for errors in the documentation (resources + examples).")]
        public BasicCheckOptions CheckDocsVerb { get; set; }

        [VerbOption(VerbCheckAll, HelpText = "Check for errors in the documentation (links + resources + examples)")]
        public BasicCheckOptions CheckAllVerbs { get; set; }

        [VerbOption(VerbService, HelpText = "Check for errors between the documentation and service.")]
        public CheckServiceOptions CheckServiceVerb { get; set; }

        [VerbOption(VerbPublish, HelpText="Publish a version of the documentation, optionally converting it into other formats.")]
        public PublishOptions PublishVerb { get; set; }

        [VerbOption(VerbPublishMetadata, HelpText="Publish or update metadata based on information in the docset.")]
        public PublishMetadataOptions EdmxPublishVerb { get; set; }

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

        [Option("ignore-errors", HelpText="Prevent errors from generating a non-zero return code.")]
        public bool IgnoreErrors { get; set; }

        [Option("parameters", HelpText = "Specify additional page variables that are used by the publishing engine. URL encoded: key=value&key2=value2.")]
        public string AdditionalPageParameters { get; set; }

        [Option("print-failures-only", HelpText = "Only prints test failures to the console.")]
        public bool PrintFailuresOnly { get; set; }



        public Dictionary<string, string> PageParameterDict {
            get
            {
                if (string.IsNullOrEmpty(AdditionalPageParameters))
                    return null;

                var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                var parameters = Validation.Http.HttpParser.ParseQueryString(AdditionalPageParameters);
                foreach (var key in parameters.AllKeys)
                {
                    data[key] = parameters[key];
                }
                return data;
            }
        }

#if DEBUG
        [Option("debug", HelpText="Launch the debugger before doing anything interesting")]
        public bool AttachDebugger { get; set; }
#endif

        public virtual bool HasRequiredProperties(out string[] missingArguments)
        {
            missingArguments = new string[0];
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
            if (string.IsNullOrEmpty(this.DocumentationSetPath))
            {
                this.DocumentationSetPath = Environment.CurrentDirectory;
            }

            FancyConsole.WriteLine("Documentation path: " + this.DocumentationSetPath);

            missingArguments = new string[0];
            return true;
        }
    }

    class CheckMetadataOptions : DocSetOptions
    {
        [Option("metadata", HelpText = "Path or URL for the service metadata CSDL")]
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

        [Option("accounts", HelpText="Print the list of accounts discovered in the documentation.")]
        public bool PrintAccounts { get; set; }

        public override bool HasRequiredProperties(out string[] missingArguments)
        {
            if (!base.HasRequiredProperties(out missingArguments))
                return false;

            if (!this.PrintFiles && !this.PrintResources && !this.PrintMethods && !this.PrintAccounts)
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

        [Option("file", HelpText="Name of the files to test. Wildcard(*) is allowed. If missing, methods across all files are tested.", MutuallyExclusiveSet="fileOrMethod")]
        public string FileName { get; set; }

        [Option("force-all", HelpText="Force all defined scenarios to be executed, even if disabled.")]
        public bool ForceAllScenarios { get; set; }

        [Option("relax-string-validation", HelpText = "Relax the validation of JSON string properties.")]
        public bool RelaxStringTypeValidation { get; set; }

        [Option("changes-since-branch-only", HelpText="Only perform validation on files changed since the specified branch.")]
        public string FilesChangedFromOriginalBranch { get; set; }

        [Option("git-path", HelpText="Path to the git executable. Required for changes-since-branch-only.")]
        public string GitExecutablePath { get; set; }

        [Option("link-case-match", HelpText = "Require the CaSe of relative links within the content to match the filenames.")]
        public bool RequireFilenameCaseMatch { get; set; }
    }

    class CheckServiceOptions : BasicCheckOptions
    {
        private const string AccessTokenArgument = "access-token";
        private const string ServiceUrlArgument = "url";
        private const string UsernameArgument = "username";
        private const string PasswordArgument = "password";
        private const string HttpLoggerArgument = "httplog";
        private const string IgnoreRequiredScopesArgument = "ignore-scopes";
        private const string ProvidedScopesArgument = "scopes";

        public CheckServiceOptions()
        {
            this.FoundAccounts = new List<IServiceAccount>();
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

        public List<IServiceAccount> FoundAccounts { get; set; }

        [Option("branch-name")]
        public string BranchName { get; set; }

        [Option("parallel", HelpText = "Run service tests in parallel.", DefaultValue = false)]
        public bool ParallelTests { get; set; }

        [Option("username", HelpText = "Provide a username for basic authentication.")]
        public string Username { get; set; }
        [Option("password", HelpText="Provide a password for basic authentication.")]
        public string Password { get; set; }

        [Option(HttpLoggerArgument, HelpText="Create an HTTP Session archive at the specify path.")]
        public string HttpLoggerOutputPath { get; set; }

        [Option(IgnoreRequiredScopesArgument, HelpText="Disable checking accounts for required scopes before calling methods")]
        public bool IgnoreRequiredScopes { get; set; }

        [Option(ProvidedScopesArgument, HelpText = "Comma separated list of scopes provided for the command line account")]
        public string ProvidedScopes { get; set; }

        private IServiceAccount GetEnvironmentVariablesAccount()
        {
            // Try to add an account from environment variables
            try
            {
                var account = OAuthAccount.CreateAccountFromEnvironmentVariables();
                account.BaseUrl = this.ServiceRootUrl;
                return account;
            }
            catch (OAuthAccountException ex)
            {
                Console.WriteLine("Exception while getting account: {0}", ex.Message);
                return null;
            }
        }

        public override bool HasRequiredProperties(out string[] missingArguments)
        {
            var result = base.HasRequiredProperties(out missingArguments);
            if (!result) return result;

            this.FoundAccounts = new List<IServiceAccount>();

            List<string> props = new List<string>(missingArguments);
            Program.LoadCurrentConfiguration(this);

            if (!string.IsNullOrEmpty(this.AccessToken))
            {
                // Run the tests with a single account based on this access token
                if (string.IsNullOrEmpty(this.ServiceRootUrl))
                {
                    props.Add(ServiceUrlArgument);
                }
                else
                {
                    this.FoundAccounts.Add(
                        new OAuthAccount
                        {
                            Name = "command-line-oauth",
                            Enabled = true,
                            AccessToken = this.AccessToken,
                            BaseUrl = this.ServiceRootUrl,
                            Scopes = (null != this.ProvidedScopes) ? this.ProvidedScopes.Split(',') : new string[0]
                        });
                }
            }
            else if (!string.IsNullOrEmpty(this.Username))
            {
                // Run the tests with a single account based on this username + password
                if (string.IsNullOrEmpty(this.Password))
                    props.Add(PasswordArgument);
                if (string.IsNullOrEmpty(this.ServiceRootUrl))
                    props.Add(ServiceUrlArgument);

                if (this.Username != null && this.Password != null && this.ServiceRootUrl != null)
                {
                    this.FoundAccounts.Add(
                        new BasicAccount
                        {
                            Name = "command-line-basic",
                            Enabled = true,
                            Username = this.Username,
                            Password = this.Password,
                            BaseUrl = this.ServiceRootUrl
                        });
                }
            }
            else // see where else we can find an account
            {
                // Look at the environment variables for an account
                var enviroAccount = GetEnvironmentVariablesAccount();
                if (null != enviroAccount)
                {
                    this.FoundAccounts.Add(enviroAccount);
                }

                // Look at the accounts.json to see if there are other accounts we should use
                if (Program.CurrentConfiguration != null)
                {
                    this.FoundAccounts.AddRange(Program.CurrentConfiguration.Accounts);
                }
            }

            missingArguments = props.ToArray();
            return missingArguments.Length == 0;
        }

    }

    class PublishMetadataOptions : DocSetOptions
    {
        [Option("output", Required = true, HelpText = "Folder for the output metadata file.")]
        public string OutputDirectory { get; set; }

        [Option("source", HelpText="Source metadata input file.")]
        public string SourceMetadataPath { get; set; }

        [Option("format", DefaultValue=MetadataFormat.Default, HelpText="Specify the input and output formats for metadata.")]
        public MetadataFormat DataFormat { get; set; }

        [Option("namespaces", HelpText = "Specify the namespaces that are included when publishing Edmx. Semicolon separated values.")]
        public string Namespaces { get; set; }

        [Option("sort", HelpText = "Sort the output. This is supported for EDMX publishing currently.")]
        public bool SortOutput { get; set; }

        [Option("base-url", HelpText = "Specify the base service URL included in method examples to be removed when generating metadata")]
        public string BaseUrl { get; set; }

        [Option("transform", HelpText="Apply a named publishSchemaChanges transformation to the output file.")]
        public string TransformOutput { get; set; }

        [Option("version", HelpText = "Specify a version to generate the output file for. By default elements from all versions are included in the output.")]
        public string Version { get; set; }

        [Option("skip-generation", HelpText = "Skip generating new elements in the metadata, and only augment elements in the metadata. Can only be used with the --source parameter.")]
        public bool SkipMetadataGeneration { get; set; }

        [Option("validate", HelpText = "Perform validation on the resulting schema to check for errors.")]
        public bool ValidateSchema { get; set; }

        [Option("new-line-attributes", HelpText = "Put XML attributes on a new line")]
        public bool AttributesOnNewLines { get; set; }

        public CsdlWriterOptions GetOptions()
        {
            return new CsdlWriterOptions
            {
                BaseUrl = BaseUrl,
                Formats = DataFormat,
                Sort = SortOutput,
                OutputDirectoryPath = OutputDirectory,
                SourceMetadataPath = SourceMetadataPath,
                Namespaces = Namespaces?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries),
                TransformOutput = TransformOutput,
                DocumentationSetPath = DocumentationSetPath,
                Version = Version,
                SkipMetadataGeneration = SkipMetadataGeneration,
                ValidateSchema = ValidateSchema,
                AttributesOnNewLines = AttributesOnNewLines
            };
        }

        

    }

    class PublishOptions : DocSetOptions, IPublishOptions
    {
        [Option("output", Required=true, HelpText="Output directory for sanitized documentation.")]
        public string OutputDirectory { get; set; }

        [Option("format", DefaultValue=PublishFormat.Markdown, HelpText="Format of the output documentation. Possiblev values are html, markdown, mustache, jsontoc, swagger, and edmx.")]
        public PublishFormat Format { get; set; }

        [Option("template", HelpText = "Specify the folder where output template files are located.")]
        public string TemplatePath { get; set; }

        [Option("template-filename", HelpText="Override the default template filename.", DefaultValue = "template.htm")]
        public string TemplateFilename { get; set; }

        [Option("file-ext", HelpText="Override the default output file extension.", DefaultValue = ".htm")]
        public string OutputExtension { get; set; }

        [Option("line-ending",
            DefaultValue=LineEndings.Default,
            HelpText="Change the line endings for output files. Values: default, windows, unix, or macintosh")]
        public LineEndings LineEndings { get; set; }

        [Option("files", HelpText = "Specify a particular source file that should be published, semicolon separated.")]
        public string SourceFiles { get; set; }

        [Option("toc", HelpText="Specify the relative path to the output folder where the TOC should be written.")]
        public string TableOfContentsOutputRelativePath
        {
            get; set;
        }

        public string[] FilesToPublish {
            get { return (this.SourceFiles ?? string.Empty).Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries); }
            set { this.SourceFiles = value.ComponentsJoinedByString(";"); }
        }

        [Option("allow-unsafe-html", HelpText="Allows HTML tags in the markdown source to be passed through to the output markdown.")]
        public bool AllowUnsafeHtmlContentInMarkdown { get; set; }

        #region Swagger2 output controls

        [Option("swagger-title", DefaultValue=null, HelpText="Title to include in the published documentation")]
        public string Title { get; set; }
        
        [Option("swagger-description", DefaultValue = null, HelpText = "Description to include in the published documentation")]
        public string Description { get; set; }
        
        [Option("swagger-version", DefaultValue=null, HelpText="Api Version information to include in documentation")]
        public string Version { get; set; }

        [Option("swagger-auth-scope", HelpText = "Override the auth scope detection with a default auth scope on every method")]
        public string AuthScopeDefault { get; set; }

        [Option("base-url", HelpText = "Specify the base service URL included in method examples to be removed when generating metadata")]
        public string BaseUrl { get; set; }

        [Option("template-format", HelpText = "For EDMX publishing, only publish a single schema in CSDL format instead of the full EDMX.", DefaultValue = MetadataFormat.Default)]
        public MetadataFormat TemplateFormat { get; set; }


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
                if (string.IsNullOrEmpty(this.TemplatePath))
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
            Mustache,
            JsonToc
        }
    }
}
