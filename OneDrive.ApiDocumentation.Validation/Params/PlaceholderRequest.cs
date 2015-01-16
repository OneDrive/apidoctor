namespace OneDrive.ApiDocumentation.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using System.ComponentModel;

    public class PlaceholderRequest
    {
        private const string SourceName = "PlaceholderRequest";

        [JsonProperty("request")]
        public string HttpRequest { get; set; }

        /// <summary>
        /// Query to find the requested value in the response object. For JSON responses
        /// the query should use the JSONpath language: http://goessner.net/articles/JsonPath/
        /// </summary>
        /// <value>The value path.</value>
        [JsonProperty("values")]
        public List<PlaceholderValue> Values { get; set; }


        /// <summary>
        /// Make a call to the HttpRequest and populate the Value property of 
        /// every PlaceholderValue in Values based on the result.
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public async Task<ValidationResult<bool>> PopulateValuesFromRequestAsync(string baseUrl, AuthenicationCredentials credentials)
        {
            List<ValidationError> errors = new List<ValidationError>();
            errors.Add(new ValidationMessage(null, "Making HTTP call to get values to fill into the request."));
            if (string.IsNullOrEmpty(HttpRequest))
            {
                errors.Add(new ValidationError(ValidationErrorCode.RequestWasEmptyOrNull, SourceName, "Request was missing or null."));
                return new ValidationResult<bool>(false, errors);
            }
            if (null == Values || Values.Count == 0)
            {
                // No values are populated by the result, so we don't bother calling it
                return new ValidationResult<bool>(true, errors);
            }

            Http.HttpParser parser = new Http.HttpParser();
            Http.HttpRequest request = null;
            try
            {
                errors.Add(new ValidationMessage(null, "HTTP Request:\n{0}\n\n", HttpRequest));
                request = parser.ParseHttpRequest(HttpRequest);
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError(ValidationErrorCode.InvalidRequestFormat, SourceName, "Request was not parsed: {0}", ex.Message));
                return new ValidationResult<bool>(false, errors);
            }

            MethodDefinition.AddAccessTokenToRequest(credentials, request);

            try
            {
                var webRequest = request.PrepareHttpWebRequest(baseUrl);
                var response = await Http.HttpResponse.ResponseFromHttpWebResponseAsync(webRequest);

                errors.Add(new ValidationMessage(null, "HTTP Response:\n{0}\n\n", response.FullHttpText()));

                if (response.WasSuccessful)
                {
                    string expectedContentType = ExpectedContentType(Values);
                    
                    if (expectedContentType == null || (response.ContentType != null && response.ContentType.StartsWith(expectedContentType)))
                    {
                        foreach (var parameter in Values)
                        {
                            SetParameterValueFromResponse(parameter, response, errors);
                        }
                        return new ValidationResult<bool>(!errors.Any(x => x.IsError), errors);
                    }
                    else
                    {
                        errors.Add(new ValidationError(ValidationErrorCode.UnsupportedContentType, SourceName, "Content-Type of response was not supported: {0}", response.ContentType));
                        return new ValidationResult<bool>(false, errors);
                    }
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

        private static void SetParameterValueFromResponse(PlaceholderValue parameter, Http.HttpResponse response, List<ValidationError> errors)
        {
            try
            {
                if (parameter.PathTarget == ResponseField.Json)
                {
                    parameter.Value = Json.JsonPath.ValueFromJsonPath(response.Body, parameter.Path).ToString();
                    errors.Add(new ValidationMessage(null, "Json Value Setting {0} = {1}", parameter.PlaceholderText, parameter.Value));
                }
                else if (parameter.PathTarget == ResponseField.Header)
                {
                    parameter.Value = response.Headers[parameter.Path];
                    errors.Add(new ValidationMessage(null, "Header Value Setting {0} = {1}", parameter.PlaceholderText, parameter.Value));
                }
                else if (parameter.PathTarget == ResponseField.None)
                {
                    errors.Add(new ValidationMessage(null, "None value setting {0} = {1}", parameter.PlaceholderText, parameter.Value));
                }
                else
                {
                    errors.Add(new ValidationWarning(ValidationErrorCode.BadPathTargetValue, null, "Unsupported PathTarget value: {0}", parameter.PathTarget));
                }
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError(ValidationErrorCode.ParameterParserError, null, "Exception getting parameter value {1}: {0}", ex.Message, parameter.PlaceholderText));
            }
        }

        private static string ExpectedContentType(IEnumerable<PlaceholderValue> expectedValues)
        {
            if (expectedValues.Any(x => x.PathTarget == ResponseField.Json))
                return "application/json";
            else
                return null;
        }


    }
}
