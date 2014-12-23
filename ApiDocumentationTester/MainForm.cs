using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OneDrive.ApiDocumentation.Validation;
using OneDrive.ApiDocumentation.Validation.Json;
using OneDrive.ApiDocumentation.Validation.Http;
using OneDrive.ApiDocumentation.Validation.Param;

namespace OneDrive.ApiDocumentation.Windows
{
    public partial class MainForm : Form
    {
        DocSet CurrentDocSet { get; set; }
        string m_AccessToken = null;
        BindingList<RequestParameters> m_Parameters = new BindingList<RequestParameters>();

        public MainForm()
        {
            InitializeComponent();

            textBoxBaseURL.Text = Properties.Settings.Default.ApiBaseRoot;
            textBoxClientId.Text = Properties.Settings.Default.ClientId;
            textBoxAuthScopes.Text = Properties.Settings.Default.AuthScopes;
            textBoxMethodRequestParameterFile.Text = Properties.Settings.Default.RequestParametersFile;
        }

        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog { ShowNewFolderButton = false };
            var result = dialog.ShowDialog();
            if (DialogResult.OK == result)
            {
                Properties.Settings.Default.LastOpenedPath = dialog.SelectedPath;
                Properties.Settings.Default.Save();
                OpenFolder(dialog.SelectedPath);
            }
        }

        private void OpenFolder(string path)
        {
            CurrentDocSet = new DocSet(path);

            listBoxDocuments.DisplayMember = "DisplayName";
            listBoxDocuments.DataSource = CurrentDocSet.Files;

            CurrentDocSet.ScanDocumentation();
            CurrentDocSet.TryReadRequestParameters(textBoxMethodRequestParameterFile.Text);

            listBoxResources.DisplayMember = "ResourceType";
            listBoxResources.DataSource = CurrentDocSet.Resources;

            listBoxMethods.DisplayMember = "DisplayName";
            listBoxMethods.DataSource = CurrentDocSet.Methods;


            m_Parameters = new BindingList<RequestParameters>(CurrentDocSet.RequestParameters);

            listBoxParameters.DisplayMember = "Method";
            listBoxParameters.DataSource = m_Parameters;

            LoadSelectedDocumentPreview();
        }

