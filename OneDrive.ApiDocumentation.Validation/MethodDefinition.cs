using System.Collections.Generic;

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

        public static MethodDefinition FromRequest(string request, CodeBlockAnnotation annotation, DocFile source)
        {
            var method = new MethodDefinition { Request = request, RequestMetadata = annotation };
            method.DisplayName = annotation.MethodName;
            method.SourceFile = source;
            return method;
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
        public string ExpectedResponse { get; private set; }

        /// <summary>
        /// Properties about the Response
        /// </summary>
        public CodeBlockAnnotation ExpectedResponseMetadata { get; set; }

        /// <summary>
        /// The documentation file that was the source of this method
        /// </summary>
        /// <value>The source file.</value>
        public DocFile SourceFile {get; private set;}

        public void AddExpectedResponse(string rawResponse, CodeBlockAnnotation annotation)
        {
            ExpectedResponse = rawResponse;
            ExpectedResponseMetadata = annotation;
        }

        public string ActualResponse { get; set; }

        /// <summary>
        /// Converts the raw HTTP request in Request into a callable HttpWebRequest
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public async Task<HttpWebRequest> BuildRequestAsync(string baseUrl, string accessToken, RequestParameters methodParameters = null)
        {
            var request = await PreviewRequestAsync(methodParameters, baseUrl, accessToken);
            return request.PrepareHttpWebRequest(baseUrl);
        }

        public async Task<HttpRequest> PreviewRequestAsync(RequestParameters methodParameters, string baseUrl, string accessToken)
        {
            var parser = new HttpParser();
            var request = parser.ParseHttpRequest(Request);
            if (!string.IsNullOrEmpty(accessToken) && string.IsNullOrEmpty(request.Authorization))
            {
                request.Authorization = "Bearer " + accessToken;
            }

            if (null != methodParameters)
            {

                if (null != methodParameters.DynamicParameters)
                {
                    bool success = await methodParameters.DynamicParameters.PopulateValuesAsync(baseUrl, accessToken);
                    if (!success)
                    {
                        // TODO: Do something better here.
                        return null;
                    }
                }

                try 
                {
                    RewriteRequestWithParameters(request, methodParameters);
                }
                catch (Exception ex)
                {
                    // Error when applying parameters to the request
                }
            }

            if (string.IsNullOrEmpty(request.Accept))
            {
                request.Accept = "application/json";
            }

            return request;
        }

        private static void RewriteRequestWithParameters(HttpRequest request, RequestParameters parameters)
        {
            List<ParameterValue> paramValues = new List<ParameterValue>();
            paramValues.AddRange(parameters.StaticParameters);
            if (null != parameters.DynamicParameters)
            {
                paramValues.AddRange(parameters.DynamicParameters.Values);
            }

            request.Url = RewriteUrlWithParameters(request.Url, from p in paramValues
                                                              where p.Location == ParameterLocation.Url
                                                              select p);

            var bodyParameters = from p in paramValues
                                          where p.Location == ParameterLocation.Body
                                          select p;
            var jsonBodyParameters = from p in paramValues
                                              where p.Location == ParameterLocation.Json
                                              select p;
            if (jsonBodyParameters.FirstOrDefault() != null && request.ContentType.StartsWith("application/json"))
            {
                RewriteJsonBodyWithParameters(request.Body, jsonBodyParameters);
            }
            else if (bodyParameters.FirstOrDefault() != null)
            {
                request.Body = bodyParameters.First().Value.ToString();
            }
        }

        private static string RewriteUrlWithParameters(string url, IEnumerable<ParameterValue> parameters)
        {
            foreach (var parameter in parameters)
            {
                string placeholder = string.Concat("{", parameter.Id, "}");
                url = url.Replace(placeholder, parameter.Value.ToString());
            }
            return url;
        }

        private static string RewriteJsonBodyWithParameters(string jsonSource, IEnumerable<ParameterValue> parameters)
        {
            if (string.IsNullOrEmpty(jsonSource)) return jsonSource;

            var jsonParameters = (from p in parameters
                                  where p.Location == ParameterLocation.Json
                                  select p);

            if (jsonParameters.FirstOrDefault() != null)
            {
                Newtonsoft.Json.Linq.JObject bodyObject = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(jsonSource);

                foreach (var jsonParam in jsonParameters)
                {
                    bodyObject[jsonParam.Id] = (dynamic)jsonParam.Value;
                }

                return Newtonsoft.Json.JsonConvert.SerializeObject(bodyObject);
            }
            else
            {
                return jsonSource;
            }
        }


        public async Task<HttpResponse> ApiResponseForMethod(string baseUrl, string accessToken, RequestParameters methodParameters = null)
        {
            var request = await BuildRequestAsync(baseUrl, accessToken, methodParameters);
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

