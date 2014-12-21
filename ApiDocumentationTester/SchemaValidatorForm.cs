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
    public partial class SchemaValidatorForm : Form
    {

        public JsonValidator Validator { get; set; }

        public SchemaValidatorForm()
        {
            InitializeComponent();
        }

        private void SchemaValidator_Load(object sender, EventArgs e)
        {
            comboBoxSchema.DisplayMember = "ResourceName";
            comboBoxSchema.DataSource = Validator.RegisteredSchema;

        }

        private void buttonValidate_Click(object sender, EventArgs e)
        {
            ValidationError[] errors;
            bool result = Validator.ValidateJson(comboBoxSchema.Text, textBoxJsonToValidate.Text, out errors);

            if (result)
            {
                MessageBox.Show("JSON checks out");
            }
            else
            {
                var output = from m in errors
                             select m.Message;

                string errorStatements = output.ComponentsJoinedByString(Environment.NewLine);
                             
                MessageBox.Show("JSON had the following errors:\n " + errorStatements);
            }
        }
    }
}
