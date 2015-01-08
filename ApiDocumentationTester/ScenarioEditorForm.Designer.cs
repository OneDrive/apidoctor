namespace OneDrive.ApiDocumentation.Windows
{
    partial class ScenarioEditorForm
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
            this.buttonSaveParameterFile = new System.Windows.Forms.Button();
            this.buttonDeleteParameters = new System.Windows.Forms.Button();
            this.buttonAddParameters = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.treeViewScenarios = new System.Windows.Forms.TreeView();
            this.scenarioEditorControl = new OneDrive.ApiDocumentation.Windows.ScenarioEditorControl();
            this.SuspendLayout();
            // 
            // buttonSaveParameterFile
            // 
            this.buttonSaveParameterFile.Location = new System.Drawing.Point(185, 28);
            this.buttonSaveParameterFile.Name = "buttonSaveParameterFile";
            this.buttonSaveParameterFile.Size = new System.Drawing.Size(80, 33);
            this.buttonSaveParameterFile.TabIndex = 17;
            this.buttonSaveParameterFile.Text = "Save";
            this.buttonSaveParameterFile.UseVisualStyleBackColor = true;
            this.buttonSaveParameterFile.Click += new System.EventHandler(this.buttonSaveParameterFile_Click);
            // 
            // buttonDeleteParameters
            // 
            this.buttonDeleteParameters.Location = new System.Drawing.Point(99, 28);
            this.buttonDeleteParameters.Name = "buttonDeleteParameters";
            this.buttonDeleteParameters.Size = new System.Drawing.Size(80, 33);
            this.buttonDeleteParameters.TabIndex = 16;
            this.buttonDeleteParameters.Text = "Delete";
            this.buttonDeleteParameters.UseVisualStyleBackColor = true;
            this.buttonDeleteParameters.Click += new System.EventHandler(this.buttonDeleteParameters_Click);
            // 
            // buttonAddParameters
            // 
            this.buttonAddParameters.Location = new System.Drawing.Point(14, 28);
            this.buttonAddParameters.Name = "buttonAddParameters";
            this.buttonAddParameters.Size = new System.Drawing.Size(80, 33);
            this.buttonAddParameters.TabIndex = 15;
            this.buttonAddParameters.Text = "Add";
            this.buttonAddParameters.UseVisualStyleBackColor = true;
            this.buttonAddParameters.Click += new System.EventHandler(this.buttonAddParameters_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(8, 5);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(54, 13);
            this.label7.TabIndex = 13;
            this.label7.Text = "Scenarios";
            // 
            // treeViewScenarios
            // 
            this.treeViewScenarios.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.treeViewScenarios.FullRowSelect = true;
            this.treeViewScenarios.HideSelection = false;
            this.treeViewScenarios.Location = new System.Drawing.Point(14, 67);
            this.treeViewScenarios.Name = "treeViewScenarios";
            this.treeViewScenarios.Size = new System.Drawing.Size(253, 483);
            this.treeViewScenarios.TabIndex = 19;
            this.treeViewScenarios.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewScenarios_AfterSelect);
            // 
            // scenarioEditorControl
            // 
            this.scenarioEditorControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.scenarioEditorControl.DocumentSet = null;
            this.scenarioEditorControl.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.scenarioEditorControl.Location = new System.Drawing.Point(272, 28);
            this.scenarioEditorControl.Margin = new System.Windows.Forms.Padding(2);
            this.scenarioEditorControl.MinimumSize = new System.Drawing.Size(0, 277);
            this.scenarioEditorControl.Name = "scenarioEditorControl";
            this.scenarioEditorControl.Size = new System.Drawing.Size(636, 523);
            this.scenarioEditorControl.TabIndex = 18;
            // 
            // ScenarioEditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(919, 562);
            this.Controls.Add(this.treeViewScenarios);
            this.Controls.Add(this.scenarioEditorControl);
            this.Controls.Add(this.buttonSaveParameterFile);
            this.Controls.Add(this.buttonDeleteParameters);
            this.Controls.Add(this.buttonAddParameters);
            this.Controls.Add(this.label7);
            this.Name = "ScenarioEditorForm";
            this.Text = "ScenarioEditorForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ScenarioEditorForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ScenarioEditorControl scenarioEditorControl;
        private System.Windows.Forms.Button buttonSaveParameterFile;
        private System.Windows.Forms.Button buttonDeleteParameters;
        private System.Windows.Forms.Button buttonAddParameters;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TreeView treeViewScenarios;
    }
}