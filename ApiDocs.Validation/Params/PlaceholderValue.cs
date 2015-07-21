namespace ApiDocs.Validation.Params
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class PlaceholderValue : INotifyPropertyChanged
    {
        private string placeholderText;

        public string PlaceholderKey 
        {
            get { return this.placeholderText; }
            set
            {
                if (value != this.placeholderText)
                {
                    this.placeholderText = value;
                    this.RaisePropertyChanged();
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
            var evt = this.PropertyChanged;
            if (null != evt)
            {
                evt(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        
    }

}
