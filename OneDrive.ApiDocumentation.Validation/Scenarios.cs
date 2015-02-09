using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.Validation
{
    public class Scenarios
    {
        private List<ScenarioDefinition> _scenarios;

        public event EventHandler<ScenarioEventArgs> DefinitionsChanged;

        /// <summary>
        /// Indicates that scenarios have been loaded from a file
        /// </summary>
        public bool Loaded { get; private set; }

        /// <summary>
        /// If true, changes have been made to the scenarios collection since the file was loaded.
        /// </summary>
        public bool Dirty { get; private set; }

        /// <summary>
        /// Collection of the defined scenarios
        /// </summary>
        public IReadOnlyList<ScenarioDefinition> Definitions
        {
            get { return _scenarios; }
        }

        private string RelativePath { get; set; }
        
        private DocSet DocumentSet { get; set; }

        internal Scenarios()
        {
            _scenarios = new List<ScenarioDefinition>();
        }

        internal Scenarios(DocSet set, string relativePath) : this()
        {
            Loaded = TryReadScenarios(set, relativePath);
        }

        public bool TryReadScenarios(DocSet set, string relativePath)
        {
            var path = string.Concat(set.SourceFolderPath, relativePath);
            if (!File.Exists(path))
                return false;

            DocumentSet = set;
            RelativePath = relativePath;

            try
            {
                string rawJson = null;
                using (StreamReader reader = File.OpenText(path))
                {
                    rawJson = reader.ReadToEnd();
                }
                var parameters = ScenarioDefinition.ReadFromJson(rawJson).ToList();

                // Make sure we have consistent method names
                foreach (var request in parameters)
                {
                    request.MethodName = ConvertPathSeparatorsToLocal(request.MethodName);
                }
                _scenarios = parameters;

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error reading parameters: {0}", (object)ex.Message);
                return false;
            }
        }

        public bool TrySaveToFile()
        {
            bool result = TryWriteScenarios(DocumentSet, RelativePath, Definitions.ToArray());
            if (result)
            {
                Dirty = false;
            }
            return result;
        }

        public static bool TryWriteScenarios(DocSet set, string relativePath, ScenarioDefinition[] parameters)
        {
            var path = string.Concat(set.SourceFolderPath, relativePath);
            foreach (var request in parameters)
            {
                request.MethodName = ConvertPathSeparatorsToGlobal(request.MethodName);
            }

            bool result = false;
            try
            {
                var text = Newtonsoft.Json.JsonConvert.SerializeObject(parameters, Newtonsoft.Json.Formatting.Indented);
                using (var writer = System.IO.File.CreateText(path))
                {
                    writer.Write(text);
                }
                result = true;
            }
            catch (Exception)
            {
                result = false;
            }

            foreach (var request in parameters)
            {
                request.MethodName = ConvertPathSeparatorsToLocal(request.MethodName);
            }
            return result;
        }

        private static string ConvertPathSeparatorsToLocal(string p)
        {
            return p.Replace('/', Path.DirectorySeparatorChar);
        }

        private static string ConvertPathSeparatorsToGlobal(string p)
        {
            return p.Replace(Path.DirectorySeparatorChar, '/');
        }

        public ScenarioDefinition FirstScenarioForMethod(MethodDefinition method)
        {
            var parameters = ScenariosForMethod(method);
            if (null != parameters)
                return parameters.FirstOrDefault(p => p.Enabled);
            else
                return null;
        }

        public ScenarioDefinition[] ScenariosForMethod(MethodDefinition method)
        {
            if (null == Definitions) return null;

            var id = method.DisplayName;
            var query = from p in Definitions
                        where p.MethodName == id
                        select p;

            return query.ToArray();
        }

        public void Add(ScenarioDefinition scenario)
        {
            _scenarios.Add(scenario);
            var evt = DefinitionsChanged;
            if (evt != null)
                evt(this, new ScenarioEventArgs(scenario, ScenarioEventAction.Added));
            Dirty = true;
        }

        public void Remove(ScenarioDefinition scenario)
        {
            if (_scenarios.Remove(scenario))
            {
                var evt = DefinitionsChanged;
                if (evt != null)
                    evt(this, new ScenarioEventArgs(scenario, ScenarioEventAction.Removed));
                Dirty = true;
            }
        }
    }


    public class ScenarioEventArgs : EventArgs
    {
        public ScenarioDefinition Scenario { get; private set; }
        public ScenarioEventAction Action { get; private set; }

        public ScenarioEventArgs(ScenarioDefinition scenario, ScenarioEventAction action)
        {
            Scenario = scenario;
            Action = action;
        }
    }

    public enum ScenarioEventAction
    {
        Added,
        Removed
    }
}
