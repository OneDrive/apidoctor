namespace ApiDocs.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ValidationMessage : ValidationError
    {
        public ValidationMessage(string source, string format, params object[] formatParams)
            : base(ValidationErrorCode.Unknown, source, format, formatParams)
        {

        }

        public override bool IsWarning { get { return false; } }
        public override bool IsError { get { return false; } }
    }
}
