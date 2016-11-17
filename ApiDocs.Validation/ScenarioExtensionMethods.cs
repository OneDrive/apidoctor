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

namespace ApiDocs.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ApiDocs.Validation.Error;
    using ApiDocs.Validation.Http;
    using ApiDocs.Validation.Json;
    using ApiDocs.Validation.Params;
    using Newtonsoft.Json.Linq;

    internal static class InternalScenarioExtensionMethods
    {

        /// <summary>
        /// Verify that the expectations in the scenario are met by the response
        /// </summary>
        /// <param name="scenario"></param>
        /// <param name="actualResponse"></param>
        /// <param name="detectedErrors"></param>
        public static void ValidateExpectations(this ScenarioDefinition scenario, HttpResponse actualResponse, List<ValidationError> detectedErrors)
        {
            if (scenario == null) throw new ArgumentNullException("scenario");
            if (actualResponse == null) throw new ArgumentNullException("actualResponse");
            if (detectedErrors == null) throw new ArgumentNullException("detectedErrors");

            var expectations = scenario.Expectations;
            if (null == expectations || expectations.Count == 0)
                return;

            foreach (string key in expectations.Keys)
            {
                string keyIndex;
                var type = BasicRequestDefinition.LocationForKey(key, out keyIndex);
                object expectedValues = expectations[key];
                switch (type)
                {
                    case PlaceholderLocation.Body:
                        ExpectationSatisfied(key, actualResponse.Body, expectedValues, detectedErrors);
                        break;

                    case PlaceholderLocation.HttpHeader:
                        ExpectationSatisfied(key, actualResponse.Headers[keyIndex], expectedValues, detectedErrors);
                        break;

                    case PlaceholderLocation.Json:
                        try
                        {
                            object value = JsonPath.ValueFromJsonPath(actualResponse.Body, keyIndex);
                            ExpectationSatisfied(key, value, expectedValues, detectedErrors);
                        }
                        catch (Exception ex)
                        {
                            detectedErrors.Add(new ValidationError(ValidationErrorCode.JsonParserException, null, ex.Message));
                        }
                        break;

                    case PlaceholderLocation.Invalid:
                    case PlaceholderLocation.StoredValue:
                    case PlaceholderLocation.Url:
                        detectedErrors.Add(new ValidationWarning(ValidationErrorCode.InvalidExpectationKey, null, "The expectation key {0} is invalid. Supported types are Body, HttpHeader, and JsonPath.", key));
                        break;
                }
            }
        }

        /// <summary>
        /// Check to see if an expectation is met.
        /// </summary>
        /// <param name="key">The name of the expectation being checked.</param>
        /// <param name="actualValue">The value for the expectation to check.</param>
        /// <param name="expectedValues">Can either be a single value or an array of values that are considered valid.</param>
        /// <param name="detectedErrors">A collection of validation errors that will be added to when errors are found.</param>
        /// <returns></returns>
        private static bool ExpectationSatisfied(string key, object actualValue, object expectedValues, List<ValidationError> detectedErrors)
        {
            if (null == key) throw new ArgumentNullException("key");
            if (null == detectedErrors) throw new ArgumentNullException("detectedErrors");

            if (actualValue == null && expectedValues != null)
            {
                detectedErrors.Add(new ValidationError(ValidationErrorCode.ExpectationConditionFailed, null, "Expectation {0}={1} failed. Actual value was null and a value was expected.", key, expectedValues));
                return false;
            }

            if (expectedValues == null)
            {
                return true;
            }


            // Possible states
            //   1. expectedValues is an Array, but actualValue is a value type. ==> actualValue must match a value the expected values array
            //   2. expectedValues is an Array, and actualValue is an array ==> Arrays must match
            //   3. expectedValues is a value, and actualValue is a value. ==> Values must match
            //   4. expectedValues is a value, and actualValue is an array ==> Error


            var actualValueArray = actualValue as IList<JToken>;
            var expectedValueArray = expectedValues as IList<JToken>;

            if (null != actualValueArray && null != expectedValueArray)
            {
                // All items must match
                if (actualValueArray.Count == expectedValueArray.Count)
                {
                    var sortedActualValue = actualValueArray.ToList();
                    sortedActualValue.Sort();
                    var sortedExpectedValue = expectedValueArray.ToList();
                    sortedExpectedValue.Sort();

                    bool foundMismatch = false;
                    for(int i=0; i<sortedActualValue.Count; i++)
                    {
                        if (!sortedActualValue[i].Equals(sortedExpectedValue[i]))
                        {
                            foundMismatch = true;
                            break;
                        }
                    }
                    if (!foundMismatch)
                        return true;
                }
            }
            else if (null != actualValueArray)
            {
                // Error state, because we're comparing an array to a single value.
            }
            else if (null != expectedValueArray)
            {
                // actualValue is a single value, which must exist in expectedValueArray
                if (expectedValueArray.Any(possibleVAlue => JsonPath.TokenEquals(possibleVAlue, actualValue)))
                {
                    return true;
                }
            }
            else
            {
                var token = expectedValues as JToken;
                if (token != null)
                {
                    if (JsonPath.TokenEquals(token, actualValue))
                    {
                        return true;
                    }
                }
                else if (actualValue.Equals(expectedValues))
                {
                    return true;
                }
            }

            detectedErrors.Add(new ValidationError(ValidationErrorCode.ExpectationConditionFailed, null, "Expectation {0} = {1} failed. Actual value: {2}", key, expectedValues, actualValue));
            return false;
        }
    }
}
