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
using OneDrive.ApiDocumentation.Validation.Http;

namespace OneDrive.ApiDocumentation.Windows.TabPages
{
    public partial class MethodsPage : UserControl
    {
        public MethodsPage()
        {
            InitializeComponent();
        }

        public DocSet CurrentDocSet { get; private set; }
        public new MainForm ParentForm 
        {
            get
            {
                var myParent = Parent;
                while (myParent != null && (myParent as MainForm) == null)
                {
                    myParent = myParent.Parent;
                }

                return myParent as MainForm;
            }
        }

        public void LoadFromDocSet(DocSet docset)
        {
            CurrentDocSet = docset;

            listViewMethods.BeginUpdate();
            listViewMethods.Items.Clear();
            var listViewItems = from m in docset.Methods
                select new ListViewItem() { Text = m.Identifier, Tag = m, StateImageIndex = -1 };
            listViewMethods.Items.AddRange(listViewItems.ToArray());
            listViewMethods.EndUpdate();
        }

        private MethodDefinition SelectedMethod
        {
            get
            {
                if (listViewMethods.SelectedItems.Count == 0)
                    return null;

                var selectedItem = listViewMethods.SelectedItems[0];
                var method = (MethodDefinition)selectedItem.Tag;
                return method;
            }
        }

        private void listViewMethods_SelectedIndexChanged(object sender, EventArgs e)
        {
            var method = SelectedMethod;
            if (method == null)
                return;

            if (textBoxRequest.Tag == method)
                return;

            labelExpectedResponseType.Text = string.Format("{0} (col: {1})", method.ExpectedResponseMetadata.ResourceType, method.ExpectedResponseMetadata.IsCollection);
            textBoxRequest.Text = method.Request;
            textBoxRequest.Tag = method;
            textBoxResponseExpected.Text = method.ExpectedResponse;
            textBoxResponseActual.Text = method.ActualResponse;
        }

        private async void buttonPreviewRequest_Click(object sender, EventArgs e)
        {
            var originalMethod = SelectedMethod;
            var method = MethodDefinition.FromRequest(textBoxRequest.Text, originalMethod.RequestMetadata, originalMethod.SourceFile);

            ScenarioDefinition requestParams = null;
            if (checkBoxUseParameters.Checked)
            {
                requestParams = CurrentDocSet.TestScenarios.ScenariosForMethod(originalMethod).FirstOrDefault();
            }

            await MainForm.MostRecentInstance.SignInAsync();
            var credentials = AuthenicationCredentials.CreateAutoCredentials(MainForm.MostRecentInstance.AccessToken);

            var buildRequestResult = await method.PreviewRequestAsync(requestParams, Properties.Settings.Default.ApiBaseRoot, credentials);
            if (buildRequestResult.IsWarningOrError)
            {
                ErrorDisplayForm.ShowErrorDialog(buildRequestResult.Messages);
                return;
            }

            var request = buildRequestResult.Value;
            var requestText = request.FullHttpText();

            ErrorDisplayForm form = new ErrorDisplayForm("Request Preview", requestText);
            form.Show(this);
        }

        private async Task<bool> EnsureAccessTokenAvailable()
        {
            if (string.IsNullOrEmpty(ParentForm.AccessToken))
            {
                await ParentForm.SignInAsync();
                if (string.IsNullOrEmpty(ParentForm.AccessToken))
                {
                    return false;
                }
            }
            return true;
        }

        private async void buttonMakeRequest_Click(object sender, EventArgs e)
        {
            var selectedListViewItem = (listViewMethods.SelectedItems.Count > 0) ? listViewMethods.SelectedItems[0] : null;

            textBoxResponseActual.Clear();
            SelectedMethod.ActualResponse = string.Empty;

            var result = await TestRunMethod(SelectedMethod, textBoxRequest.Text, checkBoxUseParameters.Checked, textBoxResponseExpected.Text);
            
            SetItemState(selectedListViewItem, result);

            if (result.Response != null)
                textBoxResponseActual.Text = result.Response.FullHttpText();
            else
                textBoxResponseActual.Clear();
            textBoxResponseActual.Tag = result.Response;

            if (result.Result != RunResult.Success)
            {
                ErrorDisplayForm.ShowErrorDialog(result.Errors, this);
            }

            //var tokenAvailable = await EnsureAccessTokenAvailable();
            //if (!tokenAvailable) return;

            //textBoxResponseActual.Clear();
            //textBoxResponseActual.Tag = null;

            //var originalMethod = SelectedMethod;
            //var method = MethodDefinition.FromRequest(textBoxRequest.Text, originalMethod.RequestMetadata);

            //RequestParameters requestParams = null;
            //if (checkBoxUseParameters.Checked)
            //{
            //    requestParams = CurrentDocSet.RequestParamtersForMethod(originalMethod);
            //}

            //var baseUrl = Properties.Settings.Default.ApiBaseRoot;
            //var response = await method.ApiResponseForMethod(baseUrl, ParentForm.AccessToken, requestParams);

            //textBoxResponseActual.Text = response.FullHttpText();
            //textBoxResponseActual.Tag = response;
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
                ErrorDisplayForm.ShowErrorDialog(new ValidationError[] { new ValidationError(ValidationErrorCode.HttpResponseFormatInvalid, null, "Error parsing HTTP response: {0}", ex.Message) });
                return;
            }

