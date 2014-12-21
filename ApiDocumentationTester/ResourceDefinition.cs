using System;

namespace ApiDocumentationTester
{
    public class ResourceDefinition
    {
        public ResourceDefinition()
        {
        }

        public string OdataType {get;set;}

        public string JsonFormat {get;set;}

        public bool IgnoreMissingProperties { get; set; }

        public string OptionalProperties { get; set; }
    }
}

