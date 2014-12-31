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
    public partial class ScenarioEditorControl : UserControl
    {
        private bool ignoreValueChanges = false;
        public ScenarioDefinition RequestParameters { get; private set; }
        public DocSet DocumentSet {get; private set;}

        private BindingList<PlaceholderValue> m_StaticPlaceholders;
        private BindingList<PlaceholderValue> m_DynamicPlaceholders;

        public ScenarioEditorControl()
        {
            InitializeComponent();
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

        private void UpdateTabPageNames()
        {
            tabPageStaticValues.Text = string.Format("Static Values ({0})", m_StaticPlaceholders.Count);
            tabPageRequestValues.Text = string.Format("Requested Values ({0})", m_DynamicPlaceholders.Count);
        }

        private void LoadControlsWithData()
        {
            if (RequestParameters == null)
                return;

            ignoreValueChanges = true;

            comboBoxMethod.DataSource = DocumentSet.Methods;
            comboBoxMethod.DisplayMember = "DisplayName";
            comboBoxMethod.Text = RequestParameters.Method;

            textBoxScenarioName.Text = RequestParameters.Name;
            checkBoxEnabled.Checked = RequestParameters.Enabled;

            m_StaticPlaceholders = new BindingList<PlaceholderValue>(RequestParameters.StaticParameters);
            listBoxStaticPlaceholders.DisplayMember = "PlaceholderText";
            listBoxStaticPlaceholders.DataSource = m_StaticPlaceholders;

            if (RequestParameters.StaticParameters.Count > 0)
                listBoxStaticPlaceholders.SelectedIndex = 0;
            else
                staticPlaceholderEditor.LoadPlaceholder(new PlaceholderValue(), false);


            if (null != RequestParameters.DynamicParameters)
            {
                checkBoxEnableDynamicRequest.Checked = true;
                m_DynamicPlaceholders = new BindingList<PlaceholderValue>(RequestParameters.DynamicParameters.Values);
                textBoxHttpRequest.Text = RequestParameters.DynamicParameters.HttpRequest;
            }
            else
            {
                checkBoxEnableDynamicRequest.Checked = false;
                m_DynamicPlaceholders = new BindingList<PlaceholderValue>();
                textBoxHttpRequest.Text = "";
            }
            checkBoxEnableDynamicRequest_CheckedChanged(checkBoxEnableDynamicRequest, EventArgs.Empty);

            listBoxDynamicPlaceholders.DisplayMember = "PlaceholderText";
            listBoxDynamicPlaceholders.DataSource = m_DynamicPlaceholders;

            if (m_DynamicPlaceholders.Count > 0)
                listBoxDynamicPlaceholders.SelectedIndex = 0;
            else
                dynamicPlaceholderEditor.LoadPlaceholder(new PlaceholderValue(), false);

            UpdateTabPageNames();

            ignoreValueChanges = false;
        }

        private PlaceholderValue SelectedStaticPlaceholder
        {
            get
            {
                return listBoxStaticPlaceholders.SelectedItem as PlaceholderValue;
            }
        }

        private PlaceholderValue SelectedDynamicPlaceholder
        {
            get { return listBoxDynamicPlaceholders.SelectedItem as PlaceholderValue; }
        }

        private void listBoxStaticPlaceholders_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedValue = SelectedStaticPlaceholder;
            if (null != selectedValue)
            {
                staticPlaceholderEditor.LoadPlaceholder(selectedValue, false);
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

            string token = await EnsureAccessTokenAvailable();

            var requestResult = await method.PreviewRequestAsync(RequestParameters, Properties.Settings.Default.ApiBaseRoot, token);
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
            newItem.PlaceholderText = "new-placeholder";
            m_StaticPlaceholders.Add(newItem);

            listBoxStaticPlaceholders.SelectedItem = newItem;
            UpdateTabPageNames();
        }

        private void buttonDeleteParameter_Click(object sender, EventArgs e)
        {
            var selectedValue = SelectedStaticPlaceholder;
            if (selectedValue != null)
            {
                m_StaticPlaceholders.Remove(selectedValue);
            }
            UpdateTabPageNames();
        }

        private void buttonPreviewMethod_Click(object sender, EventArgs e)
        {
            ShowRequestPreview();
        }

        private async Task<string> EnsureAccessTokenAvailable()
        {
            var form = (MainForm)ParentForm;
            if (string.IsNullOrEmpty(form.AccessToken))
            {
                await form.SignInAsync();
                if (string.IsNullOrEmpty(form.AccessToken))
                {
                    return null;
                }
            }
            return form.AccessToken;
        }

        private void requestParameterField_TextChanged(object sender, EventArgs e)
        {
            UpdateRequestParameters();
        }

        private void UpdateRequestParameters()
        {
            if (ignoreValueChanges) return;

            RequestParameters.Method = comboBoxMethod.Text;
            RequestParameters.Name = textBoxScenarioName.Text;
            RequestParameters.Enabled = checkBoxEnabled.Checked;

            if (checkBoxEnableDynamicRequest.Checked)
            {
                if (RequestParameters.DynamicParameters == null)
                    RequestParameters.DynamicParameters = new PlaceholderRequest();
                var request = RequestParameters.DynamicParameters;
                request.HttpRequest = textBoxHttpRequest.Text;
                request.Values = m_DynamicPlaceholders.ToList();
            }
            else
            {
                RequestParameters.DynamicParameters = null;
            }
        }

        private void checkBoxEnabled_CheckedChanged(object sender, EventArgs e)
        {
            UpdateRequestParameters();
        }

        private void listBoxDynamicPlaceholders_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedValue = SelectedDynamicPlaceholder;
            if (null != selectedValue)
            {
                dynamicPlaceholderEditor.LoadPlaceholder(selectedValue, true);
            }

        }

        private void buttonNewDynmaicPlaceholder_Click(object sender, EventArgs e)
        {
            var newItem = new PlaceholderValue();
            newItem.PlaceholderText = "new-placeholder";
            m_DynamicPlaceholders.Add(newItem);

            listBoxDynamicPlaceholders.SelectedItem = newItem;
            UpdateTabPageNames();
        }

        private void buttonDeleteDynamicPlaceholder_Click(object sender, EventArgs e)
        {
            var selectedValue = SelectedDynamicPlaceholder;
            if (selectedValue != null)
            {
                m_DynamicPlaceholders.Remove(selectedValue);
            }
            UpdateTabPageNames();
        }

        private void checkBoxEnableDynamicRequest_CheckedChanged(object sender, EventArgs e)
        {
            bool enabled = checkBoxEnableDynamicRequest.Checked;
            textBoxHttpRequest.Enabled = enabled;
            listBoxDynamicPlaceholders.Enabled = enabled;
            buttonNewDynmaicPlaceholder.Enabled = enabled;
            buttonDeleteDynamicPlaceholder.Enabled = enabled;
            dynamicPlaceholderEditor.Enabled = enabled;

            UpdateRequestParameters();
        }

        private void dynamicPlaceholderEditor_PlaceholderChanged(object sender, EventArgs e)
        {
            UpdateRequestParameters();
        }

    }
}
