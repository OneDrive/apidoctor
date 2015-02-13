using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.Validation
{
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
        /// <param name="data"></param>
        /// <returns></returns>
        public static TableBlockType ParseTableSpec(MarkdownDeep.Block tableSpecBlock, MarkdownDeep.Block lastHeaderBlock, out ItemDefinition[] data, out ValidationError[] errors)
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

                case TableBlockType.Unknown:
                    discoveredErrors.Add(new ValidationMessage(null, "Ignored unclassified table: headerText='{0}', tableHeaders='{1}'", headerText, tableShape.ColumnHeaders.ComponentsJoinedByString(",")));
                    break;
                default:
                    discoveredErrors.Add(new ValidationMessage(null, "Ignored table: classification='{2}', headerText='{0}', tableHeaders='{1}'", headerText, tableShape.ColumnHeaders.ComponentsJoinedByString(","), discoveredTableType));
                    break;
            }

            data = items.ToArray();
            errors = discoveredErrors.ToArray();
            return discoveredTableType;
        }

        /// <summary>
        /// Convert a markdown table into ErrorDefinition objects
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private static IEnumerable<ErrorDefinition> ParseErrorTable(MarkdownDeep.IMarkdownTable table)
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

        private static IEnumerable<ParameterDefinition> ParseParameterTable(MarkdownDeep.IMarkdownTable table, ParameterLocation location)
        {
            var records = from r in table.RowValues
                          select new ParameterDefinition
                          {
                              Name = r.ValueForColumn(table, "Parameter Name", "Property Name", "Name"),
                              Type = r.ValueForColumn(table, "Type", "Value").ToDataType(),
                              Description = r.ValueForColumn(table, "Description"),
                              Location = location,
                              Required = r.ValueForColumn(table, "Description").IsRequired()
                          };
            return records;
        }

        private static IEnumerable<ParameterDefinition> ParseHeadersTable(MarkdownDeep.IMarkdownTable table)
        {
            var records = from r in table.RowValues
                          select new ParameterDefinition
                          {
                              Name = r.ValueForColumn(table, "Name"),
                              Type = Json.JsonDataType.String,
                              Description = r.ValueForColumn(table, "Description"),
                              Location = ParameterLocation.Header
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
        };

        private static TableBlockType CommonHeaderMatch(string lastHeader)
        {
            foreach (var key in CommonHeaderContentMap.Keys)
            {
                if (lastHeader.Contains(key))
                    return CommonHeaderContentMap[key];
            }

            return TableBlockType.Unknown;
        }

        private static TableBlockType TableShapeMatch(MarkdownDeep.IMarkdownTable table)
        {
            return TableBlockType.Unknown;
        }


        // Enumeration are usual Value | Description

    }


    internal enum TableBlockType
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
        PathParameters

    }
}
