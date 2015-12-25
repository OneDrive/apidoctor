using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation.TableSpec
{

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
