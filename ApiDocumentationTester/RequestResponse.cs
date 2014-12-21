using System;

namespace ApiDocumentationTester
{
    public class RequestResponse
    {
        public RequestResponse()
        {
        }

        public string DisplayName { get; set; }

        public string Request {get;set;}

        public string Response {get;set;}

        public string ResponseType {get;set;}

        public string[] Parameters { get; set; }
    }
}

