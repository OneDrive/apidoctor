using Newtonsoft.Json.Linq;
using OneDrive.ApiDocumentation.Validation.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.Validation
{
    internal static class InternalScenarioExtensionMethods
    {

        /// <summary>
        /// Verify that the expectations in the scenario are met by the response
        /// </summary>
        /// <param name="scenario"></param>
        /// <param name="actualResponse"></param>
        /// <param name="detectedErrors"></param>
        public static void ValidateExpectations(this ScenarioDefinition scenario, Http.HttpResponse actualResponse, List<ValidationError> detectedErrors)
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
                        ExpectationSatisfied(key, actualResponse.Headers[keyIndex].FirstOrDefault(), expectedValues, detectedErrors);
                        break;

                    case PlaceholderLocation.Json:
                        object value = Json.JsonPath.ValueFromJsonPath(actualResponse.Body, keyIndex);
                        ExpectationSatisfied(key, value, expectedValues, detectedErrors);
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


            if (expectedValues is IList<JToken>)
            {
                foreach (JToken possibleValue in (IList<JToken>)expectedValues)
                {
                    if (JsonPath.TokenEquals(possibleValue, actualValue))
                        return true;
                }
            }
            else if (expectedValues is JToken)
            {
                if (JsonPath.TokenEquals((JToken)expectedValues, actualValue))
                    return true;
            }
            else if (null != expectedValues && actualValue.Equals(expectedValues))
            {
                return true;
            }

            detectedErrors.Add(new ValidationError(ValidationErrorCode.ExpectationConditionFailed, null, "Expectation {0} = {1} failed. Actual value: {2}", key, expectedValues, actualValue));
            return false;
        }
    }
}
