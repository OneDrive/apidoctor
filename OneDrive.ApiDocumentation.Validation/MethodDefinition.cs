namespace OneDrive.ApiDocumentation.Validation
{
    using System;
    using System.Net;
    using OneDrive.ApiDocumentation.Validation.Http;

    /// <summary>
    /// Definition of a request / response pair for the API
    /// </summary>
    public class MethodDefinition
    {
        public MethodDefinition()
        {
        }

        public static MethodDefinition FromRequest(string request, CodeBlockAnnotation annotation)
        {
            return new MethodDefinition { Request = request, RequestMetadata = annotation };
        }


        /// <summary>
        /// Friendly name of this request/response pair
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The raw request data from the documentation (fenced code block with annotation)
        /// </summary>
        public string Request {get; private set;}

        /// <summary>
        /// Properties about the Request
        /// </summary>
        public CodeBlockAnnotation RequestMetadata { get; private set; }

        /// <summary>
        /// The raw response data from the documentation (fenced code block with annotation)
        /// </summary>
        public string Response { get; private set; }

        /// <summary>
        /// Properties about the Response
        /// </summary>
        public CodeBlockAnnotation ResponseMetadata { get; set; }

        public void AddResponse(string rawResponse, CodeBlockAnnotation annotation)
        {
            Response = rawResponse;
            ResponseMetadata = annotation;
        }

        /// <summary>
        /// Converts the raw HTTP request in Request into a callable HttpWebRequest
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
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

