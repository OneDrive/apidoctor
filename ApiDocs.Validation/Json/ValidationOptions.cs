namespace ApiDocs.Validation.Json
{
    using System.Linq;

    public class ValidationOptions
    {
        public ValidationOptions()
        {
            this.CollectionPropertyName = "value";
        }

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

        /// <summary>
        /// For collection results, what is the name of the property that 
        /// includes the collection. Defaults to 'value'.
        /// </summary>
        public string CollectionPropertyName { get; set; }

        public JsonSchema ExpectedJsonSchema { get; set; }

        public string[] NullablePropertyNames { get; set; }

        /// <summary>
        /// When set to True, string types are not validated
        /// for the types defined in the documetnation (url, 
        /// dateTime, enumerated values, etc)
        /// </summary>
        public bool RelaxedStringValidation { get; set; }

        internal ValidationOptions CreateForProperty(string propertyName)
        {
            var newOption = new ValidationOptions
            {
                AllowTruncatedResponses = this.AllowTruncatedResponses,
                CollectionPropertyName = this.CollectionPropertyName
            };

            if (null != this.ExpectedJsonSchema)
            {
                var propertyData = this.ExpectedJsonSchema.Properties.FirstOrDefault(x => x.Name.Equals(propertyName));
                if (null != propertyData && null != propertyData.OriginalValue)
                {
                    // Compute a new expected schema for the property's contents.
                    newOption.ExpectedJsonSchema = new JsonSchema(propertyData.OriginalValue, new CodeBlockAnnotation());
                    newOption.RequiredPropertyNames = newOption.ExpectedJsonSchema.Properties.Select(x => x.Name).ToArray();
                }
                else
                {
                    newOption.ExpectedJsonSchema = null;
                }
            }
            else
            {
                newOption.RequiredPropertyNames = this.RequiredPropertyNames;
            }
            return newOption;
        }
    }
}
