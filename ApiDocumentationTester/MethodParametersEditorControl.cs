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
    public partial class MethodParametersEditorControl : UserControl
    {

        public ScenarioDefinition RequestParameters { get; private set; }
        public DocSet DocumentSet {get; private set;}

        private BindingList<PlaceholderValue> m_ParamValues;

        public MethodParametersEditorControl()
        {
            InitializeComponent();

            comboBoxParamLocation.DataSource = Enum.GetNames(typeof(PlaceholderLocation));
        }

        public void OpenRequestParameters(ScenarioDefinition param, DocSet docset)
        {
            if (DocumentSet != docset)
                DocumentSet = docset;
            if (param != RequestParameters)
            {
                RequestParameters = param;
                LoadControlsWithData();
            }
        }

        private void LoadControlsWithData()
        {
            if (RequestParameters == null)
                return;

            ignoreValueChanges = true;

            comboBoxMethod.DataSource = DocumentSet.Methods;
            comboBoxMethod.DisplayMember = "DisplayName";
            comboBoxMethod.Text = RequestParameters.Method;

            textBoxNotes.Text = RequestParameters.Name;
            checkBoxEnabled.Checked = RequestParameters.Enabled;

            ignoreValueChanges = false;

            m_ParamValues = new BindingList<PlaceholderValue>(RequestParameters.StaticParameters);
            
            listBoxParameters.DisplayMember = "Id";
            listBoxParameters.DataSource = m_ParamValues;

            

            if (RequestParameters.StaticParameters.Count > 0)
            {
                listBoxParameters.SelectedIndex = 0;
            }
            else
            {
                LoadSelectedParameter(new PlaceholderValue());
            }
            
        }

        private PlaceholderValue SelectedParameterValue
        {
            get
            {
                return listBoxParameters.SelectedItem as PlaceholderValue;
            }
        }

        private void listBoxParameters_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedValue = SelectedParameterValue;
            if (null != selectedValue)
            {
                LoadSelectedParameter(selectedValue);
            }
        }

        bool ignoreValueChanges = false;
        private PlaceholderValue m_lastParameterLoaded;
        private void LoadSelectedParameter(PlaceholderValue value)
        {
            if (value == m_lastParameterLoaded)
                return;

            ignoreValueChanges = true;
            textBoxParamName.Text = value.Id;
            comboBoxParamLocation.SelectedItem = value.Location.ToString();
            textBoxParamValue.Text = value.Value.ToString();
            m_lastParameterLoaded = value;
            ignoreValueChanges = false;
        }

        private void UpdateSelectedParameter(PlaceholderValue value)
        {
            if (ignoreValueChanges) return;

            value.Id = textBoxParamName.Text;
            value.Value = textBoxParamValue.Text;

            PlaceholderLocation location;
            if (Enum.TryParse<PlaceholderLocation>(comboBoxParamLocation.Text, out location))
            {
                value.Location = location;
            }
        }

        private async void ShowRequestPreview()
        {
            var method = (from m in DocumentSet.Methods where m.DisplayName == RequestParameters.Method select m).FirstOrDefault();
            if (null == method)
            {
                MessageBox.Show("No matching method definition found.");
                return;
            }

            var requestResult = await method.PreviewRequestAsync(RequestParameters, string.Empty, string.Empty);
            if (requestResult.IsWarningOrError)
            {
                ErrorDisplayForm.ShowErrorDialog(requestResult.Messages);
                return;
            }

            var requestText = requestResult.Value.FullHttpText();

            ErrorDisplayForm form = new ErrorDisplayForm("Request Preview", requestText);
            form.ShowDialog(this);
        }

        private void buttonNewParameter_Click(object sender, EventArgs e)
        {
            var newItem = new PlaceholderValue();
            newItem.Id = "new-property";
            m_ParamValues.Add(newItem);

            listBoxParameters.SelectedItem = newItem;
        }

        private void buttonDeleteParameter_Click(object sender, EventArgs e)
        {
            var selectedValue = SelectedParameterValue;
            if (selectedValue != null)
            {
                m_ParamValues.Remove(selectedValue);
            }
        }

        private void parameterValue_TextChanged(object sender, EventArgs e)
        {
            var selected = SelectedParameterValue;
            if (null != selected)
            {
                UpdateSelectedParameter(SelectedParameterValue);
            }
            else if (RequestParameters != null)
            {
                PlaceholderValue value = new PlaceholderValue();
                UpdateSelectedParameter(value);
                m_ParamValues.Add(value);
            }
        }

        private void buttonPreviewMethod_Click(object sender, EventArgs e)
        {
            ShowRequestPreview();
        }

        private void comboBoxParamLocation_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selected = SelectedParameterValue;
            if (null != selected)
            {
                UpdateSelectedParameter(SelectedParameterValue);
            }
            else if (RequestParameters != null)
            {
                PlaceholderValue value = new PlaceholderValue();
                UpdateSelectedParameter(value);
                m_ParamValues.Add(value);
            }
        }

        private void requestParameterField_TextChanged(object sender, EventArgs e)
        {
            UpdateRequestParameters();
        }

        private void UpdateRequestParameters()
        {
            if (ignoreValueChanges) return;

            RequestParameters.Method = comboBoxMethod.Text;
            RequestParameters.Name = textBoxNotes.Text;
            RequestParameters.Enabled = checkBoxEnabled.Checked;
        }

        private void checkBoxEnabled_CheckedChanged(object sender, EventArgs e)
        {
            UpdateRequestParameters();
        }

        private void comboBoxMethod_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

    }
}
