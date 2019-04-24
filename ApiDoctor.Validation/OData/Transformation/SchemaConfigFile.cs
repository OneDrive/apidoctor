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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace ApiDoctor.Validation.OData.Transformation
{
    public class SchemaConfigFile : Config.ConfigFile
    {
        [JsonProperty("schemaConfig")]
        public SchemaConfig SchemaConfig { get; set; }

        [JsonProperty("schemaDiffConfig")]
        public SchemaDiffConfig SchemaDiffConfig { get; set; }

        public override bool IsValid => this.SchemaConfig != null;
    }

    public class SchemaConfig
    {
        public SchemaConfig()
        {
            RequiredYamlHeaders = new string[] {};
            TreatErrorsAsWarningsWorkloads = new List<string>();
        }

        /// <summary>
        /// default namespace for types
        /// </summary>
        [JsonProperty("defaultNamespace")]
        public string DefaultNamespace { get; set; }

        /// <summary>
        /// Specifies the base service URLs included in method examples to be removed when generating metadata.
        /// </summary>
        [JsonProperty("baseUrls")]
        public string[] BaseUrls { get; set; }

        /// <summary>
        /// apiDoctor expects names to be lowerCamel. declare any exceptions here.
        /// </summary>
        [JsonProperty("notLowerCamel")]
        public string[] NotLowerCamel { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("supportedTags")]
        public string[] SupportedTags { get; set; }

        /// <summary>
        /// Specifies the mandatory YAML headers for all docs
        /// </summary>
        [JsonProperty("requiredYamlHeaders")]
        public string[] RequiredYamlHeaders { get; set; }

        /// <summary>
        /// optionally specify workloads whose errors should be treated as warnings
        /// </summary>
        [JsonProperty("treatErrorsAsWarningsWorkloads")]
        public List<string> TreatErrorsAsWarningsWorkloads { get; set; }
    }

    public class SchemaDiffConfig
    {
        /// <summary>
        /// keep elements that have attributes containing any of these values
        /// </summary>
        [JsonProperty("keepElementsContaining")]
        public string[] KeepElementsContaining { get; set; }

        /// <summary>
        /// drop elements containing children with any of these values
        /// </summary>
        [JsonProperty("dropElementsContaining")]
        public string[] DropElementsContaining { get; set; }
    }
}
