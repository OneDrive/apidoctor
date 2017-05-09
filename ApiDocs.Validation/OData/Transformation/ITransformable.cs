using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation.OData.Transformation
{
    public interface ITransformable
    {
        string ElementIdentifier { get; set; }

        void ApplyTransformation(BaseModifications value, EntityFramework edmx, string[] versions);
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
