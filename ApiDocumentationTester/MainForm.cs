using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ApiDocumentationTester
{
    public partial class MainForm : Form
    {
        DocSet CurrentDocSet { get; set; }

        public MainForm()
        {
            InitializeComponent();
        }

        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog { ShowNewFolderButton = false };
            var result = dialog.ShowDialog();
            if (DialogResult.OK == result)
            {
                CurrentDocSet = new DocSet(dialog.SelectedPath);
                listBoxDocuments.DisplayMember = "DisplayName";
                listBoxDocuments.DataSource = CurrentDocSet.Files;
            }
        }

        private void listBoxDocuments_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadDocumentIntoEditor(((ListBox)sender).SelectedItem as DocFile);
        }

        private void LoadDocumentIntoEditor(DocFile docFile)
        {
            docFileEditorControl1.LoadFile(docFile);
        }
    }
}
