using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OneDrive.ApiDocumentation.Validation;

namespace OneDrive.ApiDocumentation.Windows
{
    public partial class ScenarioPlaceholderEditor : UserControl
    {

        public PlaceholderValue Placeholder
        {
            get;
            private set;
        }

        public bool IsRequestValuePlaceholder { get; private set; }

        //private bool m_loading;

        public ScenarioPlaceholderEditor()
        {
            InitializeComponent();

            var locations = Enum.GetNames(typeof(PlaceholderLocation));
            comboBoxLocation.Items.AddRange(locations);

            var targets = Enum.GetNames(typeof(PlaceholderLocation));
            comboBoxPathTarget.Items.AddRange(targets);
        }

        public void LoadPlaceholder(PlaceholderValue value, bool isRequestValue)
        {
            //m_loading = true;
            Placeholder = value;
            IsRequestValuePlaceholder = isRequestValue;

            textBoxName.Text = value.PlaceholderKey;
            comboBoxLocation.SelectedItem = value.Location.ToString();
            
            textBoxValueOrPath.Text = value.Value;
            labelValueOrPath.Text = isRequestValue ? "Path:" : "Value:";

            //if (isRequestValue)
            //{
            //    comboBoxPathTarget.SelectedItem = value.PathTarget.ToString();
            //    comboBoxPathTarget.Enabled = true;
            //    labelTarget.Enabled = true;
            //}
            //else
            //{
            //    labelTarget.Enabled = false;
            //    comboBoxPathTarget.Enabled = false;
            //    comboBoxPathTarget.Text = "Static Value";
            //}

            //m_loading = false;
        }

        private void ScenarioField_TextChanged(object sender, EventArgs e)
        {
            //if (m_loading) return;

            //var v = Placeholder;
            //if (null != v)
            //{
            //    v.PlaceholderKey = textBoxName.Text;
            //    v.Location = (PlaceholderLocation)Enum.Parse(typeof(PlaceholderLocation), comboBoxLocation.Text);
            //    if (IsRequestValuePlaceholder)
            //    {
            //        v.Path = textBoxValueOrPath.Text;
            //        v.PathTarget = (ResponseField)Enum.Parse(typeof(ResponseField), comboBoxPathTarget.Text);
            //        v.Value = null;
            //    }
            //    else
            //    {
            //        v.Path = null;
            //        v.PathTarget = ResponseField.None;
            //        v.Value = textBoxValueOrPath.Text;
            //    }

            //    var evt = PlaceholderChanged;
            //    if (null != evt) evt(this, EventArgs.Empty);
            //}
        }
    }
}
