namespace ApiDocumentationTester
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
            this.label1 = new System.Windows.Forms.Label();
            this.comboBoxFileType = new System.Windows.Forms.ComboBox();
            this.buttonScanFile = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.listBoxBlocks = new System.Windows.Forms.ListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.labelCodeBlockLanguage = new System.Windows.Forms.Label();
            this.textBoxCode = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 5);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Type:";
            // 
            // comboBoxFileType
            // 
            this.comboBoxFileType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxFileType.FormattingEnabled = true;
            this.comboBoxFileType.Location = new System.Drawing.Point(73, 1);
            this.comboBoxFileType.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.comboBoxFileType.Name = "comboBoxFileType";
            this.comboBoxFileType.Size = new System.Drawing.Size(198, 28);
            this.comboBoxFileType.TabIndex = 1;
            this.comboBoxFileType.SelectedIndexChanged += new System.EventHandler(this.comboBoxFileType_SelectedIndexChanged);
            // 
            // buttonScanFile
            // 
            this.buttonScanFile.Location = new System.Drawing.Point(307, 0);
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
            this.listBoxBlocks.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.listBoxBlocks.FormattingEnabled = true;
            this.listBoxBlocks.ItemHeight = 20;
            this.listBoxBlocks.Location = new System.Drawing.Point(10, 74);
            this.listBoxBlocks.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listBoxBlocks.Name = "listBoxBlocks";
            this.listBoxBlocks.Size = new System.Drawing.Size(120, 624);
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
            this.textBoxCode.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxCode.Font = new System.Drawing.Font("Consolas", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxCode.Location = new System.Drawing.Point(149, 74);
            this.textBoxCode.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxCode.Multiline = true;
            this.textBoxCode.Name = "textBoxCode";
            this.textBoxCode.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxCode.Size = new System.Drawing.Size(526, 624);
            this.textBoxCode.TabIndex = 7;
            this.textBoxCode.WordWrap = false;
            // 
            // DocFileEditorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.textBoxCode);
            this.Controls.Add(this.labelCodeBlockLanguage);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.listBoxBlocks);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.buttonScanFile);
            this.Controls.Add(this.comboBoxFileType);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "DocFileEditorControl";
            this.Size = new System.Drawing.Size(692, 710);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBoxFileType;
        private System.Windows.Forms.Button buttonScanFile;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListBox listBoxBlocks;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label labelCodeBlockLanguage;
        private System.Windows.Forms.TextBox textBoxCode;
    }
}
