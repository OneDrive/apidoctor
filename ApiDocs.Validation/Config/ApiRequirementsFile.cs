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

