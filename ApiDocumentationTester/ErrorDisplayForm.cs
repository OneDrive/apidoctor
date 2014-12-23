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

namespace OneDrive.ApiDocumentation.Windows
{
    public partial class ErrorDisplayForm : Form
    {
        public ErrorDisplayForm(ValidationError[] errors)
        {
            InitializeComponent();

            textBoxErrors.Text = "The following errors were detetected:\r\n";
            textBoxErrors.AppendText(errors.AllErrors());
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
