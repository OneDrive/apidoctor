namespace ApiDocumentationTester
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
            this.baseUrlToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripTextBoxAPIRoot = new System.Windows.Forms.ToolStripTextBox();
            this.validateJsonToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listBoxDocuments = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxResponseExpected = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.listBoxResources = new System.Windows.Forms.ListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.listBoxMethods = new System.Windows.Forms.ListBox();
            this.buttonMakeRequest = new System.Windows.Forms.Button();
            this.textBoxRequest = new System.Windows.Forms.TextBox();
            this.labelRequestTitle = new System.Windows.Forms.Label();
            this.labelExpectedResposne = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.textBoxResponseActual = new System.Windows.Forms.TextBox();
            this.buttonValidate = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.accountToolStripMenuItem,
            this.baseUrlToolStripMenuItem,
            this.validateJsonToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1138, 24);
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
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // openFolderToolStripMenuItem
            // 
            this.openFolderToolStripMenuItem.Name = "openFolderToolStripMenuItem";
            this.openFolderToolStripMenuItem.Size = new System.Drawing.Size(163, 22);
            this.openFolderToolStripMenuItem.Text = "&Open Folder...";
            this.openFolderToolStripMenuItem.Click += new System.EventHandler(this.openFolderToolStripMenuItem_Click);
            // 
            // openLastFolderToolStripMenuItem
            // 
            this.openLastFolderToolStripMenuItem.Name = "openLastFolderToolStripMenuItem";
            this.openLastFolderToolStripMenuItem.Size = new System.Drawing.Size(163, 22);
            this.openLastFolderToolStripMenuItem.Text = "Open Last Folder";
            this.openLastFolderToolStripMenuItem.Click += new System.EventHandler(this.openLastFolderToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(160, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(163, 22);
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
            this.signInToolStripMenuItem.Size = new System.Drawing.Size(119, 22);
            this.signInToolStripMenuItem.Text = "Sign In...";
            this.signInToolStripMenuItem.Click += new System.EventHandler(this.signInToolStripMenuItem_Click);
            // 
            // baseUrlToolStripMenuItem
            // 
            this.baseUrlToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripTextBoxAPIRoot});
            this.baseUrlToolStripMenuItem.Name = "baseUrlToolStripMenuItem";
            this.baseUrlToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.baseUrlToolStripMenuItem.Text = "Base Url";
            // 
            // toolStripTextBoxAPIRoot
            // 
            this.toolStripTextBoxAPIRoot.Name = "toolStripTextBoxAPIRoot";
            this.toolStripTextBoxAPIRoot.Size = new System.Drawing.Size(100, 23);
            this.toolStripTextBoxAPIRoot.Text = "https://df.api.onedrive.com/v1.0";
            this.toolStripTextBoxAPIRoot.TextChanged += new System.EventHandler(this.toolStripTextBoxAPIRoot_TextChanged);
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
            this.listBoxDocuments.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.listBoxDocuments.FormattingEnabled = true;
            this.listBoxDocuments.ItemHeight = 15;
            this.listBoxDocuments.Location = new System.Drawing.Point(12, 76);
            this.listBoxDocuments.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBoxDocuments.Name = "listBoxDocuments";
            this.listBoxDocuments.Size = new System.Drawing.Size(254, 589);
            this.listBoxDocuments.TabIndex = 1;
            this.listBoxDocuments.SelectedIndexChanged += new System.EventHandler(this.listBoxDocuments_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 51);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(119, 15);
            this.label1.TabIndex = 2;
            this.label1.Text = "Documentation Files:";
            // 
            // textBoxResponseExpected
            // 
            this.textBoxResponseExpected.Location = new System.Drawing.Point(555, 231);
            this.textBoxResponseExpected.Multiline = true;
            this.textBoxResponseExpected.Name = "textBoxResponseExpected";
            this.textBoxResponseExpected.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxResponseExpected.Size = new System.Drawing.Size(440, 179);
            this.textBoxResponseExpected.TabIndex = 3;
            this.textBoxResponseExpected.WordWrap = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(277, 51);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(60, 15);
            this.label2.TabIndex = 4;
            this.label2.Text = "Resources";
            // 
            // listBoxResources
            // 
            this.listBoxResources.FormattingEnabled = true;
            this.listBoxResources.ItemHeight = 15;
            this.listBoxResources.Location = new System.Drawing.Point(280, 76);
            this.listBoxResources.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBoxResources.Name = "listBoxResources";
            this.listBoxResources.Size = new System.Drawing.Size(254, 199);
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
            // listBoxMethods
            // 
            this.listBoxMethods.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.listBoxMethods.FormattingEnabled = true;
            this.listBoxMethods.ItemHeight = 15;
            this.listBoxMethods.Location = new System.Drawing.Point(280, 302);
            this.listBoxMethods.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBoxMethods.Name = "listBoxMethods";
            this.listBoxMethods.Size = new System.Drawing.Size(254, 364);
            this.listBoxMethods.TabIndex = 7;
            this.listBoxMethods.SelectedIndexChanged += new System.EventHandler(this.listBoxMethods_SelectedIndexChanged);
            // 
            // buttonMakeRequest
            // 
            this.buttonMakeRequest.Location = new System.Drawing.Point(1002, 76);
            this.buttonMakeRequest.Name = "buttonMakeRequest";
            this.buttonMakeRequest.Size = new System.Drawing.Size(122, 23);
            this.buttonMakeRequest.TabIndex = 8;
            this.buttonMakeRequest.Text = "Make Request";
            this.buttonMakeRequest.UseVisualStyleBackColor = true;
            this.buttonMakeRequest.Click += new System.EventHandler(this.buttonMakeRequest_Click);
            // 
            // textBoxRequest
            // 
            this.textBoxRequest.Location = new System.Drawing.Point(555, 76);
            this.textBoxRequest.Multiline = true;
            this.textBoxRequest.Name = "textBoxRequest";
            this.textBoxRequest.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxRequest.Size = new System.Drawing.Size(440, 124);
            this.textBoxRequest.TabIndex = 9;
            this.textBoxRequest.WordWrap = false;
            // 
            // labelRequestTitle
            // 
            this.labelRequestTitle.AutoSize = true;
            this.labelRequestTitle.Location = new System.Drawing.Point(552, 51);
            this.labelRequestTitle.Name = "labelRequestTitle";
            this.labelRequestTitle.Size = new System.Drawing.Size(52, 15);
            this.labelRequestTitle.TabIndex = 10;
            this.labelRequestTitle.Text = "Request:";
            // 
            // labelExpectedResposne
            // 
            this.labelExpectedResposne.AutoSize = true;
            this.labelExpectedResposne.Location = new System.Drawing.Point(552, 213);
            this.labelExpectedResposne.Name = "labelExpectedResposne";
            this.labelExpectedResposne.Size = new System.Drawing.Size(113, 15);
            this.labelExpectedResposne.TabIndex = 11;
            this.labelExpectedResposne.Text = "Expected Response :";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(552, 418);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(94, 15);
            this.label6.TabIndex = 13;
            this.label6.Text = "Actual Response";
            // 
            // textBoxResponseActual
            // 
            this.textBoxResponseActual.Location = new System.Drawing.Point(555, 436);
            this.textBoxResponseActual.Multiline = true;
            this.textBoxResponseActual.Name = "textBoxResponseActual";
            this.textBoxResponseActual.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxResponseActual.Size = new System.Drawing.Size(440, 230);
            this.textBoxResponseActual.TabIndex = 12;
            this.textBoxResponseActual.WordWrap = false;
            // 
            // buttonValidate
            // 
            this.buttonValidate.Location = new System.Drawing.Point(1001, 435);
            this.buttonValidate.Name = "buttonValidate";
            this.buttonValidate.Size = new System.Drawing.Size(122, 23);
            this.buttonValidate.TabIndex = 14;
            this.buttonValidate.Text = "Validate";
            this.buttonValidate.UseVisualStyleBackColor = true;
            this.buttonValidate.Click += new System.EventHandler(this.buttonValidate_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1138, 735);
            this.Controls.Add(this.buttonValidate);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textBoxResponseActual);
            this.Controls.Add(this.labelExpectedResposne);
            this.Controls.Add(this.labelRequestTitle);
            this.Controls.Add(this.textBoxRequest);
            this.Controls.Add(this.buttonMakeRequest);
            this.Controls.Add(this.listBoxMethods);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.listBoxResources);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxResponseExpected);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listBoxDocuments);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "MainForm";
            this.Text = "API Documentation Test Tool";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
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
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ToolStripMenuItem openLastFolderToolStripMenuItem;
        private System.Windows.Forms.TextBox textBoxResponseExpected;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListBox listBoxResources;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ListBox listBoxMethods;
        private System.Windows.Forms.Button buttonMakeRequest;
        private System.Windows.Forms.ToolStripMenuItem accountToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem signInToolStripMenuItem;
        private System.Windows.Forms.TextBox textBoxRequest;
        private System.Windows.Forms.Label labelRequestTitle;
        private System.Windows.Forms.Label labelExpectedResposne;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBoxResponseActual;
        private System.Windows.Forms.ToolStripMenuItem baseUrlToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBoxAPIRoot;
        private System.Windows.Forms.ToolStripMenuItem validateJsonToolStripMenuItem;
        private System.Windows.Forms.Button buttonValidate;
    }
}

