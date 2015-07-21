namespace ApiDocs.Validation.TableSpec
{
    using System.Collections.Generic;
    using System.Linq;

    public class TableDefinition
    {
        public TableBlockType Type { get; set; }

        public ItemDefinition[] Rows { get; set; }

        public string Title { get; set; }

        public TableDefinition(TableBlockType type, IEnumerable<ItemDefinition> rows, string headerText)
        {
            this.Type = type;
            this.Rows = rows.ToArray();
            this.Title = headerText;
        }
    }
}
