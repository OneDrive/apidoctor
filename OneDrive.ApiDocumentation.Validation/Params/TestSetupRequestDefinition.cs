namespace OneDrive.ApiDocumentation.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using System.ComponentModel;

    public class TestSetupRequestDefinition : BasicRequestDefinition
    {
        private const string SourceName = "PlaceholderRequest";
        
        /// <summary>
        /// Define which values should be captured from the Http Response and stored for another call
        /// </summary>
        [JsonProperty("capture", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, string> OutputValues { get; set; }

        public override ValidationError[] CheckForErrors()
        {
            List<ValidationError> errors = new List<ValidationError>();
            errors.AddRange(base.CheckForErrors());

            foreach (string key in OutputValues.Keys)
            {
                if (BasicRequestDefinition.LocationForKey(key) != PlaceholderLocation.StoredValue)
                    errors.Add(new ValidationError(ValidationErrorCode.OutputValueKeyNameIncorrect, null, "capture key name must be a stored value name (missing square brackets): {0}", key));

                var valueType = BasicRequestDefinition.LocationForKey(OutputValues[key]);
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
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public async Task<ValidationResult<bool>> MakeSetupRequestAsync(string baseUrl, AuthenicationCredentials credentials, Dictionary<string, string> storedValues)
        {
            List<ValidationError> errors = new List<ValidationError>();
            
            // Get the HttpRequest, either from MethodName or by parsing HttpRequest

            Http.HttpRequest request = null;
            try
            {
                request = this.GetHttpRequest(baseUrl);
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError(ValidationErrorCode.InvalidRequestFormat, null, "An error occured creating the http request: {0}", ex.Message));
                return new ValidationResult<bool>(false, errors);
            }

            var placeholderValues = RequestParameters.ToPlaceholderValuesArray(storedValues);

            // Update the request with the parameters in request-parameters
            request.RewriteRequestWithParameters(placeholderValues);
            MethodDefinition.AddAccessTokenToRequest(credentials, request);

            try
            {
                var webRequest = request.PrepareHttpWebRequest(baseUrl);
                var response = await Http.HttpResponse.ResponseFromHttpWebResponseAsync(webRequest);

                errors.Add(new ValidationMessage(null, "HTTP Response:\n{0}\n\n", response.FullHttpText()));

                // Check to see if this request is "successful" or not
                if ( (AllowedStatusCodes == null && response.WasSuccessful) ||
                    AllowedStatusCodes.Contains(response.StatusCode))
                {
                    string expectedContentType = ExpectedResponseContentType(OutputValues.Values);

                    // Check for content type mismatch
                    if (string.IsNullOrEmpty(response.ContentType) && expectedContentType != null)
                    {
                        return new ValidationResult<bool>(false, new ValidationError(ValidationErrorCode.UnsupportedContentType, SourceName, "No Content-Type found for a non-204 response"));
                    }

                    // Load requested values into stored values
                    foreach (var outputKey in OutputValues.Keys)
                    {
                        var source = OutputValues[outputKey];
                        storedValues[outputKey] = response.ValueForKeyedIdentifier(source);
                    }

                    return new ValidationResult<bool>(!errors.Any(x => x.IsError), errors);
                }
                else
                {
                    errors.Add(new ValidationError(ValidationErrorCode.HttpStatusCodeDifferent, SourceName, "Http response was not successful: {0}", response.ContentType));
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
            if (propertiesToBeRead.Any(x => BasicRequestDefinition.LocationForKey(x) == PlaceholderLocation.Json))
                return MethodDefinition.MimeTypeJson;
            
            return null;
        }


    }
}
