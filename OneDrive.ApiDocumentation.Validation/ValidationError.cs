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
    }
}
