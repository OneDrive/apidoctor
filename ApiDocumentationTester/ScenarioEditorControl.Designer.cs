namespace OneDrive.ApiDocumentation.Windows
{
    partial class ScenarioEditorControl
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
            this.textBoxScenarioName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.listBoxStaticPlaceholders = new System.Windows.Forms.ListBox();
            this.buttonNewParameter = new System.Windows.Forms.Button();
            this.buttonDeleteParameter = new System.Windows.Forms.Button();
            this.buttonPreviewMethod = new System.Windows.Forms.Button();
            this.tabControlPlaceholders = new System.Windows.Forms.TabControl();
            this.tabPageStaticValues = new System.Windows.Forms.TabPage();
            this.tabPageRequestValues = new System.Windows.Forms.TabPage();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxHttpRequest = new System.Windows.Forms.TextBox();
            this.listBoxDynamicPlaceholders = new System.Windows.Forms.ListBox();
            this.buttonNewDynmaicPlaceholder = new System.Windows.Forms.Button();
            this.buttonDeleteDynamicPlaceholder = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.staticPlaceholderEditor = new OneDrive.ApiDocumentation.Windows.ScenarioPlaceholderEditor();
            this.dynamicPlaceholderEditor = new OneDrive.ApiDocumentation.Windows.ScenarioPlaceholderEditor();
            this.checkBoxEnableDynamicRequest = new System.Windows.Forms.CheckBox();
            this.tabControlPlaceholders.SuspendLayout();
            this.tabPageStaticValues.SuspendLayout();
            this.tabPageRequestValues.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 32);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(52, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Method:";
            // 
            // comboBoxMethod
            // 
            this.comboBoxMethod.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxMethod.DisplayMember = "Method";
            this.comboBoxMethod.FormattingEnabled = true;
            this.comboBoxMethod.Location = new System.Drawing.Point(61, 31);
            this.comboBoxMethod.Margin = new System.Windows.Forms.Padding(2);
            this.comboBoxMethod.Name = "comboBoxMethod";
            this.comboBoxMethod.Size = new System.Drawing.Size(648, 23);
            this.comboBoxMethod.TabIndex = 1;
            this.comboBoxMethod.TextChanged += new System.EventHandler(this.requestParameterField_TextChanged);
            // 
            // checkBoxEnabled
            // 
            this.checkBoxEnabled.AutoSize = true;
            this.checkBoxEnabled.Location = new System.Drawing.Point(61, 58);
            this.checkBoxEnabled.Margin = new System.Windows.Forms.Padding(2);
            this.checkBoxEnabled.Name = "checkBoxEnabled";
            this.checkBoxEnabled.Size = new System.Drawing.Size(68, 19);
            this.checkBoxEnabled.TabIndex = 2;
            this.checkBoxEnabled.Text = "Enabled";
            this.checkBoxEnabled.UseVisualStyleBackColor = true;
            this.checkBoxEnabled.CheckedChanged += new System.EventHandler(this.checkBoxEnabled_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 4);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(42, 15);
            this.label2.TabIndex = 3;
            this.label2.Text = "Name:";
            // 
            // textBoxScenarioName
            // 
            this.textBoxScenarioName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxScenarioName.Location = new System.Drawing.Point(61, 4);
            this.textBoxScenarioName.Margin = new System.Windows.Forms.Padding(2);
            this.textBoxScenarioName.Name = "textBoxScenarioName";
            this.textBoxScenarioName.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxScenarioName.Size = new System.Drawing.Size(648, 23);
            this.textBoxScenarioName.TabIndex = 4;
            this.textBoxScenarioName.TextChanged += new System.EventHandler(this.requestParameterField_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 7);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(77, 15);
            this.label3.TabIndex = 5;
            this.label3.Text = "Placeholders:";
            // 
            // listBoxStaticPlaceholders
            // 
            this.listBoxStaticPlaceholders.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.listBoxStaticPlaceholders.FormattingEnabled = true;
            this.listBoxStaticPlaceholders.ItemHeight = 15;
            this.listBoxStaticPlaceholders.Location = new System.Drawing.Point(11, 32);
            this.listBoxStaticPlaceholders.Margin = new System.Windows.Forms.Padding(2);
            this.listBoxStaticPlaceholders.Name = "listBoxStaticPlaceholders";
            this.listBoxStaticPlaceholders.Size = new System.Drawing.Size(153, 169);
            this.listBoxStaticPlaceholders.TabIndex = 6;
            this.listBoxStaticPlaceholders.SelectedIndexChanged += new System.EventHandler(this.listBoxStaticPlaceholders_SelectedIndexChanged);
            // 
            // buttonNewParameter
            // 
            this.buttonNewParameter.Location = new System.Drawing.Point(168, 137);
            this.buttonNewParameter.Margin = new System.Windows.Forms.Padding(2);
            this.buttonNewParameter.Name = "buttonNewParameter";
            this.buttonNewParameter.Size = new System.Drawing.Size(72, 27);
            this.buttonNewParameter.TabIndex = 7;
            this.buttonNewParameter.Text = "New";
            this.buttonNewParameter.UseVisualStyleBackColor = true;
            this.buttonNewParameter.Click += new System.EventHandler(this.buttonNewParameter_Click);
            // 
            // buttonDeleteParameter
            // 
            this.buttonDeleteParameter.Location = new System.Drawing.Point(245, 137);
            this.buttonDeleteParameter.Margin = new System.Windows.Forms.Padding(2);
            this.buttonDeleteParameter.Name = "buttonDeleteParameter";
            this.buttonDeleteParameter.Size = new System.Drawing.Size(72, 27);
            this.buttonDeleteParameter.TabIndex = 8;
            this.buttonDeleteParameter.Text = "Delete";
            this.buttonDeleteParameter.UseVisualStyleBackColor = true;
            this.buttonDeleteParameter.Click += new System.EventHandler(this.buttonDeleteParameter_Click);
            // 
            // buttonPreviewMethod
            // 
            this.buttonPreviewMethod.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonPreviewMethod.Location = new System.Drawing.Point(713, 4);
            this.buttonPreviewMethod.Margin = new System.Windows.Forms.Padding(2);
            this.buttonPreviewMethod.Name = "buttonPreviewMethod";
            this.buttonPreviewMethod.Size = new System.Drawing.Size(68, 23);
            this.buttonPreviewMethod.TabIndex = 15;
            this.buttonPreviewMethod.Text = "Preview";
            this.buttonPreviewMethod.UseVisualStyleBackColor = true;
            this.buttonPreviewMethod.Click += new System.EventHandler(this.buttonPreviewMethod_Click);
            // 
            // tabControlPlaceholders
            // 
            this.tabControlPlaceholders.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControlPlaceholders.Controls.Add(this.tabPageStaticValues);
            this.tabControlPlaceholders.Controls.Add(this.tabPageRequestValues);
            this.tabControlPlaceholders.Location = new System.Drawing.Point(6, 98);
            this.tabControlPlaceholders.Name = "tabControlPlaceholders";
            this.tabControlPlaceholders.SelectedIndex = 0;
            this.tabControlPlaceholders.Size = new System.Drawing.Size(775, 313);
            this.tabControlPlaceholders.TabIndex = 17;
            // 
            // tabPageStaticValues
            // 
            this.tabPageStaticValues.Controls.Add(this.listBoxStaticPlaceholders);
            this.tabPageStaticValues.Controls.Add(this.staticPlaceholderEditor);
            this.tabPageStaticValues.Controls.Add(this.label3);
            this.tabPageStaticValues.Controls.Add(this.buttonNewParameter);
            this.tabPageStaticValues.Controls.Add(this.buttonDeleteParameter);
            this.tabPageStaticValues.Location = new System.Drawing.Point(4, 24);
            this.tabPageStaticValues.Name = "tabPageStaticValues";
            this.tabPageStaticValues.Size = new System.Drawing.Size(767, 285);
            this.tabPageStaticValues.TabIndex = 0;
            this.tabPageStaticValues.Text = "Static Values";
            this.tabPageStaticValues.UseVisualStyleBackColor = true;
            // 
            // tabPageRequestValues
            // 
            this.tabPageRequestValues.Controls.Add(this.label5);
            this.tabPageRequestValues.Controls.Add(this.listBoxDynamicPlaceholders);
            this.tabPageRequestValues.Controls.Add(this.dynamicPlaceholderEditor);
            this.tabPageRequestValues.Controls.Add(this.buttonNewDynmaicPlaceholder);
            this.tabPageRequestValues.Controls.Add(this.buttonDeleteDynamicPlaceholder);
            this.tabPageRequestValues.Controls.Add(this.textBoxHttpRequest);
            this.tabPageRequestValues.Controls.Add(this.label4);
            this.tabPageRequestValues.Controls.Add(this.checkBoxEnableDynamicRequest);
            this.tabPageRequestValues.Location = new System.Drawing.Point(4, 24);
            this.tabPageRequestValues.Name = "tabPageRequestValues";
            this.tabPageRequestValues.Size = new System.Drawing.Size(767, 285);
            this.tabPageRequestValues.TabIndex = 1;
            this.tabPageRequestValues.Text = "Request Values";
            this.tabPageRequestValues.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 7);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(85, 15);
            this.label4.TabIndex = 0;
            this.label4.Text = "HTTP Request:";
            // 
            // textBoxHttpRequest
            // 
            this.textBoxHttpRequest.Location = new System.Drawing.Point(11, 25);
            this.textBoxHttpRequest.Multiline = true;
            this.textBoxHttpRequest.Name = "textBoxHttpRequest";
            this.textBoxHttpRequest.Size = new System.Drawing.Size(336, 65);
            this.textBoxHttpRequest.TabIndex = 1;
            // 
            // listBoxDynamicPlaceholders
            // 
            this.listBoxDynamicPlaceholders.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.listBoxDynamicPlaceholders.FormattingEnabled = true;
            this.listBoxDynamicPlaceholders.ItemHeight = 15;
            this.listBoxDynamicPlaceholders.Location = new System.Drawing.Point(11, 117);
            this.listBoxDynamicPlaceholders.Margin = new System.Windows.Forms.Padding(2);
            this.listBoxDynamicPlaceholders.Name = "listBoxDynamicPlaceholders";
            this.listBoxDynamicPlaceholders.Size = new System.Drawing.Size(153, 154);
            this.listBoxDynamicPlaceholders.TabIndex = 17;
            this.listBoxDynamicPlaceholders.SelectedIndexChanged += new System.EventHandler(this.listBoxDynamicPlaceholders_SelectedIndexChanged);
            // 
            // buttonNewDynmaicPlaceholder
            // 
            this.buttonNewDynmaicPlaceholder.Location = new System.Drawing.Point(175, 222);
            this.buttonNewDynmaicPlaceholder.Margin = new System.Windows.Forms.Padding(2);
            this.buttonNewDynmaicPlaceholder.Name = "buttonNewDynmaicPlaceholder";
            this.buttonNewDynmaicPlaceholder.Size = new System.Drawing.Size(72, 27);
            this.buttonNewDynmaicPlaceholder.TabIndex = 18;
            this.buttonNewDynmaicPlaceholder.Text = "New";
            this.buttonNewDynmaicPlaceholder.UseVisualStyleBackColor = true;
            this.buttonNewDynmaicPlaceholder.Click += new System.EventHandler(this.buttonNewDynmaicPlaceholder_Click);
            // 
            // buttonDeleteDynamicPlaceholder
            // 
            this.buttonDeleteDynamicPlaceholder.Location = new System.Drawing.Point(252, 222);
            this.buttonDeleteDynamicPlaceholder.Margin = new System.Windows.Forms.Padding(2);
            this.buttonDeleteDynamicPlaceholder.Name = "buttonDeleteDynamicPlaceholder";
            this.buttonDeleteDynamicPlaceholder.Size = new System.Drawing.Size(72, 27);
            this.buttonDeleteDynamicPlaceholder.TabIndex = 19;
            this.buttonDeleteDynamicPlaceholder.Text = "Delete";
            this.buttonDeleteDynamicPlaceholder.UseVisualStyleBackColor = true;
            this.buttonDeleteDynamicPlaceholder.Click += new System.EventHandler(this.buttonDeleteDynamicPlaceholder_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(8, 100);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(77, 15);
            this.label5.TabIndex = 21;
            this.label5.Text = "Placeholders:";
            // 
            // staticPlaceholderEditor
            // 
            this.staticPlaceholderEditor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.staticPlaceholderEditor.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.staticPlaceholderEditor.Location = new System.Drawing.Point(172, 32);
            this.staticPlaceholderEditor.MinimumSize = new System.Drawing.Size(255, 100);
            this.staticPlaceholderEditor.Name = "staticPlaceholderEditor";
            this.staticPlaceholderEditor.Size = new System.Drawing.Size(568, 100);
            this.staticPlaceholderEditor.TabIndex = 16;
            // 
            // dynamicPlaceholderEditor
            // 
            this.dynamicPlaceholderEditor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dynamicPlaceholderEditor.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dynamicPlaceholderEditor.Location = new System.Drawing.Point(179, 117);
            this.dynamicPlaceholderEditor.MinimumSize = new System.Drawing.Size(255, 100);
            this.dynamicPlaceholderEditor.Name = "dynamicPlaceholderEditor";
            this.dynamicPlaceholderEditor.Size = new System.Drawing.Size(491, 100);
            this.dynamicPlaceholderEditor.TabIndex = 20;
            this.dynamicPlaceholderEditor.PlaceholderChanged += new System.EventHandler(this.dynamicPlaceholderEditor_PlaceholderChanged);
            // 
            // checkBoxEnableDynamicRequest
            // 
            this.checkBoxEnableDynamicRequest.AutoSize = true;
            this.checkBoxEnableDynamicRequest.Location = new System.Drawing.Point(99, 7);
            this.checkBoxEnableDynamicRequest.Name = "checkBoxEnableDynamicRequest";
            this.checkBoxEnableDynamicRequest.Size = new System.Drawing.Size(68, 19);
            this.checkBoxEnableDynamicRequest.TabIndex = 22;
            this.checkBoxEnableDynamicRequest.Text = "Enabled";
            this.checkBoxEnableDynamicRequest.UseVisualStyleBackColor = true;
            this.checkBoxEnableDynamicRequest.CheckedChanged += new System.EventHandler(this.checkBoxEnableDynamicRequest_CheckedChanged);
            // 
            // ScenarioEditorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControlPlaceholders);
            this.Controls.Add(this.buttonPreviewMethod);
            this.Controls.Add(this.textBoxScenarioName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.checkBoxEnabled);
            this.Controls.Add(this.comboBoxMethod);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MinimumSize = new System.Drawing.Size(0, 277);
            this.Name = "ScenarioEditorControl";
            this.Size = new System.Drawing.Size(787, 414);
            this.tabControlPlaceholders.ResumeLayout(false);
            this.tabPageStaticValues.ResumeLayout(false);
            this.tabPageStaticValues.PerformLayout();
            this.tabPageRequestValues.ResumeLayout(false);
            this.tabPageRequestValues.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBoxMethod;
        private System.Windows.Forms.CheckBox checkBoxEnabled;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxScenarioName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ListBox listBoxStaticPlaceholders;
        private System.Windows.Forms.Button buttonNewParameter;
        private System.Windows.Forms.Button buttonDeleteParameter;
        private System.Windows.Forms.Button buttonPreviewMethod;
        private ScenarioPlaceholderEditor staticPlaceholderEditor;
        private System.Windows.Forms.TabControl tabControlPlaceholders;
        private System.Windows.Forms.TabPage tabPageStaticValues;
        private System.Windows.Forms.TabPage tabPageRequestValues;
        private System.Windows.Forms.TextBox textBoxHttpRequest;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ListBox listBoxDynamicPlaceholders;
        private ScenarioPlaceholderEditor dynamicPlaceholderEditor;
        private System.Windows.Forms.Button buttonNewDynmaicPlaceholder;
        private System.Windows.Forms.Button buttonDeleteDynamicPlaceholder;
        private System.Windows.Forms.CheckBox checkBoxEnableDynamicRequest;
    }
}
