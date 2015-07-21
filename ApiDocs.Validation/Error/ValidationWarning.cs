namespace ApiDocs.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ValidationWarning : ValidationError
    {

        public ValidationWarning(ValidationErrorCode code, string source, string format, params object[] formatParams)
            : base(code, source, format, formatParams)
        {

        }

        public override bool IsWarning { get { return true; } }

        public override bool IsError { get { return false; } }
    }
}
