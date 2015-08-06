using Newtonsoft.Json;
using System;
using ApiDocs.Validation.Error;

namespace ApiDocs.Validation
{
    public class ExampleDefinition : ItemDefinition
    {
        public ExampleDefinition(CodeBlockAnnotation annotation, string content, DocFile source, string language)
        {
            if (string.IsNullOrEmpty(language))
                throw new ArgumentNullException("language");

            this.Metadata = annotation;
            this.SourceExample = content;
            this.SourceFile = source;

            switch (language.ToLower())
            {
                case "json":
                    {
                        this.Language = CodeLanguage.Json;
                        try
                        {
                            object inputObject = JsonConvert.DeserializeObject(content);
                            this.ParsedExample = JsonConvert.SerializeObject(inputObject, Formatting.Indented);
                        }
                        catch (Exception ex)
                        {
                            Logging.LogMessage(
                                new ValidationError(
                                    ValidationErrorCode.JsonParserException,
                                    source.DisplayName,
                                    "Error parsing resource definition: {0}",
                                    ex.Message));
                        }
                        break;
                    }
                case "http":
                    this.ParsedExample = this.SourceExample;
                    this.Language = CodeLanguage.Http;
                    break;
                default:
                    this.Language = CodeLanguage.Unsupported;
                    Logging.LogMessage(
                        new ValidationError(
                            ValidationErrorCode.UnsupportedLanguage,
                            source.DisplayName,
                            "The code language for this example is unuspported: {0}", language));
                    break;
            }
        }

        /// <summary>
        /// Metadata read from the code block annotation
        /// </summary>
        public CodeBlockAnnotation Metadata { get; private set; }

        /// <summary>
        /// The type identifier for the resource defined in this class
        /// </summary>
        public string ResourceType { get { return this.Metadata.ResourceType; } }

        /// <summary>
        /// Parsed and reformatted json resource read from the documentation
        /// </summary>
        public string ParsedExample { get; private set; }

        /// <summary>
        /// Original json example as written in the documentation.
        /// </summary>
        public string SourceExample { get; private set; }

        /// <summary>
        /// The documentation file that was the source of this resource
        /// </summary>
        /// <value>The source file.</value>
        public DocFile SourceFile {get; private set;}    

        public CodeLanguage Language {get; private set;}
    
    }

    public enum CodeLanguage
    {
        Unsupported,
        Json,
        Http
    }
}
