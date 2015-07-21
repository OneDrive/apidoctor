using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.Validation
{
    public class ItemDefinition
    {
        public string Title { get; set; }
        public string Description { get; set; }

        public List<ParameterDefinition> Parameters { get; set; }

        public ItemDefinition()
        {
            Parameters = new List<ParameterDefinition>();
        }

    }
}