        private void listBoxDocuments_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadSelectedDocumentPreview();
        }

        private void LoadSelectedDocumentPreview()
        {
            var doc = listBoxDocuments.SelectedItem as DocFile;
            if (null != doc && !string.IsNullOrEmpty(doc.HtmlContent) && null != webBrowserPreview)
            {
                webBrowserPreview.DocumentText = doc.HtmlContent;
            }
        }

        private void LoadDocumentIntoEditor(DocFile docFile)
        {
            //docFileEditorControl1.LoadFile(docFile);
        }

        private void openLastFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var lastPath = Properties.Settings.Default.LastOpenedPath;
            if (!string.IsNullOrEmpty(lastPath))
            {
                OpenFolder(lastPath);
            }
        }

        private void listBoxResources_SelectedIndexChanged(object sender, EventArgs e)
        {
            var resource = ((ListBox)sender).SelectedItem as ResourceDefinition;
            textBoxResourceData.Text = resource.JsonExample;
        }

        private void listBoxMethods_SelectedIndexChanged(object sender, EventArgs e)
        {
            var method = ((ListBox)sender).SelectedItem as MethodDefinition;
            labelExpectedResponseType.Text = string.Format("{0} (col: {1})", method.ResponseMetadata.ResourceType, method.ResponseMetadata.IsCollection);
            textBoxRequest.Text = method.Request;
            textBoxRequest.Tag = method;
            textBoxResponseExpected.Text = method.Response;
            textBoxResponseActual.Clear();
        }

        private async void buttonMakeRequest_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(m_AccessToken))
            {
                await SignInAsync();

                if (string.IsNullOrEmpty(m_AccessToken))
                {
                    return;
                }
            }

            var method = listBoxMethods.SelectedItem as MethodDefinition;
            var requestParams = CurrentDocSet.RequestParamtersForMethod(method);
            var response = await method.ApiResponseForMethod(textBoxBaseURL.Text, m_AccessToken, requestParams);

            textBoxResponseActual.Text = response.FullResponse;
            textBoxResponseActual.Tag = response;
        }

        private async void signInToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await SignInAsync();
        }

        private async Task SignInAsync()
        {
            var clientId = textBoxClientId.Text;
            var scopes = textBoxAuthScopes.Text.Split(',');

            m_AccessToken = await MicrosoftAccountSDK.Windows.FormMicrosoftAccountAuth.GetAuthenticationToken(clientId, scopes, MicrosoftAccountSDK.Windows.OAuthFlow.ImplicitGrant, this);
        }

        private void toolStripTextBoxAPIRoot_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.ApiBaseRoot = ((TextBox)sender).Text;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void validateJsonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SchemaValidatorForm form = new SchemaValidatorForm { ResourceCollection = CurrentDocSet.ResourceCollection };
            form.Show();
        }

        private void buttonValidate_Click(object sender, EventArgs e)
        {
            HttpParser parser = new HttpParser();
            var expectedResponse = parser.ParseHttpResponse(textBoxResponseExpected.Text);
            var response = textBoxResponseActual.Tag as HttpResponse;
            ValidateHttpResponse(textBoxRequest.Tag as MethodDefinition, response, expectedResponse);

        }

        private void ValidateHttpResponse(MethodDefinition method, HttpResponse response, HttpResponse expectedResponse = null)
        {

            ValidationError[] errors;
            if (!CurrentDocSet.ValidateApiMethod(method, response, expectedResponse, out errors))
            {
                ErrorDisplayForm.ShowErrorDialog(errors, this);   
            }
            else
            {
                MessageBox.Show("No errors detected.");
            }
        }

        private void buttonValidateExpectedResponse_Click(object sender, EventArgs e)
        {
            var parser = new HttpParser();

            HttpResponse response = null;
            try
            {
                response = parser.ParseHttpResponse(textBoxResponseExpected.Text);
            }
            catch (Exception ex)
            {
                ErrorDisplayForm.ShowErrorDialog(new ValidationError[] { new ValidationError(null, "Error parsing HTTP response: {0}", ex.Message) });
                return;
            }

            ValidateHttpResponse(textBoxRequest.Tag as MethodDefinition, response);
        }

        private void textBoxClientId_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.ClientId = textBoxClientId.Text;
            Properties.Settings.Default.Save();
        }

        private void textBoxAuthScopes_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.AuthScopes = textBoxAuthScopes.Text;
            Properties.Settings.Default.Save();

        }

        private void buttonFormat_Click(object sender, EventArgs e)
        {
            var response = textBoxResponseActual.Tag as HttpResponse;
            var jsonString = response.Body;

            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);
            var cleanJson = Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);

            textBoxResponseActual.Text = response.FormatFullResponse(cleanJson);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ValidationError[] errors;
            if (!CurrentDocSet.ValidateLinks(checkBoxLinkWarnings.Checked, out errors))
            {
                textBoxLinkValidation.Text = (from m in errors select string.Format("{0}: {1}", m.Source, m.Message)).ComponentsJoinedByString(Environment.NewLine);
            }
            else
            {
                textBoxLinkValidation.Text = "No link errors detected";
            }
        }

        private void textBoxMethodRequestParameterFile_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.RequestParametersFile = textBoxMethodRequestParameterFile.Text;
            Properties.Settings.Default.Save();
        }


        private RequestParameters SelectedRequestConfiguration { get { return listBoxParameters.SelectedItem as RequestParameters; } }
        private void listBoxParameters_SelectedIndexChanged(object sender, EventArgs e)
        {
            methodParametersEditorControl1.OpenRequestParameters(SelectedRequestConfiguration, CurrentDocSet);
        }

        private void buttonAddParameters_Click(object sender, EventArgs e)
        {
            RequestParameters newParams = new RequestParameters { Note = "New request configuration." };
            m_Parameters.Add(newParams);
            listBoxParameters.SelectedItem = newParams;
        }

        private void buttonDeleteParameters_Click(object sender, EventArgs e)
        {
            var itemToRemove = listBoxParameters.SelectedItem as RequestParameters;
            m_Parameters.Remove(itemToRemove);
        }

        private void buttonSaveParameterFile_Click(object sender, EventArgs e)
        {
            // Write out m_Parameters to disk somewhere
            CurrentDocSet.TryWriteRequestParameters(textBoxMethodRequestParameterFile.Text, m_Parameters.ToArray());
        }

        private void methodParametersEditorControl1_Load(object sender, EventArgs e)
        {

        }
    }
}
