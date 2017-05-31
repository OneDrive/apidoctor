using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ApiDocs.Validation.OData.Transformation
{
    public interface ITransformable
    {
        [XmlIgnore]
        string ElementIdentifier { get; set; }

        void ApplyTransformation(BaseModifications value, EntityFramework edmx, string[] versions);
    }

    public abstract class XmlBackedTransformableObject : XmlBackedObject, ITransformable
    {
        [XmlIgnore]
        public abstract string ElementIdentifier { get; set; }

        public virtual void ApplyTransformation(BaseModifications value, EntityFramework edmx, string[] versions)
        {
            TransformationHelper.ApplyTransformation(this, value, edmx, versions);
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ContainsTypeAttribute : Attribute
    {

    }


    public enum RenameableType
    {
        EntityType,
        ComplexType
    }
}
