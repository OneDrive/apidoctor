﻿namespace ApiDocs.ConsoleApp
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using Newtonsoft.Json;

    public class SavedSettings
    {
        [JsonProperty("path")]
        public string DocumentationPath { get; set; }
        [JsonProperty("access-token")]
        public string AccessToken { get; set; }
        [JsonProperty("url")]
        public string ServiceUrl { get; set; }

        [JsonIgnore()]
        public string AppName { get; private set; }
        [JsonIgnore()]
        public string DataFilename { get; private set; }


        public SavedSettings(string appname, string dataFilename)
        {
            this.AppName = appname;
            this.DataFilename = dataFilename;

            this.Load();
        }

        public void Load()
        {
            var dataFile = this.PathToAppDataFile;
            if (!File.Exists(dataFile))
                return;

            try
            {
                using (var reader = File.OpenText(dataFile))
                {
                    var contents = reader.ReadToEnd();
                    JsonConvert.PopulateObject(contents, this);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Failed to read saved settings file: {0}", ex.Message));
            }
        }

        public void Save()
        {
            var dataFile = this.PathToAppDataFile;
            try
            {
                var contents = JsonConvert.SerializeObject(this);
                using (var writer = new StreamWriter(dataFile, false))
                {
                    writer.Write(contents);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Failed to write saved settings file: {0}", ex.Message));
            }
        }

        protected string PathToAppDataFile
        {
            get
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var appFolderPath = Path.Combine(appDataPath, this.AppName);
                Directory.CreateDirectory(appFolderPath);

                return Path.Combine(appFolderPath, this.DataFilename);
            }
        }

    }
}