namespace OneDrive.ApiDocumentation.Windows
{
    partial class SchemaValidatorForm
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
            this.label1 = new System.Windows.Forms.Label();
            this.comboBoxSchema = new System.Windows.Forms.ComboBox();
            this.checkBoxCollection = new System.Windows.Forms.CheckBox();
            this.textBoxJsonToValidate = new System.Windows.Forms.TextBox();
            this.buttonValidate = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Resource Type:";
            // 
            // comboBoxSchema
            // 
            this.comboBoxSchema.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSchema.FormattingEnabled = true;
            this.comboBoxSchema.Location = new System.Drawing.Point(122, 10);
            this.comboBoxSchema.Name = "comboBoxSchema";
            this.comboBoxSchema.Size = new System.Drawing.Size(268, 21);
            this.comboBoxSchema.TabIndex = 1;
            // 
            // checkBoxCollection
            // 
            this.checkBoxCollection.AutoSize = true;
            this.checkBoxCollection.Location = new System.Drawing.Point(396, 14);
            this.checkBoxCollection.Name = "checkBoxCollection";
            this.checkBoxCollection.Size = new System.Drawing.Size(127, 17);
            this.checkBoxCollection.TabIndex = 2;
            this.checkBoxCollection.Text = "Validate as Collection";
            this.checkBoxCollection.UseVisualStyleBackColor = true;
            // 
            // textBoxJsonToValidate
            // 
            this.textBoxJsonToValidate.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxJsonToValidate.Location = new System.Drawing.Point(12, 44);
            this.textBoxJsonToValidate.Multiline = true;
            this.textBoxJsonToValidate.Name = "textBoxJsonToValidate";
            this.textBoxJsonToValidate.Size = new System.Drawing.Size(641, 329);
            this.textBoxJsonToValidate.TabIndex = 3;
            // 
            // buttonValidate
            // 
            this.buttonValidate.Location = new System.Drawing.Point(13, 380);
            this.buttonValidate.Name = "buttonValidate";
            this.buttonValidate.Size = new System.Drawing.Size(75, 23);
            this.buttonValidate.TabIndex = 4;
            this.buttonValidate.Text = "Validate";
            this.buttonValidate.UseVisualStyleBackColor = true;
            this.buttonValidate.Click += new System.EventHandler(this.buttonValidate_Click);
            // 
            // SchemaValidatorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(665, 419);
            this.Controls.Add(this.buttonValidate);
            this.Controls.Add(this.textBoxJsonToValidate);
            this.Controls.Add(this.checkBoxCollection);
            this.Controls.Add(this.comboBoxSchema);
            this.Controls.Add(this.label1);
            this.Name = "SchemaValidatorForm";
            this.Text = "Schema Validator";
            this.Load += new System.EventHandler(this.SchemaValidator_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBoxSchema;
        private System.Windows.Forms.CheckBox checkBoxCollection;
        private System.Windows.Forms.TextBox textBoxJsonToValidate;
        private System.Windows.Forms.Button buttonValidate;
    }
}