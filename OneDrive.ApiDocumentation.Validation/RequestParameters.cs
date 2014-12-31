namespace OneDrive.ApiDocumentation.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using System.ComponentModel;

    public class RequestParameters : INotifyPropertyChanged
    {
        private string m_name;

        public RequestParameters()
        {
            this.Parameters = new List<ParameterValue>();
        }
            
        [JsonProperty("method")]
        public string Method 
        {
            get { return m_name; }
            set
            {
                m_name = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("note")]
        public string Note { get; set; }

        [JsonProperty("parameters")]
        public List<ParameterValue> Parameters { get; set; }

        public static RequestParameters[] ReadFromJson(string json)
        {
            var results = JsonConvert.DeserializeObject<List<RequestParameters>>(json);
            return results.ToArray();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            var evt = this.PropertyChanged;
            if (null != evt)
            {
                evt(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion


    }

    public class ParameterValue : INotifyPropertyChanged
    {
        private string m_id;
        private string m_value;
        private ParameterLocation m_location;

        [JsonProperty("name")]
        public string Id 
        {
            get { return m_id; }
            set
            {
                m_id = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty("value")]
        public string Value 
        {
            get { return m_value; }
            set
            {
                m_value = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty("location"), JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public ParameterLocation Location 
        {
            get { return m_location; }
            set
            {
                m_location = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty("source")]
        public ValueSourceRequest ValueSource
        {
            get;
            set;
        }

        public async Task<bool> ReadValueFromSourceAsync(string baseUrl, string accessToken)
        {
            if (null == ValueSource && null != Value)
                return true;
            else if (null == ValueSource)
                return false;

            // Make a request based on ValueSource and load the value from the response
            Http.HttpParser parser = new Http.HttpParser();
            var request = parser.ParseHttpRequest(ValueSource.HttpRequest);
            request.Authorization = "Bearer " + accessToken;
            var webRequest = request.PrepareHttpWebRequest(baseUrl);

            try
            {
                var response = await Http.HttpResponse.ResponseFromHttpWebResponseAsync(webRequest);
                if (response.WasSuccessful)
                {
                    if (response.ContentType.StartsWith("application/json"))
                    {
                        Value = Json.JsonPath.ValueFromJsonPath(response.Body, ValueSource.Query).ToString();
                        return true;
                    }
                    else
                    {
                        // TODO: Handle any other relevent formats
                        return false;
                    }
                }

            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while reading value from source: " + ex.Message);
                return false;
            }

            return false;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            var evt = this.PropertyChanged;
            if (null != evt)
            {
                evt(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }

    public class ValueSourceRequest
    {
        [JsonProperty("request")]
        public string HttpRequest { get; set; }

        /// <summary>
        /// Query to find the requested value in the response object. For JSON responses
        /// the query should use the JSONpath language: http://goessner.net/articles/JsonPath/
        /// </summary>
        /// <value>The value path.</value>
        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("useAuth")]
        public bool UsesAuth { get; set; }
    }

    public enum ParameterLocation
    {
        Url,
        Json,
        Body
    }
}
