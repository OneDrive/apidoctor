﻿/*
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

namespace ApiDoctor.Validation.Params
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using ApiDoctor.Validation.Error;
    using ApiDoctor.Validation.Http;
    using ApiDoctor.Validation.Json;
    using Newtonsoft.Json;

    public static class RequestDefinitionExtensions
    {
        /// <summary>
        /// Locate the Http.HttpRequest instance for this request definition either by 
        /// parsing the RawHttpRequest or resolving MethodName into a request.
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="documents"></param>
        /// <returns></returns>
        public static HttpRequest GetHttpRequest(this BasicRequestDefinition definition, DocSet documents, IServiceAccount account, IssueLogger issues)
        {
            HttpRequest foundRequest = null;
            if (!string.IsNullOrEmpty(definition.RawHttpRequest))
            {
                foundRequest = ParseHttpRequest(definition.RawHttpRequest, issues);
            }
            else if (!string.IsNullOrEmpty(definition.MethodName))
            {
                foundRequest = LookupHttpRequestForMethod(definition.MethodName, documents, account, issues);
            }
            else if (!string.IsNullOrEmpty(definition.CannedRequestName))
            {
                foundRequest = HttpRequestForCannedRequest(definition.CannedRequestName, documents, account, issues);
            }

            return foundRequest;
        }

        private static HttpRequest LookupHttpRequestForMethod(string methodName, DocSet docset, IServiceAccount account, IssueLogger issues)
        {
            var queryForMethod = from m in docset.Methods
                                 where m.Identifier == methodName
                                 select m;

            var foundMethod = queryForMethod.FirstOrDefault();
            if (null == foundMethod)
            {
                throw new Exception(string.Format("Failed to locate method {0} in the docset.", methodName));
            }

            var request = ParseHttpRequest(foundMethod.Request, issues);
            foundMethod.ModifyRequestForAccount(request, account);

            return request;
        }

        private static HttpRequest HttpRequestForCannedRequest(string requestName, DocSet docset, IServiceAccount account, IssueLogger issues)
        {
            var queryForRequest = from m in docset.CannedRequests
                where m.Name == requestName
                select m;
            var foundRequest = queryForRequest.FirstOrDefault();
            if (null == foundRequest)
            {
                throw new Exception(string.Format("Failed to find canned response {0} in the docset.", requestName));
            }

            return GetHttpRequest(foundRequest, docset, account, issues);

        }

        private static HttpRequest ParseHttpRequest(string rawHttpRequest, IssueLogger issues)
        {
            HttpRequest request;
            HttpParser.TryParseHttpRequest(rawHttpRequest, out request, issues);
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
            var bodyParam = (from pv in placeholderValues
                where pv.Location == PlaceholderLocation.Body || pv.Location == PlaceholderLocation.BodyBase64Encoded
                select pv).SingleOrDefault();
            if (bodyParam != null)
            {
                if (bodyParam.Location == PlaceholderLocation.BodyBase64Encoded)
                {
                    var bytes = Convert.FromBase64String(bodyParam.Value);
                    request.BodyBytes = bytes;
                    request.Body = null;
                }
                else
                {
                    request.Body = bodyParam.Value;
                    request.BodyBytes = null;
                }
            }

            // Json
            var jsonParams = from pv in placeholderValues where pv.Location == PlaceholderLocation.Json select pv;
            if (jsonParams.Any() && request.IsMatchingContentType("application/json"))
            {
                request.Body = MethodDefinition.RewriteJsonBodyWithParameters(request.Body, jsonParams);
            }
        }

        /// <summary>
        /// Apply any namespace translations defined in the account to the parameters in the request body
        /// </summary>
        /// <param name="request"></param>
        /// <param name="account"></param>
        public static void RewriteRequestBodyNamespaces(this HttpRequest request, IServiceAccount account, IssueLogger issues)
        {
            if (account.Transformations?.Request?.Properties == null)
                return;

            if (request.IsMatchingContentType("application/json"))
            {
                var translatedBody = JsonRewriter.RewriteJsonProperties(request.Body, account.Transformations.Request.Properties, issues);
                request.Body = translatedBody;
            }
            else if (request.IsMatchingContentType("multipart/related"))
            {
                var parsedBody = new MultipartMime.MultipartMimeContent(request.ContentType, request.Body);
                var partsToRewrite = parsedBody.PartsWithContentType("application/json");
                foreach(var part in partsToRewrite)
                {
                    part.Body = JsonRewriter.RewriteJsonProperties(part.Body, account.Transformations.Request.Properties, issues);
                }
                request.Body = parsedBody.ToString();
            }
        }

        public static void RewriteResponseBodyNamespaces(this HttpResponse response, IServiceAccount account, IssueLogger issues)
        {
            if (account.Transformations?.Response?.Properties == null)
                return;
            if (!response.IsMatchingContentType("application/json"))
                return;
            if (response.Body.Length == 0)
                return;

            var translatedBody = JsonRewriter.RewriteJsonProperties(response.Body, account.Transformations.Response.Properties, issues);
            response.Body = translatedBody;
        }

        public static Dictionary<string, string> RemoveKnownEntities(this Dictionary<string, string> namespaceMap)
        {
            var knownKeys = new string[] { "!action" };
            var output = new Dictionary<string, string>(namespaceMap);
            foreach(var key in knownKeys)
            {
                output.Remove(key);
            }
            return output;
        }

        public static PlaceholderValue[] ToPlaceholderValuesArray(this Dictionary<string, string> parameters, Dictionary<string, string> storedValues)
        {
            if (parameters == null)
                return new PlaceholderValue[0];

            var placeholderValues = from key in parameters.Keys
                                    select ConvertToPlaceholderValue(key, parameters[key], storedValues);
            return placeholderValues.ToArray();
        }

        private const string RandomFilenameValuePrefix = "!random-filename";

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
            string replacementValue = value;
            switch(BasicRequestDefinition.LocationForKey(value))
            {
                case PlaceholderLocation.StoredValue:
                    if (!storedValues.TryGetValue(value, out replacementValue))
                    {
                        throw new PlaceholderValueNotFoundException($"Unable to locate the placeholder value {value} in available values: {storedValues.Keys.ComponentsJoinedByString(",")}");
                    }
                    break;
                case PlaceholderLocation.CSharpCode:
                    replacementValue = CSharpEval.Evaluate(value.Substring(1), storedValues);
                    break;
            }

            PlaceholderValue v = new PlaceholderValue
            {
                PlaceholderKey = key,
                DefinedValue = value,
                Location = BasicRequestDefinition.LocationForKey(key),
                Value = replacementValue
            };

            // Allow the random-filename generator to swap the value with a randomly generated filename.
            int index = value == null ? -1 : value.IndexOf(RandomFilenameValuePrefix);
            if (index >= 0)
            {
                int endIndex = value.IndexOf("!", index + 1);
                string placeholder = null;
                if (endIndex > -1)
                {
                    placeholder = value.Substring(index, endIndex - index + 1);
                }
                else
                {
                    // Be backwards comaptible with previous behavior.
                    placeholder = value.Substring(index);
                    if (!placeholder.EndsWith("!"))
                    {
                        if (placeholder != RandomFilenameValuePrefix)
                            value = "/" + value.Replace(placeholder, placeholder + "!");
                        else
                            value = value.Replace(placeholder, placeholder + "!");
                        placeholder = placeholder + "!";
                    }
                }
                string randomFilename = GenerateRandomFilename(placeholder);
                v.Value = value.Replace(placeholder, randomFilename);
            }

            Debug.WriteLine("Converting \"{0}: {1}\" into loc={2},value={3}", key, value, v.Location, v.Value);

            return v;
        }

        private static string GenerateRandomFilename(string placeholder)
        {
            if (placeholder == (RandomFilenameValuePrefix + "!"))
                return string.Format("apidoctor-{0:D}", Guid.NewGuid());

            string extension = placeholder.Substring(
                RandomFilenameValuePrefix.Length+1,
                placeholder.Length - RandomFilenameValuePrefix.Length - 2);
            return string.Format("apidoctor-{0:D}.{1}", Guid.NewGuid(), extension);
        }

        public static bool MatchesContentTypeIdentifier(this string contentType, string expectedContentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            string[] contentTypeParts = contentType.Split(';');
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
