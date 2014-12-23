namespace OneDrive.ApiDocumentation.Windows
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openLastFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.accountToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.signInToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.validateJsonToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listBoxDocuments = new System.Windows.Forms.ListBox();
            this.textBoxResponseExpected = new System.Windows.Forms.TextBox();
            this.listBoxResources = new System.Windows.Forms.ListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.listBoxMethods = new System.Windows.Forms.ListBox();
            this.buttonMakeRequest = new System.Windows.Forms.Button();
            this.labelExpectedResposne = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.textBoxResponseActual = new System.Windows.Forms.TextBox();
            this.buttonValidateActualResponse = new System.Windows.Forms.Button();
            this.buttonValidateExpectedResponse = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageFiles = new System.Windows.Forms.TabPage();
            this.webBrowserPreview = new System.Windows.Forms.WebBrowser();
            this.tabPageLinks = new System.Windows.Forms.TabPage();
            this.checkBoxLinkWarnings = new System.Windows.Forms.CheckBox();
            this.textBoxLinkValidation = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.tabPageResources = new System.Windows.Forms.TabPage();
            this.textBoxResourceData = new System.Windows.Forms.TextBox();
            this.tabPageMethods = new System.Windows.Forms.TabPage();
            this.splitContainerRequestResponse = new System.Windows.Forms.SplitContainer();
            this.textBoxRequest = new System.Windows.Forms.TextBox();
            this.labelRequestTitle = new System.Windows.Forms.Label();
            this.splitContainerResponseActualExpected = new System.Windows.Forms.SplitContainer();
            this.panel1 = new System.Windows.Forms.Panel();
            this.labelExpectedResponseType = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.buttonFormat = new System.Windows.Forms.Button();
            this.tabPageOptions = new System.Windows.Forms.TabPage();
            this.textBoxMethodRequestParameterFile = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxAuthScopes = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxClientId = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxBaseURL = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPageParameters = new System.Windows.Forms.TabPage();
            this.buttonSaveParameterFile = new System.Windows.Forms.Button();
            this.buttonDeleteParameters = new System.Windows.Forms.Button();
            this.buttonAddParameters = new System.Windows.Forms.Button();
            this.listBoxParameters = new System.Windows.Forms.ListBox();
            this.label7 = new System.Windows.Forms.Label();
            this.methodParametersEditorControl1 = new MethodParametersEditorControl();
            this.menuStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPageFiles.SuspendLayout();
            this.tabPageLinks.SuspendLayout();
            this.tabPageResources.SuspendLayout();
            this.tabPageMethods.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerRequestResponse)).BeginInit();
            this.splitContainerRequestResponse.Panel1.SuspendLayout();
            this.splitContainerRequestResponse.Panel2.SuspendLayout();
            this.splitContainerRequestResponse.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerResponseActualExpected)).BeginInit();
            this.splitContainerResponseActualExpected.Panel1.SuspendLayout();
            this.splitContainerResponseActualExpected.Panel2.SuspendLayout();
            this.splitContainerResponseActualExpected.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.tabPageOptions.SuspendLayout();
            this.tabPageParameters.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.accountToolStripMenuItem,
            this.validateJsonToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(996, 28);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openFolderToolStripMenuItem,
            this.openLastFolderToolStripMenuItem,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(44, 24);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // openFolderToolStripMenuItem
            // 
            this.openFolderToolStripMenuItem.Name = "openFolderToolStripMenuItem";
            this.openFolderToolStripMenuItem.Size = new System.Drawing.Size(190, 24);
            this.openFolderToolStripMenuItem.Text = "&Open Folder...";
            this.openFolderToolStripMenuItem.Click += new System.EventHandler(this.openFolderToolStripMenuItem_Click);
            // 
            // openLastFolderToolStripMenuItem
            // 
            this.openLastFolderToolStripMenuItem.Name = "openLastFolderToolStripMenuItem";
            this.openLastFolderToolStripMenuItem.Size = new System.Drawing.Size(190, 24);
            this.openLastFolderToolStripMenuItem.Text = "Open Last Folder";
            this.openLastFolderToolStripMenuItem.Click += new System.EventHandler(this.openLastFolderToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(187, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(190, 24);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // accountToolStripMenuItem
            // 
            this.accountToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.signInToolStripMenuItem});
            this.accountToolStripMenuItem.Name = "accountToolStripMenuItem";
            this.accountToolStripMenuItem.Size = new System.Drawing.Size(75, 24);
            this.accountToolStripMenuItem.Text = "Account";
            // 
            // signInToolStripMenuItem
            // 
            this.signInToolStripMenuItem.Name = "signInToolStripMenuItem";
            this.signInToolStripMenuItem.Size = new System.Drawing.Size(132, 24);
            this.signInToolStripMenuItem.Text = "Sign In...";
            this.signInToolStripMenuItem.Click += new System.EventHandler(this.signInToolStripMenuItem_Click);
            // 
            // validateJsonToolStripMenuItem
            // 
            this.validateJsonToolStripMenuItem.Name = "validateJsonToolStripMenuItem";
            this.validateJsonToolStripMenuItem.Size = new System.Drawing.Size(108, 24);
            this.validateJsonToolStripMenuItem.Text = "Validate Json";
            this.validateJsonToolStripMenuItem.Click += new System.EventHandler(this.validateJsonToolStripMenuItem_Click);
            // 
            // listBoxDocuments
            // 
            this.listBoxDocuments.Dock = System.Windows.Forms.DockStyle.Left;
            this.listBoxDocuments.FormattingEnabled = true;
            this.listBoxDocuments.ItemHeight = 20;
            this.listBoxDocuments.Location = new System.Drawing.Point(0, 0);
            this.listBoxDocuments.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBoxDocuments.Name = "listBoxDocuments";
            this.listBoxDocuments.Size = new System.Drawing.Size(254, 606);
            this.listBoxDocuments.TabIndex = 1;
            this.listBoxDocuments.SelectedIndexChanged += new System.EventHandler(this.listBoxDocuments_SelectedIndexChanged);
            // 
            // textBoxResponseExpected
            // 
            this.textBoxResponseExpected.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxResponseExpected.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxResponseExpected.Location = new System.Drawing.Point(0, 75);
            this.textBoxResponseExpected.Multiline = true;
            this.textBoxResponseExpected.Name = "textBoxResponseExpected";
            this.textBoxResponseExpected.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxResponseExpected.Size = new System.Drawing.Size(356, 327);
            this.textBoxResponseExpected.TabIndex = 3;
            this.textBoxResponseExpected.WordWrap = false;
            // 
            // listBoxResources
            // 
            this.listBoxResources.Dock = System.Windows.Forms.DockStyle.Left;
            this.listBoxResources.FormattingEnabled = true;
            this.listBoxResources.ItemHeight = 20;
            this.listBoxResources.Location = new System.Drawing.Point(0, 0);
            this.listBoxResources.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBoxResources.Name = "listBoxResources";
            this.listBoxResources.Size = new System.Drawing.Size(254, 606);
            this.listBoxResources.TabIndex = 5;
            this.listBoxResources.SelectedIndexChanged += new System.EventHandler(this.listBoxResources_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(280, 283);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(67, 20);
            this.label3.TabIndex = 6;
            this.label3.Text = "Methods";
            // 
            // listBoxMethods
            // 
            this.listBoxMethods.Dock = System.Windows.Forms.DockStyle.Left;
            this.listBoxMethods.FormattingEnabled = true;
            this.listBoxMethods.ItemHeight = 20;
            this.listBoxMethods.Location = new System.Drawing.Point(0, 0);
            this.listBoxMethods.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBoxMethods.Name = "listBoxMethods";
            this.listBoxMethods.Size = new System.Drawing.Size(254, 606);
            this.listBoxMethods.TabIndex = 7;
            this.listBoxMethods.SelectedIndexChanged += new System.EventHandler(this.listBoxMethods_SelectedIndexChanged);
            // 
            // buttonMakeRequest
            // 
            this.buttonMakeRequest.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonMakeRequest.Location = new System.Drawing.Point(593, 3);
            this.buttonMakeRequest.Name = "buttonMakeRequest";
            this.buttonMakeRequest.Size = new System.Drawing.Size(123, 28);
            this.buttonMakeRequest.TabIndex = 8;
            this.buttonMakeRequest.Text = "Submit Request";
            this.buttonMakeRequest.UseVisualStyleBackColor = true;
            this.buttonMakeRequest.Click += new System.EventHandler(this.buttonMakeRequest_Click);
            // 
            // labelExpectedResposne
            // 
            this.labelExpectedResposne.AutoSize = true;
            this.labelExpectedResposne.Location = new System.Drawing.Point(3, 9);
            this.labelExpectedResposne.Name = "labelExpectedResposne";
            this.labelExpectedResposne.Size = new System.Drawing.Size(140, 20);
            this.labelExpectedResposne.TabIndex = 11;
            this.labelExpectedResposne.Text = "Expected Response:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 9);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(118, 20);
            this.label6.TabIndex = 13;
            this.label6.Text = "Actual Response";
            // 
            // textBoxResponseActual
            // 
            this.textBoxResponseActual.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxResponseActual.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxResponseActual.Location = new System.Drawing.Point(0, 75);
            this.textBoxResponseActual.Multiline = true;
            this.textBoxResponseActual.Name = "textBoxResponseActual";
            this.textBoxResponseActual.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxResponseActual.Size = new System.Drawing.Size(358, 327);
            this.textBoxResponseActual.TabIndex = 12;
            this.textBoxResponseActual.WordWrap = false;
            // 
            // buttonValidateActualResponse
            // 
            this.buttonValidateActualResponse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonValidateActualResponse.Location = new System.Drawing.Point(271, 5);
            this.buttonValidateActualResponse.Name = "buttonValidateActualResponse";
            this.buttonValidateActualResponse.Size = new System.Drawing.Size(84, 28);
            this.buttonValidateActualResponse.TabIndex = 14;
            this.buttonValidateActualResponse.Text = "Validate";
            this.buttonValidateActualResponse.UseVisualStyleBackColor = true;
            this.buttonValidateActualResponse.Click += new System.EventHandler(this.buttonValidate_Click);
            // 
            // buttonValidateExpectedResponse
            // 
            this.buttonValidateExpectedResponse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonValidateExpectedResponse.Location = new System.Drawing.Point(265, 5);
            this.buttonValidateExpectedResponse.Name = "buttonValidateExpectedResponse";
            this.buttonValidateExpectedResponse.Size = new System.Drawing.Size(88, 28);
            this.buttonValidateExpectedResponse.TabIndex = 15;
            this.buttonValidateExpectedResponse.Text = "Validate";
            this.buttonValidateExpectedResponse.UseVisualStyleBackColor = true;
            this.buttonValidateExpectedResponse.Click += new System.EventHandler(this.buttonValidateExpectedResponse_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPageFiles);
            this.tabControl1.Controls.Add(this.tabPageLinks);
            this.tabControl1.Controls.Add(this.tabPageResources);
            this.tabControl1.Controls.Add(this.tabPageMethods);
            this.tabControl1.Controls.Add(this.tabPageOptions);
            this.tabControl1.Controls.Add(this.tabPageParameters);
            this.tabControl1.Location = new System.Drawing.Point(12, 44);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(980, 639);
            this.tabControl1.TabIndex = 16;
            // 
            // tabPageFiles
            // 
            this.tabPageFiles.Controls.Add(this.webBrowserPreview);
            this.tabPageFiles.Controls.Add(this.listBoxDocuments);
            this.tabPageFiles.Location = new System.Drawing.Point(4, 29);
            this.tabPageFiles.Name = "tabPageFiles";
            this.tabPageFiles.Size = new System.Drawing.Size(972, 606);
            this.tabPageFiles.TabIndex = 0;
            this.tabPageFiles.Text = "Files";
            this.tabPageFiles.UseVisualStyleBackColor = true;
            // 
            // webBrowserPreview
            // 
            this.webBrowserPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowserPreview.Location = new System.Drawing.Point(254, 0);
            this.webBrowserPreview.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowserPreview.Name = "webBrowserPreview";
            this.webBrowserPreview.Size = new System.Drawing.Size(718, 606);
            this.webBrowserPreview.TabIndex = 2;
            // 
            // tabPageLinks
            // 
            this.tabPageLinks.Controls.Add(this.checkBoxLinkWarnings);
            this.tabPageLinks.Controls.Add(this.textBoxLinkValidation);
            this.tabPageLinks.Controls.Add(this.button1);
            this.tabPageLinks.Location = new System.Drawing.Point(4, 29);
            this.tabPageLinks.Name = "tabPageLinks";
            this.tabPageLinks.Size = new System.Drawing.Size(972, 606);
            this.tabPageLinks.TabIndex = 4;
            this.tabPageLinks.Text = "Links";
            this.tabPageLinks.UseVisualStyleBackColor = true;
            // 
            // checkBoxLinkWarnings
            // 
            this.checkBoxLinkWarnings.AutoSize = true;
            this.checkBoxLinkWarnings.Location = new System.Drawing.Point(113, 9);
            this.checkBoxLinkWarnings.Name = "checkBoxLinkWarnings";
            this.checkBoxLinkWarnings.Size = new System.Drawing.Size(133, 24);
            this.checkBoxLinkWarnings.TabIndex = 2;
            this.checkBoxLinkWarnings.Text = "Show Warnings";
            this.checkBoxLinkWarnings.UseVisualStyleBackColor = true;
            // 
            // textBoxLinkValidation
            // 
            this.textBoxLinkValidation.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxLinkValidation.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxLinkValidation.Location = new System.Drawing.Point(4, 42);
            this.textBoxLinkValidation.Multiline = true;
            this.textBoxLinkValidation.Name = "textBoxLinkValidation";
            this.textBoxLinkValidation.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxLinkValidation.Size = new System.Drawing.Size(964, 561);
            this.textBoxLinkValidation.TabIndex = 1;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(4, 4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(103, 32);
            this.button1.TabIndex = 0;
            this.button1.Text = "Verify Links";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // tabPageResources
            // 
            this.tabPageResources.Controls.Add(this.textBoxResourceData);
            this.tabPageResources.Controls.Add(this.listBoxResources);
            this.tabPageResources.Location = new System.Drawing.Point(4, 29);
            this.tabPageResources.Name = "tabPageResources";
            this.tabPageResources.Size = new System.Drawing.Size(972, 606);
            this.tabPageResources.TabIndex = 1;
            this.tabPageResources.Text = "Resources";
            this.tabPageResources.UseVisualStyleBackColor = true;
            // 
            // textBoxResourceData
            // 
            this.textBoxResourceData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxResourceData.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxResourceData.Location = new System.Drawing.Point(254, 0);
            this.textBoxResourceData.Multiline = true;
            this.textBoxResourceData.Name = "textBoxResourceData";
            this.textBoxResourceData.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxResourceData.Size = new System.Drawing.Size(718, 606);
            this.textBoxResourceData.TabIndex = 17;
            this.textBoxResourceData.WordWrap = false;
            // 
            // tabPageMethods
            // 
            this.tabPageMethods.Controls.Add(this.splitContainerRequestResponse);
            this.tabPageMethods.Controls.Add(this.listBoxMethods);
            this.tabPageMethods.Location = new System.Drawing.Point(4, 29);
            this.tabPageMethods.Name = "tabPageMethods";
            this.tabPageMethods.Size = new System.Drawing.Size(972, 606);
            this.tabPageMethods.TabIndex = 2;
            this.tabPageMethods.Text = "Methods";
            this.tabPageMethods.UseVisualStyleBackColor = true;
            // 
            // splitContainerRequestResponse
            // 
            this.splitContainerRequestResponse.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerRequestResponse.Location = new System.Drawing.Point(254, 0);
            this.splitContainerRequestResponse.Name = "splitContainerRequestResponse";
            this.splitContainerRequestResponse.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerRequestResponse.Panel1
            // 
            this.splitContainerRequestResponse.Panel1.Controls.Add(this.textBoxRequest);
            this.splitContainerRequestResponse.Panel1.Controls.Add(this.buttonMakeRequest);
            this.splitContainerRequestResponse.Panel1.Controls.Add(this.labelRequestTitle);
            // 
            // splitContainerRequestResponse.Panel2
            // 
            this.splitContainerRequestResponse.Panel2.Controls.Add(this.splitContainerResponseActualExpected);
            this.splitContainerRequestResponse.Size = new System.Drawing.Size(718, 606);
            this.splitContainerRequestResponse.SplitterDistance = 200;
            this.splitContainerRequestResponse.TabIndex = 17;
            // 
            // textBoxRequest
            // 
            this.textBoxRequest.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxRequest.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxRequest.Location = new System.Drawing.Point(3, 32);
            this.textBoxRequest.Multiline = true;
            this.textBoxRequest.Name = "textBoxRequest";
            this.textBoxRequest.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxRequest.Size = new System.Drawing.Size(715, 165);
            this.textBoxRequest.TabIndex = 11;
            this.textBoxRequest.WordWrap = false;
            // 
            // labelRequestTitle
            // 
            this.labelRequestTitle.AutoSize = true;
            this.labelRequestTitle.Location = new System.Drawing.Point(0, 7);
            this.labelRequestTitle.Name = "labelRequestTitle";
            this.labelRequestTitle.Size = new System.Drawing.Size(65, 20);
            this.labelRequestTitle.TabIndex = 12;
            this.labelRequestTitle.Text = "Request:";
            // 
            // splitContainerResponseActualExpected
            // 
            this.splitContainerResponseActualExpected.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerResponseActualExpected.Location = new System.Drawing.Point(0, 0);
            this.splitContainerResponseActualExpected.Name = "splitContainerResponseActualExpected";
            // 
            // splitContainerResponseActualExpected.Panel1
            // 
            this.splitContainerResponseActualExpected.Panel1.Controls.Add(this.textBoxResponseExpected);
            this.splitContainerResponseActualExpected.Panel1.Controls.Add(this.panel1);
            // 
            // splitContainerResponseActualExpected.Panel2
            // 
            this.splitContainerResponseActualExpected.Panel2.Controls.Add(this.textBoxResponseActual);
            this.splitContainerResponseActualExpected.Panel2.Controls.Add(this.panel2);
            this.splitContainerResponseActualExpected.Size = new System.Drawing.Size(718, 402);
            this.splitContainerResponseActualExpected.SplitterDistance = 356;
            this.splitContainerResponseActualExpected.TabIndex = 16;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.labelExpectedResponseType);
            this.panel1.Controls.Add(this.labelExpectedResposne);
            this.panel1.Controls.Add(this.buttonValidateExpectedResponse);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(356, 75);
            this.panel1.TabIndex = 16;
            // 
            // labelExpectedResponseType
            // 
            this.labelExpectedResponseType.AutoSize = true;
            this.labelExpectedResponseType.Location = new System.Drawing.Point(4, 39);
            this.labelExpectedResponseType.Name = "labelExpectedResponseType";
            this.labelExpectedResponseType.Size = new System.Drawing.Size(105, 20);
            this.labelExpectedResponseType.TabIndex = 16;
            this.labelExpectedResponseType.Text = "oneDrive.drive";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.buttonFormat);
            this.panel2.Controls.Add(this.label6);
            this.panel2.Controls.Add(this.buttonValidateActualResponse);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(358, 75);
            this.panel2.TabIndex = 15;
            // 
            // buttonFormat
            // 
            this.buttonFormat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonFormat.Location = new System.Drawing.Point(271, 38);
            this.buttonFormat.Name = "buttonFormat";
            this.buttonFormat.Size = new System.Drawing.Size(84, 28);
            this.buttonFormat.TabIndex = 15;
            this.buttonFormat.Text = "Format";
            this.buttonFormat.UseVisualStyleBackColor = true;
            this.buttonFormat.Click += new System.EventHandler(this.buttonFormat_Click);
            // 
            // tabPageOptions
            // 
            this.tabPageOptions.Controls.Add(this.textBoxMethodRequestParameterFile);
            this.tabPageOptions.Controls.Add(this.label5);
            this.tabPageOptions.Controls.Add(this.textBoxAuthScopes);
            this.tabPageOptions.Controls.Add(this.label4);
            this.tabPageOptions.Controls.Add(this.textBoxClientId);
            this.tabPageOptions.Controls.Add(this.label2);
            this.tabPageOptions.Controls.Add(this.textBoxBaseURL);
            this.tabPageOptions.Controls.Add(this.label1);
            this.tabPageOptions.Location = new System.Drawing.Point(4, 29);
            this.tabPageOptions.Name = "tabPageOptions";
            this.tabPageOptions.Size = new System.Drawing.Size(972, 606);
            this.tabPageOptions.TabIndex = 3;
            this.tabPageOptions.Text = "Options";
            this.tabPageOptions.UseVisualStyleBackColor = true;
            // 
            // textBoxMethodRequestParameterFile
            // 
            this.textBoxMethodRequestParameterFile.Location = new System.Drawing.Point(104, 111);
            this.textBoxMethodRequestParameterFile.Name = "textBoxMethodRequestParameterFile";
            this.textBoxMethodRequestParameterFile.Size = new System.Drawing.Size(384, 27);
            this.textBoxMethodRequestParameterFile.TabIndex = 7;
            this.textBoxMethodRequestParameterFile.TextChanged += new System.EventHandler(this.textBoxMethodRequestParameterFile_TextChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(14, 114);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(86, 20);
            this.label5.TabIndex = 6;
            this.label5.Text = "Parameters:";
            // 
            // textBoxAuthScopes
            // 
            this.textBoxAuthScopes.Location = new System.Drawing.Point(104, 78);
            this.textBoxAuthScopes.Name = "textBoxAuthScopes";
            this.textBoxAuthScopes.Size = new System.Drawing.Size(384, 27);
            this.textBoxAuthScopes.TabIndex = 5;
            this.textBoxAuthScopes.TextChanged += new System.EventHandler(this.textBoxAuthScopes_TextChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(14, 81);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(59, 20);
            this.label4.TabIndex = 4;
            this.label4.Text = "Scopes:";
            // 
            // textBoxClientId
            // 
            this.textBoxClientId.Location = new System.Drawing.Point(104, 45);
            this.textBoxClientId.Name = "textBoxClientId";
            this.textBoxClientId.Size = new System.Drawing.Size(384, 27);
            this.textBoxClientId.TabIndex = 3;
            this.textBoxClientId.TextChanged += new System.EventHandler(this.textBoxClientId_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(69, 20);
            this.label2.TabIndex = 2;
            this.label2.Text = "Client ID:";
            // 
            // textBoxBaseURL
            // 
            this.textBoxBaseURL.Location = new System.Drawing.Point(104, 12);
            this.textBoxBaseURL.Name = "textBoxBaseURL";
            this.textBoxBaseURL.Size = new System.Drawing.Size(384, 27);
            this.textBoxBaseURL.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(73, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Base URL:";
            // 
            // tabPageParameters
            // 
            this.tabPageParameters.Controls.Add(this.buttonSaveParameterFile);
            this.tabPageParameters.Controls.Add(this.buttonDeleteParameters);
            this.tabPageParameters.Controls.Add(this.buttonAddParameters);
            this.tabPageParameters.Controls.Add(this.listBoxParameters);
            this.tabPageParameters.Controls.Add(this.label7);
            this.tabPageParameters.Controls.Add(this.methodParametersEditorControl1);
            this.tabPageParameters.Location = new System.Drawing.Point(4, 29);
            this.tabPageParameters.Name = "tabPageParameters";
            this.tabPageParameters.Size = new System.Drawing.Size(972, 606);
            this.tabPageParameters.TabIndex = 5;
            this.tabPageParameters.Text = "Parameters";
            this.tabPageParameters.UseVisualStyleBackColor = true;
            // 
            // buttonSaveParameterFile
            // 
            this.buttonSaveParameterFile.Location = new System.Drawing.Point(181, 34);
            this.buttonSaveParameterFile.Name = "buttonSaveParameterFile";
            this.buttonSaveParameterFile.Size = new System.Drawing.Size(80, 33);
            this.buttonSaveParameterFile.TabIndex = 11;
            this.buttonSaveParameterFile.Text = "Save";
            this.buttonSaveParameterFile.UseVisualStyleBackColor = true;
            this.buttonSaveParameterFile.Click += new System.EventHandler(this.buttonSaveParameterFile_Click);
            // 
            // buttonDeleteParameters
            // 
            this.buttonDeleteParameters.Location = new System.Drawing.Point(95, 35);
            this.buttonDeleteParameters.Name = "buttonDeleteParameters";
            this.buttonDeleteParameters.Size = new System.Drawing.Size(80, 33);
            this.buttonDeleteParameters.TabIndex = 10;
            this.buttonDeleteParameters.Text = "Delete";
            this.buttonDeleteParameters.UseVisualStyleBackColor = true;
            this.buttonDeleteParameters.Click += new System.EventHandler(this.buttonDeleteParameters_Click);
            // 
            // buttonAddParameters
            // 
            this.buttonAddParameters.Location = new System.Drawing.Point(9, 35);
            this.buttonAddParameters.Name = "buttonAddParameters";
            this.buttonAddParameters.Size = new System.Drawing.Size(80, 33);
            this.buttonAddParameters.TabIndex = 9;
            this.buttonAddParameters.Text = "Add";
            this.buttonAddParameters.UseVisualStyleBackColor = true;
            this.buttonAddParameters.Click += new System.EventHandler(this.buttonAddParameters_Click);
            // 
            // listBoxParameters
            // 
            this.listBoxParameters.FormattingEnabled = true;
            this.listBoxParameters.ItemHeight = 20;
            this.listBoxParameters.Location = new System.Drawing.Point(7, 75);
            this.listBoxParameters.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBoxParameters.Name = "listBoxParameters";
            this.listBoxParameters.Size = new System.Drawing.Size(254, 524);
            this.listBoxParameters.TabIndex = 8;
            this.listBoxParameters.SelectedIndexChanged += new System.EventHandler(this.listBoxParameters_SelectedIndexChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 11);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(251, 20);
            this.label7.TabIndex = 0;
            this.label7.Text = "Request Parameters For Service Calls";
            // 
            // methodParametersEditorControl1
            // 
            this.methodParametersEditorControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.methodParametersEditorControl1.Location = new System.Drawing.Point(279, 35);
            this.methodParametersEditorControl1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.methodParametersEditorControl1.MinimumSize = new System.Drawing.Size(0, 369);
            this.methodParametersEditorControl1.Name = "methodParametersEditorControl1";
            this.methodParametersEditorControl1.Size = new System.Drawing.Size(689, 564);
            this.methodParametersEditorControl1.TabIndex = 1;
            this.methodParametersEditorControl1.Load += new System.EventHandler(this.methodParametersEditorControl1_Load);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(996, 695);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "MainForm";
            this.Text = "API Documentation Test Tool";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPageFiles.ResumeLayout(false);
            this.tabPageLinks.ResumeLayout(false);
            this.tabPageLinks.PerformLayout();
            this.tabPageResources.ResumeLayout(false);
            this.tabPageResources.PerformLayout();
            this.tabPageMethods.ResumeLayout(false);
            this.splitContainerRequestResponse.Panel1.ResumeLayout(false);
            this.splitContainerRequestResponse.Panel1.PerformLayout();
            this.splitContainerRequestResponse.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerRequestResponse)).EndInit();
            this.splitContainerRequestResponse.ResumeLayout(false);
            this.splitContainerResponseActualExpected.Panel1.ResumeLayout(false);
            this.splitContainerResponseActualExpected.Panel1.PerformLayout();
            this.splitContainerResponseActualExpected.Panel2.ResumeLayout(false);
            this.splitContainerResponseActualExpected.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerResponseActualExpected)).EndInit();
            this.splitContainerResponseActualExpected.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.tabPageOptions.ResumeLayout(false);
            this.tabPageOptions.PerformLayout();
            this.tabPageParameters.ResumeLayout(false);
            this.tabPageParameters.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openFolderToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ListBox listBoxDocuments;
        private System.Windows.Forms.ToolStripMenuItem openLastFolderToolStripMenuItem;
        private System.Windows.Forms.TextBox textBoxResponseExpected;
        private System.Windows.Forms.ListBox listBoxResources;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ListBox listBoxMethods;
        private System.Windows.Forms.Button buttonMakeRequest;
        private System.Windows.Forms.ToolStripMenuItem accountToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem signInToolStripMenuItem;
        private System.Windows.Forms.Label labelExpectedResposne;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBoxResponseActual;
        private System.Windows.Forms.ToolStripMenuItem validateJsonToolStripMenuItem;
        private System.Windows.Forms.Button buttonValidateActualResponse;
        private System.Windows.Forms.Button buttonValidateExpectedResponse;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageFiles;
        private System.Windows.Forms.TabPage tabPageResources;
        private System.Windows.Forms.TextBox textBoxResourceData;
        private System.Windows.Forms.TabPage tabPageMethods;
        private System.Windows.Forms.SplitContainer splitContainerResponseActualExpected;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label labelRequestTitle;
        private System.Windows.Forms.TextBox textBoxRequest;
        private System.Windows.Forms.Label labelExpectedResponseType;
        private System.Windows.Forms.WebBrowser webBrowserPreview;
        private System.Windows.Forms.TabPage tabPageOptions;
        private System.Windows.Forms.TextBox textBoxBaseURL;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxAuthScopes;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxClientId;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonFormat;
        private System.Windows.Forms.SplitContainer splitContainerRequestResponse;
        private System.Windows.Forms.TabPage tabPageLinks;
        private System.Windows.Forms.TextBox textBoxLinkValidation;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox checkBoxLinkWarnings;
        private System.Windows.Forms.TextBox textBoxMethodRequestParameterFile;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TabPage tabPageParameters;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ListBox listBoxParameters;
        private MethodParametersEditorControl methodParametersEditorControl1;
        private System.Windows.Forms.Button buttonSaveParameterFile;
        private System.Windows.Forms.Button buttonDeleteParameters;
        private System.Windows.Forms.Button buttonAddParameters;
    }
}

