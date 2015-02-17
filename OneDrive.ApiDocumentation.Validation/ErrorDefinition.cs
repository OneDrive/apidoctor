using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.Validation
{
    public class ErrorDefinition : ItemDefinition
    {
        public string HttpStatusCode { get; set; }

        public string HttpStatusMessage { get; set; }

        public string ErrorCode { get; set; }

    }
}
