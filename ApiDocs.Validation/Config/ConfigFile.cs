namespace ApiDocs.Validation.Config
{
    public abstract class ConfigFile
    {
        public abstract bool IsValid { get; }
        public string SourcePath {get;set;}
    }
}

