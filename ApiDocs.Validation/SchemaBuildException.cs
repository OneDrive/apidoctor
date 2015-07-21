using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation
{
    [Serializable]
    public class SchemaBuildException : Exception
    {
        public SchemaBuildException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
