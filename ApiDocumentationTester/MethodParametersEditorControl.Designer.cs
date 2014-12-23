namespace OneDrive.ApiDocumentation.Windows
{
    partial class MethodParametersEditorControl
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
            this.comboBoxMethod = new System.Windows.Forms.ComboBox();
            this.checkBoxEnabled = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxNotes = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.listBoxParameters = new System.Windows.Forms.ListBox();
            this.buttonNewParameter = new System.Windows.Forms.Button();
            this.buttonDeleteParameter = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxParamName = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.comboBoxParamLocation = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textBoxParamValue = new System.Windows.Forms.TextBox();
            this.buttonPreviewMethod = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 4);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Method:";
            // 
            // comboBoxMethod
            // 
            this.comboBoxMethod.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxMethod.DisplayMember = "Method";
            this.comboBoxMethod.FormattingEnabled = true;
            this.comboBoxMethod.Location = new System.Drawing.Point(69, 3);
            this.comboBoxMethod.Name = "comboBoxMethod";
            this.comboBoxMethod.Size = new System.Drawing.Size(537, 24);
            this.comboBoxMethod.TabIndex = 1;
            this.comboBoxMethod.TextChanged += new System.EventHandler(this.requestParameterField_TextChanged);
            // 
            // checkBoxEnabled
            // 
            this.checkBoxEnabled.AutoSize = true;
            this.checkBoxEnabled.Location = new System.Drawing.Point(69, 109);
            this.checkBoxEnabled.Name = "checkBoxEnabled";
            this.checkBoxEnabled.Size = new System.Drawing.Size(82, 21);
            this.checkBoxEnabled.TabIndex = 2;
            this.checkBoxEnabled.Text = "Enabled";
            this.checkBoxEnabled.UseVisualStyleBackColor = true;
            this.checkBoxEnabled.CheckedChanged += new System.EventHandler(this.checkBoxEnabled_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(4, 33);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 17);
            this.label2.TabIndex = 3;
            this.label2.Text = "Notes:";
            // 
            // textBoxNotes
            // 
            this.textBoxNotes.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxNotes.Location = new System.Drawing.Point(69, 33);
            this.textBoxNotes.Multiline = true;
            this.textBoxNotes.Name = "textBoxNotes";
            this.textBoxNotes.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxNotes.Size = new System.Drawing.Size(621, 67);
            this.textBoxNotes.TabIndex = 4;
            this.textBoxNotes.TextChanged += new System.EventHandler(this.requestParameterField_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(4, 133);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(85, 17);
            this.label3.TabIndex = 5;
            this.label3.Text = "Parameters:";
            // 
            // listBoxParameters
            // 
            this.listBoxParameters.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.listBoxParameters.FormattingEnabled = true;
            this.listBoxParameters.ItemHeight = 16;
            this.listBoxParameters.Location = new System.Drawing.Point(7, 153);
            this.listBoxParameters.Name = "listBoxParameters";
            this.listBoxParameters.Size = new System.Drawing.Size(170, 164);
            this.listBoxParameters.TabIndex = 6;
            this.listBoxParameters.SelectedIndexChanged += new System.EventHandler(this.listBoxParameters_SelectedIndexChanged);
            // 
            // buttonNewParameter
            // 
            this.buttonNewParameter.Location = new System.Drawing.Point(275, 239);
            this.buttonNewParameter.Name = "buttonNewParameter";
            this.buttonNewParameter.Size = new System.Drawing.Size(82, 28);
            this.buttonNewParameter.TabIndex = 7;
            this.buttonNewParameter.Text = "New";
            this.buttonNewParameter.UseVisualStyleBackColor = true;
            this.buttonNewParameter.Click += new System.EventHandler(this.buttonNewParameter_Click);
            // 
            // buttonDeleteParameter
            // 
            this.buttonDeleteParameter.Location = new System.Drawing.Point(363, 239);
            this.buttonDeleteParameter.Name = "buttonDeleteParameter";
            this.buttonDeleteParameter.Size = new System.Drawing.Size(82, 28);
            this.buttonDeleteParameter.TabIndex = 8;
            this.buttonDeleteParameter.Text = "Delete";
            this.buttonDeleteParameter.UseVisualStyleBackColor = true;
            this.buttonDeleteParameter.Click += new System.EventHandler(this.buttonDeleteParameter_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(192, 153);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(49, 17);
            this.label4.TabIndex = 9;
            this.label4.Text = "Name:";
            // 
            // textBoxParamName
            // 
            this.textBoxParamName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxParamName.Location = new System.Drawing.Point(275, 153);
            this.textBoxParamName.Name = "textBoxParamName";
            this.textBoxParamName.Size = new System.Drawing.Size(415, 22);
            this.textBoxParamName.TabIndex = 10;
            this.textBoxParamName.TextChanged += new System.EventHandler(this.parameterValue_TextChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(192, 184);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(66, 17);
            this.label5.TabIndex = 11;
            this.label5.Text = "Location:";
            // 
            // comboBoxParamLocation
            // 
            this.comboBoxParamLocation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxParamLocation.FormattingEnabled = true;
            this.comboBoxParamLocation.Location = new System.Drawing.Point(275, 181);
            this.comboBoxParamLocation.Name = "comboBoxParamLocation";
            this.comboBoxParamLocation.Size = new System.Drawing.Size(415, 24);
            this.comboBoxParamLocation.TabIndex = 12;
            this.comboBoxParamLocation.SelectedIndexChanged += new System.EventHandler(this.comboBoxParamLocation_SelectedIndexChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(192, 214);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(48, 17);
            this.label6.TabIndex = 13;
            this.label6.Text = "Value:";
            // 
            // textBoxParamValue
            // 
            this.textBoxParamValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxParamValue.Location = new System.Drawing.Point(275, 211);
            this.textBoxParamValue.Name = "textBoxParamValue";
            this.textBoxParamValue.Size = new System.Drawing.Size(415, 22);
            this.textBoxParamValue.TabIndex = 14;
            this.textBoxParamValue.TextChanged += new System.EventHandler(this.parameterValue_TextChanged);
            // 
            // buttonPreviewMethod
            // 
            this.buttonPreviewMethod.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonPreviewMethod.Location = new System.Drawing.Point(612, 0);
            this.buttonPreviewMethod.Name = "buttonPreviewMethod";
            this.buttonPreviewMethod.Size = new System.Drawing.Size(78, 30);
            this.buttonPreviewMethod.TabIndex = 15;
            this.buttonPreviewMethod.Text = "Preview";
            this.buttonPreviewMethod.UseVisualStyleBackColor = true;
            this.buttonPreviewMethod.Click += new System.EventHandler(this.buttonPreviewMethod_Click);
            // 
            // MethodParametersEditorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.buttonPreviewMethod);
            this.Controls.Add(this.textBoxParamValue);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.comboBoxParamLocation);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBoxParamName);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.buttonDeleteParameter);
            this.Controls.Add(this.buttonNewParameter);
            this.Controls.Add(this.listBoxParameters);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxNotes);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.checkBoxEnabled);
            this.Controls.Add(this.comboBoxMethod);
            this.Controls.Add(this.label1);
            this.MinimumSize = new System.Drawing.Size(0, 295);
            this.Name = "MethodParametersEditorControl";
            this.Size = new System.Drawing.Size(697, 340);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBoxMethod;
        private System.Windows.Forms.CheckBox checkBoxEnabled;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxNotes;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ListBox listBoxParameters;
        private System.Windows.Forms.Button buttonNewParameter;
        private System.Windows.Forms.Button buttonDeleteParameter;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxParamName;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox comboBoxParamLocation;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBoxParamValue;
        private System.Windows.Forms.Button buttonPreviewMethod;
    }
}
