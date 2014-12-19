using System;
using CommandLine;
using CommandLine.Text;

namespace MarkdownScanner
{
    public class ScannerOptions
    {
        [Option("source", Required=true, HelpText="Specify the source folder for markdown documents")]
        public string SourcePath {get;set;}

        [Option('r', "recursive", DefaultValue=true, HelpText="Indicate that the scan should happen recursively on folders within source")]
        public bool Recursive {get;set;}

        [Option("extension", DefaultValue=".md", HelpText="The file extension for markdown files, defaults to \".md\"")]
        public string FileExtension {get;set;}

        [Option("verbose", DefaultValue=false, HelpText="Verbose output")]
        public bool Verbose {get;set;}

        public string UsageText 
        {
            get 
            {
                return HelpText.AutoBuild(this);
            }
        }
    }
}

