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

namespace ApiDoctor.Validation.TableSpec
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ApiDoctor.Validation.Error;
    using ApiDoctor.Validation.Json;
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
        /// table parser rules from API Doctor.
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
        public TableDefinition ParseTableSpec(Block tableSpecBlock, Stack<Config.DocumentHeader> headerStack, IssueLogger issues)
        {
            List<ValidationError> discoveredErrors = new List<ValidationError>();
            List<ItemDefinition> items = new List<ItemDefinition>();

            var tableShape = tableSpecBlock.Table;

            TableDecoder decoder = new TableDecoder { Type = TableBlockType.Unknown };

            var headerText = headerStack.Peek()?.Title;
            
            // Try matching based on header
            if (headerText != null)
            {
                var matchingDecoder = FindDecoderFromHeaderText(headerStack);
                if (null != matchingDecoder)
                {
                    decoder = matchingDecoder;
                }
            }

            // Try matching based on shape
            if (decoder.Type == TableBlockType.Unknown && null != tableSpecBlock.Table)
            {
                var matchingDecoder = FindDecoderFromShape(tableShape);
                if (null != matchingDecoder)
                {
                    decoder = matchingDecoder;
                }
            }

            switch (decoder.Type)
            {
                case TableBlockType.ErrorCodes:
                    items.AddRange(ParseErrorTable(tableShape, decoder));
                    break;

                case TableBlockType.PathParameters:
                    items.AddRange(ParseParameterTable(tableShape, ParameterLocation.Path, decoder, issues.For($"{decoder.Type}Table")));
                    break;

                case TableBlockType.ResourcePropertyDescriptions:
                case TableBlockType.RequestObjectProperties:
                case TableBlockType.ResponseObjectProperties:
                    items.AddRange(ParseParameterTable(tableShape, ParameterLocation.JsonObject, decoder, issues));
                    break;
                case TableBlockType.ResourceNavigationPropertyDescriptions:
                    items.AddRange(ParseParameterTable(tableShape, ParameterLocation.JsonObject, decoder, issues, true));
                    break;

                case TableBlockType.HttpHeaders:
                    items.AddRange(ParseParameterTable(tableShape, ParameterLocation.Header, decoder, issues));
                    break;

                case TableBlockType.QueryStringParameters:
                    items.AddRange(ParseParameterTable(tableShape, ParameterLocation.QueryString, decoder, issues));
                    break;

                case TableBlockType.EnumerationValues:
                    items.AddRange(ParseEnumerationTable(tableShape, decoder));
                    break;

                case TableBlockType.AuthScopes:
                    items.AddRange(ParseAuthScopeTable(tableShape, decoder));
                    break;

                case TableBlockType.Unknown:
                    var headers = tableShape.ColumnHeaders != null ? tableShape.ColumnHeaders.ComponentsJoinedByString(",") : "null";
                    issues.Message($"Ignored unclassified table: headerText='{headerText}', tableHeaders='{headers}'");
                    break;
                default:
                    var hdrs = tableShape.ColumnHeaders != null ? tableShape.ColumnHeaders.ComponentsJoinedByString(",") : "null";
                    issues.Message($"Ignored table: classification='{decoder.Type}', headerText='{headerText}', tableHeaders='{hdrs}'");
                    break;
            }

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

        private static IEnumerable<ParameterDefinition> ParseParameterTable(IMarkdownTable table, ParameterLocation location, TableDecoder decoder, IssueLogger issues, bool navigationProperties = false)
        {
            // tables sometimes have column spans to delineate different sections of the table. for instance:
            //
            // | Name | Type   | Description
            // |------|--------|--------------
            // | one  | int    | first number
            // | two  | int    | second number
            // | **fancy numbers**
            // | pi   | double | third number
            //
            // our markdown parser captures this as a regular row with all the columns, except with &nbsp; for all the blanks.
            // we try to infer such rows by looking for a **bold** first cell, followed by nbsp in all the other cells.
            // see below.

            var records = table.RowValues.
                Where(r => !r[0].StartsWith("**") || r.Skip(1).Any(c => c != "&nbsp;")). // see comment above
                Select(r => new ParameterDefinition
                {
                    Name = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["name"]),
                    Type = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["type"]).ParseParameterDataType(defaultValue: ParameterDataType.String),
                    Description = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["description"]),
                    Required = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["description"]).IsRequired(),
                    Optional = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["description"]).IsOptional(),
                    Location = location,
                    IsNavigatable = navigationProperties,
                }).ToList();

            var badRows = records.Count(r => string.IsNullOrEmpty(r.Name));
            if (badRows > 0)
            {
                var tableHeaders = $"|{ string.Join("|", table.ColumnHeaders)}|";
                if (badRows == records.Count)
                {
                    issues.Warning(ValidationErrorCode.MarkdownParserError, $"Failed to parse any rows out of table with headers: {tableHeaders}");
                    return Enumerable.Empty<ParameterDefinition>();
                }

                issues.Warning(ValidationErrorCode.ParameterParserError, $"Failed to parse {badRows} row(s) in table with headers: {tableHeaders}");
                records = records.Where(r => !string.IsNullOrEmpty(r.Name)).ToList();
            }

            return records;
        }

        private static IEnumerable<EnumerationDefinition> ParseEnumerationTable(IMarkdownTable table, TableDecoder decoder)
        {
            List<EnumerationDefinition> records = new List<EnumerationDefinition>();
            foreach (var r in table.RowValues)
            {
                var usedColumns = new List<string>();
                records.Add(new EnumerationDefinition
                {
                    MemberName = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["memberName"], usedColumns),
                    NumericValue = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["numericValue"], usedColumns).ToInt32(),
                    Description = r.ValueForColumn(table, decoder.ParseRule.ColumnNames["description"], usedColumns),
                    TypeName = decoder.Stamp,
                    IsFlags = decoder.IsFlags,
                });
            }

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

        private TableDecoder FindDecoderFromHeaderText(Stack<Config.DocumentHeader> headerStack)
        {
            foreach (var kvp in this.CommonHeaderContentMap)
            {
                var lastHeader = headerStack.Peek().Title;
                if (lastHeader.IContains(kvp.Key))
                {
                    var decoder = kvp.Value;
                    if (decoder.Parents != null)
                    {
                        foreach (var parentTitle in decoder.Parents.Where(t => t.Contains("{x}")))
                        {
                            var matchKey = parentTitle.Replace("{x} ", string.Empty);
                            foreach (var parentHeader in headerStack.Skip(1).Select(h => h.Title))
                            {
                                var matchIndex = parentHeader.IndexOf(matchKey);
                                if (matchIndex != -1)
                                {
                                    var matchWord = parentHeader.Substring(0, matchIndex - 1).Split(' ').LastOrDefault();
                                    if (char.IsUpper(matchWord[0]))
                                    {
                                        matchWord = char.ToLowerInvariant(matchWord[0]) + matchWord.Substring(1);
                                    }

                                    decoder = decoder.StampedWith(matchWord);
                                    decoder.IsFlags = lastHeader.Contains("flags") || parentHeader.Contains("flags");
                                    return decoder;
                                }
                            }
                        }
                    }

                    return decoder;
                }
                else if (kvp.Key.Contains("{x}"))
                {
                    // eventually bring FriendlyWildcard into this code...
                    var matchIndex = lastHeader.IndexOf(kvp.Key.Replace("{x} ", string.Empty));
                    if (matchIndex != -1)
                    {
                        var lastWord = lastHeader.Substring(0, matchIndex - 1).Split(' ').LastOrDefault();
                        if (char.IsUpper(lastWord[0]))
                        {
                            lastWord = char.ToLowerInvariant(lastWord[0]) + lastWord.Substring(1);
                        }

                        var decoder = kvp.Value.StampedWith(lastWord);
                        decoder.IsFlags = lastHeader.Contains("flags");
                        return decoder;
                    }
                }
            }

            return null;
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
