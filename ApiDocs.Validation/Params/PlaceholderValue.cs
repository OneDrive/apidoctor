namespace ApiDocs.Validation
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

        public string PlaceholderKey 
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

        public PlaceholderLocation Location { get; set; }

        public string Value { get; set; }

        public string DefinedValue { get; set; }

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

}
