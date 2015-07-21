namespace ApiDocs.Validation.Http
{
    using System.Collections.Generic;
    using System.Linq;
    using ApiDocs.Validation.Config;
    using ApiDocs.Validation.Error;

    public static class HttpValidationExtensionMethods
    {
        /// <summary>
        /// Evaluate the ApiRequirements file and return any inconsistencies
        /// as ValidationErrors.
        /// </summary>
        /// <returns>The http request.</returns>
        /// <param name="request">Request.</param>
        /// <param name="sourceFile"></param>
        /// <param name="apiRequirements"></param>
        public static ValidationResult<bool> IsRequestValid(this HttpRequest request, string sourceFile, ApiRequirements apiRequirements)
        {
            if (null == apiRequirements || null == apiRequirements.HttpRequest)
                return new ValidationResult<bool>(true);

            List<ValidationError> errors = new List<ValidationError>();

            var reqs = apiRequirements.HttpRequest;

            var requestMimeType = ConvertToMimeType(request.ContentType);
            if (null != reqs.ContentTypes && null != requestMimeType 
                && !reqs.ContentTypes.Contains(requestMimeType))
            {
                errors.Add(new ValidationWarning(ValidationErrorCode.InvalidContentType, sourceFile, "Request content-type header value is not in the supported list of content-types: {0}", request.ContentType));
            }

            if (reqs.HttpMethods != null && !reqs.HttpMethods.Contains(request.Method))
            {
                errors.Add(new ValidationError(ValidationErrorCode.InvalidHttpMethod, sourceFile, "Request HTTP method is not in the supported list of HTTP methods: {0}", request.Method));
            }

            if (reqs.MaxUrlLength > 0 && request.Url.Length > reqs.MaxUrlLength)
            {
                errors.Add(new ValidationError(ValidationErrorCode.UrlLengthExceedsMaximum, sourceFile, "Request URL is longer than the defined maximum URL length: [{0}] {1}", request.Url.Length, request.Url));
            }

            if (reqs.StandardHeaders != null && request.Headers.Count > 0)
            {
                foreach (var headerName in request.Headers.AllKeys)
                {
                    if (!reqs.StandardHeaders.Contains(headerName))
                    {
                        errors.Add(new ValidationWarning(ValidationErrorCode.NonStandardHeaderUsed, sourceFile, "Request includes a non-standard header: {0}", headerName));
                    }
                }
            }

            return new ValidationResult<bool>(!errors.Any(), errors);
        }

        /// <summary>
        /// Converts to just the MIME type (application/json) for a content type header (application/json; odata.metadata=full)
        /// </summary>
        /// <param name="contentTypeHeaderValue"></param>
        /// <returns></returns>
        public static string ConvertToMimeType(string contentTypeHeaderValue)
        {
            if (null == contentTypeHeaderValue)
                return null;

            var splitIndex = contentTypeHeaderValue.IndexOf(';');
            if (-1 == splitIndex)
                return contentTypeHeaderValue;

            return contentTypeHeaderValue.Substring(0, splitIndex).TrimEnd();
        }

        public static ValidationResult<bool> IsResponseValid(this HttpResponse response, string sourceFile, ApiRequirements requirements)
        {
            if (null == requirements || null == requirements.HttpResponse)
                return new ValidationResult<bool>(true);
            
            List<ValidationError> errors = new List<ValidationError>();

            var reqs = requirements.HttpResponse;

            var responseMimeType = ConvertToMimeType(response.ContentType);
            if (reqs.ContentTypes != null && null != responseMimeType 
                && !reqs.ContentTypes.Contains(responseMimeType))
            {
                errors.Add(new ValidationWarning(ValidationErrorCode.InvalidContentType, sourceFile, "Response Content-Type header value is not in the supported list of content-types: {0}", response.ContentType));
            }

            return new ValidationResult<bool>(!errors.Any(), errors);
        }

    }
}

