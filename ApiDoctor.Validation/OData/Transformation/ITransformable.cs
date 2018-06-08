using ApiDoctor.Validation.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ApiDoctor.Validation.OData.Transformation
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

        /// <summary>
        /// Specify a parameter index so that parameter order can be maintained even if we sort the collections.
        /// This would come from a transform rule and not from CSDL
        /// </summary>
        [XmlIgnore, SortBy(0), MergePolicy(MergePolicy.Ignore)]
        public int? CollectionIndex { get; set; }
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
