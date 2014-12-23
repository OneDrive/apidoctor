using OneDrive.ApiDocumentation.Validation;
using OneDrive.ApiDocumentation.Validation.Json;
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
    public partial class SchemaValidatorForm : Form
    {

        public JsonResourceCollection ResourceCollection { get; set; }

        public SchemaValidatorForm()
        {
            InitializeComponent();
        }

        private void SchemaValidator_Load(object sender, EventArgs e)
        {

            if (null == ResourceCollection)
            {
                MessageBox.Show("Resource collection was not initialized.");
                return;
            }

            comboBoxSchema.DisplayMember = "ResourceName";
            comboBoxSchema.DataSource = ResourceCollection.RegisteredSchema;
        }

        private void buttonValidate_Click(object sender, EventArgs e)
        {
            if (null == ResourceCollection) return;

            ValidationError[] errors;
            bool result = ResourceCollection.ValidateJson(comboBoxSchema.Text, textBoxJsonToValidate.Text, checkBoxCollection.Checked, out errors);

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
