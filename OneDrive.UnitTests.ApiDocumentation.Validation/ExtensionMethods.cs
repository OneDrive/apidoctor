using OneDrive.ApiDocumentation.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.UnitTests.ApiDocumentation.Validation
{
    static class ExtensionMethods
    {
        internal static IEnumerable<ValidationError> WarningsOrErrorsOnly(this IEnumerable<ValidationError> errors)
        {
            return from e in errors
                   where e.IsWarning || e.IsError
                   select e;
        }

    }
}
