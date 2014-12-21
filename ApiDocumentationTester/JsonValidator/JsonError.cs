using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocumentationTester
{
    public class ValidationError
    {

        public string Message { get; set; }

        public ValidationError[] InnerErrors { get; set; }
    }
}
