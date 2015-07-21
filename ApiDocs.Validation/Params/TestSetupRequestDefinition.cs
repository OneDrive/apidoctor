namespace ApiDocs.Validation.Params
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using ApiDocs.Validation.Error;
    using ApiDocs.Validation.Http;
    using Newtonsoft.Json;

    public class TestSetupRequestDefinition : BasicRequestDefinition
    {
        private const string SourceName = "PlaceholderRequest";
        internal const string MimeTypeEtag = "If-Match: *";

        /// <summary>
        /// Define which values should be captured from the Http Response and stored for another call
        /// </summary>
        [JsonProperty("capture", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, string> OutputValues { get; set; }

        public override ValidationError[] CheckForErrors()
        {
            List<ValidationError> errors = new List<ValidationError>();
            errors.AddRange(base.CheckForErrors());

            foreach (string key in this.OutputValues.Keys)
            {
                if (LocationForKey(key) != PlaceholderLocation.StoredValue)
                    errors.Add(new ValidationError(ValidationErrorCode.OutputValueKeyNameIncorrect, null, "capture key name must be a stored value name (missing square brackets): {0}", key));

                var valueType = LocationForKey(this.OutputValues[key]);
                switch (valueType)
                {
                    case PlaceholderLocation.Invalid:
                    case PlaceholderLocation.StoredValue:
                    case PlaceholderLocation.Url:
                        errors.Add(new ValidationError(ValidationErrorCode.OutputValueValueIncorrect, null, "capture value name is invalid. Must be Json, Body, or HttpHeader. Found: {0}", valueType));
                        break;
                }
            }

            return errors.ToArray();
        }

        /// <summary>
        /// Make a call to the HttpRequest and populate the Value property of 
        /// every PlaceholderValue in Values based on the result.
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <param name="credentials"></param>
        /// <param name="storedValues"></param>
        /// <param name="documents"></param>
        /// <returns></returns>
        public async Task<ValidationResult<bool>> MakeSetupRequestAsync(string baseUrl, AuthenicationCredentials credentials, Dictionary<string, string> storedValues, DocSet documents)
        {
            var errors = new List<ValidationError>();
            
            // Get the HttpRequest, either from MethodName or by parsing HttpRequest

            HttpRequest request;
            try
            {
                request = this.GetHttpRequest(baseUrl, documents);
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError(ValidationErrorCode.InvalidRequestFormat, null, "An error occured creating the http request: {0}", ex.Message));
                return new ValidationResult<bool>(false, errors);
            }

            var placeholderValues = this.RequestParameters.ToPlaceholderValuesArray(storedValues);

            // Update the request with the parameters in request-parameters
            request.RewriteRequestWithParameters(placeholderValues);
            MethodDefinition.AddAccessTokenToRequest(credentials, request);

            documents.RecordLogMessage(true, "Test-setup request:", "{0}", request.FullHttpText());

            try
            {
                var response = await request.GetResponseAsync(baseUrl);

                documents.RecordLogMessage(true, "Test-setup response:", "{0}", response.FullHttpText());
                if (response.RetryCount > 0)
                {
                    errors.Add(new ValidationMessage(null, "HTTP request was retried {0} times.", response.RetryCount));
                }
                errors.Add(new ValidationMessage(null, "HTTP Response:\n{0}\n\n", response.FullHttpText()));

                // Check to see if this request is "successful" or not
                if ( (this.AllowedStatusCodes == null && response.WasSuccessful) ||
                    (this.AllowedStatusCodes != null && this.AllowedStatusCodes.Contains(response.StatusCode)))
                {
                    string expectedContentType = (null != this.OutputValues) ? ExpectedResponseContentType(this.OutputValues.Values) : null;

                    // Check for content type mismatch
                    if (string.IsNullOrEmpty(response.ContentType) && expectedContentType != null)
                    {
                        return new ValidationResult<bool>(false, new ValidationError(ValidationErrorCode.UnsupportedContentType, SourceName, "No Content-Type found for a non-204 response"));
                    }

                    // Load requested values into stored values
                    if (null != this.OutputValues)
                    {
                        foreach (var outputKey in this.OutputValues.Keys)
                        {
                            var source = this.OutputValues[outputKey];
                            storedValues[outputKey] = response.ValueForKeyedIdentifier(source);
                        }
                    }

                    return new ValidationResult<bool>(!errors.Any(x => x.IsError), errors);
                }
                else
                {
                    if ((this.AllowedStatusCodes != null && !this.AllowedStatusCodes.Contains(response.StatusCode)) || !response.WasSuccessful)
                    {
                        string expectedCodes = "200-299";
                        if (this.AllowedStatusCodes != null)
                            expectedCodes = this.AllowedStatusCodes.ComponentsJoinedByString(",");
                        errors.Add(new ValidationError(ValidationErrorCode.HttpStatusCodeDifferent, SourceName, "Http response status code {0} didn't match expected values: {1}", response.StatusCode, expectedCodes));
                    }
                    else
                    {
                        errors.Add(new ValidationError(ValidationErrorCode.HttpStatusCodeDifferent, SourceName, "Http response content type was invalid: {0}", response.ContentType));
                    }
                    
                    return new ValidationResult<bool>(false, errors);
                }
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError(ValidationErrorCode.Unknown, SourceName, "Exception while making request: {0}", ex.Message));
                return new ValidationResult<bool>(false, errors);
            }
        }

        private static string ExpectedResponseContentType(IEnumerable<string> propertiesToBeRead)
        {
            if (propertiesToBeRead.Any(x => LocationForKey(x) == PlaceholderLocation.Json))
                return MethodDefinition.MimeTypeJson;
            
            return null;
        }


    }
}
