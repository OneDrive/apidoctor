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

namespace ApiDoctor.Validation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using ApiDoctor.Validation.Error;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Represents a class, resource, complex type, or entity type from an API.
    /// </summary>
    public class ResourceDefinition : ItemDefinition
    {
        private readonly CodeBlockAnnotation sourceAnnotation;

        protected ResourceDefinition(CodeBlockAnnotation sourceAnnotation, string content, DocFile source, string language)
        {
            this.sourceAnnotation = sourceAnnotation;
            this.OriginalExampleText = content;
            this.SourceFile = source;
            this.Abstract = sourceAnnotation.Abstract;
            this.Name = sourceAnnotation.ResourceType;
            this.NameAka = sourceAnnotation.ResourceTypeAka;
            this.KeyPropertyName = sourceAnnotation.KeyPropertyName;
            this.BaseType = sourceAnnotation.BaseType;

            if (string.IsNullOrEmpty(sourceAnnotation.ResourceType))
            {
                Logging.LogMessage(
                    new ValidationError(
                        ValidationErrorCode.MissingResourceName,
                        source.DisplayName,
                        this.SourceFile.DisplayName,
                        "Resource definition is missing Name value"));
            }
        }

        /// <summary>
        /// abstract
        /// </summary>
        public bool Abstract { get; set; }

        /// <summary>
        /// base type
        /// </summary>
        public string BaseType { get; set; }

        /// <summary>
        /// resolved basetype ref
        /// </summary>
        public ResourceDefinition ResolvedBaseTypeReference { get; set; }

        /// <summary>
        /// For indexed resources, this specifies the property which is used as the index.
        /// </summary>
        public string KeyPropertyName { get; set; }

        public string ExplicitOrInheritedKeyPropertyName
        {
            get
            {
                return this.KeyPropertyName ?? this.ResolvedBaseTypeReference?.ExplicitOrInheritedKeyPropertyName ?? null;
            }
        }

        /// <summary>
        /// Metadata read from the code block sourceAnnotation
        /// </summary>
        public CodeBlockAnnotation OriginalMetadata
        {
            get { return sourceAnnotation; }
        }

        /// <summary>
        /// The type identifier for the resource defined in this class (@odata.type)
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// The oneDrive type for the resource defined in this class (@type.aka)
        /// </summary>
        public string NameAka { get; protected set; }

        /// <summary>
        /// Parsed and reformatted resource read from the documentation
        /// </summary>
        public string ExampleText { get; protected set; }

        /// <summary>
        /// Original json example as written in the documentation.
        /// </summary>
        public string OriginalExampleText { get; protected set; }

        /// <summary>
        /// The documentation file that was the sourceFile of this resource
        /// </summary>
        /// <value>The sourceFile file.</value>
        public DocFile SourceFile {get; protected set;}

        public bool HasOrInheritsProperty(string name)
        {
            if (this.Parameters != null && this.Parameters.Any(p => p.Name == name))
            {
                return true;
            }

            if (this.ResolvedBaseTypeReference == null)
            {
                return false;
            }

            return this.ResolvedBaseTypeReference.HasOrInheritsProperty(name);
        }
    }


    public class JsonResourceDefinition : ResourceDefinition
    {
        public JsonResourceDefinition(CodeBlockAnnotation sourceAnnotation, string json, DocFile source, IssueLogger issues)
            : base(sourceAnnotation, json, source, "json")
        {
            ParseJsonInput(issues);
        }

        public JObject SourceJObject { get; set; }

        public void PatchSourceFile()
        {
            if (this.SourceFile == null)
            {
                throw new InvalidOperationException("can only patch a resource that was read from a file");
            }

            var newJson = JsonConvert.SerializeObject(this.SourceJObject, serializerSettings);
            var originalFile = File.ReadAllText(this.SourceFile.FullPath);
            var modified = originalFile.Replace(this.OriginalExampleText.Trim(), newJson);
            this.OriginalExampleText = newJson;
            File.WriteAllText(this.SourceFile.FullPath, modified);
        }

        /// <summary>
        /// Parse the example resource conetent and populate the resource definition
        /// </summary>
        /// <param name="jsonContent">JSON string representation of an object</param>
        /// <param name="sourceFile"></param>
        private void ParseJsonInput(IssueLogger issues)
        {
            try
            {
                var inputObject = JsonConvert.DeserializeObject(this.OriginalExampleText);

                JObject parsedObject = this.SourceJObject = inputObject as JObject;
                if (parsedObject == null)
                {
                    JArray parsedArray = inputObject as JArray;
                    if (parsedArray == null)
                    {
                        throw new ArgumentException("failed to parse example as JObject or JArray");
                    }

                    if (parsedArray.Count > 0)
                    {
                        parsedObject = parsedArray[0] as JObject;
                    }
                }

                if (parsedObject != null)
                {
                    this.ExtractProperties(parsedObject);
                }

                this.ExampleText = JsonConvert.SerializeObject(inputObject, Formatting.Indented);
            }
            catch (Exception ex)
            {
                issues.Error(ValidationErrorCode.JsonParserException,
                        $"Error parsing resource definition: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="inputObject"></param>
        private void ExtractProperties(JObject input)
        {
            var properties = input.Properties();

            List<ParameterDefinition> parameters = new List<ParameterDefinition>();
            foreach (var p in properties)
            {
                var param = Json.JsonSchema.ParseProperty(p.Name, p.Value, null);
                param.Location = ParameterLocation.JsonObject;
                param.Required = (this.OriginalMetadata != null && this.OriginalMetadata.OptionalProperties != null) ? !this.OriginalMetadata.OptionalProperties.Contains(param.Name) : true;
                parameters.Add(param);
            }

            this.Parameters = parameters;
        }

        private static JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Formatting = Formatting.Indented,
        };
    }
}

