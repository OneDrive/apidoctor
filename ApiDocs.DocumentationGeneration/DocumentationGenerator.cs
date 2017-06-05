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

namespace ApiDocs.DocumentationGeneration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    using ApiDocs.DocumentationGeneration.Extensions;
    using ApiDocs.DocumentationGeneration.Properties;
    using ApiDocs.Validation.OData;

    using Mustache;

    public class DocumentationGenerator
    {
        private readonly Generator resourceMarkDownGenerator;

        public DocumentationGenerator() : this(null)
        {
        }

        public DocumentationGenerator(string resourceTemplateFile)
        {
            string template = null;

            if (string.IsNullOrWhiteSpace(resourceTemplateFile))
            {
                using (var ms = new MemoryStream(Templates.resourceMarkDown))
                using (var reader = new StreamReader(ms))
                {
                    template = reader.ReadToEnd();
                }
            }
            else
            {
                if (!File.Exists(resourceTemplateFile))
                {
                    throw new FileNotFoundException($"Resource MarkDown template not found", resourceTemplateFile);
                }
                template = File.ReadAllText(resourceTemplateFile);
            }

            if (string.IsNullOrWhiteSpace(template))
            {
                throw new InvalidOperationException("Failed to load resource templase");
            }

            FormatCompiler compiler = new FormatCompiler() { RemoveNewLines = false };
            this.resourceMarkDownGenerator = compiler.Compile(template);
            this.resourceMarkDownGenerator.KeyNotFound += MarkDownGenerator_KeyNotFound;

        }

        /// <summary>
        /// Generates documentation from a <see cref="EntityFramework"/> instance.
        /// </summary>
        /// <param name="entityFramework">The <see cref="EntityFramework"/> instance.</param>
        /// <param name="outputFolder">The folder to write the output to.</param>
        public void GenerateDocumentationFromEntityFrameworkAsync(EntityFramework entityFramework, string outputFolder)
        {
            string resourceFolder = Path.Combine(outputFolder, "resource");
            if (!Directory.Exists(resourceFolder))
            {
                Directory.CreateDirectory(resourceFolder);
            }

            foreach (Schema schema in entityFramework.DataServices.Schemas)
            {

                foreach (var complexType in schema.ComplexTypes)
                {
                    Console.WriteLine("Creating file for complex type {0}", complexType.Name);
                    string output = GetMarkDownForType(entityFramework, complexType);
                    File.WriteAllText(Path.Combine(resourceFolder, $"{complexType.Name}.md"), output);
                }

                foreach (var entity in schema.EntityTypes)
                {
                    Console.WriteLine("Creating file for entity {0}", entity.Name);
                    string output = GetMarkDownForType(entityFramework, entity);
                    File.WriteAllText(Path.Combine(resourceFolder, $"{entity.Name}.md"), output);
                }
            }
        }

        internal string GetMarkDownForType(EntityFramework entityFramework, EntityType entity)
        {
            var output = this.resourceMarkDownGenerator.Render(entity.ToDocumentationEntityType(entityFramework));
            return output;
        }

        internal string GetMarkDownForType(EntityFramework entityFramework, ComplexType complexType)
        {
            var output = this.resourceMarkDownGenerator.Render(complexType.ToDocumentationComplexType(entityFramework));
            return output;
        }

        private static void MarkDownGenerator_KeyNotFound(object sender, KeyNotFoundEventArgs e)
        {
            e.Substitute = string.Empty;
            e.Handled = true;
        }
    }
}