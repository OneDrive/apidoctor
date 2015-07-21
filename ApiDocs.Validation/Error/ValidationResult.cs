using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation
{
    public class ValidationResult<T>
    {
        public T Value { get; private set; }
        public ValidationError[] Messages { get; private set; }

        public bool IsError
        {
            get
            {
                var query = from m in Messages where m.IsError == true select m;
                return query.FirstOrDefault() != null;
            }
        }

        public bool IsWarningOrError
        {
            get
            {
                var query = from m in Messages where m.IsError == true || m.IsWarning == true select m;
                return query.FirstOrDefault() != null;
            }
        }

        public ValidationResult(T result)
        {
            Value = result;
            Messages = new ValidationError[0];
        }

        public ValidationResult(T result, IEnumerable<ValidationError> messages)
        {
            Value = result;
            Messages = messages.ToArray();
        }

        public ValidationResult(T result, ValidationError error)
        {
            Value = result;
            Messages = new ValidationError[] { error };
        }

    }
}
