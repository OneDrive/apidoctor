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

        List<Resource> m_Resources = new List<Resource>();
        List<RequestResponse> m_Methods = new List<RequestResponse>();

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
                Properties.Settings.Default.LastOpenedPath = dialog.SelectedPath;
                Properties.Settings.Default.Save();
                OpenFolder(dialog.SelectedPath);
            }
        }

        private void OpenFolder(string path)
        {
            CurrentDocSet = new DocSet(path);
            listBoxDocuments.DisplayMember = "DisplayName";
            listBoxDocuments.DataSource = CurrentDocSet.Files;

            foreach (var file in CurrentDocSet.Files)
            {
                file.Scan();

                if (file.Resources.Count > 0)
                    m_Resources.AddRange(file.Resources.Values);
                if (file.Requests.Length > 0)
                    m_Methods.AddRange(file.Requests);
            }

            listBoxResources.DisplayMember = "OdataType";
            listBoxResources.DataSource = m_Resources;

            listBoxMethods.DisplayMember = "DisplayName";
            listBoxMethods.DataSource = m_Methods;
        }

        private void listBoxDocuments_SelectedIndexChanged(object sender, EventArgs e)
        {
            //LoadDocumentIntoEditor(((ListBox)sender).SelectedItem as DocFile);
        }

        private void LoadDocumentIntoEditor(DocFile docFile)
        {
            //docFileEditorControl1.LoadFile(docFile);
        }

        private void openLastFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var lastPath = Properties.Settings.Default.LastOpenedPath;
            if (!string.IsNullOrEmpty(lastPath))
            {
                OpenFolder(lastPath);
            }
        }

        private void listBoxResources_SelectedIndexChanged(object sender, EventArgs e)
        {
            var resource = ((ListBox)sender).SelectedItem as Resource;
            textBox1.Text = resource.JsonFormat;
        }

        private void listBoxMethods_SelectedIndexChanged(object sender, EventArgs e)
        {
            var method = ((ListBox)sender).SelectedItem as RequestResponse;
            
            string format = @"{0}

-----Response: {2}--------

{1}";

            textBox1.Text = string.Format(format, method.Request, method.Response, method.ResponseType);
        }
    }
}
