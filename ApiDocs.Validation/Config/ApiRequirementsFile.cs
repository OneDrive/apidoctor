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

namespace ApiDocs.Validation.Config
{
    using Newtonsoft.Json;

    public class ApiRequirementsFile : ConfigFile
    {
        [JsonProperty("api-requirements")]
        public ApiRequirements ApiRequirements {get;set;}

        public override bool IsValid
        {
            get
            {
                return this.ApiRequirements != null;
            }
        }
    }

    public class ApiRequirements
    {
        [JsonProperty("httpRequest")]
        public HttpRequestRequirements HttpRequest {get;set;}

        [JsonProperty("httpResponse")]
        public HttpResponseRequirements HttpResponse {get;set;}

        [JsonProperty("jsonSerialization")]
        public JsonSerializationRequirements JsonSerialization {get;set;}

        [JsonProperty("ignorableProperties")]

        public string[] IgnorableProperties { get; set; }
        [JsonProperty("caseSensativeHeaders")]
        public bool CaseSensativeHeaders { get; set; }
    }

    public class HttpRequestRequirements
    {
        [JsonProperty("maxUrlLength")]
        public int MaxUrlLength {get;set;}
        [JsonProperty("httpMethods")]
        public string[] HttpMethods {get;set;}
        [JsonProperty("standardHeaders")]
        public string[] StandardHeaders {get;set;}
        [JsonProperty("contentTypes")]
        public string[] ContentTypes {get;set;}
    }

    public class HttpResponseRequirements
    {
        [JsonProperty("contentTypes")]
        public string[] ContentTypes {get;set;}
    }

    public class JsonSerializationRequirements
    {
        [JsonProperty("collectionPropertyNames")]
        public string[] CollectionPropertyNames {get;set;}
        [JsonProperty("dateTimeFormats")]
        public string[] DateTimeFormats {get;set;}
    }

}

