using ApiDocs.Validation.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation
{
    public class ParameterDefinition : ItemDefinition
    {
        /// <summary>
        /// The name of the parameter.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// The type of object the parameter expects
        /// </summary>
        public JsonDataType Type { get; set; }

        /// <summary>
        /// The location of the parameter in the request/response
        /// </summary>
        public ParameterLocation Location { get; set; }

        /// <summary>
        /// True if the parameter is required to have a value.
        /// </summary>
        public bool Required { get; set; }

        public List<EnumerationDefinition> Enumeration { get; set; }
    }
    
    public enum ParameterLocation
    {
        Path,
        QueryString,
        Header,
        JsonObject
    }
}
