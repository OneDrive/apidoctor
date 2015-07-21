namespace ApiDocs.Validation.Params
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using ApiDocs.Validation.Http;
    using ApiDocs.Validation.Json;

    public static class RequestDefinitionExtensions
    {
        /// <summary>
        /// Locate the Http.HttpRequest instance for this request definition either by 
        /// parsing the RawHttpRequest or resolving MethodName into a request.
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="baseUrl"></param>
        /// <param name="documents"></param>
        /// <returns></returns>
        public static HttpRequest GetHttpRequest(this BasicRequestDefinition definition, string baseUrl, DocSet documents)
        {
            HttpRequest foundRequest = null;
            if (!string.IsNullOrEmpty(definition.RawHttpRequest))
            {
                foundRequest = ParseHttpRequest(definition.RawHttpRequest);
            }
            else if (!string.IsNullOrEmpty(definition.MethodName))
            {
                foundRequest = LookupHttpRequestForMethod(definition.MethodName, baseUrl, documents);
            }

            return foundRequest;
        }

        private static HttpRequest LookupHttpRequestForMethod(string methodName, string baseUrl, DocSet docset)
        {
            var queryForMethod = from m in docset.Methods
                                 where m.Identifier == methodName
                                 select m;

            var foundMethod = queryForMethod.FirstOrDefault();
            if (null == foundMethod)
            {
                throw new Exception(string.Format("Failed to locate method {0} in the docset.", methodName));
            }

            return ParseHttpRequest(foundMethod.Request);
        }

        private static HttpRequest ParseHttpRequest(string rawHttpRequest)
        {
            HttpParser parser = new HttpParser();
            HttpRequest request = parser.ParseHttpRequest(rawHttpRequest);
            return request;
        }


        /// <summary>
        /// Updates an HttpRequest with values from the request-parameters dictionary
        /// </summary>
        /// <param name="request"></param>
        /// <param name="placeholderValues"></param>
        public static void RewriteRequestWithParameters(this HttpRequest request, IEnumerable<PlaceholderValue> placeholderValues)
        {
            // URL
            request.Url = MethodDefinition.RewriteUrlWithParameters(request.Url, from pv in placeholderValues where pv.Location == PlaceholderLocation.Url select pv);
            
            // Headers
            MethodDefinition.RewriteHeadersWithParameters(request, from pv in placeholderValues where pv.Location == PlaceholderLocation.HttpHeader select pv);

            // Body
            var bodyParam = (from pv in placeholderValues where pv.Location == PlaceholderLocation.Body select pv).SingleOrDefault();
            if (bodyParam != null)
                request.Body = bodyParam.Value;

            // Json
            var jsonParams = from pv in placeholderValues where pv.Location == PlaceholderLocation.Json select pv;
            if (jsonParams.Any() && request.IsMatchingContentType("application/json"))
            {
                request.Body = MethodDefinition.RewriteJsonBodyWithParameters(request.Body, jsonParams);
            }
        }

        public static PlaceholderValue[] ToPlaceholderValuesArray(this Dictionary<string, string> parameters, Dictionary<string, string> storedValues)
        {
            if (parameters == null)
                return new PlaceholderValue[0];

            var placeholderValues = from key in parameters.Keys
                                    select ConvertToPlaceholderValue(key, parameters[key], storedValues);
            return placeholderValues.ToArray();
        }


        /// <summary>
        /// Convert the input into a PlaceholderValue and realize any reference to a stored value into
        /// the actual value at the given time.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="storedValues"></param>
        /// <returns></returns>
        public static PlaceholderValue ConvertToPlaceholderValue(string key, string value, Dictionary<string, string> storedValues)
        {
            PlaceholderValue v = new PlaceholderValue
            {
                PlaceholderKey = key,
                DefinedValue = value,
                Location = BasicRequestDefinition.LocationForKey(key)
            };

            v.Value = BasicRequestDefinition.LocationForKey(value) == PlaceholderLocation.StoredValue ? storedValues[value] : value;

            Debug.WriteLine("Converting \"{0}: {1}\" into loc={2},value={3}", key, value, v.Location, v.Value);

            return v;
        }

        public static bool MatchesContentTypeIdentifier(this string contentType, string expectedContentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            string[] contentTypeParts = contentType.Split(new char[] { ';' });
            if (contentTypeParts.Length > 0)
            {
                string contentTypeId = contentTypeParts[0].Trim();
                return contentTypeId.Equals(expectedContentType, StringComparison.OrdinalIgnoreCase);
            }
            
            return false;
        }

        public static string ValueForKeyedIdentifier(this HttpResponse response, string key)
        {
            var keyType = BasicRequestDefinition.LocationForKey(key);
            switch (keyType)
            {
                case PlaceholderLocation.Body:
                    return response.Body;
                case PlaceholderLocation.HttpHeader:
                    string headerTrueName = key.Substring(0, key.Length - 1);
                    if (response.Headers.AllKeys.Contains(headerTrueName))
                    {
                        return response.Headers[headerTrueName];
                    }
                    return null;
                case PlaceholderLocation.Json:
                    if (!response.ContentType.MatchesContentTypeIdentifier(MethodDefinition.MimeTypeJson))
                    {
                        throw new NotSupportedException(string.Format("Cannot read JPath property from response with content-type: {0}", response.ContentType));
                    }

                    return JsonPath.ValueFromJsonPath(response.Body, key).ToString();
                default:
                    throw new NotSupportedException(string.Format("Unsupported location for keyed identifier {0}: {1}", key, keyType));
            }
        }

    }
}
