using ApiDocs.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation.UnitTests
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
