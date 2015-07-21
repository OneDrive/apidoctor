namespace ApiDocs.Validation.UnitTests
{
    using System.Collections.Generic;
    using System.Linq;
    using ApiDocs.Validation.Error;

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
