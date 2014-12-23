namespace OneDrive.ApiDocumentation.Validation
{
    using System;
    using System.Net;
    using System.Linq;
    using OneDrive.ApiDocumentation.Validation.Http;
    using System.Threading.Tasks;

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
        public HttpWebRequest BuildRequest(string baseUrl, string accessToken, Param.RequestParameters methodParameters = null)
        {
            var request = PreviewRequest(methodParameters);
            request.Headers.Add("Authorization", "Bearer " + accessToken);

            return request.PrepareHttpWebRequest(baseUrl);
        }

        public HttpRequest PreviewRequest(Param.RequestParameters methodParameters)
        {
            var parser = new HttpParser();
            var request = parser.ParseHttpRequest(Request);

            if (null != methodParameters)
            {
                string newUrl = RewriteUrl(request.Url, methodParameters.Parameters.ToArray());
                request.Url = newUrl;

                string newBody = RewriteJsonBody(request.Body, methodParameters.Parameters.ToArray());
                request.Body = newBody;
            }

            return request;
        }

        private static string RewriteUrl(string url, Param.ParameterValue[] parameters)
        {
            var urlParameters = (from p in parameters
                                 where p.Location == Param.ParameterLocation.Url
                                 select p);
            if (urlParameters.FirstOrDefault() != null)
            {
                foreach (var parameter in urlParameters)
                {
                    string placeholder = string.Concat("{", parameter.Id, "}");
                    url = url.Replace(placeholder, parameter.Value);
                }
            }
            return url;
        }

        private string RewriteJsonBody(string jsonSource, Param.ParameterValue[] parameters)
        {
            if (string.IsNullOrEmpty(jsonSource)) return jsonSource;

            var jsonParameters = (from p in parameters
                                  where p.Location == Param.ParameterLocation.Json
                                  select p);

            if (jsonParameters.FirstOrDefault() != null)
            {
                Newtonsoft.Json.Linq.JObject bodyObject = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(jsonSource);

                foreach (var jsonParam in jsonParameters)
                {
                    bodyObject[jsonParam.Id] = jsonParam.Value;
                }

                return Newtonsoft.Json.JsonConvert.SerializeObject(bodyObject);
            }
            else
            {
                return jsonSource;
            }
        }


        public async Task<HttpResponse> ApiResponseForMethod(string baseUrl, string accessToken, Param.RequestParameters methodParameters = null)
        {
            var request = BuildRequest(baseUrl, accessToken, methodParameters);
            var response = await HttpResponse.ResponseFromHttpWebResponseAsync(request);
            return response;
        }

        class DynamicBinder : System.Dynamic.SetMemberBinder
        {
            public DynamicBinder(string propertyName) : base(propertyName, true)
            {
            }

            public override System.Dynamic.DynamicMetaObject FallbackSetMember(System.Dynamic.DynamicMetaObject target, System.Dynamic.DynamicMetaObject value, System.Dynamic.DynamicMetaObject errorSuggestion)
            {
                throw new NotImplementedException();
            }
        }
    }
}

