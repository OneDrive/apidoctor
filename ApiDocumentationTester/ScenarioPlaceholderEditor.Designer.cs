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
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxName = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // textBoxValueOrPath
            // 
            this.textBoxValueOrPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxValueOrPath.Location = new System.Drawing.Point(78, 67);
            this.textBoxValueOrPath.Margin = new System.Windows.Forms.Padding(2);
            this.textBoxValueOrPath.Name = "textBoxValueOrPath";
            this.textBoxValueOrPath.Size = new System.Drawing.Size(229, 23);
            this.textBoxValueOrPath.TabIndex = 20;
            this.textBoxValueOrPath.TextChanged += new System.EventHandler(this.ScenarioField_TextChanged);
            // 
            // labelValueOrPath
            // 
            this.labelValueOrPath.AutoSize = true;
            this.labelValueOrPath.Location = new System.Drawing.Point(-2, 70);
            this.labelValueOrPath.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelValueOrPath.Name = "labelValueOrPath";
            this.labelValueOrPath.Size = new System.Drawing.Size(39, 15);
            this.labelValueOrPath.TabIndex = 19;
            this.labelValueOrPath.Text = "Value:";
            // 
            // comboBoxLocation
            // 
            this.comboBoxLocation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLocation.FormattingEnabled = true;
            this.comboBoxLocation.Location = new System.Drawing.Point(78, 35);
            this.comboBoxLocation.Margin = new System.Windows.Forms.Padding(2);
            this.comboBoxLocation.Name = "comboBoxLocation";
            this.comboBoxLocation.Size = new System.Drawing.Size(174, 23);
            this.comboBoxLocation.TabIndex = 18;
            this.comboBoxLocation.TextChanged += new System.EventHandler(this.ScenarioField_TextChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(-3, 38);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(56, 15);
            this.label5.TabIndex = 17;
            this.label5.Text = "Location:";
            // 
            // textBoxName
            // 
            this.textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxName.Location = new System.Drawing.Point(78, 2);
            this.textBoxName.Margin = new System.Windows.Forms.Padding(2);
            this.textBoxName.Name = "textBoxName";
            this.textBoxName.Size = new System.Drawing.Size(229, 23);
            this.textBoxName.TabIndex = 16;
            this.textBoxName.TextChanged += new System.EventHandler(this.ScenarioField_TextChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(-3, 6);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(72, 15);
            this.label4.TabIndex = 15;
            this.label4.Text = "Placeholder:";
            // 
            // ScenarioPlaceholderEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.textBoxValueOrPath);
            this.Controls.Add(this.labelValueOrPath);
            this.Controls.Add(this.comboBoxLocation);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBoxName);
            this.Controls.Add(this.label4);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimumSize = new System.Drawing.Size(255, 100);
            this.Name = "ScenarioPlaceholderEditor";
            this.Size = new System.Drawing.Size(310, 100);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxValueOrPath;
        private System.Windows.Forms.Label labelValueOrPath;
        private System.Windows.Forms.ComboBox comboBoxLocation;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBoxName;
        private System.Windows.Forms.Label label4;
    }
}
