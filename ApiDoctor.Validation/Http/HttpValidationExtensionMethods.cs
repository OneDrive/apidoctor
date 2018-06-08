/*
 * API Doctor
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

namespace ApiDoctor.Validation.Http
{
    using System.Collections.Generic;
    using System.Linq;
    using ApiDoctor.Validation.Config;
    using ApiDoctor.Validation.Error;

    public static class HttpValidationExtensionMethods
    {
        /// <summary>
        /// Evaluate the ApiRequirements file and return any inconsistencies
        /// as ValidationErrors.
        /// </summary>
        public static ValidationResult<bool> IsRequestValid(this HttpRequest request, string sourceFile, ApiRequirements apiRequirements, IssueLogger issues)
        {
            if (null == apiRequirements || null == apiRequirements.HttpRequest)
                return new ValidationResult<bool>(true);

            var reqs = apiRequirements.HttpRequest;

            var requestMimeType = new MultipartMime.MimeContentType(request.ContentType);
            if (null != reqs.ContentTypes && null != requestMimeType.MimeType 
                && !reqs.ContentTypes.Contains(requestMimeType.MimeType))
            {
                issues.Warning(ValidationErrorCode.InvalidContentType, $"Request content-type header value is not in the supported list of content-types: {request.ContentType}");
            }

            if (reqs.HttpMethods != null && !reqs.HttpMethods.Contains(request.Method))
            {
                issues.Error(ValidationErrorCode.InvalidHttpMethod, $"Request HTTP method is not in the supported list of HTTP methods: {request.Method}");
            }

            if (reqs.MaxUrlLength > 0 && request.Url.Length > reqs.MaxUrlLength)
            {
                issues.Error(ValidationErrorCode.UrlLengthExceedsMaximum, $"Request URL is longer than the defined maximum URL length: [{request.Url.Length}] {request.Url}");
            }

            if (reqs.StandardHeaders != null && request.Headers.Count > 0)
            {
                foreach (var headerName in request.Headers.AllKeys)
                {
                    if (!reqs.StandardHeaders.ContainsString(headerName, apiRequirements.CaseSensativeHeaders))
                    {
                        issues.Warning(ValidationErrorCode.NonStandardHeaderUsed, $"Request includes a non-standard header: {headerName}");
                    }
                }
            }

            return new ValidationResult<bool>(issues.IssuesInCurrentScope == 0);
        }

        public static bool ContainsString(this string[] array, string value, bool caseSenativeComparison = false)
        {
            return array.Any(x => x.Equals(value, caseSenativeComparison ? System.StringComparison.Ordinal : System.StringComparison.OrdinalIgnoreCase));
        }

        public static ValidationResult<bool> IsResponseValid(this HttpResponse response, string sourceFile, ApiRequirements requirements, IssueLogger issues)
        {
            if (null == requirements || null == requirements.HttpResponse)
                return new ValidationResult<bool>(true);
            
            List<ValidationError> errors = new List<ValidationError>();

            var reqs = requirements.HttpResponse;

            var responseMimeType = new MultipartMime.MimeContentType(response.ContentType);
            if (reqs.ContentTypes != null && null != responseMimeType 
                && !reqs.ContentTypes.Contains(responseMimeType.MimeType))
            {
                issues.Warning(ValidationErrorCode.InvalidContentType, $"Response Content-Type header value is not in the supported list of content-types: {response.ContentType}");
            }

            return new ValidationResult<bool>(!errors.Any());
        }

    }
}

