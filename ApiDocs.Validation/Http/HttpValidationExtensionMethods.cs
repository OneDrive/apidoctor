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

            var requestMimeType = new MultipartMime.MimeContentType(request.ContentType);
            if (null != reqs.ContentTypes && null != requestMimeType.MimeType 
                && !reqs.ContentTypes.Contains(requestMimeType.MimeType))
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
                    if (!reqs.StandardHeaders.ContainsString(headerName, apiRequirements.CaseSensativeHeaders))
                    {
                        errors.Add(new ValidationWarning(ValidationErrorCode.NonStandardHeaderUsed, sourceFile, "Request includes a non-standard header: {0}", headerName));
                    }
                }
            }

            return new ValidationResult<bool>(!errors.Any(), errors);
        }

        public static bool ContainsString(this string[] array, string value, bool caseSenativeComparison = false)
        {
            return array.Any(x => x.Equals(value, caseSenativeComparison ? System.StringComparison.Ordinal : System.StringComparison.OrdinalIgnoreCase));
        }

        public static ValidationResult<bool> IsResponseValid(this HttpResponse response, string sourceFile, ApiRequirements requirements)
        {
            if (null == requirements || null == requirements.HttpResponse)
                return new ValidationResult<bool>(true);
            
            List<ValidationError> errors = new List<ValidationError>();

            var reqs = requirements.HttpResponse;

            var responseMimeType = new MultipartMime.MimeContentType(response.ContentType);
            if (reqs.ContentTypes != null && null != responseMimeType 
                && !reqs.ContentTypes.Contains(responseMimeType.MimeType))
            {
                errors.Add(new ValidationWarning(ValidationErrorCode.InvalidContentType, sourceFile, "Response Content-Type header value is not in the supported list of content-types: {0}", response.ContentType));
            }

            return new ValidationResult<bool>(!errors.Any(), errors);
        }

    }
}

