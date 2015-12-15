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
        /// <summary>
        /// Convert a tablespec block into one of our internal object model representations
        /// </summary>
        /// <param name="tableSpecBlock"></param>
        /// <param name="lastHeaderBlock"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        public static TableDefinition ParseTableSpec(Block tableSpecBlock, Block lastHeaderBlock, out ValidationError[] errors)
        {
            List<ValidationError> discoveredErrors = new List<ValidationError>();
            List<ItemDefinition> items = new List<ItemDefinition>();

            var tableShape = tableSpecBlock.Table;
            TableBlockType discoveredTableType = TableBlockType.Unknown;
            string headerText = null;
            // Try matching based on header
            if (null != lastHeaderBlock && null != lastHeaderBlock.Content)
            {
                headerText = lastHeaderBlock.Content;
                discoveredTableType = CommonHeaderMatch(headerText);
            }

            // Try matching based on shape
            if (discoveredTableType == TableBlockType.Unknown && null != tableSpecBlock.Table)
            {
                discoveredTableType = TableShapeMatch(tableShape);
            }

            switch (discoveredTableType)
            {
                case TableBlockType.ErrorCodes:
                    items.AddRange(ParseErrorTable(tableShape));
                    break;

                case TableBlockType.PathParameters:
                    items.AddRange(ParseParameterTable(tableShape, ParameterLocation.Path));
                    break;

                case TableBlockType.ResourcePropertyDescriptions:
                case TableBlockType.RequestObjectProperties:
                case TableBlockType.ResponseObjectProperties:
                    items.AddRange(ParseParameterTable(tableShape, ParameterLocation.JsonObject));
                    break;

                case TableBlockType.HttpHeaders:
                    items.AddRange(ParseHeadersTable(tableShape));
                    break;

                case TableBlockType.QueryStringParameters:
                    items.AddRange(ParseParameterTable(tableShape, ParameterLocation.QueryString));
                    break;

                case TableBlockType.EnumerationValues:
                    items.AddRange(ParseEnumerationTable(tableShape));
                    break;

                case TableBlockType.AuthScopes:
                    items.AddRange(ParseAuthScopeTable(tableShape));
                    break;

                case TableBlockType.Unknown:
                    discoveredErrors.Add(new ValidationMessage(null, "Ignored unclassified table: headerText='{0}', tableHeaders='{1}'", headerText, tableShape.ColumnHeaders.ComponentsJoinedByString(",")));
                    break;
                default:
                    discoveredErrors.Add(new ValidationMessage(null, "Ignored table: classification='{2}', headerText='{0}', tableHeaders='{1}'", headerText, tableShape.ColumnHeaders.ComponentsJoinedByString(","), discoveredTableType));
                    break;
            }

            errors = discoveredErrors.ToArray();

            return new TableDefinition(discoveredTableType, items, headerText);
        }

        /// <summary>
        /// Convert a markdown table into ErrorDefinition objects
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private static IEnumerable<ErrorDefinition> ParseErrorTable(IMarkdownTable table)
        {
            var records = from r in table.RowValues
                          select new ErrorDefinition 
                          { 
                              HttpStatusCode = r.ValueForColumn(table, "HTTP Code"),
                              HttpStatusMessage = r.ValueForColumn(table, "HTTP Error Message"),
                              ErrorCode = r.ValueForColumn(table, "Error Code"),
                              Description = r.ValueForColumn(table, "Error Message")
                          };

            return records;
        }

        private static IEnumerable<ParameterDefinition> ParseParameterTable(IMarkdownTable table, ParameterLocation location)
        {
            var records = from r in table.RowValues
                          select new ParameterDefinition
                          {
                              Name = r.ValueForColumn(table, "Parameter Name", "Property Name", "Name"),
                              Type = r.ValueForColumn(table, "Type", "Value").ParseParameterDataType(),
                              Description = r.ValueForColumn(table, "Description"),
                              Location = location,
                              Required = r.ValueForColumn(table, "Description").IsRequired()
                          };
            return records;
        }

        private static IEnumerable<ParameterDefinition> ParseHeadersTable(IMarkdownTable table)
        {
            var records = from r in table.RowValues
                          select new ParameterDefinition
                          {
                              Name = r.ValueForColumn(table, "Name", "Header Name"),
                              Type = ParameterDataType.String,
                              Description = r.ValueForColumn(table, "Description"),
                              Location = ParameterLocation.Header
                          };
            return records;
        }

        private static IEnumerable<EnumerationDefinition> ParseEnumerationTable(IMarkdownTable table)
        {
            var records = from r in table.RowValues
                          select new EnumerationDefinition
                          {
                              Value = r.ValueForColumn(table, "Value"),
                              Description = r.ValueForColumn(table, "Description")
                          };
            return records;
        }

        private static IEnumerable<AuthScopeDefinition> ParseAuthScopeTable(IMarkdownTable table)
        {
            var records = from r in table.RowValues
                          select new AuthScopeDefinition
                          {
                              Scope = r.ValueForColumn(table, "Scope Name"),
                              Title = r.ValueForColumn(table, "Title"),
                              Description = r.ValueForColumn(table, "Description"),
                              Required = r.ValueForColumn(table, "Required").ToBoolean()
                          };
            return records;
        }

        public static Dictionary<string, TableBlockType> CommonHeaderContentMap = new Dictionary<string, TableBlockType>
        {
            { "Error Response", TableBlockType.ErrorCodes },
            { "Path Parameters", TableBlockType.PathParameters },
            { "Properties", TableBlockType.ResourcePropertyDescriptions },
            { "Request Body", TableBlockType.RequestObjectProperties },
            { "Query String Parameters", TableBlockType.QueryStringParameters },
            { "Request Headers", TableBlockType.HttpHeaders },
            { "Authentication Scopes", TableBlockType.AuthScopes },
            { "EnumeratedValues", TableBlockType.EnumerationValues }
        };
            
        private static TableBlockType CommonHeaderMatch(string lastHeader)
        {
            return (from key in CommonHeaderContentMap.Keys 
                    where lastHeader.ContainsIgnoreCase(key) 
                    select CommonHeaderContentMap[key]).FirstOrDefault();
        }

        private static TableBlockType TableShapeMatch(IMarkdownTable table)
        {
            return TableBlockType.Unknown;
        }


        // EnumeratedValues are usual Value | Description

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
        AuthScopes

    }
}
