namespace ApiDocs.Validation.Error
{
    using System.Collections.Generic;
    using System.Linq;

    public class ValidationResult<T>
    {
        public T Value { get; private set; }
        public ValidationError[] Messages { get; private set; }

        public bool IsError
        {
            get
            {
                var query = from m in this.Messages where m.IsError select m;
                return query.FirstOrDefault() != null;
            }
        }

        public bool IsWarningOrError
        {
            get
            {
                var query = from m in this.Messages where m.IsError || m.IsWarning select m;
                return query.FirstOrDefault() != null;
            }
        }

        public ValidationResult(T result)
        {
            this.Value = result;
            this.Messages = new ValidationError[0];
        }

        public ValidationResult(T result, IEnumerable<ValidationError> messages)
        {
            this.Value = result;
            this.Messages = messages.ToArray();
        }

        public ValidationResult(T result, ValidationError error)
        {
            this.Value = result;
            this.Messages = new ValidationError[] { error };
        }

    }
}
