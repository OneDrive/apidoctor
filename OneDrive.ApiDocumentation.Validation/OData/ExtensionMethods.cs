using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.Validation.OData
{
    internal static class ExtensionMethods
    {
        internal static bool ToBoolean(this string source)
        {
            if (string.IsNullOrEmpty(source)) return false;

            bool value;
            if (Boolean.TryParse(source, out value))
                return value;

            throw new ArgumentException(string.Format("Failed to convert {0} into a boolean"), source);
        }

        internal static string AttributeValue(this System.Xml.Linq.XElement xml, System.Xml.Linq.XName attributeName)
        {
            var attribute = xml.Attribute(attributeName);
            if (null == attribute)
                return null;
            return attribute.Value;
        }

        internal static ComplexType FindTypeWithIdentifier(this IEnumerable<Schema> schemas, string identifier)
        {
            string[] parts = identifier.Split('.');
            if (parts.Length != 2)
                throw new ArgumentException("identifier should be of format {schema}.{type}");

            string schemaName = parts[0];
            string typeName = parts[1];

            var schema = (from s in schemas where s.Namespace == schemaName select s).FirstOrDefault();
            if (null == schema) return null;

            var matchingComplexType = (from ct in schema.ComplexTypes where ct.Name == typeName select ct).FirstOrDefault();
            if (null != matchingComplexType) return matchingComplexType;

            var matchingEntityType = (from et in schema.Entities where et.Name == typeName select et).FirstOrDefault();
            if (null != matchingEntityType) return matchingEntityType;

            return null;
        }
    }
}
