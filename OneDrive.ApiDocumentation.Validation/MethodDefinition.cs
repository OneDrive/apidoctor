using System.Collections.Generic;

namespace OneDrive.ApiDocumentation.Validation
{
    using System;
    using System.Net;
    using System.Linq;
    using OneDrive.ApiDocumentation.Validation.Http;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    /// <summary>
    /// Definition of a request / response pair for the API
    /// </summary>
    public class MethodDefinition
    {
        internal const string MimeTypeJson = "application/json";
        internal const string MimeTypeMultipartRelated = "multipart/related";
        internal const string MimeTypePlainText = "text/plain";

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
        public async Task<ValidationResult<HttpWebRequest>> BuildRequestAsync(string baseUrl, AuthenicationCredentials credentials, ScenarioDefinition scenario = null)
        {
            var previewResult = await PreviewRequestAsync(scenario, baseUrl, credentials);
            if (previewResult.IsWarningOrError)
            {
                return new ValidationResult<HttpWebRequest>(null, previewResult.Messages);
            }

            var httpRequest = previewResult.Value;
            HttpWebRequest request = httpRequest.PrepareHttpWebRequest(baseUrl);
            return new ValidationResult<HttpWebRequest>(request);
        }

        public async Task<ValidationResult<HttpRequest>> PreviewRequestAsync(ScenarioDefinition scenario, string baseUrl, AuthenicationCredentials credentials)
        {
            var parser = new HttpParser();
            var request = parser.ParseHttpRequest(Request);
            AddAccessTokenToRequest(credentials, request);

            if (null != scenario)
            {
                var storedValuesForScenario = new Dictionary<string, string>();
                if (null != scenario.TestSetupRequests)
                {
                    foreach (var setupRequest in scenario.TestSetupRequests)
                    {
                        var result = await setupRequest.MakeSetupRequestAsync(baseUrl, credentials, storedValuesForScenario);
                        if (result.IsWarningOrError)
                        {
                            return new ValidationResult<HttpRequest>(null, result.Messages);
                        }
                    }
                }

                try 
                {
                    var placeholderValues = scenario.RequestParameters.ToPlaceholderValuesArray(storedValuesForScenario);
                    request.RewriteRequestWithParameters(placeholderValues);
                }
                catch (Exception ex)
                {
                    // Error when applying parameters to the request
                    return new ValidationResult<HttpRequest>(null, new ValidationError(ValidationErrorCode.RewriteRequestFailure, "PreviewRequestAsync", ex.Message));
                }
            }

            if (string.IsNullOrEmpty(request.Accept))
            {
                request.Accept = MimeTypeJson;
            }

            return new ValidationResult<HttpRequest>(request);
        }

        internal static void AddAccessTokenToRequest(AuthenicationCredentials credentials, HttpRequest request)
        {
            if (!(credentials is NoCredentials) && string.IsNullOrEmpty(request.Authorization))
            {
                request.Authorization =  credentials.AuthenicationToken;
            }
        }

        internal static string RewriteUrlWithParameters(string url, IEnumerable<PlaceholderValue> parameters)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.PlaceholderKey == "!url")
                {
                    url = parameter.Value;
                }
                else if (parameter.PlaceholderKey.StartsWith("{") && parameter.PlaceholderKey.EndsWith("}"))
                {
                    url = url.Replace(parameter.PlaceholderKey, parameter.Value);
                }
                else
                {
                    string placeholder = string.Concat("{", parameter.PlaceholderKey, "}");
                    url = url.Replace(placeholder, parameter.Value);
                }
            }

            return url;
        }

        internal static string RewriteJsonBodyWithParameters(string jsonSource, IEnumerable<PlaceholderValue> parameters)
        {
            if (string.IsNullOrEmpty(jsonSource)) return jsonSource;

            var jsonParameters = (from p in parameters
                                  where p.Location == PlaceholderLocation.Json
                                  select p);


            foreach (var parameter in jsonParameters)
            {
                jsonSource = Json.JsonPath.SetValueForJsonPath(jsonSource, parameter.PlaceholderKey, parameter.Value);
            }

            return jsonSource;
        }

        internal static void RewriteHeadersWithParameters(HttpRequest request, IEnumerable<PlaceholderValue> headerParameters)
        {
            foreach (var param in headerParameters)
            {
                string headerName = param.PlaceholderKey;
                if (param.PlaceholderKey.EndsWith(":"))
                    headerName = param.PlaceholderKey.Substring(0, param.PlaceholderKey.Length - 1);

                request.Headers[headerName] = param.Value;
            }
        }

        public async Task<ValidationResult<HttpResponse>> ApiResponseForMethod(string baseUrl, AuthenicationCredentials credentials, ScenarioDefinition scenario = null)
        {
            var buildResult = await BuildRequestAsync(baseUrl, credentials, scenario);
            if (buildResult.IsWarningOrError)
            {
                return new ValidationResult<HttpResponse>(null, buildResult.Messages);
            }

            var response = await HttpResponse.ResponseFromHttpWebResponseAsync(buildResult.Value);
            return new ValidationResult<HttpResponse>(response);
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

        /// <summary>
        /// Check to ensure the http request is valid
        /// </summary>
        /// <param name="detectedErrors"></param>
        internal void VerifyHttpRequest(List<ValidationError> detectedErrors)
        {
            HttpParser parser = new HttpParser();
            HttpRequest request = null;
            try
            {
                request = parser.ParseHttpRequest(Request);
            }
            catch (Exception ex)
            {
                detectedErrors.Add(new ValidationError(ValidationErrorCode.HttpParserError, null, "Exception while parsing HTTP request: {0}", ex.Message));
                return;
            }

            if (null != request.ContentType)
            {
                if (request.IsMatchingContentType(MimeTypeJson))
                {
                    // Verify that the request is valid JSON
                    try
                    {
                        JsonConvert.DeserializeObject(request.Body);
                    }
                    catch (Exception ex)
                    {
                        detectedErrors.Add(new ValidationError(ValidationErrorCode.JsonParserException, null, "Invalid JSON format: {0}", ex.Message));
                    }
                }
                else if (request.IsMatchingContentType(MimeTypeMultipartRelated))
                {
                    // TODO: Parse the multipart/form-data body to ensure it's properly formatted
                }
                else if (request.IsMatchingContentType(MimeTypePlainText))
                {
                    // Ignore this, because it isn't something we can verify
                }
                else
                {
                    detectedErrors.Add(new ValidationWarning(ValidationErrorCode.UnsupportedContentType, null, "Unvalidated request content type: {0}", request.ContentType));
                }
            }
        }


    }
}

