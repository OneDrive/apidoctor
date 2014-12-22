namespace OneDrive.ApiDocumentation.Validation
{
    using System;

    /// <summary>
    /// Represents an entity resource in the API.
    /// </summary>
    public class ResourceDefinition
    {
        public ResourceDefinition(CodeBlockAnnotation annotation, string jsonContent)
        {
            Metadata = annotation;
            JsonExample = jsonContent;
        }

        /// <summary>
        /// Metadata read from the code block annotation
        /// </summary>
        public CodeBlockAnnotation Metadata { get; private set; }

        public string ResourceType { get { return Metadata.ResourceType; } }

        /// <summary>
        /// The raw resource definition from the documentation (fenced code block with annotation)
        /// </summary>
        public string JsonExample { get; private set; }

    }
}

