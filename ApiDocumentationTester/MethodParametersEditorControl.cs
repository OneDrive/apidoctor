using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OneDrive.ApiDocumentation.Validation.Param;
using OneDrive.ApiDocumentation.Validation;

namespace ApiDocumentationTester
{
    public partial class MethodParametersEditorControl : UserControl
    {

        public RequestParameters RequestParameters { get; private set; }
        public DocSet DocumentSet {get; private set;}

        private BindingList<ParameterValue> m_ParamValues;

        public MethodParametersEditorControl()
        {
            InitializeComponent();

            comboBoxParamLocation.DataSource = Enum.GetNames(typeof(ParameterLocation));
        }

        public void OpenRequestParameters(RequestParameters param, DocSet docset)
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

            textBoxNotes.Text = RequestParameters.Note;
            checkBoxEnabled.Checked = RequestParameters.Enabled;

            ignoreValueChanges = false;

            m_ParamValues = new BindingList<ParameterValue>(RequestParameters.Parameters);
            
            listBoxParameters.DisplayMember = "Id";
            listBoxParameters.DataSource = m_ParamValues;

            

            if (RequestParameters.Parameters.Count > 0)
            {
                listBoxParameters.SelectedIndex = 0;
            }
            else
            {
                LoadSelectedParameter(new ParameterValue());
            }
            
        }

        private ParameterValue SelectedParameterValue
        {
            get
            {
                return listBoxParameters.SelectedItem as ParameterValue;
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
        private ParameterValue m_lastParameterLoaded;
        private void LoadSelectedParameter(ParameterValue value)
        {
            if (value == m_lastParameterLoaded)
                return;

            ignoreValueChanges = true;
            textBoxParamName.Text = value.Id;
            comboBoxParamLocation.SelectedItem = value.Location.ToString();
            textBoxParamValue.Text = value.Value;
            m_lastParameterLoaded = value;
            ignoreValueChanges = false;
        }

        private void UpdateSelectedParameter(ParameterValue value)
        {
            if (ignoreValueChanges) return;

            value.Id = textBoxParamName.Text;
            value.Value = textBoxParamValue.Text;

            ParameterLocation location;
            if (Enum.TryParse<ParameterLocation>(comboBoxParamLocation.Text, out location))
            {
                value.Location = location;
            }
        }

        private void ShowRequestPreview()
        {
            var method = (from m in DocumentSet.Methods where m.DisplayName == RequestParameters.Method select m).FirstOrDefault();
            if (null == method)
            {
                MessageBox.Show("No matching method definition found.");
                return;
            }

            var request = method.PreviewRequest(RequestParameters);
            var requestText = request.FullHttpText();

            ErrorDisplayForm form = new ErrorDisplayForm("Request Preview", requestText);
            form.ShowDialog(this);
        }

        private void buttonNewParameter_Click(object sender, EventArgs e)
        {
            var newItem = new ParameterValue();
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
                ParameterValue value = new ParameterValue();
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
                ParameterValue value = new ParameterValue();
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
            RequestParameters.Note = textBoxNotes.Text;
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
