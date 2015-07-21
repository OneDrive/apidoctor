using System;

namespace ApiDocs.Validation
{
    public abstract class ConfigFile
    {
        public ConfigFile()
        {
        }

        public abstract bool IsValid { get; }
        public string SourcePath {get;set;}
    }
}

