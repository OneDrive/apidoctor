using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.Validation
{
    public static class ValidationConfig
    {
        static ValidationConfig()
        {
            ValidationConfig.ExpectedResponseAsRequiredProperties = true;
        }

        public static bool ExpectedResponseAsRequiredProperties { get; set; }

    }
}
