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
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listBoxDocuments = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.openLastFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.listBoxResources = new System.Windows.Forms.ListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.listBoxMethods = new System.Windows.Forms.ListBox();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1136, 24);
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
            this.listBoxDocuments.Size = new System.Drawing.Size(254, 454);
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
            // openLastFolderToolStripMenuItem
            // 
            this.openLastFolderToolStripMenuItem.Name = "openLastFolderToolStripMenuItem";
            this.openLastFolderToolStripMenuItem.Size = new System.Drawing.Size(163, 22);
            this.openLastFolderToolStripMenuItem.Text = "Open Last Folder";
            this.openLastFolderToolStripMenuItem.Click += new System.EventHandler(this.openLastFolderToolStripMenuItem_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(555, 76);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox1.Size = new System.Drawing.Size(440, 454);
            this.textBox1.TabIndex = 3;
            this.textBox1.WordWrap = false;
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
            this.listBoxResources.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
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
            this.listBoxMethods.Size = new System.Drawing.Size(254, 229);
            this.listBoxMethods.TabIndex = 7;
            this.listBoxMethods.SelectedIndexChanged += new System.EventHandler(this.listBoxMethods_SelectedIndexChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1136, 581);
            this.Controls.Add(this.listBoxMethods);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.listBoxResources);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBox1);
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
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListBox listBoxResources;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ListBox listBoxMethods;
    }
}

