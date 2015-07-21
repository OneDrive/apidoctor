namespace ApiDocs.Validation.Error
{
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
