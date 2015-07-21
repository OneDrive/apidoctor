namespace ApiDocs.Validation.Json
{
    public class JsonExample
    {

        public JsonExample(string json, CodeBlockAnnotation annotation = null)
        {
            this.JsonData = json;
            this.Annotation = annotation ?? new CodeBlockAnnotation();
        }

        public string JsonData { get; set; }
        public CodeBlockAnnotation Annotation { get; set; }
    }
}
