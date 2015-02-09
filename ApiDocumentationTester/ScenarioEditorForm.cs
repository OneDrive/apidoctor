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
    public partial class ScenarioEditorForm : Form
    {

        public DocSet CurrentDocSet { get; private set; }

        public ScenarioEditorForm(DocSet docSet)
        {
            InitializeComponent();

            CurrentDocSet = docSet;
            docSet.LoadTestScenarios(Properties.Settings.Default.ScenariosFile);
            docSet.TestScenarios.DefinitionsChanged += TestScenarios_DefinitionsChanged;
            scenarioEditorControl.DocumentSet = docSet;
            scenarioEditorControl.ScenarioChanged += scenarioEditorControl_ScenarioChanged;
            BuildTreeView();
        }

        void scenarioEditorControl_ScenarioChanged(object sender, EventArgs e)
        {
            TreeNode node = GetSelectedNode<ScenarioDefinition>();
            if (null != node)
            {
                var scenario = node.Tag as ScenarioDefinition;
                node.Text = string.Format("[{0}] {1}", scenario.Enabled ? "X" : "  ", scenario.Description);;
            }
        }

        void TestScenarios_DefinitionsChanged(object sender, ScenarioEventArgs e)
        {
            if (e.Action == ScenarioEventAction.Added)
            {

            }
            else if (e.Action == ScenarioEventAction.Removed)
            {

            }
        }

        private void BuildTreeView()
        {
            // Tree view should look like this
            /*
             * file #1
             *   method #1
             *     scenario #1
             *     scenario #2
             *   method #2
             * file #2
             *   method #1
             *      scenario #1
             */

            treeViewScenarios.BeginUpdate();
            foreach (var file in CurrentDocSet.Files)
            {
                if (file.Requests.Length == 0) continue;
                TreeNode node = new TreeNode(file.DisplayName) { Tag = file };
                AddMethodNodes(node, file.Requests);
                treeViewScenarios.Nodes.Add(node);
            }
            treeViewScenarios.EndUpdate();

        }

        private TreeNode GetSelectedNode<T>() where T : class
        {
            var node = treeViewScenarios.SelectedNode;

            T tagValue = null;
            while ((tagValue = node.Tag as T) == null)
            {
                node = node.Parent;
                if (node == null)
                    return null;
            }
            return node;
        }


        private void AddMethodNodes(TreeNode fileNode, MethodDefinition[] methodDefinition)
        {
            foreach (var method in methodDefinition)
            {
                TreeNode node = new TreeNode(method.DisplayName) { Tag = method };
                AddScenarioNodes(node, CurrentDocSet.TestScenarios.ScenariosForMethod(method));
                fileNode.Nodes.Add(node);
            }
        }

        private static void AddScenarioNodes(TreeNode methodNode, ScenarioDefinition[] scenarioDefinition)
        {
            foreach (var scenario in scenarioDefinition)
            {
                AddScenarioNode(methodNode, scenario);
            }
        }

        private static TreeNode AddScenarioNode(TreeNode methodNode, ScenarioDefinition scenario)
        {
            var name = string.Format("[{0}] {1}", scenario.Enabled ? "X" : "  ", scenario.Description);
            TreeNode node = new TreeNode(name) { Tag = scenario };
            methodNode.Nodes.Add(node);
            return node;
        }

        private void buttonAddParameters_Click(object sender, EventArgs e)
        {
            ScenarioDefinition newScenario = null;
            TreeNode node = null;

            if (null != scenarioEditorControl.Scenario && !CurrentDocSet.TestScenarios.Definitions.Contains(scenarioEditorControl.Scenario))
            {
                // Potentially edited a new scenario. Add that instead
                newScenario = scenarioEditorControl.Scenario;
            }
            else if ((node = GetSelectedNode<ScenarioDefinition>()) != null)
            {
                // Create a copy of this scenario
                var scenario = node.Tag as ScenarioDefinition;
                newScenario = scenario.Copy();
            }
            else if ((node = GetSelectedNode<MethodDefinition>()) != null)
            {
                var method = node.Tag as MethodDefinition;
                newScenario = new ScenarioDefinition { Method = method.DisplayName, Description = "new scenario" };
            }


            if (newScenario != null)
            {
                CurrentDocSet.TestScenarios.Add(newScenario);
                var methodNode = GetSelectedNode<MethodDefinition>();
                var newNode = AddScenarioNode(methodNode, newScenario);
                treeViewScenarios.SelectedNode = newNode;
                
            }
        }

        private void buttonDeleteParameters_Click(object sender, EventArgs e)
        {
            var scenarioNode = GetSelectedNode<ScenarioDefinition>();
            if (null != scenarioNode)
            {
                CurrentDocSet.TestScenarios.Remove(scenarioNode.Tag as ScenarioDefinition);
                scenarioNode.Remove();
            }
        }

        private void buttonSaveParameterFile_Click(object sender, EventArgs e)
        {
            CurrentDocSet.TestScenarios.TrySaveToFile();
        }

        private void treeViewScenarios_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode node = e.Node;
            ScenarioDefinition scenario = node.Tag as ScenarioDefinition;
            MethodDefinition method = node.Tag as MethodDefinition;

            if (scenario == null && method == null)
            {
                scenarioEditorControl.Clear();
                scenarioEditorControl.Enabled = false;
                buttonDeleteParameters.Enabled = false;
                buttonAddParameters.Enabled = false;
                return;
            }
            else if (scenario != null)
            {
                scenarioEditorControl.LoadScenario(scenario, CurrentDocSet);
                buttonDeleteParameters.Enabled = true;
                buttonAddParameters.Enabled = true;
                scenarioEditorControl.Enabled = true;
            }
            else if (method != null)
            {
                scenarioEditorControl.CreateNewScenario(method, CurrentDocSet);
                buttonDeleteParameters.Enabled = false;
                buttonAddParameters.Enabled = true;
                scenarioEditorControl.Enabled = true;
            }
        }

        private void ScenarioEditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (CurrentDocSet.TestScenarios.Dirty)
            {
                var result = MessageBox.Show("Do you want to save your changes to the scenario file?", "Save Changes", MessageBoxButtons.YesNo);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    CurrentDocSet.TestScenarios.TrySaveToFile();
                }
            }
        }
    }
}
