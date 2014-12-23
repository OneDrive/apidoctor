namespace OneDrive.ApiDocumentation.Windows
{
    partial class DocFileEditorControl
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
            this.buttonScanFile = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.listBoxBlocks = new System.Windows.Forms.ListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.labelCodeBlockLanguage = new System.Windows.Forms.Label();
            this.textBoxCode = new System.Windows.Forms.TextBox();
            this.listBoxResources = new System.Windows.Forms.ListBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxResource = new System.Windows.Forms.TextBox();
            this.textBoxRequest = new System.Windows.Forms.TextBox();
            this.listBoxMethods = new System.Windows.Forms.ListBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxResponse = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // buttonScanFile
            // 
            this.buttonScanFile.Location = new System.Drawing.Point(11, 4);
            this.buttonScanFile.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.buttonScanFile.Name = "buttonScanFile";
            this.buttonScanFile.Size = new System.Drawing.Size(81, 34);
            this.buttonScanFile.TabIndex = 2;
            this.buttonScanFile.Text = "&Scan";
            this.buttonScanFile.UseVisualStyleBackColor = true;
            this.buttonScanFile.Click += new System.EventHandler(this.buttonScanFile_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(90, 20);
            this.label2.TabIndex = 3;
            this.label2.Text = "Code Blocks";
            // 
            // listBoxBlocks
            // 
            this.listBoxBlocks.FormattingEnabled = true;
            this.listBoxBlocks.ItemHeight = 20;
            this.listBoxBlocks.Location = new System.Drawing.Point(10, 74);
            this.listBoxBlocks.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBoxBlocks.Name = "listBoxBlocks";
            this.listBoxBlocks.Size = new System.Drawing.Size(120, 124);
            this.listBoxBlocks.TabIndex = 4;
            this.listBoxBlocks.SelectedIndexChanged += new System.EventHandler(this.listBoxBlocks_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(146, 49);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(77, 20);
            this.label3.TabIndex = 5;
            this.label3.Text = "Language:";
            // 
            // labelCodeBlockLanguage
            // 
            this.labelCodeBlockLanguage.AutoSize = true;
            this.labelCodeBlockLanguage.Location = new System.Drawing.Point(228, 49);
            this.labelCodeBlockLanguage.Name = "labelCodeBlockLanguage";
            this.labelCodeBlockLanguage.Size = new System.Drawing.Size(31, 20);
            this.labelCodeBlockLanguage.TabIndex = 6;
            this.labelCodeBlockLanguage.Text = "n/a";
            // 
            // textBoxCode
            // 
            this.textBoxCode.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxCode.Font = new System.Drawing.Font("Consolas", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxCode.Location = new System.Drawing.Point(149, 74);
            this.textBoxCode.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxCode.Multiline = true;
            this.textBoxCode.Name = "textBoxCode";
            this.textBoxCode.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxCode.Size = new System.Drawing.Size(526, 145);
            this.textBoxCode.TabIndex = 7;
            this.textBoxCode.WordWrap = false;
            // 
            // listBoxResources
            // 
            this.listBoxResources.FormattingEnabled = true;
            this.listBoxResources.ItemHeight = 20;
            this.listBoxResources.Location = new System.Drawing.Point(10, 250);
            this.listBoxResources.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBoxResources.Name = "listBoxResources";
            this.listBoxResources.Size = new System.Drawing.Size(120, 84);
            this.listBoxResources.TabIndex = 9;
            this.listBoxResources.SelectedIndexChanged += new System.EventHandler(this.listBoxResources_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(7, 225);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(75, 20);
            this.label4.TabIndex = 8;
            this.label4.Text = "Resources";
            // 
            // textBoxResource
            // 
            this.textBoxResource.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxResource.Font = new System.Drawing.Font("Consolas", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxResource.Location = new System.Drawing.Point(149, 250);
            this.textBoxResource.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxResource.Multiline = true;
            this.textBoxResource.Name = "textBoxResource";
            this.textBoxResource.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxResource.Size = new System.Drawing.Size(526, 94);
            this.textBoxResource.TabIndex = 10;
            this.textBoxResource.WordWrap = false;
            // 
            // textBoxRequest
            // 
            this.textBoxRequest.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxRequest.Font = new System.Drawing.Font("Consolas", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxRequest.Location = new System.Drawing.Point(149, 384);
            this.textBoxRequest.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxRequest.Multiline = true;
            this.textBoxRequest.Name = "textBoxRequest";
            this.textBoxRequest.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxRequest.Size = new System.Drawing.Size(262, 128);
            this.textBoxRequest.TabIndex = 13;
            this.textBoxRequest.WordWrap = false;
            // 
            // listBoxMethods
            // 
            this.listBoxMethods.FormattingEnabled = true;
            this.listBoxMethods.ItemHeight = 20;
            this.listBoxMethods.Location = new System.Drawing.Point(10, 384);
            this.listBoxMethods.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBoxMethods.Name = "listBoxMethods";
            this.listBoxMethods.Size = new System.Drawing.Size(120, 124);
            this.listBoxMethods.TabIndex = 12;
            this.listBoxMethods.SelectedIndexChanged += new System.EventHandler(this.listBoxMethods_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(7, 359);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(67, 20);
            this.label5.TabIndex = 11;
            this.label5.Text = "Methods";
            // 
            // textBoxResponse
            // 
            this.textBoxResponse.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxResponse.Font = new System.Drawing.Font("Consolas", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxResponse.Location = new System.Drawing.Point(417, 384);
            this.textBoxResponse.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxResponse.Multiline = true;
            this.textBoxResponse.Name = "textBoxResponse";
            this.textBoxResponse.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxResponse.Size = new System.Drawing.Size(258, 128);
            this.textBoxResponse.TabIndex = 14;
            this.textBoxResponse.WordWrap = false;
            // 
            // DocFileEditorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.textBoxResponse);
            this.Controls.Add(this.textBoxRequest);
            this.Controls.Add(this.listBoxMethods);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBoxResource);
            this.Controls.Add(this.listBoxResources);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBoxCode);
            this.Controls.Add(this.labelCodeBlockLanguage);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.listBoxBlocks);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.buttonScanFile);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "DocFileEditorControl";
            this.Size = new System.Drawing.Size(692, 540);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonScanFile;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListBox listBoxBlocks;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label labelCodeBlockLanguage;
        private System.Windows.Forms.TextBox textBoxCode;
        private System.Windows.Forms.ListBox listBoxResources;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxResource;
        private System.Windows.Forms.TextBox textBoxRequest;
        private System.Windows.Forms.ListBox listBoxMethods;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBoxResponse;
    }
}
