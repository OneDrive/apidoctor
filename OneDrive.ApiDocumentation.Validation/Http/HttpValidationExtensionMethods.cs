using System;
using System.Collections.Generic;
using System.Linq;

namespace OneDrive.ApiDocumentation.Validation
{
    public static class HttpValidationExtensionMethods
    {
        /// <summary>
        /// Evaluate the ApiRequirements file and return any inconsistencies
        /// as ValidationErrors.
        /// </summary>
        /// <returns>The http request.</returns>
        /// <param name="request">Request.</param>
        /// <param name="requirements">Requirements.</param>
        public static ValidationResult<bool> IsRequestValid(this Http.HttpRequest request, string sourceFile, ApiRequirements apiRequirements)
        {
            if (null == apiRequirements)
                return new ValidationResult<bool>(true);

            List<ValidationError> errors = new List<ValidationError>();

            var reqs = apiRequirements.HttpRequest;

            if (null != request.ContentType && !reqs.ContentTypes.Contains(request.ContentType))
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

        public static ValidationResult<bool> IsResponseValid(this Http.HttpResponse response, string sourceFile, ApiRequirements requirements)
        {
            if (null == requirements)
                return new ValidationResult<bool>(true);
            
            List<ValidationError> errors = new List<ValidationError>();

            var reqs = requirements.HttpResponse;

            if (reqs.ContentTypes != null && !string.IsNullOrEmpty(response.ContentType) && 
                !reqs.ContentTypes.Contains(response.ContentType))
            {
                errors.Add(new ValidationWarning(ValidationErrorCode.InvalidContentType, sourceFile, "Response Content-Type header value is not in the supported list of content-types: {0}", response.ContentType));
            }

            return new ValidationResult<bool>(!errors.Any(), errors);
        }

    }
}

