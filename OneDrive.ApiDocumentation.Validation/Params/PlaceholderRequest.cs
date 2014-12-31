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
        public async Task<ValidationResult<bool>> PopulateValuesFromRequestAsync(string baseUrl, string accessToken)
        {
            if (string.IsNullOrEmpty(HttpRequest))
            {
                return new ValidationResult<bool>(false,
                    new ValidationError(ValidationErrorCode.RequestWasEmptyOrNull, SourceName, "Request was missing or null."));
            }
            if (null == Values || Values.Count == 0)
            {
                // No values are populated by the result, so we don't bother calling it
                return new ValidationResult<bool>(true);
            }

            Http.HttpParser parser = new Http.HttpParser();
            Http.HttpRequest request = null;
            try
            {
                request = parser.ParseHttpRequest(HttpRequest);
            }
            catch (Exception ex)
            {
                return new ValidationResult<bool>(false, new ValidationError(ValidationErrorCode.InvalidRequestFormat, SourceName, "Request was not parsed: {0}", ex.Message));
            }

            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Authorization = "Bearer " + accessToken;
            }
            
            try
            {
                var webRequest = request.PrepareHttpWebRequest(baseUrl);
                var response = await Http.HttpResponse.ResponseFromHttpWebResponseAsync(webRequest);
                if (response.WasSuccessful)
                {
                    if (response.ContentType.StartsWith("application/json"))
                    {
                        foreach (var parameter in Values)
                        {
                            parameter.Value = Json.JsonPath.ValueFromJsonPath(response.Body, parameter.Path).ToString();
                        }
                        return new ValidationResult<bool>(true);
                    }
                    else
                    {
                        // TODO: Handle any other relevent formats
                        return new ValidationResult<bool>(false, new ValidationError(ValidationErrorCode.UnsupportedContentType, SourceName, "Content-Type of response was not supported: {0}", response.ContentType));
                    }
                }
                else
                {
                    return new ValidationResult<bool>(false, new ValidationError(ValidationErrorCode.HttpStatusCodeDifferent, SourceName, "Http response was not successful: {0}", response.ContentType));
                }
            }
            catch (Exception ex)
            {
                return new ValidationResult<bool>(false, new ValidationError(ValidationErrorCode.Unknown, SourceName, "Exception while making request: {0}", ex.Message));
            }
        }
    }
}
