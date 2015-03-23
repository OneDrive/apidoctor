using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.Validation.Json
{
    public class JsonExample
    {

        public JsonExample(string json, CodeBlockAnnotation annotation = null)
        {
            JsonData = json;
            Annotation = annotation ?? new CodeBlockAnnotation();
        }

        public string JsonData { get; set; }
        public CodeBlockAnnotation Annotation { get; set; }
    }
}
