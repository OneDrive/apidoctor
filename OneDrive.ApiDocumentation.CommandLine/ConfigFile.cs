using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.ConsoleApp
{
    public class ConfigFile
    {
        public static ConfigFile LoadFromDocumentSet(string docSetPath)
        {
            DirectoryInfo source = new DirectoryInfo(docSetPath);
            var potentialFiles = source.GetFiles("*.json", SearchOption.AllDirectories);
            foreach (var file in potentialFiles)
            {
                // See if this file maps to what we're looking for.
                using (var reader = file.OpenText())
                {
                    try
                    {
                        var configFile = JsonConvert.DeserializeObject<ConfigFile>(reader.ReadToEnd());
                        if (configFile.IsValid)
                        {
                            Console.WriteLine("Using configuration file: {0}", file.FullName);
                            return configFile;
                        }
                    }
                    catch { }
                }
            }
            return null;
        }


        [JsonProperty("accounts")]
        public Account[] Accounts { get; set; }

        [JsonProperty("checkServiceEnabledBranches")]
        public string[] CheckServiceEnabledBranches { get; set; }


        public bool IsValid
        {
            get { return null != Accounts || null != CheckServiceEnabledBranches; }
        }
    }
}
