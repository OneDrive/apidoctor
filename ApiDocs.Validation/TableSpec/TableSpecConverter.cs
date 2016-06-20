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
    using System.Collections.Generic;
    using System.Linq;
    using ApiDocs.Validation.Error;
    using ApiDocs.Validation.Json;
    using MarkdownDeep;

    /// <summary>
    /// Manages to convert between a tablespec MarkdownDeep.Block object and useful object model
    /// concepts that we know how to deal with
    /// </summary>
    internal class TableSpecConverter
    {
        private readonly Dictionary<string, TableDecoder> CommonHeaderContentMap;
        public TableSpecConverter(TableParserConfig config)
        {
            Dictionary<string, TableDecoder> decoderRing = GenerateDecoderRing(config);
            this.CommonHeaderContentMap = decoderRing;
        }

        private Dictionary<string, TableDecoder> GenerateDecoderRing(TableParserConfig config)
        {
            var decoderRing = new Dictionary<string, TableDecoder>();
            foreach (var t in config.Tables)
            {
                foreach (var title in t.Titles)
                {
                    decoderRing[title] = t;
                    t.ParseRule = config.Rules.Where(x => x.Type == t.ParseAs).Single();
                }
            }

            return decoderRing;
        }

        /// <summary>
        /// Creates a new instance of TableSpecConverter using the pre-programmed
        /// table parser rules from Markdown Scanner.
        /// </summary>
        /// <returns></returns>
        public static TableSpecConverter FromDefaultConfiguration()
        {
            var configFile = LoadDefaultConfiguration();
            return new TableSpecConverter(configFile.TableDefinitions);
        }


        /// <summary>
        /// Convert a tablespec block into one of our internal object model representations
        /// </summary>
        /// <param name="tableSpecBlock"></param>
        /// <param name="lastHeaderBlock"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public TableDefinition ParseTableSpec(Block tableSpecBlock, Block lastHeaderBlock, out ValidationError[] errors)
        {
            List<ValidationError> discoveredErrors = new List<ValidationError>();
            List<ItemDefinition> items = new List<ItemDefinition>();

            var tableShape = tableSpecBlock.Table;

            TableDecoder decoder = new TableDecoder { Type = TableBlockType.Unknown };
            string headerText = null;
            // Try matching based on header
            if (null != lastHeaderBlock && null != lastHeaderBlock.Content)
            {
                headerText = lastHeaderBlock.Content;
                var matchingDecoder = FindDecoderFromHeaderText(headerText);
                if (null != matchingDecoder)
                    decoder = matchingDecoder;
            }

            // Try matching based on shape
            if (decoder.Type == TableBlockType.Unknown && null != tableSpecBlock.Table)
            {
                var matchingDecoder = FindDecoderFromShape(tableShape);
                if (null != matchingDecoder)
                    decoder = matchingDecoder;
            }

            switch (decoder.Type)
            {
                case TableBlockType.ErrorCodes:
                    items.AddRange(ParseErrorTable(tableShape, decoder));
                    break;

                case TableBlockType.PathParameters:
                    items.AddRange(ParseParameterTable(tableShape, ParameterLocation.Path, decoder));
                    break;

                case TableBlockType.ResourcePropertyDescriptions:
                case TableBlockType.RequestObjectProperties:
                case TableBlockType.ResponseObjectProperties:
                    items.AddRange(ParseParameterTable(tableShape, ParameterLocation.JsonObject, decoder));
                    break;
                case TableBlockType.ResourceNavigationPropertyDescriptions:
                    items.AddRange(ParseParameterTable(tableShape, ParameterLocation.JsonObject, decoder, true));
                    break;

                case TableBlockType.HttpHeaders:
                    items.AddRange(ParseParameterTable(tableShape, ParameterLocation.Header, decoder));
                    break;

                case TableBlockType.QueryStringParameters:
                    items.AddRange(ParseParameterTable(tableShape, ParameterLocation.QueryString, decoder));
                    break;

                case TableBlockType.EnumerationValues:
                    items.AddRange(ParseEnumerationTable(tableShape, decoder));
                    break;

                case TableBlockType.AuthScopes:
                    items.AddRange(ParseAuthScopeTable(tableShape, decoder));
                    break;

                case TableBlockType.Unknown:
                    discoveredErrors.Add(new ValidationMessage(null, "Ignored unclassified table: headerText='{0}', tableHeaders='{1}'", headerText, tableShape.ColumnHeaders != null ? tableShape.ColumnHeaders.ComponentsJoinedByString(",") : "null"));
                    break;
                default:
                    discoveredErrors.Add(new ValidationMessage(null, "Ignored table: classification='{2}', headerText='{0}', tableHeaders='{1}'", headerText, tableShape.ColumnHeaders != null ? tableShape.ColumnHeaders.ComponentsJoinedByString(",") : "null", decoder.Type));
                    break;
            }

            errors = discoveredErrors.ToArray();

            return new TableDefinition(decoder.Type, items, headerText);
        }

        /// <summary>
        /// Convert a markdown table into ErrorDefinition objects
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private static IEnumerable<ErrorDefinition> ParseErrorTable(IMarkdownTable table, TableDecoder decoder)
        {
            var records = from r in table.RowValues
                          select new ErrorDefinition 
                          { 
                              HttpStatusCode = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["httpStatusCode"]),
                              HttpStatusMessage = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["httpStatusMessage"]),
                              ErrorCode = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["errorCode"]),
                              Description = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["description"])
                          };

            return records;
        }

        private static IEnumerable<ParameterDefinition> ParseParameterTable(IMarkdownTable table, ParameterLocation location, TableDecoder decoder, bool navigationProperties = false)
        {
            var records = from r in table.RowValues
                          select new ParameterDefinition
                          {
                              Name = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["name"]),
                              Type = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["type"]).ParseParameterDataType(defaultValue: ParameterDataType.String),
                              Description = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["description"]),
                              Required = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["description"]).IsRequired(),
                              Location = location,
                              IsNavigatable = navigationProperties
                          };
            return records;
        }

        private static IEnumerable<EnumerationDefinition> ParseEnumerationTable(IMarkdownTable table, TableDecoder decoder)
        {
            var records = from r in table.RowValues
                          select new EnumerationDefinition
                          {
                              Value = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["value"]),
                              Description = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["description"])
                          };
            return records;
        }

        private static IEnumerable<AuthScopeDefinition> ParseAuthScopeTable(IMarkdownTable table, TableDecoder decoder)
        {
            var records = from r in table.RowValues
                          select new AuthScopeDefinition
                          {
                              Scope = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["scope"]),
                              Title = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["title"]),
                              Description = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["description"]),
                              Required = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["required"]).ToBoolean()
                          };
            return records;
        }

       
            
        private TableDecoder FindDecoderFromHeaderText(string lastHeader)
        {
            TableDecoder decoder = (from key in CommonHeaderContentMap.Keys
                                    where lastHeader.ContainsIgnoreCase(key)
                                    select CommonHeaderContentMap[key]).FirstOrDefault();
            return decoder;
        }

        private TableDecoder FindDecoderFromShape(IMarkdownTable table)
        {
            // TODO: Attempt to match this table based on the columns that are available
            return new TableDecoder { Type = TableBlockType.Unknown };
        }


        private static TableParserConfigFile LoadDefaultConfiguration()
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<TableParserConfigFile>(Properties.Resources.DefaultTableParserConfig);
        }
    }


    public enum TableBlockType
    {

        /// <summary>
        /// The type of information in the table could not be parsed.
        /// </summary>
        Unknown,

        /// <summary>
        /// Collection of ParameterDefinition objects for a resource type
        /// </summary>
        ResourcePropertyDescriptions,

        /// <summary>
        /// Collection of EnumerationDefinition objects
        /// </summary>
        EnumerationValues,

        /// <summary>
        /// Collection of ParameterDefinition objects for a request.
        /// </summary>
        RequestObjectProperties,

        /// <summary>
        /// Collection of ParameterDefinition objects for a response
        /// </summary>
        ResponseObjectProperties,

        /// <summary>
        /// Collection of ErrorDefinition objects
        /// </summary>
        ErrorCodes,

        /// <summary>
        /// Collection of ParameterDefinition objects for querystring values
        /// </summary>
        QueryStringParameters,

        /// <summary>
        /// Collection of ParameterDefinition objects for http headers on a request or response
        /// </summary>
        HttpHeaders,

        /// <summary>
        /// Collection of ParameterDefinition objects for the URL path
        /// </summary>
        PathParameters,
        AuthScopes,
        ResourceNavigationPropertyDescriptions
    }
}
