namespace OneDrive.ApiDocumentation.Validation.Json
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

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
                switch (Type)
                {
                    case JsonDataType.ODataType:
                        return ODataTypeName;
                    case JsonDataType.Object:
                        return "Object";
                    default:
                        return Type.ToString();
                }
            }
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

}
