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

        public event EventHandler ScenarioChanged;

        public ScenarioDefinition Scenario { get; private set; }
        public DocSet DocumentSet {get; set;}

        private BindingList<PlaceholderValue> m_StaticPlaceholders;
        private BindingList<PlaceholderValue> m_DynamicPlaceholders;

        public ScenarioEditorControl()
        {
            InitializeComponent();
        }

        public void LoadScenario(ScenarioDefinition param, DocSet docset)
        {
            if (DocumentSet != docset)
                DocumentSet = docset;

            if (param != Scenario)
            {
                Scenario = param;
                LoadControlsWithData();
            }
        }

        public ScenarioDefinition CreateNewScenario(MethodDefinition method, DocSet docset)
        {
            if (DocumentSet != docset)
                DocumentSet = docset;

            Scenario = new ScenarioDefinition { Method = method.DisplayName };
            LoadControlsWithData();

            return Scenario;
        }

        public void Clear()
        {
            Scenario = new ScenarioDefinition();
            LoadControlsWithData();
        }


        private void UpdateTabPageNames()
        {
            tabPageStaticValues.Text = string.Format("Static Values ({0})", m_StaticPlaceholders.Count);
            tabPageRequestValues.Text = string.Format("Requested Values ({0})", m_DynamicPlaceholders.Count);
        }

        private void LoadControlsWithData()
        {
            if (Scenario == null)
                return;

            ignoreValueChanges = true;

            comboBoxMethod.DataSource = DocumentSet.Methods;
            comboBoxMethod.DisplayMember = "DisplayName";
            comboBoxMethod.Text = Scenario.Method;

            textBoxScenarioName.Text = Scenario.Name;
            checkBoxEnabled.Checked = Scenario.Enabled;

            m_StaticPlaceholders = new BindingList<PlaceholderValue>(Scenario.StaticParameters);
            listBoxStaticPlaceholders.DisplayMember = "PlaceholderText";
            listBoxStaticPlaceholders.DataSource = m_StaticPlaceholders;

            if (Scenario.StaticParameters.Count > 0)
                listBoxStaticPlaceholders.SelectedIndex = 0;
            else
                staticPlaceholderEditor.LoadPlaceholder(new PlaceholderValue(), false);


            if (null != Scenario.DynamicParameters)
            {
                checkBoxEnableDynamicRequest.Checked = true;
                m_DynamicPlaceholders = new BindingList<PlaceholderValue>(Scenario.DynamicParameters.Values);
                textBoxHttpRequest.Text = Scenario.DynamicParameters.HttpRequest;
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
            {
                listBoxDynamicPlaceholders.SelectedIndex = 0;
                tabControlPlaceholders.SelectedIndex = 1;
            }
            else
            {
                tabControlPlaceholders.SelectedIndex = 0;
                dynamicPlaceholderEditor.LoadPlaceholder(new PlaceholderValue(), false);
            }

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
            var method = (from m in DocumentSet.Methods where m.DisplayName == Scenario.Method select m).FirstOrDefault();
            if (null == method)
            {
                MessageBox.Show("No matching method definition found.");
                return;
            }

            string token = await EnsureAccessTokenAvailable();
            if (null == token) return;

            var requestResult = await method.PreviewRequestAsync(Scenario, Properties.Settings.Default.ApiBaseRoot, token);
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
            var form = MainForm.MostRecentInstance;
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

            Scenario.Method = comboBoxMethod.Text;
            Scenario.Name = textBoxScenarioName.Text;
            Scenario.Enabled = checkBoxEnabled.Checked;

            if (checkBoxEnableDynamicRequest.Checked)
            {
                if (Scenario.DynamicParameters == null)
                    Scenario.DynamicParameters = new PlaceholderRequest();
                var request = Scenario.DynamicParameters;
                request.HttpRequest = textBoxHttpRequest.Text;
                request.Values = m_DynamicPlaceholders.ToList();
            }
            else
            {
                Scenario.DynamicParameters = null;
            }
            RaiseScenarioChanged();

            listBoxDynamicPlaceholders.DataSource = listBoxDynamicPlaceholders.DataSource;
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

        private void comboBoxMethod_SelectedIndexChanged(object sender, EventArgs e)
        {
            MethodDefinition selectedMethod = comboBoxMethod.SelectedItem as MethodDefinition;
            if (null == selectedMethod)
            {
                textBoxRequestPreview.Clear();
                return;
            }

            textBoxRequestPreview.Text = selectedMethod.Request;
        }

        private void RaiseScenarioChanged()
        {
            var evt = ScenarioChanged;
            if (null != evt)
            {
                evt(this, EventArgs.Empty);
            }
        }


    }
}
