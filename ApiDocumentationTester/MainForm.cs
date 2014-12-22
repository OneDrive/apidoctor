using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ApiDocumentationTester
{
    public partial class MainForm : Form
    {
        DocSet CurrentDocSet { get; set; }

        List<ResourceDefinition> m_Resources = new List<ResourceDefinition>();
        List<MethodDefinition> m_Methods = new List<MethodDefinition>();

        JsonValidator m_Validator = new JsonValidator();

        string m_AccessToken = null;

        public MainForm()
        {
            InitializeComponent();

            toolStripTextBoxAPIRoot.Text = Properties.Settings.Default.ApiBaseRoot;
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

            m_Resources.Clear();
            m_Methods.Clear();
            m_Validator = new JsonValidator();

            foreach (var file in CurrentDocSet.Files)
            {
                file.Scan();

                if (file.Resources.Count > 0)
                {
                    m_Resources.AddRange(file.Resources.Values);
                    foreach (var resource in file.Resources.Values)
                    {
                        m_Validator.RegisterJsonResource(resource);
                    }
                }
                if (file.Requests.Length > 0)
                {
                    m_Methods.AddRange(file.Requests);
                }
            }

            listBoxResources.DisplayMember = "OdataType";
            listBoxResources.DataSource = m_Resources;

            listBoxMethods.DisplayMember = "DisplayName";
            listBoxMethods.DataSource = m_Methods;
        }

        private void listBoxDocuments_SelectedIndexChanged(object sender, EventArgs e)
        {
            //LoadDocumentIntoEditor(((ListBox)sender).SelectedItem as DocFile);
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
            textBoxResponseExpected.Text = resource.JsonFormat;
            labelRequestTitle.Text = "Resource Definition";
        }

        private void listBoxMethods_SelectedIndexChanged(object sender, EventArgs e)
        {
            var method = ((ListBox)sender).SelectedItem as MethodDefinition;
            labelRequestTitle.Text = "Request"; 
            labelExpectedResposne.Text = string.Format("Expected Response (resource type: {0})", method.ResponseType);
            textBoxRequest.Text = method.Request;
            textBoxRequest.Tag = method;
            textBoxResponseExpected.Text = method.Response;
            textBoxResponseActual.Clear();
        }

        private async void buttonMakeRequest_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(m_AccessToken))
            {
                MessageBox.Show("Please sign in before executing a method.");
                return;
            }

            var method = listBoxMethods.SelectedItem as MethodDefinition;
            var request = method.BuildRequest(toolStripTextBoxAPIRoot.Text, m_AccessToken);

            var response = await HttpRequestParser.HttpResponse.ResponseFromHttpWebResponseAsync(request);
            textBoxResponseActual.Text = response.FullResponse;
            textBoxResponseActual.Tag = response;
        }

        private async void signInToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_AccessToken = await MicrosoftAccountSDK.Windows.FormMicrosoftAccountAuth.GetAuthenticationToken("0000000044128B55", new string[] { "wl.skydrive_update", "wl.contacts_skydrive", "wl.signin" }, MicrosoftAccountSDK.Windows.OAuthFlow.ImplicitGrant, this);
            
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
            SchemaValidatorForm form = new SchemaValidatorForm { Validator = m_Validator };
            form.Show();

        }

        private void buttonValidate_Click(object sender, EventArgs e)
        {
            HttpRequestParser.HttpParser parser = new HttpRequestParser.HttpParser();
            var expectedResponse = parser.ParseHttpResponse(textBoxResponseExpected.Text);
            var response = textBoxResponseActual.Tag as HttpRequestParser.HttpResponse;
            ValidateHttpResponse(textBoxRequest.Tag as MethodDefinition, response, expectedResponse);

        }

        private void ValidateHttpResponse(MethodDefinition method, HttpRequestParser.HttpResponse response, HttpRequestParser.HttpResponse expectedResponse = null)
        {
            if (null == response)
            {
                MessageBox.Show("No response object available");
                return;
            }

            if (null == method)
            {
                MessageBox.Show("No method definition available");
                return;
            }

            if (string.IsNullOrEmpty(response.Body))
            {
                MessageBox.Show("No response body to validate");
                return;
            }

            StringBuilder detectedErrors = new StringBuilder();

            if (null != expectedResponse)
            {
                // TODO: verify that the HTTP portion of the response is correct (Status code, message, etc)
                ValidationError[] httpErrors;
                if (!expectedResponse.CompareToResponse(response, out httpErrors))
                {
                    detectedErrors.AppendLine((from m in httpErrors select m.Message).ComponentsJoinedByString(Environment.NewLine));
                }
            }


            // Verify the JSON portion of the response is correct
            var responseResourceType = method.ResponseType;
            ValidationError[] errors;
            if (!m_Validator.ValidateJson(responseResourceType, response.Body, method.ResponseIsCollection, out errors))
            {
                detectedErrors.AppendLine((from m in errors select m.Message).ComponentsJoinedByString(Environment.NewLine));
            }

            if (detectedErrors.Length > 0)
            {
                MessageBox.Show("API response is incorrect. The following errors were detected:\n\n" + detectedErrors.ToString());
            }
            else
            {
                MessageBox.Show("API response matches the documentation.");
            }
        }

        private void buttonValidateExpectedResponse_Click(object sender, EventArgs e)
        {
            var parser = new HttpRequestParser.HttpParser();
            var response = parser.ParseHttpResponse(textBoxResponseExpected.Text);
            ValidateHttpResponse(textBoxRequest.Tag as MethodDefinition, response);
        }
    }
}
