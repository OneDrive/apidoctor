namespace OneDrive.ApiDocumentation.Windows
{
    partial class ScenarioPlaceholderEditor
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
            this.textBoxValueOrPath = new System.Windows.Forms.TextBox();
            this.labelValueOrPath = new System.Windows.Forms.Label();
            this.comboBoxLocation = new System.Windows.Forms.ComboBox();
            this.textBoxName = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.comboBoxPathTarget = new System.Windows.Forms.ComboBox();
            this.labelTarget = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // textBoxValueOrPath
            // 
            this.textBoxValueOrPath.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxValueOrPath.Location = new System.Drawing.Point(91, 69);
            this.textBoxValueOrPath.Margin = new System.Windows.Forms.Padding(2);
            this.textBoxValueOrPath.Multiline = true;
            this.textBoxValueOrPath.Name = "textBoxValueOrPath";
            this.textBoxValueOrPath.Size = new System.Drawing.Size(459, 149);
            this.textBoxValueOrPath.TabIndex = 20;
            this.textBoxValueOrPath.TextChanged += new System.EventHandler(this.ScenarioField_TextChanged);
            // 
            // labelValueOrPath
            // 
            this.labelValueOrPath.AutoSize = true;
            this.labelValueOrPath.Location = new System.Drawing.Point(-3, 69);
            this.labelValueOrPath.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelValueOrPath.Name = "labelValueOrPath";
            this.labelValueOrPath.Size = new System.Drawing.Size(49, 20);
            this.labelValueOrPath.TabIndex = 19;
            this.labelValueOrPath.Text = "Value:";
            // 
            // comboBoxLocation
            // 
            this.comboBoxLocation.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxLocation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLocation.FormattingEnabled = true;
            this.comboBoxLocation.Location = new System.Drawing.Point(427, 2);
            this.comboBoxLocation.Margin = new System.Windows.Forms.Padding(2);
            this.comboBoxLocation.Name = "comboBoxLocation";
            this.comboBoxLocation.Size = new System.Drawing.Size(123, 28);
            this.comboBoxLocation.TabIndex = 18;
            this.comboBoxLocation.TextChanged += new System.EventHandler(this.ScenarioField_TextChanged);
            // 
            // textBoxName
            // 
            this.textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxName.Location = new System.Drawing.Point(91, 2);
            this.textBoxName.Margin = new System.Windows.Forms.Padding(2);
            this.textBoxName.Name = "textBoxName";
            this.textBoxName.Size = new System.Drawing.Size(332, 27);
            this.textBoxName.TabIndex = 16;
            this.textBoxName.TextChanged += new System.EventHandler(this.ScenarioField_TextChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(-3, 6);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(90, 20);
            this.label4.TabIndex = 15;
            this.label4.Text = "Placeholder:";
            // 
            // comboBoxPathTarget
            // 
            this.comboBoxPathTarget.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxPathTarget.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPathTarget.FormattingEnabled = true;
            this.comboBoxPathTarget.Location = new System.Drawing.Point(91, 34);
            this.comboBoxPathTarget.Margin = new System.Windows.Forms.Padding(2);
            this.comboBoxPathTarget.Name = "comboBoxPathTarget";
            this.comboBoxPathTarget.Size = new System.Drawing.Size(460, 28);
            this.comboBoxPathTarget.TabIndex = 22;
            this.comboBoxPathTarget.SelectedIndexChanged += new System.EventHandler(this.ScenarioField_TextChanged);
            // 
            // labelTarget
            // 
            this.labelTarget.AutoSize = true;
            this.labelTarget.Location = new System.Drawing.Point(-4, 37);
            this.labelTarget.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelTarget.Name = "labelTarget";
            this.labelTarget.Size = new System.Drawing.Size(55, 20);
            this.labelTarget.TabIndex = 21;
            this.labelTarget.Text = "Target:";
            // 
            // ScenarioPlaceholderEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.comboBoxPathTarget);
            this.Controls.Add(this.labelTarget);
            this.Controls.Add(this.textBoxValueOrPath);
            this.Controls.Add(this.labelValueOrPath);
            this.Controls.Add(this.comboBoxLocation);
            this.Controls.Add(this.textBoxName);
            this.Controls.Add(this.label4);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimumSize = new System.Drawing.Size(255, 100);
            this.Name = "ScenarioPlaceholderEditor";
            this.Size = new System.Drawing.Size(553, 229);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxValueOrPath;
        private System.Windows.Forms.Label labelValueOrPath;
        private System.Windows.Forms.ComboBox comboBoxLocation;
        private System.Windows.Forms.TextBox textBoxName;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox comboBoxPathTarget;
        private System.Windows.Forms.Label labelTarget;
    }
}
