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
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.showLoadErrorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.accountToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.signInToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.validateJsonToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listBoxDocuments = new System.Windows.Forms.ListBox();
            this.listBoxResources = new System.Windows.Forms.ListBox();
            this.label3 = new System.Windows.Forms.Label();
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
            this.tabPageParameters = new System.Windows.Forms.TabPage();
            this.buttonSaveParameterFile = new System.Windows.Forms.Button();
            this.buttonDeleteParameters = new System.Windows.Forms.Button();
            this.buttonAddParameters = new System.Windows.Forms.Button();
            this.listBoxScenarios = new System.Windows.Forms.ListBox();
            this.label7 = new System.Windows.Forms.Label();
            this.tabPageOptions = new System.Windows.Forms.TabPage();
            this.textBoxMethodRequestParameterFile = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxAuthScopes = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxClientId = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxBaseURL = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.methodsPage = new OneDrive.ApiDocumentation.Windows.TabPages.MethodsPage();
            this.methodParametersEditorControl1 = new OneDrive.ApiDocumentation.Windows.ScenarioEditorControl();
            this.menuStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPageFiles.SuspendLayout();
            this.tabPageLinks.SuspendLayout();
            this.tabPageResources.SuspendLayout();
            this.tabPageMethods.SuspendLayout();
            this.tabPageParameters.SuspendLayout();
            this.tabPageOptions.SuspendLayout();
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
            this.menuStrip1.Size = new System.Drawing.Size(949, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openFolderToolStripMenuItem,
            this.openLastFolderToolStripMenuItem,
            this.toolStripMenuItem2,
            this.showLoadErrorsToolStripMenuItem,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // openFolderToolStripMenuItem
            // 
            this.openFolderToolStripMenuItem.Name = "openFolderToolStripMenuItem";
            this.openFolderToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.openFolderToolStripMenuItem.Text = "&Open Folder...";
            this.openFolderToolStripMenuItem.Click += new System.EventHandler(this.openFolderToolStripMenuItem_Click);
            // 
            // openLastFolderToolStripMenuItem
            // 
            this.openLastFolderToolStripMenuItem.Name = "openLastFolderToolStripMenuItem";
            this.openLastFolderToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.openLastFolderToolStripMenuItem.Text = "Open Last Folder";
            this.openLastFolderToolStripMenuItem.Click += new System.EventHandler(this.openLastFolderToolStripMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(171, 6);
            // 
            // showLoadErrorsToolStripMenuItem
            // 
            this.showLoadErrorsToolStripMenuItem.Name = "showLoadErrorsToolStripMenuItem";
            this.showLoadErrorsToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.showLoadErrorsToolStripMenuItem.Text = "Show Load Errors...";
            this.showLoadErrorsToolStripMenuItem.Click += new System.EventHandler(this.showLoadErrorsToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(171, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // accountToolStripMenuItem
            // 
            this.accountToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.signInToolStripMenuItem});
            this.accountToolStripMenuItem.Name = "accountToolStripMenuItem";
            this.accountToolStripMenuItem.Size = new System.Drawing.Size(64, 20);
            this.accountToolStripMenuItem.Text = "Account";
            // 
            // signInToolStripMenuItem
            // 
            this.signInToolStripMenuItem.Name = "signInToolStripMenuItem";
            this.signInToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.signInToolStripMenuItem.Text = "Sign In...";
            this.signInToolStripMenuItem.Click += new System.EventHandler(this.signInToolStripMenuItem_Click);
            // 
            // validateJsonToolStripMenuItem
            // 
            this.validateJsonToolStripMenuItem.Name = "validateJsonToolStripMenuItem";
            this.validateJsonToolStripMenuItem.Size = new System.Drawing.Size(87, 20);
            this.validateJsonToolStripMenuItem.Text = "Validate Json";
            this.validateJsonToolStripMenuItem.Click += new System.EventHandler(this.validateJsonToolStripMenuItem_Click);
            // 
            // listBoxDocuments
            // 
            this.listBoxDocuments.Dock = System.Windows.Forms.DockStyle.Left;
            this.listBoxDocuments.FormattingEnabled = true;
            this.listBoxDocuments.ItemHeight = 15;
            this.listBoxDocuments.Location = new System.Drawing.Point(0, 0);
            this.listBoxDocuments.Name = "listBoxDocuments";
            this.listBoxDocuments.Size = new System.Drawing.Size(254, 466);
            this.listBoxDocuments.TabIndex = 1;
            this.listBoxDocuments.SelectedIndexChanged += new System.EventHandler(this.listBoxDocuments_SelectedIndexChanged);
            // 
            // listBoxResources
            // 
            this.listBoxResources.Dock = System.Windows.Forms.DockStyle.Left;
            this.listBoxResources.FormattingEnabled = true;
            this.listBoxResources.ItemHeight = 15;
            this.listBoxResources.Location = new System.Drawing.Point(0, 0);
            this.listBoxResources.Name = "listBoxResources";
            this.listBoxResources.Size = new System.Drawing.Size(254, 468);
            this.listBoxResources.TabIndex = 5;
            this.listBoxResources.SelectedIndexChanged += new System.EventHandler(this.listBoxResources_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(280, 283);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(54, 15);
            this.label3.TabIndex = 6;
            this.label3.Text = "Methods";
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
            this.tabControl1.Controls.Add(this.tabPageParameters);
            this.tabControl1.Controls.Add(this.tabPageOptions);
            this.tabControl1.Location = new System.Drawing.Point(12, 44);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(933, 494);
            this.tabControl1.TabIndex = 16;
            // 
            // tabPageFiles
            // 
            this.tabPageFiles.Controls.Add(this.webBrowserPreview);
            this.tabPageFiles.Controls.Add(this.listBoxDocuments);
            this.tabPageFiles.Location = new System.Drawing.Point(4, 24);
            this.tabPageFiles.Name = "tabPageFiles";
            this.tabPageFiles.Size = new System.Drawing.Size(925, 466);
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
            this.webBrowserPreview.Size = new System.Drawing.Size(671, 466);
            this.webBrowserPreview.TabIndex = 2;
            // 
            // tabPageLinks
            // 
            this.tabPageLinks.Controls.Add(this.checkBoxLinkWarnings);
            this.tabPageLinks.Controls.Add(this.textBoxLinkValidation);
            this.tabPageLinks.Controls.Add(this.button1);
            this.tabPageLinks.Location = new System.Drawing.Point(4, 24);
            this.tabPageLinks.Name = "tabPageLinks";
            this.tabPageLinks.Size = new System.Drawing.Size(925, 466);
            this.tabPageLinks.TabIndex = 4;
            this.tabPageLinks.Text = "Links";
            this.tabPageLinks.UseVisualStyleBackColor = true;
            // 
            // checkBoxLinkWarnings
            // 
            this.checkBoxLinkWarnings.AutoSize = true;
            this.checkBoxLinkWarnings.Location = new System.Drawing.Point(113, 9);
            this.checkBoxLinkWarnings.Name = "checkBoxLinkWarnings";
            this.checkBoxLinkWarnings.Size = new System.Drawing.Size(108, 19);
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
            this.textBoxLinkValidation.Location = new System.Drawing.Point(3, 42);
            this.textBoxLinkValidation.Multiline = true;
            this.textBoxLinkValidation.Name = "textBoxLinkValidation";
            this.textBoxLinkValidation.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxLinkValidation.Size = new System.Drawing.Size(964, 561);
            this.textBoxLinkValidation.TabIndex = 1;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(3, 3);
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
            this.tabPageResources.Location = new System.Drawing.Point(4, 24);
            this.tabPageResources.Name = "tabPageResources";
            this.tabPageResources.Size = new System.Drawing.Size(925, 466);
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
            this.textBoxResourceData.Size = new System.Drawing.Size(671, 468);
            this.textBoxResourceData.TabIndex = 17;
            this.textBoxResourceData.WordWrap = false;
            // 
            // tabPageMethods
            // 
            this.tabPageMethods.Controls.Add(this.methodsPage);
            this.tabPageMethods.Location = new System.Drawing.Point(4, 24);
            this.tabPageMethods.Name = "tabPageMethods";
            this.tabPageMethods.Size = new System.Drawing.Size(925, 466);
            this.tabPageMethods.TabIndex = 2;
            this.tabPageMethods.Text = "Methods";
            this.tabPageMethods.UseVisualStyleBackColor = true;
            // 
            // tabPageParameters
            // 
            this.tabPageParameters.Controls.Add(this.methodParametersEditorControl1);
            this.tabPageParameters.Controls.Add(this.buttonSaveParameterFile);
            this.tabPageParameters.Controls.Add(this.buttonDeleteParameters);
            this.tabPageParameters.Controls.Add(this.buttonAddParameters);
            this.tabPageParameters.Controls.Add(this.listBoxScenarios);
            this.tabPageParameters.Controls.Add(this.label7);
            this.tabPageParameters.Location = new System.Drawing.Point(4, 24);
            this.tabPageParameters.Name = "tabPageParameters";
            this.tabPageParameters.Size = new System.Drawing.Size(925, 466);
            this.tabPageParameters.TabIndex = 5;
            this.tabPageParameters.Text = "Scenarios";
            this.tabPageParameters.UseVisualStyleBackColor = true;
            // 
            // buttonSaveParameterFile
            // 
            this.buttonSaveParameterFile.Location = new System.Drawing.Point(180, 35);
            this.buttonSaveParameterFile.Name = "buttonSaveParameterFile";
            this.buttonSaveParameterFile.Size = new System.Drawing.Size(80, 33);
            this.buttonSaveParameterFile.TabIndex = 11;
            this.buttonSaveParameterFile.Text = "Save";
            this.buttonSaveParameterFile.UseVisualStyleBackColor = true;
            this.buttonSaveParameterFile.Click += new System.EventHandler(this.buttonSaveParameterFile_Click);
            // 
            // buttonDeleteParameters
            // 
            this.buttonDeleteParameters.Location = new System.Drawing.Point(94, 35);
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
            // listBoxScenarios
            // 
            this.listBoxScenarios.FormattingEnabled = true;
            this.listBoxScenarios.ItemHeight = 15;
            this.listBoxScenarios.Location = new System.Drawing.Point(7, 75);
            this.listBoxScenarios.Name = "listBoxScenarios";
            this.listBoxScenarios.Size = new System.Drawing.Size(254, 514);
            this.listBoxScenarios.TabIndex = 8;
            this.listBoxScenarios.SelectedIndexChanged += new System.EventHandler(this.listBoxParameters_SelectedIndexChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 12);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(57, 15);
            this.label7.TabIndex = 0;
            this.label7.Text = "Scenarios";
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
            this.tabPageOptions.Location = new System.Drawing.Point(4, 24);
            this.tabPageOptions.Name = "tabPageOptions";
            this.tabPageOptions.Size = new System.Drawing.Size(925, 466);
            this.tabPageOptions.TabIndex = 3;
            this.tabPageOptions.Text = "Options";
            this.tabPageOptions.UseVisualStyleBackColor = true;
            // 
            // textBoxMethodRequestParameterFile
            // 
            this.textBoxMethodRequestParameterFile.Location = new System.Drawing.Point(104, 111);
            this.textBoxMethodRequestParameterFile.Name = "textBoxMethodRequestParameterFile";
            this.textBoxMethodRequestParameterFile.Size = new System.Drawing.Size(384, 23);
            this.textBoxMethodRequestParameterFile.TabIndex = 7;
            this.textBoxMethodRequestParameterFile.TextChanged += new System.EventHandler(this.textBoxMethodRequestParameterFile_TextChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(14, 114);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(69, 15);
            this.label5.TabIndex = 6;
            this.label5.Text = "Parameters:";
            // 
            // textBoxAuthScopes
            // 
            this.textBoxAuthScopes.Location = new System.Drawing.Point(104, 78);
            this.textBoxAuthScopes.Name = "textBoxAuthScopes";
            this.textBoxAuthScopes.Size = new System.Drawing.Size(384, 23);
            this.textBoxAuthScopes.TabIndex = 5;
            this.textBoxAuthScopes.TextChanged += new System.EventHandler(this.textBoxAuthScopes_TextChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(14, 81);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(47, 15);
            this.label4.TabIndex = 4;
            this.label4.Text = "Scopes:";
            // 
            // textBoxClientId
            // 
            this.textBoxClientId.Location = new System.Drawing.Point(104, 45);
            this.textBoxClientId.Name = "textBoxClientId";
            this.textBoxClientId.Size = new System.Drawing.Size(384, 23);
            this.textBoxClientId.TabIndex = 3;
            this.textBoxClientId.TextChanged += new System.EventHandler(this.textBoxClientId_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(55, 15);
            this.label2.TabIndex = 2;
            this.label2.Text = "Client ID:";
            // 
            // textBoxBaseURL
            // 
            this.textBoxBaseURL.Location = new System.Drawing.Point(104, 12);
            this.textBoxBaseURL.Name = "textBoxBaseURL";
            this.textBoxBaseURL.Size = new System.Drawing.Size(384, 23);
            this.textBoxBaseURL.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Base URL:";
            // 
            // methodsPage
            // 
            this.methodsPage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.methodsPage.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.methodsPage.Location = new System.Drawing.Point(0, 0);
            this.methodsPage.Name = "methodsPage";
            this.methodsPage.Size = new System.Drawing.Size(925, 468);
            this.methodsPage.TabIndex = 0;
            // 
            // methodParametersEditorControl1
            // 
            this.methodParametersEditorControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.methodParametersEditorControl1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.methodParametersEditorControl1.Location = new System.Drawing.Point(267, 35);
            this.methodParametersEditorControl1.Margin = new System.Windows.Forms.Padding(2);
            this.methodParametersEditorControl1.MinimumSize = new System.Drawing.Size(0, 277);
            this.methodParametersEditorControl1.Name = "methodParametersEditorControl1";
            this.methodParametersEditorControl1.Size = new System.Drawing.Size(655, 429);
            this.methodParametersEditorControl1.TabIndex = 12;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(949, 550);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MainMenuStrip = this.menuStrip1;
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
            this.tabPageParameters.ResumeLayout(false);
            this.tabPageParameters.PerformLayout();
            this.tabPageOptions.ResumeLayout(false);
            this.tabPageOptions.PerformLayout();
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
        private System.Windows.Forms.ListBox listBoxResources;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ToolStripMenuItem accountToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem signInToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem validateJsonToolStripMenuItem;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageFiles;
        private System.Windows.Forms.TabPage tabPageResources;
        private System.Windows.Forms.TextBox textBoxResourceData;
        private System.Windows.Forms.TabPage tabPageMethods;
        private System.Windows.Forms.WebBrowser webBrowserPreview;
        private System.Windows.Forms.TabPage tabPageOptions;
        private System.Windows.Forms.TextBox textBoxBaseURL;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxAuthScopes;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxClientId;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TabPage tabPageLinks;
        private System.Windows.Forms.TextBox textBoxLinkValidation;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox checkBoxLinkWarnings;
        private System.Windows.Forms.TextBox textBoxMethodRequestParameterFile;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TabPage tabPageParameters;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ListBox listBoxScenarios;
        private System.Windows.Forms.Button buttonSaveParameterFile;
        private System.Windows.Forms.Button buttonDeleteParameters;
        private System.Windows.Forms.Button buttonAddParameters;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem showLoadErrorsToolStripMenuItem;
        private ScenarioEditorControl methodParametersEditorControl1;
        private TabPages.MethodsPage methodsPage;
    }
}

