namespace ApiDocs.Validation
{
    public class ErrorDefinition : ItemDefinition
    {
        public string HttpStatusCode { get; set; }

        public string HttpStatusMessage { get; set; }

        public string ErrorCode { get; set; }

    }
}
