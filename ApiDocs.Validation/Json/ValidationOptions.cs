/*
 * Markdown Scanner
 * Copyright (c) Microsoft Corporation
 * All rights reserved. 
 * 
 * MIT License
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of 
 * this software and associated documentation files (the ""Software""), to deal in 
 * the Software without restriction, including without limitation the rights to use, 
 * copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
 * Software, and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all 
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace ApiDocs.Validation.Json
{
    using System.Linq;

    public class ValidationOptions
    {
        public ValidationOptions()
        {
            this.CollectionPropertyName = "value";
        }

        public ValidationOptions(CodeBlockAnnotation annotation)
        {
            if (null == annotation)
                return;

            this.CollectionPropertyName = annotation.CollectionPropertyName;
            this.AllowTruncatedResponses = annotation.TruncatedResult;
        }

        public ValidationOptions(ValidationOptions options)
        {
            if (null == options)
                return;

            this.AllowTruncatedResponses = options.AllowTruncatedResponses;
            this.RequiredPropertyNames = options.RequiredPropertyNames;
            this.CollectionPropertyName = options.CollectionPropertyName;
            this.ExpectedJsonSchema = options.ExpectedJsonSchema;
            this.NullablePropertyNames = options.NullablePropertyNames;
            this.RelaxedStringValidation = options.RelaxedStringValidation;
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

        /// <summary>
        /// Used to disable the scope requirement check and process all scenarios/methods for validation.
        /// </summary>
        public bool IgnoreRequiredScopes { get; set; }

        /// <summary>
        /// Creates a new ValidationOptions instance inheriting values from this instance, and modified for the specific options of a property.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        internal ValidationOptions CreateForProperty(string propertyName)
        {
            var newOption = new ValidationOptions
            {
                AllowTruncatedResponses = this.AllowTruncatedResponses,
                CollectionPropertyName = this.CollectionPropertyName,
                RelaxedStringValidation = this.RelaxedStringValidation
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
