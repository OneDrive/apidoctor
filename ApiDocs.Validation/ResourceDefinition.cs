namespace ApiDocs.Validation
{
    using System;
    using ApiDocs.Validation.Error;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents an entity resource in the API.
    /// </summary>
    public class ResourceDefinition : ItemDefinition
    {
        public ResourceDefinition(CodeBlockAnnotation annotation, string content, DocFile source, string language)
        {
            if (null != language && !language.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Resources only support JSON language.", "language");
            }

            this.Metadata = annotation;
            this.OriginalExample = content;
            this.SourceFile = source;

            try
            {
                object inputObject = JsonConvert.DeserializeObject(content);
                this.JsonExample = JsonConvert.SerializeObject(inputObject, Formatting.Indented);
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

            if (string.IsNullOrEmpty(annotation.ResourceType))
            {
                Logging.LogMessage(
                    new ValidationError(
                        ValidationErrorCode.MissingResourceName,
                        source.DisplayName,
                        "Resource definition is missing a @odata.type name"));
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
        public string JsonExample { get; private set; }

        /// <summary>
        /// Original json example as written in the documentation.
        /// </summary>
        public string OriginalExample { get; private set; }

        /// <summary>
        /// The documentation file that was the source of this resource
        /// </summary>
        /// <value>The source file.</value>
        public DocFile SourceFile {get; private set;}

    }
}

