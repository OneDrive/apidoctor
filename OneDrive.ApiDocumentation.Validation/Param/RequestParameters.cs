namespace OneDrive.ApiDocumentation.Validation.Param
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

    public enum ParameterLocation
    {
        Url,
        Json
    }
}
