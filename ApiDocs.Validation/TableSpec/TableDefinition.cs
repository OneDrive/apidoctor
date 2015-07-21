using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.Validation
{
    public class TableDefinition
    {
        public TableBlockType Type { get; set; }

        public ItemDefinition[] Rows { get; set; }

        public string Title { get; set; }

        public TableDefinition(TableBlockType type, IEnumerable<ItemDefinition> rows, string headerText)
        {
            Type = type;
            Rows = rows.ToArray();
            Title = headerText;
        }
    }
}
