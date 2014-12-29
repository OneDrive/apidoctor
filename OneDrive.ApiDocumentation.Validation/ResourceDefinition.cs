namespace OneDrive.ApiDocumentation.Validation
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents an entity resource in the API.
    /// </summary>
    public class ResourceDefinition
    {
        public ResourceDefinition(CodeBlockAnnotation annotation, string jsonContent, DocFile source)
        {
            Metadata = annotation;
            OriginalExample = jsonContent;
            SourceFile = source;
            
            object inputObject = JsonConvert.DeserializeObject(jsonContent);
            JsonExample = JsonConvert.SerializeObject(inputObject, Formatting.Indented);
        }

        /// <summary>
        /// Metadata read from the code block annotation
        /// </summary>
        public CodeBlockAnnotation Metadata { get; private set; }

        public string ResourceType { get { return Metadata.ResourceType; } }

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

