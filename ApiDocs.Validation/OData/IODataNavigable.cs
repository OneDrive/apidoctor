using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation.OData
{
    public interface IODataNavigable
    {
        /// <summary>
        /// Returns the next target pasted on the value of a component of the URI
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        IODataNavigable NavigateByUriComponent(string component, EntityFramework edmx);

        /// <summary>
        /// Returns the next component assuming that an entitytype key is provided
        /// </summary>
        /// <returns></returns>
        IODataNavigable NavigateByEntityTypeKey();
    }

    public class ODataTargetInfo
    {
        public ODataTargetClassification Classification { get; set; }

        public string QualifiedType { get; set; }

        public string Name { get; set; }
    }


    public enum ODataTargetClassification
    {
        Unknown,
        EntityType,
        EntitySet,
        Action,
        Function,
        EntityContainer
    }
}
