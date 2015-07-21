using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.Validation
{
    public class AuthScopeDefinition : ItemDefinition
    {
        public string Scope { get; set; }

        public bool Required { get; set; }

    }
}
