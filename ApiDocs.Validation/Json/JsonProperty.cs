/*
 * Markdown Scanner
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

namespace ApiDocs.Validation.Json
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class JsonProperty
    {
        public string Name { get; set; }

        public JsonDataType Type { get; set; }

        public string ODataTypeName { get; set; }

        public Dictionary<string, JsonProperty> CustomMembers { get; set; }

        public string OriginalValue { get; set; }

        public bool IsArray { get; set; }

        public string Description { get; set; }


        public string TypeDescription
        {
            get
            {
                switch (this.Type)
                {
                    case JsonDataType.ODataType:
                        return this.ODataTypeName;
                    case JsonDataType.Object:
                        return "Object";
                    default:
                        return this.Type.ToString();
                }
            }
        }

        public ExpectedStringFormat StringFormat
        {
            get
            {
                if (this.Type != JsonDataType.String)
                    return ExpectedStringFormat.Generic;

                if (this.OriginalValue == "timestamp" || this.OriginalValue == "datetime")
                    return ExpectedStringFormat.Iso8601Date;
                if (this.OriginalValue == "url" || this.OriginalValue == "absolute url")
                    return ExpectedStringFormat.AbsoluteUrl;
                if (this.OriginalValue.IndexOf('|') > 0)
                    return ExpectedStringFormat.EnumeratedValue;

                return ExpectedStringFormat.Generic;
            }
        }

        public string[] PossibleEnumValues()
        {
            if (this.Type != JsonDataType.String) 
                throw new InvalidOperationException("Cannot provide possible enum values on non-string data types");

            string[] possibleValues = this.OriginalValue.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            return (from v in possibleValues select v.Trim()).ToArray();
        }

        public bool IsValidEnumValue(string input)
        {
            string[] values = this.PossibleEnumValues();
            if (null != values && values.Length > 0)
            {
                return values.Contains(input);
            }
            return false;
        }
    }

    public enum JsonDataType
    {
        Boolean,
        Number,
        Integer = Number,
        String,
        Array,

        ODataType,
        Object
    }

    public enum ExpectedStringFormat
    {
        Generic,
        Iso8601Date,
        AbsoluteUrl,
        EnumeratedValue
    }

}
