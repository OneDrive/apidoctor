namespace OneDrive.ApiDocumentation.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ValidationError
    {
        protected ValidationError()
        {

        }

        public ValidationError(string source, string messageformat, params object[] formatParams)
        {
            this.Source = source;
            this.Message = string.Format(messageformat, formatParams);
        }

        public string Message { get; set; }

        public string Source { get; set; }

        public ValidationError[] InnerErrors { get; set; }

        public virtual bool IsWarning { get { return false; } }

        public virtual bool IsError { get { return true; } }

        public string ErrorText 
        {
            get 
            {
                StringBuilder sb = new StringBuilder();
                if (IsWarning)
                {
                    sb.Append("Warning: ");
                }
                else if (IsError)
                {
                    sb.Append("Error: ");
                }
                
                if (!string.IsNullOrEmpty(Source))
                {
                    sb.Append(Source);
                    sb.Append(": ");
                }
                sb.Append(Message);

                if (null != InnerErrors && InnerErrors.Length > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine(InnerErrors.ErrorsToString("   "));
                }
                
                return sb.ToString();
            }
        }

        public static ValidationError NewConsolidatedError(ValidationError[] errors, string message, params object[] parameters)
        {
            ValidationError error = null;
            if (errors.All(err => err.IsWarning))
            {
                error = new ValidationWarning(null, message, parameters);
            }
            else
            {
                error = new ValidationError(null, message, parameters);
            }

            error.InnerErrors = errors;
            return error;
        }
    }

    public class ValidationWarning : ValidationError
    {

        public ValidationWarning(string source, string format, params object[] formatParams)
            : base(source, format, formatParams)
        {

        }

        public override bool IsWarning { get { return true; } }

        public virtual bool IsError { get { return false; } }
    }

    public class ValidationMessage : ValidationError
    {
        public ValidationMessage(string source, string format, params object[] formatParams)
            : base(source, format, formatParams)
        {

        }

        public override bool IsWarning { get { return false; } }
        public override bool IsError { get { return false; } }
    }

    public static class ValidationErrorExtensions
    {
        public static string ErrorsToString(this IEnumerable<ValidationError> errors, string linePrefix = "", int linesBetweenErrors = 0)
        {
            var messages = from e in errors select string.Concat(linePrefix, e.ErrorText);

            string lineEnding = Environment.NewLine;
            if (linesBetweenErrors > 0)
            {
                StringBuilder sb = new StringBuilder(lineEnding);
                for (int i = 0; i < linesBetweenErrors; i++)
                {
                    sb.AppendLine();
                }
                lineEnding = sb.ToString();
            }
            return messages.ComponentsJoinedByString(lineEnding);
        }
    }
}
