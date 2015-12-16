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
        IODataNavigable NavigateByEntityTypeKey(EntityFramework edmx);

        string TypeIdentifier { get; }
    }

    internal class ODataCollection : IODataNavigable
    {
        public string TypeIdentifier { get; internal set; }

        public ODataCollection(string typeIdentifier)
        {
            this.TypeIdentifier = typeIdentifier;
        }

        public IODataNavigable NavigateByUriComponent(string component, EntityFramework edmx)
        {
            throw new NotSupportedException();
        }

        public IODataNavigable NavigateByEntityTypeKey(EntityFramework edmx)
        {
            return edmx.FindTypeWithIdentifier(this.TypeIdentifier) as IODataNavigable;
        }
    }

    public class ODataSimpleType : IODataNavigable
    {
        public SimpleDataType Type { get; internal set; }

        public ODataSimpleType(SimpleDataType type)
        {
            this.Type = type;
        }

        public IODataNavigable NavigateByUriComponent(string component, EntityFramework edmx)
        {
            throw new NotSupportedException();
        }

        public IODataNavigable NavigateByEntityTypeKey(EntityFramework edmx)
        {
            throw new NotSupportedException();
        }

        public string TypeIdentifier
        {
            get { return Type.ODataResourceName(); }
        }
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
        EntityContainer,
        SimpleType
    }
}
