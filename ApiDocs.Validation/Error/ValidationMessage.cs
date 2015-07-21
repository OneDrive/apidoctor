namespace ApiDocs.Validation.Error
{
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
