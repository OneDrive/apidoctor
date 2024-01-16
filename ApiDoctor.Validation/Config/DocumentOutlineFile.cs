/*
* API Doctor
* Copyright (c) Microsoft Corporation
* All rights reserved. 
* 
* MIT License
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy of 
* this software and associated documentation files (the ""Software""), to deal in 
* the Software without restriction, including without limitation the rights to use, 
* copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
* Software, and to permit persons to whom the Software is furnished to do so, 
* subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all 
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
* INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
* PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
* HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
* OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

namespace ApiDoctor.Validation.Config
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class DocumentOutlineFile : ConfigFile
    {
        [JsonProperty("apiPageType"), JsonConverter(typeof(DocumentHeaderJsonConverter))]
        public List<object> ApiPageType { get; set; }

        [JsonProperty("resourcePageType"), JsonConverter(typeof(DocumentHeaderJsonConverter))]
        public List<object> ResourcePageType { get; set; }

        [JsonProperty("conceptualPageType"), JsonConverter(typeof(DocumentHeaderJsonConverter))]
        public List<object> ConceptualPageType { get; set; }

        [JsonProperty("enumPageType"), JsonConverter(typeof(DocumentHeaderJsonConverter))]
        public List<object> EnumPageType { get; set; }

        public override bool IsValid => this.ApiPageType.Any() || this.ResourcePageType.Any() || this.ConceptualPageType.Any() || this.EnumPageType.Any();

        public DocumentOutlineFile()
        {
            ApiPageType = new List<object>();
            ResourcePageType = new List<object>();
            ConceptualPageType = new List<object>();
            EnumPageType = new List<object>();
        }
    }

    public class DocumentHeader
    {
        public DocumentHeader()
        {
            Level = 1;
            ChildHeaders = new List<DocumentHeader>();
        }

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
        /// Specifies the headers that are allowed under this header.
        /// </summary>
        [JsonProperty("headers")]
        public List<DocumentHeader> ChildHeaders { get; set; }

        internal bool Matches(DocumentHeader found, bool ignoreCase = false, bool checkStringDistance = false)
        {
            if (checkStringDistance)
                return IsMisspelt(found);

            return this.Level == found.Level && DoTitlesMatch(this.Title, found.Title, ignoreCase);
        }

        private static bool DoTitlesMatch(string expectedTitle, string foundTitle, bool ignoreCase)
        {
            StringComparison comparisonType = StringComparison.Ordinal;
            if (ignoreCase) 
                comparisonType = StringComparison.OrdinalIgnoreCase;

            if (expectedTitle.Equals(foundTitle, comparisonType))
                return true;

            if (string.IsNullOrEmpty(expectedTitle) || expectedTitle == "*") 
                return true;

            if (expectedTitle.StartsWith("* ") && foundTitle.EndsWith(expectedTitle.Substring(2), comparisonType)) 
                return true;

            if (expectedTitle.EndsWith(" *") && foundTitle.StartsWith(expectedTitle[..^2], comparisonType)) 
                return true;

            return false;
        }
        internal bool IsMisspelt(DocumentHeader found)
        {
            return this.Level == found.Level && this.Title.StringDistance(found.Title) < 5;
        }

        public override string ToString()
        {
            return this.Title;
        }
    }

    public class ExpectedHeader : DocumentHeader
    {
        public ExpectedHeader()
        {
            Level = 1;
            ChildHeaders = new List<object>();
        }

        /// <summary>
        /// Indicates that a header pattern can be repeated multiple times e.g. in the case of multiple examples
        /// </summary>
        [JsonProperty("multiple")]
        public bool Multiple { get; set; }

        /// <summary>
        /// Specifies the headers that are allowed under this header.
        /// </summary>
        [JsonProperty("headers"), JsonConverter(typeof(DocumentHeaderJsonConverter))]
        public new List<object> ChildHeaders { get; set; }

        public ExpectedHeader Clone()
        {
            var header = this.MemberwiseClone() as ExpectedHeader;
            var childHeaders = new List<object>();
            foreach (var childHeader in this.ChildHeaders)
            {
                if (childHeader is ExpectedHeader expectedChildHeader)
                    childHeaders.Add(expectedChildHeader.Clone());
                else
                    childHeaders.Add(((ConditionalHeader)childHeader).Clone());
            }
            header.ChildHeaders = childHeaders;
            return header;
        }
    }

    public class ConditionalHeader
    {
        [JsonProperty("condition")]
        public string Condition { get; set; }

        [JsonProperty("arguments"), JsonConverter(typeof(DocumentHeaderJsonConverter))]
        public List<object> Arguments { get; set; }

        public ConditionalOperator? Operator
        {
            get
            {
                return Enum.TryParse(this.Condition, true, out ConditionalOperator op) ? op : null;
            }
        }

        public ConditionalHeader Clone()
        {
            var header = (ConditionalHeader)this.MemberwiseClone();
            var arguments = new List<object>();
            foreach (var arg in this.Arguments)
            {
                if (arg is ExpectedHeader headerArg)
                    arguments.Add(headerArg.Clone());
                else
                    arguments.Add(((ConditionalHeader)arg).Clone());
            }
            header.Arguments = arguments;
            return header;
        }
    }

    public enum ConditionalOperator
    {
        OR,
        AND
    }

    public class DocumentHeaderJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartArray)
            {
                var jArray = JArray.Load(reader);
                var allowedHeaders = new List<object>();
                foreach (var item in jArray)
                {
                    bool isConditionalHeader = item.ToString().TryParseJson(out ConditionalHeader conditionalHeader);
                    if (isConditionalHeader)
                    {
                        allowedHeaders.Add(conditionalHeader);
                        continue;
                    }

                    bool isExpectedHeader = item.ToString().TryParseJson(out ExpectedHeader header);
                    if (isExpectedHeader)
                    {
                        allowedHeaders.Add(header);
                        continue;
                    }

                    // Object is neither of type ExpectedHeader nor ConditionalHeader
                    throw new JsonReaderException("Invalid document header definition");
                }
                return allowedHeaders;
            }
            else if (reader.TokenType == JsonToken.StartObject)
            {
                var jObject = JObject.Load(reader);

                bool isConditionalHeader = jObject.ToString().TryParseJson(out ConditionalHeader conditionalHeader);
                if (isConditionalHeader)
                {
                    return conditionalHeader;
                }

                bool isExpectedHeader = jObject.ToString().TryParseJson(out ExpectedHeader header);
                if (isExpectedHeader)
                {
                    return header;
                }

                // Object is neither of type ExpectedHeader nor ConditionalHeader
                throw new JsonReaderException($"Invalid document header definition: {jObject.ToString()}");
            }
            else if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            else
            {
                throw new JsonSerializationException($"Unexpected token: {existingValue}");
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return false;
        }
    }
}