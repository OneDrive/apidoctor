using ApiDocs.Validation.Error;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation.OData
{
    public static class SchemaValidation
    {
        /// <summary>
        /// Walks through the EntityFramework object graph to ensure that all types mentioned in the framework are known types
        /// </summary>
        /// <param name="framework"></param>
        public static void ValidateSchemaTypes(this EntityFramework framework)
        {
            List<ValidationError> detectedErrors = new List<Error.ValidationError>();
            ValidateTypes(framework, framework, detectedErrors);

            if (detectedErrors.Any())
            {
                foreach(var error in detectedErrors)
                {
                    Console.WriteLine(error.Message);
                }
            }
        }

        private static bool IsValidType(this EntityFramework edmx, ParameterDataType type)
        {
            if (type.Type != SimpleDataType.Object && type.Type != SimpleDataType.None)
                return true;

            if (type.Type == SimpleDataType.Object && !string.IsNullOrEmpty(type.CustomTypeName))
            {
                // See if we can find this custom type name in the EntityFramework
                var targetSchemas = (from s in edmx.DataServices.Schemas where type.CustomTypeName.StartsWith(s.Namespace + ".") select s);
                if (!targetSchemas.Any())
                {
                    return false;
                }

                foreach(var s in targetSchemas)
                {
                    // See if we can find a matching EntityType or ComplexType in this schema
                    var typeName = type.CustomTypeName.Substring(s.Namespace.Length + 1);
                    var matchingType = s.FindTypeByName(typeName);
                    if (matchingType != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static ComplexType FindTypeByName(this Schema schema, string name)
        {
            var matchingEntityType = (from t in schema.EntityTypes where t.Name.Equals(name) select t).FirstOrDefault();
            if (null != matchingEntityType)
                return matchingEntityType;

            var matchingComplexType = (from t in schema.ComplexTypes where t.Name.Equals(name) select t).FirstOrDefault();
            if (null != matchingComplexType)
                return matchingComplexType;

            return null;
        }

        private static void ValidateTypes(object firstNode, EntityFramework edmx,  IList<ValidationError> errors)
        {
            if (firstNode == null)
                return;
            if (errors == null)
                throw new ArgumentNullException("errors");


            SortCollectionsHelper.WalkObjectGraph<Transformation.ContainsTypeAttribute>(firstNode, true, (prop, source, parentObjects) =>
            {
                string value = (string)prop.GetValue(source);
                if (null != value)
                {
                    var dataType = value.ParseParameterDataType();
                    if (!edmx.IsValidType(dataType))
                    {
                        errors.Add(new ValidationError(ValidationErrorCode.InvalidContentType, null, $"Unable to locate type: {dataType}"));
                    }
                }
            });
        }
    }
}
