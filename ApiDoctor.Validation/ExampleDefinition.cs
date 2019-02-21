/*
 * API Doctor
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

using Newtonsoft.Json;
using System;
using ApiDoctor.Validation.Error;

namespace ApiDoctor.Validation
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
                                    this.SourceFile.DisplayName,
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
                            this.SourceFile.DisplayName,
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
