namespace OneDrive.ApiDocumentation.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ValidationError
    {

        public ValidationError()
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

        public string ErrorText 
        {
            get 
            {
                StringBuilder sb = new StringBuilder();
                if (!string.IsNullOrEmpty(Source))
                {
                    sb.Append(Source);
                    sb.Append(": ");
                }
                sb.Append(Message);
                return sb.ToString();
            }
        }
    }

    public static class ValidationErrorExtensions
    {
        public static string AllErrors(this IEnumerable<ValidationError> errors)
        {
            var messages = from e in errors select e.ErrorText;
            return messages.ComponentsJoinedByString(Environment.NewLine);
        }
    }
}
