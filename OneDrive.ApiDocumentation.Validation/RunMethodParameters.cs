using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.Validation
{
    public class RunMethodParameters
    {
        public bool Loaded { get; set; }
        public List<Param.RequestParameters> Definitions { get; private set; }
        private string RelativePath { get; set; }
        private DocSet DocumentSet { get; set; }

        public RunMethodParameters()
        {
            Definitions = new List<Param.RequestParameters>();
        }

        public RunMethodParameters(DocSet set, string relativePath) : this()
        {
            Loaded = TryReadRequestParameters(set, relativePath);
        }

        public bool TryReadRequestParameters(DocSet set, string relativePath)
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
                var parameters = Param.RequestParameters.ReadFromJson(rawJson).ToList();

                // Make sure we have consistent method names
                foreach (var request in parameters)
                {
                    request.Method = ConvertPathSeparatorsToLocal(request.Method);
                }
                Definitions = parameters;

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
            return TryWriteRequestParameters(DocumentSet, RelativePath, Definitions.ToArray());
        }

        public static bool TryWriteRequestParameters(DocSet set, string relativePath, Param.RequestParameters[] parameters)
        {
            var path = string.Concat(set.SourceFolderPath, relativePath);
            foreach (var request in parameters)
            {
                request.Method = ConvertPathSeparatorsToGlobal(request.Method);
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
                request.Method = ConvertPathSeparatorsToLocal(request.Method);
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

        public Param.RequestParameters RunParamtersForMethod(MethodDefinition method)
        {
            var parameters = SetOfRunParametersForMethod(method);
            if (null != parameters)
                return parameters.FirstOrDefault();
            else
                return null;
        }

        public Param.RequestParameters[] SetOfRunParametersForMethod(MethodDefinition method)
        {
            if (null == Definitions) return null;

            var id = method.DisplayName;
            var query = from p in Definitions
                        where p.Method == id && p.Enabled == true
                        select p;

            return query.ToArray();
        }
    }
}
