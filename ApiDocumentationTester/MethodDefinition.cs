using ApiDocumentationTester.HttpRequestParser;
using System;
using System.Net;

namespace ApiDocumentationTester
{
    public class MethodDefinition
    {
        public MethodDefinition()
        {
        }

        public string DisplayName { get; set; }

        public string Request {get;set;}

        public string Response {get;set;}

        public string ResponseType {get;set;}

        public string[] Parameters { get; set; }

        public bool ResponseIsCollection { get; set; }


        public HttpWebRequest BuildRequest(string baseUrl, string accessToken)
        {
            var parser = new HttpParser();
            var request = parser.ParseHttpRequest(Request);
            request.Headers.Add("Authorization", "Bearer " + accessToken);

            // TODO: process parameters and replace values in the request

            return request.PrepareHttpWebRequest(baseUrl);
        }
    }
}

