namespace ApiDocs.Validation
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents an entity resource in the API.
    /// </summary>
    public class ResourceDefinition : ItemDefinition
    {
        public ResourceDefinition(CodeBlockAnnotation annotation, string jsonContent, DocFile source)
        {
            this.Metadata = annotation;
            this.OriginalExample = jsonContent;
            this.SourceFile = source;

            try
            {
                object inputObject = JsonConvert.DeserializeObject(jsonContent);
                this.JsonExample = JsonConvert.SerializeObject(inputObject, Formatting.Indented);
            }
            catch (Exception ex)
            {
                LogHelper.LogFailure("Error parsing file {0}: {1}", source.FullPath, ex.Message);
                throw;
            }

            if (string.IsNullOrEmpty(annotation.ResourceType))
            {
                throw new InvalidOperationException("ResourceDefinition requires a resource type to be set (@odata.type property in the annotation)");
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