            ValidateHttpResponse(textBoxRequest.Tag as MethodDefinition, response);
        }

        private void buttonValidateActualResponse_Click(object sender, EventArgs e)
        {
            HttpParser parser = new HttpParser();
            var expectedResponse = parser.ParseHttpResponse(textBoxResponseExpected.Text);
            var response = textBoxResponseActual.Tag as HttpResponse;
            ValidateHttpResponse(textBoxRequest.Tag as MethodDefinition, response, expectedResponse);

        }

        private void buttonFormatActualResponse_Click(object sender, EventArgs e)
        {
            var response = textBoxResponseActual.Tag as HttpResponse;
            var jsonString = response.Body;

            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);
            var cleanJson = Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);

            textBoxResponseActual.Text = response.FormatFullResponse(cleanJson);
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


        private async void buttonRunAllMethods_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewMethods.Items)
            {
                var method = (MethodDefinition)item.Tag;

                item.StateImageIndex = imageList1.Images.IndexOfKey("refresh");

                var result = await TestRunMethod(method);
                SetItemState(item, result);
            }
        }

        private void SetItemState(ListViewItem item, MethodValidationResult result)
        {
            if (null == item) return;

            if (item.SubItems.Count > 1)
                item.SubItems.RemoveAt(1);

            if (null != result.Errors && result.Errors.Length > 0)
                item.SubItems.Add(result.Errors.ErrorsToString());

            switch (result.Result)
            {
                case RunResult.Success:
                    item.StateImageIndex = imageList1.Images.IndexOfKey("success");
                    break;
                case RunResult.Warning:
                    item.StateImageIndex = imageList1.Images.IndexOfKey("warning");
                    break;
                case RunResult.Error:
                    item.StateImageIndex = imageList1.Images.IndexOfKey("error");
                    break;
            }
        }

        private async Task<MethodValidationResult> TestRunMethod(MethodDefinition originalMethod, string requestString = null, bool applyParameters = true, string expectedResponseString = null)
        {
            var tokenAvailable = await EnsureAccessTokenAvailable();
            if (!tokenAvailable)
                return new MethodValidationResult() { Result = RunResult.Error, Errors = new ValidationError[] { new ValidationError(ValidationErrorCode.MissingAccessToken, null, "No access token available.") } };

            var httpParser = new HttpParser();

            HttpResponse expectedResponse = null;
            if (!string.IsNullOrEmpty(expectedResponseString))
            {
                expectedResponse = httpParser.ParseHttpResponse(expectedResponseString);
            }
            else
            {
                expectedResponse = httpParser.ParseHttpResponse(originalMethod.ExpectedResponse);
            }

            var method = originalMethod;
            if (!string.IsNullOrEmpty(requestString))
            {
                method = MethodDefinition.FromRequest(requestString, originalMethod.RequestMetadata, originalMethod.SourceFile);
                method.AddExpectedResponse(expectedResponseString, originalMethod.ExpectedResponseMetadata);
            }
            
            ScenarioDefinition requestParams = null;
            if (applyParameters)
            {
                requestParams = CurrentDocSet.TestScenarios.ScenariosForMethod(method).FirstOrDefault();
            }

            var baseUrl = Properties.Settings.Default.ApiBaseRoot;
            AuthenicationCredentials credentials = AuthenicationCredentials.CreateAutoCredentials(ParentForm.AccessToken);
            var responseResult = await method.ApiResponseForMethod(baseUrl, credentials, requestParams);
            if (responseResult.IsWarningOrError)
            {
                return new MethodValidationResult { Errors = responseResult.Messages, Result = RunResult.Error };
            }

            var response = responseResult.Value;
            originalMethod.ActualResponse = response.FullHttpText();

            ValidationError[] errors;
            CurrentDocSet.ValidateApiMethod(method, response, expectedResponse, out errors);

            if (null == errors || errors.Length == 0)
            {
                return new MethodValidationResult { Result = RunResult.Success, Response = response };
            }
            else if (errors.All(ve => ve.IsWarning))
            {
                return new MethodValidationResult { Result = RunResult.Warning, Response = response, Errors = errors };
            }
            else
            {
                return new MethodValidationResult { Result = RunResult.Error, Response = response, Errors = errors };
            }
        }

        enum RunResult
        {
            Success,
            Warning,
            Error
        }

        class MethodValidationResult
        {
            public RunResult Result { get; set; }
            public HttpResponse Response { get; set; }
            public ValidationError[] Errors { get; set; }
        }


    }
}
