/*
 * Markdown Scanner
 * Copyright (c) Microsoft Corporation
 * All rights reserved. 
 * 
 * MIT License
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of 
 * this software and associated documentation files (the ""Software""), to deal in 
 * the Software without restriction, including without limitation the rights to use, 
 * copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
 * Software, and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all 
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace ApiDocs.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using ApiDocs.Validation.Error;
    using ApiDocs.Validation.Http;
    using ApiDocs.Validation.Json;
    using ApiDocs.Validation.Params;
    using Newtonsoft.Json;
    using MultipartMime;

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
            this.RequiredScopes = new string[0];
        }

        public static MethodDefinition FromRequest(string request, CodeBlockAnnotation annotation, DocFile source)
        {
            var method = new MethodDefinition
            {
                Request = request,
                RequestMetadata = annotation,
                Identifier = annotation.MethodName,
                SourceFile = source,
                RequiredScopes = annotation.RequiredScopes
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

        /// <summary>
        /// Collection of required scopes for this method to be called successfully (Files.ReadWrite, User.Read, etc)
        /// </summary>
        public string[] RequiredScopes { get; set; }
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
        /// <param name="documents"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        public async Task<ValidationResult<HttpRequest>> GenerateMethodRequestAsync(ScenarioDefinition scenario, DocSet documents, IServiceAccount account)
        {
            var parser = new HttpParser();
            var request = parser.ParseHttpRequest(this.Request);
            AddAccessTokenToRequest(account.CreateCredentials(), request);
            AddTestHeaderToRequest(scenario, request);
            AddAdditionalHeadersToRequest(account, request);

            List<ValidationError> errors = new List<ValidationError>();

            if (null != scenario)
            {
                var storedValuesForScenario = new Dictionary<string, string>();

                if (null != scenario.TestSetupRequests)
                {
                    foreach (var setupRequest in scenario.TestSetupRequests)
                    {
                        try
                        {
                            var result = await setupRequest.MakeSetupRequestAsync(storedValuesForScenario, documents, scenario, account);
                            errors.AddRange(result.Messages);

                            if (result.IsWarningOrError)
                            {
                                // If we can an error or warning back from a setup method, we fail the whole request.
                                return new ValidationResult<HttpRequest>(null, errors);
                            }
                        }
                        catch (Exception ex)
                        {
                            return new ValidationResult<HttpRequest>(null, new ValidationError(ValidationErrorCode.ConsolidatedError, null, "An exception occured while processing setup-requests: {0}", ex.Message));
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
                            "GenerateMethodRequestAsync",
                            ex.Message));

                    return new ValidationResult<HttpRequest>(null, errors);
                }

                if (scenario.StatusCodesToRetry != null)
                {
                    request.RetryOnStatusCode =
                        (from status in scenario.StatusCodesToRetry select (System.Net.HttpStatusCode)status).ToList();
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

            this.ModifyRequestForAccount(request, account);
            return new ValidationResult<HttpRequest>(request, errors);
        }

        /// <summary>
        ///  This method will adapt a request based on parameters for an account. It should be the last thing we do before sending
        ///  the request to the account.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="account"></param>
        public void ModifyRequestForAccount(HttpRequest request, IServiceAccount account)
        {
            if (account.Transformations?.Request?.Actions?.Prefix == null)
                return;


            if (this.RequestMetadata.Target == TargetType.Action || this.RequestMetadata.Target == TargetType.Function)
            {
                // Add the ActionPrefix to the last path component of the URL
                AddPrefixToLastUrlComponent(request, account.Transformations.Request.Actions.Prefix);
            }
        }

        private static void AddPrefixToLastUrlComponent(HttpRequest request, string actionPrefix)
        {
            UriBuilder builder = null;
            string path = null;
            if (request.Url.StartsWith("/"))
            {
                path = request.Url;
            }
            else
            {
                builder = new UriBuilder(request.Url);
                path = builder.Path;
            }
            
            string[] parts = path.Split('/');
            if (parts.Length > 0)
            {
                parts[parts.Length - 1] = $"{actionPrefix}{parts[parts.Length - 1]}";
                path = "/"  + parts.ComponentsJoinedByString("/");
            }

            if (null != builder)
            {
                builder.Path = path;
                request.Url = builder.ToString();
            }
            else
            {
                request.Url = path;
            }
        }

        /// <summary>
        /// Add information about the test that generated this call to the request headers.
        /// </summary>
        /// <param name="scenario"></param>
        /// <param name="request"></param>
        internal static void AddTestHeaderToRequest(ScenarioDefinition scenario, HttpRequest request)
        {
            var headerValue = string.Format(
                "method-name: {0}; scenario-name: {1}",
                scenario.MethodName,
                scenario.Description);
            request.Headers.Add("ApiDocsTestInfo", headerValue);
        }

        /// <summary>
        /// Breaks down the key/value string and adds to the request header
        /// </summary>
        /// <param name="headerKeyValueString"></param>
        /// <returns></returns>
        internal static void AddAdditionalHeadersToRequest(IServiceAccount account, HttpRequest request)
        {
            // parse the passed in addtional headers and add to request..format for headers should be <HeaderName>:<HeaderValue>
            if (account.AdditionalHeaders != null && account.AdditionalHeaders.Length > 0)
            {
                foreach (string nameValueHeader in account.AdditionalHeaders)
                {
                    string[] split = nameValueHeader.Split(new Char[] { ':' }, 2);
                    request.Headers.Add(split[0], split[1]);
                }
            }
        }    

        internal static void AddAccessTokenToRequest(AuthenicationCredentials credentials, HttpRequest request)
        {
            credentials.AuthenticateRequest(request);
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

                if (param.Value == null)
                    request.Headers.Remove(headerName);
                else 
                    request.Headers[headerName] = param.Value;
            }
        }

        /// <summary>
        /// Check to ensure the http request is valid
        /// </summary>
        /// <param name="detectedErrors"></param>
        internal void VerifyRequestFormat(List<ValidationError> detectedErrors)
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
                ValidateContentForType(new MimeContentType(request.ContentType), request.Body, detectedErrors);
            }

            var verifyApiRequirementsResponse = request.IsRequestValid(this.SourceFile.DisplayName, this.SourceFile.Parent.Requirements);
            detectedErrors.AddRange(verifyApiRequirementsResponse.Messages);
        }

        private void ValidateContentForType(MimeContentType contentType, string content, List<ValidationError> detectedErrors, bool validateJsonSchema = false)
        {
            if (contentType.MimeType.Equals(MimeTypeJson, StringComparison.OrdinalIgnoreCase))
            {
                // Verify that the request is valid JSON
                try
                {
                    JsonConvert.DeserializeObject(content);

                    if (validateJsonSchema)
                    {
                        ValidationError[] schemaErrors;
                        if (!ContentMatchesResourceSchema(content, RequestMetadata.ResourceType, SourceFile.Parent.ResourceCollection, out schemaErrors))
                        {
                            detectedErrors.AddRange(schemaErrors);
                        }
                    }
                }
                catch (Exception ex)
                {
                    detectedErrors.Add(new ValidationError(ValidationErrorCode.JsonParserException, null, "Invalid JSON format: {0}", ex.Message));
                }
            }
            else if (contentType.MimeType.Equals(MimeTypeMultipartRelated, StringComparison.OrdinalIgnoreCase))
            {
                // Parse the multipart/form-data body to ensure it's properly formatted
                try
                {
                    MultipartMimeContent multipartContent = new MultipartMime.MultipartMimeContent(contentType, content);
                    var part = multipartContent.PartWithId("<metadata>");
                    ValidateContentForType(part.ContentType, part.Body, detectedErrors, validateJsonSchema: true);
                }
                catch (Exception ex)
                {
                    detectedErrors.Add(new ValidationError(ValidationErrorCode.ContentFormatException, null, "Invalid Multipart MIME content format: {0}", ex.Message));
                }

            }
            else if (contentType.MimeType.Equals(MimeTypePlainText, StringComparison.OrdinalIgnoreCase))
            {
                // Ignore this, because it isn't something we can verify
            }
            else
            {
                detectedErrors.Add(new ValidationWarning(ValidationErrorCode.UnsupportedContentType, null, "Unvalidated request content type: {0}", contentType.MimeType));
            }
        }

        private bool ContentMatchesResourceSchema(string content, string resourceType, JsonResourceCollection resources, out ValidationError[] schemaErrors)
        {
            List<ValidationError> errors = new List<Error.ValidationError>();
            var resourceTypeSchema = resources.GetJsonSchema(resourceType, errors, null);

            ValidationError[] validationErrors;
            if (!resources.ValidateJsonCompilesWithSchema(resourceTypeSchema, new JsonExample(content) { Annotation = new CodeBlockAnnotation { TruncatedResult = true } }, out validationErrors))
            {
                errors.AddRange(validationErrors);
            }

            schemaErrors = errors.ToArray();
            return errors.WereErrors();
        }


        /// <summary>
        /// Validates that a particular HttpResponse matches the method definition and optionally the expected response.
        /// </summary>
        /// <param name="method">Method definition that was used to generate a request.</param>
        /// <param name="actualResponse">Actual response from the service (this is what we validate).</param>
        /// <param name="expectedResponse">Prototype response (expected) that shows what a valid response should look like.</param>
        /// <param name="scenario">A test scenario used to generate the response, which may include additional parameters to verify.</param>
        /// <param name="errors">A collection of errors, warnings, and verbose messages generated by this process.</param>
        public void ValidateResponse(HttpResponse actualResponse, HttpResponse expectedResponse, ScenarioDefinition scenario, out ValidationError[] errors, ValidationOptions options = null)
        {
            if (null == actualResponse) throw new ArgumentNullException("actualResponse");

            List<ValidationError> detectedErrors = new List<ValidationError>();

            // Verify the request is valid (headers, etc)
            this.VerifyRequestFormat(detectedErrors);

            // Verify that the expected response headers match the actual response headers
            ValidationError[] httpErrors;
            if (null != expectedResponse && !expectedResponse.ValidateResponseHeaders(actualResponse, out httpErrors, (null != scenario) ? scenario.AllowedStatusCodes : null))
            {
                detectedErrors.AddRange(httpErrors);
            }

            // Verify the actual response body is correct according to the schema defined for the response
            ValidationError[] bodyErrors;
            this.VerifyResponseBody(actualResponse, expectedResponse, out bodyErrors, options);
            detectedErrors.AddRange(bodyErrors);

            // Verify any expectations in the scenario are met
            if (null != scenario)
            {
                scenario.ValidateExpectations(actualResponse, detectedErrors);
            }

            errors = detectedErrors.ToArray();
        }

        /// <summary>
        /// Verify that the body of the actual response is consistent with the method definition and expected response parameters
        /// </summary>
        /// <param name="method">The MethodDefinition that generated the response.</param>
        /// <param name="actualResponse">The actual response from the service to validate.</param>
        /// <param name="expectedResponse">The prototype expected response from the service.</param>
        /// <param name="detectedErrors">A collection of errors that will be appended with any detected errors</param>
        private void VerifyResponseBody(HttpResponse actualResponse, HttpResponse expectedResponse, out ValidationError[] errors, ValidationOptions options = null)
        {
            List<ValidationError> detectedErrors = new List<ValidationError>();

            if (string.IsNullOrEmpty(actualResponse.Body) &&
                (expectedResponse != null && !string.IsNullOrEmpty(expectedResponse.Body)))
            {
                detectedErrors.Add(new ValidationError(ValidationErrorCode.HttpBodyExpected, null, "Body missing from response (expected response includes a body or a response type was provided)."));
            }
            else if (!string.IsNullOrEmpty(actualResponse.Body))
            {
                ValidationError[] schemaErrors;
                if (this.ExpectedResponseMetadata == null ||
                    (string.IsNullOrEmpty(this.ExpectedResponseMetadata.ResourceType) &&
                     (expectedResponse != null && !string.IsNullOrEmpty(expectedResponse.Body))))
                {
                    detectedErrors.Add(new ValidationError(ValidationErrorCode.ResponseResourceTypeMissing, null, "Expected a response, but resource type on method is missing: {0}", this.Identifier));
                }
                else
                {
                    var otherResources = this.SourceFile.Parent.ResourceCollection;
                    if (
                        !otherResources.ValidateResponseMatchesSchema(
                            this,
                            actualResponse,
                            expectedResponse,
                            out schemaErrors, 
                            options))
                    {
                        detectedErrors.AddRange(schemaErrors);
                    }
                }

                var responseValidation = actualResponse.IsResponseValid(
                    this.SourceFile.DisplayName,
                    this.SourceFile.Parent.Requirements);
                detectedErrors.AddRange(responseValidation.Messages);
            }

            errors = detectedErrors.ToArray();
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

