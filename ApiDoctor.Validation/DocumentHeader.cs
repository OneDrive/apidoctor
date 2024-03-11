using ApiDoctor.Validation.Config;
using Fastenshtein;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ApiDoctor.Validation
{
    public class DocumentHeader
    {
        /// <summary>
        /// Represents the header level using markdown formatting (1=#, 2=##, 3=###, 4=####, 5=#####, 6=######)
        /// </summary>
        [JsonProperty("level")]
        public int Level { get; set; }

        /// <summary>
        /// Indicates that a header at this level is required.
        /// </summary>
        [JsonProperty("required")]
        public bool Required { get; set; }

        /// <summary>
        /// The expected value of a title or empty to indicate any value
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Specifies the headers that are allowed/found under this header.
        /// </summary>
        [JsonProperty("headers")]
        public List<DocumentHeader> ChildHeaders { get; set; } = new List<DocumentHeader>();

        public DocumentHeader() { }

        public DocumentHeader(DocumentHeader original)
        {
           Level = original.Level;
           Required = original.Required;
           Title = original.Title;

           if (original.ChildHeaders != null)
           {
               ChildHeaders = [];
               foreach (var header in original.ChildHeaders)
               {
                   ChildHeaders.Add(new DocumentHeader(header));
               }
           }
        }

        internal bool Matches(DocumentHeader found, bool ignoreCase = false, bool checkStringDistance = false)
        {
            if (checkStringDistance)
                return IsMisspelt(found);

            return this.Level == found.Level && DoTitlesMatch(this.Title, found.Title, ignoreCase);
        }

        private static bool DoTitlesMatch(string expectedTitle, string foundTitle, bool ignoreCase)
        {
            StringComparison comparisonType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            if (expectedTitle.Equals(foundTitle, comparisonType))
                return true;

            if (string.IsNullOrEmpty(expectedTitle) || expectedTitle == "*")
                return true;

            if (expectedTitle.StartsWith("* ") && foundTitle.EndsWith(expectedTitle[2..], comparisonType))
                return true;

            if (expectedTitle.EndsWith(" *") && foundTitle.StartsWith(expectedTitle[..^2], comparisonType))
                return true;

            return false;
        }

        internal bool IsMisspelt(DocumentHeader found)
        {
            return this.Level == found.Level && Levenshtein.Distance(this.Title, found.Title) < 3;
        }

        public override string ToString()
        {
            return this.Title;
        }
    }
}
