using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation
{
    public class ExampleDefinition : ResourceDefinition
    {
        public ExampleDefinition(CodeBlockAnnotation annotation, string jsonContent, DocFile source)
            : base(annotation, jsonContent, source)
        {

        }
    }
}
