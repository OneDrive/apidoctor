

namespace OneDrive.ApiDocumentation.Validation
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class BasicRequestDefinition
    {
        public BasicRequestDefinition()
        {
        }

        /// <summary>
        /// Raw Http Request that is invoked instead of using a method from the documentation
        /// </summary>
        [JsonProperty("http-request", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string RawHttpRequest { get; set; }

        /// <summary>
        /// Name of the method that should be invoked for this call (instead of an http request)
        /// </summary>
        [JsonProperty("method", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string MethodName { get; set; }

        [JsonProperty("request-parameters", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, string> RequestParameters { get; set; }

        /// <summary>
        /// A set of status code results that are included as "success" for this operation.
        /// </summary>
        [JsonProperty("allowed-status-codes", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<int> AllowedStatusCodes { get; set; }


        public virtual ValidationError[] CheckForErrors()
        {
            List<ValidationError> errors = new List<ValidationError>();

            if (!string.IsNullOrEmpty(RawHttpRequest) && !string.IsNullOrEmpty(MethodName))
                errors.Add(new ValidationError(ValidationErrorCode.HttpRequestAndMethodSpecified, null, "http-request and method are mutually exclusive: http-request: {0}, method: {1}", RawHttpRequest, MethodName));

            foreach (var key in RequestParameters.Keys)
            {
                var keyType = LocationForKey(key);
                switch (keyType)
                {
                    case PlaceholderLocation.Invalid:
                    case PlaceholderLocation.StoredValue:
                        errors.Add(new ValidationError(ValidationErrorCode.HttpRequestParameterInvalid, null, "request-parameters key {0} is invalid. KeyType was {1}", key, keyType));
                        break;
                }
            }

            return errors.ToArray();
        }

        public static PlaceholderLocation LocationForKey(string key)
        {
            if (key.StartsWith("{") && key.EndsWith("}") && key.Length > 2)
                return PlaceholderLocation.Url;
            if (key.StartsWith("[") && key.EndsWith("]") && key.Length > 2)
                return PlaceholderLocation.StoredValue;
            if (key == "!body")
                return PlaceholderLocation.Body;
            if (key == "!url")
                return PlaceholderLocation.Url;
            if (key.EndsWith(":") && key.Length > 1)
                return PlaceholderLocation.HttpHeader;
            if (key.StartsWith("$"))
                return PlaceholderLocation.Json;

            return PlaceholderLocation.Invalid;
        }
    }
}
