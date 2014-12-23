using OneDrive.ApiDocumentation.Validation;
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
    public partial class ErrorDisplayForm : Form
    {

        public ErrorDisplayForm()
        {
            InitializeComponent();
        }

        public ErrorDisplayForm(string title, string message) : this()
        {
            Text = title;
            textBoxErrors.Text = message;
            textBoxErrors.WordWrap = false;
        }
        
        public ErrorDisplayForm(ValidationError[] errors) : this()
        {
            textBoxErrors.Text = "The following errors were detetected:\r\n";
            textBoxErrors.AppendText(errors.AllErrors());
            textBoxErrors.WordWrap = true;
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public static void ShowErrorDialog(ValidationError[] errors, IWin32Window owner = null)
        {
            ErrorDisplayForm form = new ErrorDisplayForm(errors);
            form.Show(owner);
        }
    }
}
