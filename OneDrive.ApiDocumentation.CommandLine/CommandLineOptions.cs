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

        public CommandLineOptions()
        {

        }

        [VerbOption("files", HelpText="Print the files in the documentation set")]
        public CommonOptions FilesVerb { get; set; }

        [VerbOption("links", HelpText = "Verify links in the documentation aren't broken")]
        public CommonOptions LinksVerb { get; set; }

        [VerbOption("resources", HelpText="Print resources discovered in the documentation.")]
        public CommonOptions ResourcesVerb { get; set; }

        [VerbOption("methods", HelpText="Print methods discovered in the documentation")]
        public CommonOptions MethodsVerb { get; set; }

        [VerbOption("docs", HelpText = "Verify the internal consistency of the documentation (resources + examples)")]
        public ConsistencyCheckOptions InternalConsistencyVerb { get; set; }

        [VerbOption("service", HelpText = "Verify the documentations consistency with a live service")]
        public ServiceConsistencyOptions ServiceConsistencyVerb { get; set; }


        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }

    class CommonOptions
    {
        [Option('p', "path", Required=true, HelpText="Path to the documentation set")]
        public string PathToDocSet { get; set; }

        [Option("short", HelpText="Print output in short format. Cannot be combined with Verbose.")]
        public bool ShortForm { get; set; }

        [Option("verbose", HelpText = "Print verbose output")]
        public bool Verbose { get; set; }

    }

    class ConsistencyCheckOptions : CommonOptions
    {
        [Option('m', "method", HelpText = "Name of the method to test")]
        public string MethodName { get; set; }
    }

    class ServiceConsistencyOptions : ConsistencyCheckOptions
    {
        [Option('t', "access-token", Required=true, HelpText="OAuth access token")]
        public string AccessToken { get; set; }

        [Option('u', "url", Required=true, HelpText="Root URL for API calls, like https://api.onedrive.com/v1.0")]
        public string ServiceRootUrl { get; set; }

        [Option('f', "parameter-file", DefaultValue="/internal/api-automation.json", HelpText="Source for parameter values in API calls")]
        public string ParameterSource { get; set; }

        [Option("pause", HelpText="Pause between method requests.")]
        public bool PauseBetweenRequests { get; set; }

    }
}
