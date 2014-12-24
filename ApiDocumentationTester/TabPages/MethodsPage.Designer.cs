namespace OneDrive.ApiDocumentation.Windows.TabPages
{
    partial class MethodsPage
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem("get-default-drive");
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MethodsPage));
            this.splitContainerRequestResponse = new System.Windows.Forms.SplitContainer();
            this.checkBoxUseParameters = new System.Windows.Forms.CheckBox();
            this.buttonPreviewRequest = new System.Windows.Forms.Button();
            this.textBoxRequest = new System.Windows.Forms.TextBox();
            this.buttonMakeRequest = new System.Windows.Forms.Button();
            this.labelRequestTitle = new System.Windows.Forms.Label();
            this.splitContainerResponseActualExpected = new System.Windows.Forms.SplitContainer();
            this.textBoxResponseExpected = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.labelExpectedResponseType = new System.Windows.Forms.Label();
            this.labelExpectedResposne = new System.Windows.Forms.Label();
            this.buttonValidateExpectedResponse = new System.Windows.Forms.Button();
            this.textBoxResponseActual = new System.Windows.Forms.TextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.buttonFormat = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.buttonValidateActualResponse = new System.Windows.Forms.Button();
            this.panelLeftSide = new System.Windows.Forms.Panel();
            this.buttonRunAllMethods = new System.Windows.Forms.Button();
            this.listViewMethods = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.splitContainerLeftNavigation = new System.Windows.Forms.SplitContainer();
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
            this.panelLeftSide.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerLeftNavigation)).BeginInit();
            this.splitContainerLeftNavigation.Panel1.SuspendLayout();
            this.splitContainerLeftNavigation.Panel2.SuspendLayout();
            this.splitContainerLeftNavigation.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainerRequestResponse
            // 
            this.splitContainerRequestResponse.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerRequestResponse.Location = new System.Drawing.Point(0, 0);
            this.splitContainerRequestResponse.Name = "splitContainerRequestResponse";
            this.splitContainerRequestResponse.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerRequestResponse.Panel1
            // 
            this.splitContainerRequestResponse.Panel1.Controls.Add(this.checkBoxUseParameters);
            this.splitContainerRequestResponse.Panel1.Controls.Add(this.buttonPreviewRequest);
            this.splitContainerRequestResponse.Panel1.Controls.Add(this.textBoxRequest);
            this.splitContainerRequestResponse.Panel1.Controls.Add(this.buttonMakeRequest);
            this.splitContainerRequestResponse.Panel1.Controls.Add(this.labelRequestTitle);
            // 
            // splitContainerRequestResponse.Panel2
            // 
            this.splitContainerRequestResponse.Panel2.Controls.Add(this.splitContainerResponseActualExpected);
            this.splitContainerRequestResponse.Size = new System.Drawing.Size(609, 471);
            this.splitContainerRequestResponse.SplitterDistance = 154;
            this.splitContainerRequestResponse.SplitterWidth = 3;
            this.splitContainerRequestResponse.TabIndex = 19;
            // 
            // checkBoxUseParameters
            // 
            this.checkBoxUseParameters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxUseParameters.AutoSize = true;
            this.checkBoxUseParameters.Checked = true;
            this.checkBoxUseParameters.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxUseParameters.Location = new System.Drawing.Point(284, 9);
            this.checkBoxUseParameters.Name = "checkBoxUseParameters";
            this.checkBoxUseParameters.Size = new System.Drawing.Size(107, 19);
            this.checkBoxUseParameters.TabIndex = 14;
            this.checkBoxUseParameters.Text = "Use Parameters";
            this.checkBoxUseParameters.UseVisualStyleBackColor = true;
            // 
            // buttonPreviewRequest
            // 
            this.buttonPreviewRequest.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonPreviewRequest.Location = new System.Drawing.Point(397, 3);
            this.buttonPreviewRequest.Name = "buttonPreviewRequest";
            this.buttonPreviewRequest.Size = new System.Drawing.Size(80, 28);
            this.buttonPreviewRequest.TabIndex = 13;
            this.buttonPreviewRequest.Text = "Preview";
            this.buttonPreviewRequest.UseVisualStyleBackColor = true;
            this.buttonPreviewRequest.Click += new System.EventHandler(this.buttonPreviewRequest_Click);
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
            this.textBoxRequest.Size = new System.Drawing.Size(603, 118);
            this.textBoxRequest.TabIndex = 11;
            this.textBoxRequest.WordWrap = false;
            // 
            // buttonMakeRequest
            // 
            this.buttonMakeRequest.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonMakeRequest.Location = new System.Drawing.Point(483, 3);
            this.buttonMakeRequest.Name = "buttonMakeRequest";
            this.buttonMakeRequest.Size = new System.Drawing.Size(122, 28);
            this.buttonMakeRequest.TabIndex = 8;
            this.buttonMakeRequest.Text = "Submit Request";
            this.buttonMakeRequest.UseVisualStyleBackColor = true;
            this.buttonMakeRequest.Click += new System.EventHandler(this.buttonMakeRequest_Click);
            // 
            // labelRequestTitle
            // 
            this.labelRequestTitle.AutoSize = true;
            this.labelRequestTitle.Location = new System.Drawing.Point(6, 10);
            this.labelRequestTitle.Name = "labelRequestTitle";
            this.labelRequestTitle.Size = new System.Drawing.Size(52, 15);
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
            this.splitContainerResponseActualExpected.Size = new System.Drawing.Size(609, 314);
            this.splitContainerResponseActualExpected.SplitterDistance = 300;
            this.splitContainerResponseActualExpected.SplitterWidth = 3;
            this.splitContainerResponseActualExpected.TabIndex = 16;
            // 
            // textBoxResponseExpected
            // 
            this.textBoxResponseExpected.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxResponseExpected.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxResponseExpected.Location = new System.Drawing.Point(0, 75);
            this.textBoxResponseExpected.Multiline = true;
            this.textBoxResponseExpected.Name = "textBoxResponseExpected";
            this.textBoxResponseExpected.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxResponseExpected.Size = new System.Drawing.Size(300, 239);
            this.textBoxResponseExpected.TabIndex = 3;
            this.textBoxResponseExpected.WordWrap = false;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.labelExpectedResponseType);
            this.panel1.Controls.Add(this.labelExpectedResposne);
            this.panel1.Controls.Add(this.buttonValidateExpectedResponse);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(300, 75);
            this.panel1.TabIndex = 16;
            // 
            // labelExpectedResponseType
            // 
            this.labelExpectedResponseType.AutoSize = true;
            this.labelExpectedResponseType.Location = new System.Drawing.Point(3, 39);
            this.labelExpectedResponseType.Name = "labelExpectedResponseType";
            this.labelExpectedResponseType.Size = new System.Drawing.Size(83, 15);
            this.labelExpectedResponseType.TabIndex = 16;
            this.labelExpectedResponseType.Text = "oneDrive.drive";
            // 
            // labelExpectedResposne
            // 
            this.labelExpectedResposne.AutoSize = true;
            this.labelExpectedResposne.Location = new System.Drawing.Point(3, 9);
            this.labelExpectedResposne.Name = "labelExpectedResposne";
            this.labelExpectedResposne.Size = new System.Drawing.Size(110, 15);
            this.labelExpectedResposne.TabIndex = 11;
            this.labelExpectedResposne.Text = "Expected Response:";
            // 
            // buttonValidateExpectedResponse
            // 
            this.buttonValidateExpectedResponse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonValidateExpectedResponse.Location = new System.Drawing.Point(209, 5);
            this.buttonValidateExpectedResponse.Name = "buttonValidateExpectedResponse";
            this.buttonValidateExpectedResponse.Size = new System.Drawing.Size(87, 28);
            this.buttonValidateExpectedResponse.TabIndex = 15;
            this.buttonValidateExpectedResponse.Text = "Validate";
            this.buttonValidateExpectedResponse.UseVisualStyleBackColor = true;
            this.buttonValidateExpectedResponse.Click += new System.EventHandler(this.buttonValidateExpectedResponse_Click);
            // 
            // textBoxResponseActual
            // 
            this.textBoxResponseActual.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxResponseActual.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxResponseActual.Location = new System.Drawing.Point(0, 75);
            this.textBoxResponseActual.Multiline = true;
            this.textBoxResponseActual.Name = "textBoxResponseActual";
            this.textBoxResponseActual.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxResponseActual.Size = new System.Drawing.Size(306, 239);
            this.textBoxResponseActual.TabIndex = 12;
            this.textBoxResponseActual.WordWrap = false;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.buttonFormat);
            this.panel2.Controls.Add(this.label6);
            this.panel2.Controls.Add(this.buttonValidateActualResponse);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(306, 75);
            this.panel2.TabIndex = 15;
            // 
            // buttonFormat
            // 
            this.buttonFormat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonFormat.Location = new System.Drawing.Point(219, 38);
            this.buttonFormat.Name = "buttonFormat";
            this.buttonFormat.Size = new System.Drawing.Size(84, 28);
            this.buttonFormat.TabIndex = 15;
            this.buttonFormat.Text = "Format";
            this.buttonFormat.UseVisualStyleBackColor = true;
            this.buttonFormat.Click += new System.EventHandler(this.buttonFormatActualResponse_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 9);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(94, 15);
            this.label6.TabIndex = 13;
            this.label6.Text = "Actual Response";
            // 
            // buttonValidateActualResponse
            // 
            this.buttonValidateActualResponse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonValidateActualResponse.Location = new System.Drawing.Point(219, 5);
            this.buttonValidateActualResponse.Name = "buttonValidateActualResponse";
            this.buttonValidateActualResponse.Size = new System.Drawing.Size(84, 28);
            this.buttonValidateActualResponse.TabIndex = 14;
            this.buttonValidateActualResponse.Text = "Validate";
            this.buttonValidateActualResponse.UseVisualStyleBackColor = true;
            this.buttonValidateActualResponse.Click += new System.EventHandler(this.buttonValidateActualResponse_Click);
            // 
            // panelLeftSide
            // 
            this.panelLeftSide.Controls.Add(this.listViewMethods);
            this.panelLeftSide.Controls.Add(this.buttonRunAllMethods);
            this.panelLeftSide.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelLeftSide.Location = new System.Drawing.Point(0, 0);
            this.panelLeftSide.Name = "panelLeftSide";
            this.panelLeftSide.Size = new System.Drawing.Size(200, 471);
            this.panelLeftSide.TabIndex = 20;
            // 
            // buttonRunAllMethods
            // 
            this.buttonRunAllMethods.Location = new System.Drawing.Point(0, 2);
            this.buttonRunAllMethods.Name = "buttonRunAllMethods";
            this.buttonRunAllMethods.Size = new System.Drawing.Size(75, 23);
            this.buttonRunAllMethods.TabIndex = 8;
            this.buttonRunAllMethods.Text = "Run All";
            this.buttonRunAllMethods.UseVisualStyleBackColor = true;
            this.buttonRunAllMethods.Click += new System.EventHandler(this.buttonRunAllMethods_Click);
            // 
            // listViewMethods
            // 
            this.listViewMethods.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewMethods.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.listViewMethods.HideSelection = false;
            listViewItem1.Checked = true;
            listViewItem1.StateImageIndex = 1;
            this.listViewMethods.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1});
            this.listViewMethods.Location = new System.Drawing.Point(0, 32);
            this.listViewMethods.Name = "listViewMethods";
            this.listViewMethods.Size = new System.Drawing.Size(200, 436);
            this.listViewMethods.StateImageList = this.imageList1;
            this.listViewMethods.TabIndex = 9;
            this.listViewMethods.UseCompatibleStateImageBehavior = false;
            this.listViewMethods.View = System.Windows.Forms.View.Details;
            this.listViewMethods.SelectedIndexChanged += new System.EventHandler(this.listViewMethods_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Method";
            this.columnHeader1.Width = 167;
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "success");
            this.imageList1.Images.SetKeyName(1, "error");
            this.imageList1.Images.SetKeyName(2, "warning");
            this.imageList1.Images.SetKeyName(3, "refresh");
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Error";
            // 
            // splitContainerLeftNavigation
            // 
            this.splitContainerLeftNavigation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerLeftNavigation.Location = new System.Drawing.Point(0, 0);
            this.splitContainerLeftNavigation.Name = "splitContainerLeftNavigation";
            // 
            // splitContainerLeftNavigation.Panel1
            // 
            this.splitContainerLeftNavigation.Panel1.Controls.Add(this.panelLeftSide);
            this.splitContainerLeftNavigation.Panel1MinSize = 200;
            // 
            // splitContainerLeftNavigation.Panel2
            // 
            this.splitContainerLeftNavigation.Panel2.Controls.Add(this.splitContainerRequestResponse);
            this.splitContainerLeftNavigation.Size = new System.Drawing.Size(813, 471);
            this.splitContainerLeftNavigation.SplitterDistance = 200;
            this.splitContainerLeftNavigation.TabIndex = 21;
            // 
            // MethodsPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainerLeftNavigation);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "MethodsPage";
            this.Size = new System.Drawing.Size(813, 471);
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
            this.panelLeftSide.ResumeLayout(false);
            this.splitContainerLeftNavigation.Panel1.ResumeLayout(false);
            this.splitContainerLeftNavigation.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerLeftNavigation)).EndInit();
            this.splitContainerLeftNavigation.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainerRequestResponse;
        private System.Windows.Forms.CheckBox checkBoxUseParameters;
        private System.Windows.Forms.Button buttonPreviewRequest;
        private System.Windows.Forms.TextBox textBoxRequest;
        private System.Windows.Forms.Button buttonMakeRequest;
        private System.Windows.Forms.Label labelRequestTitle;
        private System.Windows.Forms.SplitContainer splitContainerResponseActualExpected;
        private System.Windows.Forms.TextBox textBoxResponseExpected;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label labelExpectedResponseType;
        private System.Windows.Forms.Label labelExpectedResposne;
        private System.Windows.Forms.Button buttonValidateExpectedResponse;
        private System.Windows.Forms.TextBox textBoxResponseActual;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button buttonFormat;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button buttonValidateActualResponse;
        private System.Windows.Forms.Panel panelLeftSide;
        private System.Windows.Forms.Button buttonRunAllMethods;
        private System.Windows.Forms.ListView listViewMethods;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.SplitContainer splitContainerLeftNavigation;
    }
}
