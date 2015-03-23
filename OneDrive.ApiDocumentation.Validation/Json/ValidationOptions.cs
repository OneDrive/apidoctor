using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.Validation.Json
{
    public class ValidationOptions
    {
        /// <summary>
        /// Specifies that the validate routines ignore errors/warnings 
        /// about missing properties that are defined in the schema
        /// </summary>
        public bool AllowTruncatedResponses { get; set; }
        
        /// <summary>
        /// A list of property names that are required, even if
        /// we allow truncated responses.
        /// </summary>
        public string[] RequiredPropertyNames { get; set; }

        public string CollectionPropertyName { get; set; }


        public JsonSchema ExpectedJsonSchema { get; set; }
    }
}
