using System.Globalization;
using System.Text;

namespace Kibali;

/// <summary>
/// Generates a markdown document using a StringBuilder.
/// </summary>
internal class MarkDownBuilder
{
    private readonly StringBuilder markDownStringBuilder = new();

    /// <summary>
    /// Add a table row to a markdown document.
    /// </summary>
    /// <param name="cellValues">Values to add to the table cell.</param>
    public void AddTableRow(params string[] cellValues)
    {
        _ = this.markDownStringBuilder.AppendFormat(CultureInfo.CurrentCulture, "|{0}|", string.Join("|", cellValues));
        _ = this.markDownStringBuilder.AppendLine();
    }

        /// <summary>
    /// Add a new line to end a table in the markdown.
    /// </summary>
    public void EndTable()
        => _ = this.markDownStringBuilder.AppendLine();
 
    /// <summary>
    /// Add the header row of a table.
    /// </summary>
    /// <param name="headers">Array of headers to add to the start of the row.</param>
    public void StartTable(params string[] headers)
    {
        _ = this.markDownStringBuilder.AppendFormat(CultureInfo.CurrentCulture, "|{0}|", string.Join("|", headers));
        _ = this.markDownStringBuilder.AppendLine();
        _ = this.markDownStringBuilder.Append('|');

        if (headers != null)
        {
            for (var i = 0; i < headers.Length; i++)
            {
                _ = this.markDownStringBuilder.Append(":---|");
            }
        }

        _ = this.markDownStringBuilder.AppendLine();
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>
    /// A string that represents the current object.
    /// </returns>
    public override string ToString()
        => this.markDownStringBuilder.ToString();
}
