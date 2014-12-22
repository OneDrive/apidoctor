using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OneDrive.ApiDocumentation.Validation;

namespace ApiDocumentationTester
{
    public partial class DocFileEditorControl : UserControl
    {
        public DocFileEditorControl()
        {
            InitializeComponent();
        }

        public DocFile CurrentFile { get; private set; }

        public void LoadFile(DocFile file)
        {
            CurrentFile = file;
        }

        private void comboBoxFileType_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void buttonScanFile_Click(object sender, EventArgs e)
        {
            ScanFile();
        }

        private void ScanFile()
        {
            CurrentFile.Scan();

            listBoxBlocks.DisplayMember = "Content";
            listBoxBlocks.DataSource = CurrentFile.CodeBlocks;

            listBoxResources.DisplayMember = "OdataType";
            var resources = CurrentFile.Resources;
            listBoxResources.DataSource = resources;

            listBoxMethods.DisplayMember = "DisplayName";
            listBoxMethods.DataSource = CurrentFile.Requests;
        }

        private void listBoxBlocks_SelectedIndexChanged(object sender, EventArgs e)
        {
            MarkdownDeep.Block selectedBlock = ((ListBox)sender).SelectedItem as MarkdownDeep.Block;

            textBoxCode.Text = selectedBlock.Content;
            labelCodeBlockLanguage.Text = selectedBlock.CodeLanguage;
        }

        private void listBoxResources_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedResource = (ResourceDefinition)((ListBox)sender).SelectedValue;
            textBoxResource.Text = selectedResource.JsonExample;
        }

        private void listBoxMethods_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedMethod = (MethodDefinition)((ListBox)sender).SelectedValue;
            textBoxRequest.Text = selectedMethod.Request;
            textBoxResponse.Text = selectedMethod.Response;
        }
    }
}
