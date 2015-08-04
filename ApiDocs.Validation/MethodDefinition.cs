namespace ApiDocs.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using ApiDocs.Validation.Error;
    using ApiDocs.Validation.Http;
    using ApiDocs.Validation.Json;
    using ApiDocs.Validation.Params;
    using Newtonsoft.Json;

    /// <summary>
    /// Definition of a request / response pair for the API
    /// </summary>
    public class MethodDefinition : ItemDefinition
    {
        #region Constants
        internal const string MimeTypeJson = "application/json";
        internal const string MimeTypeMultipartRelated = "multipart/related";
        internal const string MimeTypePlainText = "text/plain";
        #endregion

        #region Constructors / Class Factory
        public MethodDefinition()
        {

            this.Errors = new List<ErrorDefinition>();
            this.Enumerations = new Dictionary<string, List<ParameterDefinition>>();
            this.RequestBodyParameters = new List<ParameterDefinition>();
            this.Scenarios = new List<ScenarioDefinition>();
        }

        public static MethodDefinition FromRequest(string request, CodeBlockAnnotation annotation, DocFile source)
        {
            var method = new MethodDefinition
            {
                Request = request,
                RequestMetadata = annotation,
                Identifier = annotation.MethodName,
                SourceFile = source
            };
            method.Title = method.Identifier;
            return method;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Method identifier for the request/response pair. Used to connect 
        /// scenario tests to this method
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// The raw request data from the documentation (fenced code block with 
        /// annotation)
        /// </summary>
        public string Request {get; private set;}

        /// <summary>
        /// Properties about the Request populated from the documentation
        /// </summary>
        public CodeBlockAnnotation RequestMetadata { get; private set; }

        /// <summary>
        /// The raw response data from the documentation (fenced code block with 
        /// annotation)
        /// </summary>
        public string ExpectedResponse { get; private set; }

        /// <summary>
        /// Metadata from the expected / example response in the documentation.
        /// </summary>
        public CodeBlockAnnotation ExpectedResponseMetadata { get; set; }

        /// <summary>
        /// The documentation file that was the source of this method
        /// </summary>
        /// <value>The source file.</value>
        public DocFile SourceFile {get; private set;}
        
        /// <summary>
        /// The raw HTTP response from the actual service
        /// </summary>
        /// <value>The actual response.</value>
        public string ActualResponse { get; set; }

        /// <summary>
        /// Scenarios defined inline with this method
        /// </summary>
        public List<ScenarioDefinition> Scenarios { get; private set; }
        #endregion

        public void AddExpectedResponse(string rawResponse, CodeBlockAnnotation annotation)
        {
            if (this.ExpectedResponse != null)
            {
                throw new InvalidOperationException("An expected response was already added to this request.");
            }

            this.ExpectedResponse = rawResponse;
            this.ExpectedResponseMetadata = annotation;
        }

        public void AddSimulatedResponse(string rawResponse, CodeBlockAnnotation annotation)
        {
            this.ActualResponse = rawResponse;
        }

        public void AddTestParams(string rawContent)
        {
            var scenarios = JsonConvert.DeserializeObject<ScenarioDefinition[]>(rawContent);
            if (null != scenarios)
            {
                foreach (var scenario in scenarios)
                {
                    this.Scenarios.Add(scenario);
                }
            }
        }

        #region Validation / Request Methods

        /// <summary>
        /// Take a scenario definition and convert the prototype request into a fully formed request. This includes appending
        /// the base URL to the request URL, executing any test-setup requests, and replacing the placeholders in the prototype
        /// request with proper values.
        /// </summary>
        /// <param name="scenario"></param>
        /// <param name="baseUrl"></param>
        /// <param name="credentials"></param>
        /// <param name="documents"></param>
        /// <returns></returns>
        public async Task<ValidationResult<HttpRequest>> PreviewRequestAsync(ScenarioDefinition scenario, string baseUrl, AuthenicationCredentials credentials, DocSet documents)
        {
            var parser = new HttpParser();
            var request = parser.ParseHttpRequest(this.Request);

            AddAccessTokenToRequest(credentials, request);

            List<ValidationError> errors = new List<ValidationError>();

            if (null != scenario)
            {

                var storedValuesForScenario = new Dictionary<string, string>();

                if (null != scenario.TestSetupRequests)
                {
                    foreach (var setupRequest in scenario.TestSetupRequests)
                    {
                        var result = await setupRequest.MakeSetupRequestAsync(baseUrl, credentials, storedValuesForScenario, documents);
                        errors.AddRange(result.Messages);

                        if (result.IsWarningOrError)
                        {
                            // If we can an error or warning back from a setup method, we fail the whole request.
                            return new ValidationResult<HttpRequest>(null, errors);
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
                    errors.Add(
                        new ValidationError(
                            ValidationErrorCode.RewriteRequestFailure,
                            "PreviewRequestAsync",
                            ex.Message));
                    
                    return new ValidationResult<HttpRequest>(null, errors);
                }

            }

            if (string.IsNullOrEmpty(request.Accept))
            {
                if (!string.IsNullOrEmpty(ValidationConfig.ODataMetadataLevel))
                {
                    request.Accept = MimeTypeJson + "; " + ValidationConfig.ODataMetadataLevel;
                }
                else
                {
                    request.Accept = MimeTypeJson;
                }
            }

            return new ValidationResult<HttpRequest>(request, errors);
        }

        internal static void AddAccessTokenToRequest(AuthenicationCredentials credentials, HttpRequest request)
        {
            if (!(credentials is NoCredentials) && string.IsNullOrEmpty(request.Authorization))
            {
                request.Authorization = credentials.AuthenicationToken;
            }

            if (!string.IsNullOrEmpty(credentials.FirstPartyApplicationHeaderValue) &&
                request.Headers["Application"] == null)
            {
                request.Headers.Add("Application", credentials.FirstPartyApplicationHeaderValue);
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
                jsonSource = JsonPath.SetValueForJsonPath(jsonSource, parameter.PlaceholderKey, parameter.Value);
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

        //public async Task<ValidationResult<HttpResponse>> ApiResponseForMethod(string baseUrl, AuthenicationCredentials credentials, DocSet documents, ScenarioDefinition scenario = null)
        //{
        //    var buildResult = await BuildRequestAsync(baseUrl, credentials, documents, scenario);
        //    if (buildResult.IsWarningOrError)
        //    {
        //        return new ValidationResult<HttpResponse>(null, buildResult.Messages);
        //    }

        //    var response = await HttpResponse.ResponseFromHttpWebResponseAsync(buildResult.Value);
        //    return new ValidationResult<HttpResponse>(response);
        //}


        /// <summary>
        /// Check to ensure the http request is valid
        /// </summary>
        /// <param name="detectedErrors"></param>
        internal void VerifyHttpRequest(List<ValidationError> detectedErrors)
        {
            HttpParser parser = new HttpParser();
            HttpRequest request;
            try
            {
                request = parser.ParseHttpRequest(this.Request);
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

            var verifyApiRequirementsResponse = request.IsRequestValid(this.SourceFile.DisplayName, this.SourceFile.Parent.Requirements);
            detectedErrors.AddRange(verifyApiRequirementsResponse.Messages);
        }
        #endregion

        #region Parameter Parsing
        public void SplitRequestUrl(out string relativePath, out string queryString, out string httpMethod)
        {
            var parser = new HttpParser();
            var request = parser.ParseHttpRequest(this.Request);
            httpMethod = request.Method;

            request.Url.SplitUrlComponents(out relativePath, out queryString);
        }
        #endregion

        #region Deep extraction properties

        public List<ErrorDefinition> Errors { get; set; }

        public Dictionary<string, List<ParameterDefinition>> Enumerations { get; set; }

        public List<ParameterDefinition> RequestBodyParameters { get; set; }

        #endregion



        
    }



}

