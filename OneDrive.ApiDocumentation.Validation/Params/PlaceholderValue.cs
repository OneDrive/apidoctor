namespace OneDrive.ApiDocumentation.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class PlaceholderValue : INotifyPropertyChanged
    {
        private string _placeholderText;

        public PlaceholderValue()
        {

        }

        [JsonProperty("placeholder")]
        public string PlaceholderText 
        {
            get { return _placeholderText; }
            set
            {
                if (value != _placeholderText)
                {
                    _placeholderText = value;
                    RaisePropertyChanged();
                }
            }
        }

        [JsonProperty("location"), JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public PlaceholderLocation Location { get; set; }

        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public string Value { get; set; }

        [JsonProperty("path", NullValueHandling = NullValueHandling.Ignore)]
        public string Path { get; set; }

        [JsonProperty("target", DefaultValueHandling=DefaultValueHandling.Ignore), JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public ResponseField PathTarget { get; set; }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            var evt = PropertyChanged;
            if (null != evt)
            {
                evt(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        
    }

    public enum ResponseField
    {
        None,
        Json,
        Header
    }
}
