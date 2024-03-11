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
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    public class DocumentOutlineFile : ConfigFile
    {
        [JsonProperty("apiPageType"), JsonConverter(typeof(DocumentHeaderJsonConverter))]
        public List<object> ApiPageType { get; set; } = [];

        [JsonProperty("resourcePageType"), JsonConverter(typeof(DocumentHeaderJsonConverter))]
        public List<object> ResourcePageType { get; set; } = [];

        [JsonProperty("conceptualPageType"), JsonConverter(typeof(DocumentHeaderJsonConverter))]
        public List<object> ConceptualPageType { get; set; } = [];

        [JsonProperty("enumPageType"), JsonConverter(typeof(DocumentHeaderJsonConverter))]
        public List<object> EnumPageType { get; set; } = [];

        public override bool IsValid => ApiPageType.Count > 0 || ResourcePageType.Count > 0 || ConceptualPageType.Count > 0 || EnumPageType.Count > 0;
    }


    public class ExpectedDocumentHeader : DocumentHeader
    {
        /// <summary>
        /// Indicates that a header pattern can be repeated multiple times e.g. in the case of multiple examples
        /// </summary>
        [JsonProperty("allowMultiple")]
        public bool AllowMultiple { get; set; }

        /// <summary>
        /// Specifies the headers that are allowed under this header.
        /// </summary>
        [JsonProperty("headers"), JsonConverter(typeof(DocumentHeaderJsonConverter))]
        public new List<object> ChildHeaders { get; set; } = [];

        public ExpectedDocumentHeader() { }

        public ExpectedDocumentHeader(ExpectedDocumentHeader original) : base(original)
        {
            if (original == null)
                return;

           AllowMultiple = original.AllowMultiple;

           ChildHeaders = CopyHeaders(original.ChildHeaders);
        }

        public static List<object> CopyHeaders(List<object> headers)
        {
            if (headers == null) 
                return null;

            var headersCopy = new List<object>();
            foreach (var header in headers)
            {
                headersCopy.Add(header switch
                {
                    ConditionalDocumentHeader conditionalDocHeader => new ConditionalDocumentHeader(conditionalDocHeader),
                    ExpectedDocumentHeader expectedDocHeader => new ExpectedDocumentHeader(expectedDocHeader),
                    _ => throw new InvalidOperationException("Unexpected header type")
                });
            }
            return headersCopy;
        }
    }

    public class ConditionalDocumentHeader
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

        public ConditionalDocumentHeader(ConditionalDocumentHeader original)
        {
           if (original == null)
                return;

           Condition = original.Condition;

           Arguments = ExpectedDocumentHeader.CopyHeaders(original.Arguments);
        }
    }

    public enum ConditionalOperator
    {
        OR,
        AND
    }

    public class DocumentHeaderJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ExpectedDocumentHeader) || objectType == typeof(ConditionalDocumentHeader);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartArray)
            {
                var allowedHeaders = new List<object>();
                var jArray = JArray.Load(reader);
                foreach (var item in jArray)
                {
                    if (item["condition"] != null)
                    {
                        var conditionalHeader = item.ToObject<ConditionalDocumentHeader>(serializer);
                        allowedHeaders.Add(conditionalHeader);
                    }
                    else if (item["title"] != null)
                    {
                        var expectedHeader = item.ToObject<ExpectedDocumentHeader>(serializer);
                        allowedHeaders.Add(expectedHeader);
                    }
                    else
                    {
                        throw new JsonReaderException("Invalid document header definition");
                    }
                }
                return allowedHeaders;
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

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

}