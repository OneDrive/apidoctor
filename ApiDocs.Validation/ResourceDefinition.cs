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
                throw;
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

