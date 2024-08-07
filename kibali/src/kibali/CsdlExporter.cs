using Kibali;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace oauthpermissions
{
    public static class CsdlExporter
    {
        public static void Export(StringBuilder sb, PermissionsDocument permissions)
        {
            Export(new StringWriter(sb), permissions);
        }

        public static void Export(string fileName, PermissionsDocument permissions)
        {
            var stream = File.CreateText(fileName);
            Export(stream,permissions);
        }

        public static void Export(Stream stream, PermissionsDocument permissions)
        {
            Export(new StreamWriter(stream),permissions);
        }

        public static void Export(TextWriter writer, PermissionsDocument permissionsDocument)
        {
            using var xmlWriter = XmlWriter.Create(writer);
            xmlWriter.WriteStartDocument();

            xmlWriter.WriteStartElement("Schema");
            xmlWriter.WriteStartElement("Annotations");
            xmlWriter.WriteAttributeString("Target", "microsoft.graph.GraphService");
            xmlWriter.WriteStartElement("Annotation");
            xmlWriter.WriteAttributeString("Term", "Org.OData.Authorization.V1.Authorizations");
            xmlWriter.WriteStartElement("Collection");

            CreateSchemePermissions(permissionsDocument, xmlWriter, "DelegatedWork");
            CreateSchemePermissions(permissionsDocument, xmlWriter, "Application");

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();

            var authZChecker = new AuthZChecker();
            authZChecker.Load(permissionsDocument);

            foreach (var resource in authZChecker.Resources)
            {
                WriteResourceAnnotations(xmlWriter, resource);
            }

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
        }

        private static void CreateSchemePermissions(PermissionsDocument permissionsDocument, XmlWriter xmlWriter, string scheme)
        {
            xmlWriter.WriteStartElement("Record");
            xmlWriter.WriteAttributeString("Type", "Org.OData.Authorization.V1.Outh2Implicit");
            xmlWriter.WriteStartElement("PropertyValue");
            xmlWriter.WriteAttributeString("Name", scheme);
            xmlWriter.WriteEndElement();

            // Write out all permissions info...
            foreach (var permission in permissionsDocument.Permissions.Where(p => p.Value.Schemes.ContainsKey(scheme)))
            {
                WritePermissionAnnotations(xmlWriter, permission, scheme);
            }

            xmlWriter.WriteEndElement();
        }

        private static void WriteResourceAnnotations(XmlWriter xmlWriter, KeyValuePair<string, ProtectedResource> resource)
        {
            
            xmlWriter.WriteStartElement("Annotations");
            xmlWriter.WriteAttributeString("Target", UrlToTarget(resource.Value.Url));
            xmlWriter.WriteStartElement("Annotation");
            // ReadResrictions
            // UpdateRestrictions
            // InsertResrictions
            // DeleteResrictions
            // OperatationRestrictions
            xmlWriter.WriteAttributeString("Term", "Org.OData.Capabilities.V1.OperationRestrictions");
            xmlWriter.WriteStartElement("Record");
            xmlWriter.WriteStartElement("PropertyValue");
            xmlWriter.WriteAttributeString("Property", "Permissions");
            xmlWriter.WriteStartElement("Collection");

            xmlWriter.WriteStartElement("PropertyValue");
            xmlWriter.WriteAttributeString("SchemeName", "Permissions");
            xmlWriter.WriteEndElement();



            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();

        }

        private static string UrlToTarget(string url)
        {
            var target = urlToTarget.Replace(url, "");
            return "microsoft.graph"+target;
        }
        private static readonly Regex urlToTarget = new("/{[^/]*");

        private static void WritePermissionAnnotations(XmlWriter xmlWriter, KeyValuePair<string, Permission> permission, string scheme)
        {
            xmlWriter.WriteStartElement("Record");

            xmlWriter.WriteStartElement("PropertyValue");
            xmlWriter.WriteAttributeString("Property", "Scope");
            xmlWriter.WriteAttributeString("String", permission.Key);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("PropertyValue");
            xmlWriter.WriteAttributeString("Property", "Description");
            xmlWriter.WriteAttributeString("String", permission.Value.Schemes[scheme].UserDescription);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("PropertyValue");
            xmlWriter.WriteAttributeString("Property", "Grant");
            xmlWriter.WriteAttributeString("String", permission.Value.Schemes[scheme].RequiresAdminConsent ? "admin":"user");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement();
        }
    }
}
