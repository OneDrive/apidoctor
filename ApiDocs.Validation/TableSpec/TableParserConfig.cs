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

namespace ApiDocs.Validation.TableSpec
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public class TableParserConfigFile : Config.ConfigFile
    {
        [JsonProperty("tableDefinitions")]
        public TableParserConfig TableDefinitions
        {
            get; set;
        }

        public override bool IsValid
        {
            get
            {
                return TableDefinitions != null && TableDefinitions.Tables != null;
            }
        }
    }

    public class TableParserConfig
    {
        /// <summary>
        /// Defines the titles of tables and how they map to known types.
        /// </summary>
        [JsonProperty("tables")]
        public TableDecoder[] Tables
        {
            get; set;
        }

        /// <summary>
        /// The list of table types and how to parse the columns of those tables.
        /// </summary>
        [JsonProperty("parsingRules")]
        public TableRule[] Rules
        {
            get; set;
        }
    }


    public class TableDecoder
    {
        [JsonProperty("type")]
        public TableBlockType Type
        {
            get; set;
        }

        [JsonProperty("titles")]
        public string[] Titles
        {
            get; set;
        }

        /// <summary>
        /// Indicates the Type of TableRule used to parse a matching table.
        /// </summary>
        [JsonProperty("parseAs")]
        public string ParseAs
        {
            get; set;
        }

        [JsonIgnore]
        public TableRule ParseRule
        {
            get; set;
        }
    }

    public class TableRule
    {
        /// <summary>
        /// The type of the table decoding rule. This is referenced from 
        /// the ParseAs property. of the TableDecoder.
        /// </summary>
        public string Type
        {
            get; set;
        }

        [JsonProperty("columns")]
        public Dictionary<string, string[]> ColumnNames
        {
            get; set;
        }
    }
}
