using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ApiDoctor.Validation.OData.Transformation;

namespace ApiDoctor.Publishing.CSDL
{
    public class XmlSorter
    {
        private static HashSet<string> attributesToDrop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Nullable", // props
            "Unicode", // string props
            "EntitySetPath", // entitysets
            "ContainsTarget", // navprops
        };

        private static HashSet<string> attributesToIgnoreForSorting = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SourceFiles",
        };

        private static Dictionary<string, string> attributeValuesToRename = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["bindingparameter"] = "bindingParameter",
            ["bindParameter"] = "bindingParameter",
            ["this"] = "bindingParameter",
        };

        private bool learningMode;
        private HashSet<string> knownNames;
        private HashSet<string> unknownNames;
        private HashSet<string> keywordsToDropElements;
        private HashSet<string> keywordsToKeepElements;

        public XmlSorter(SchemaDiffConfig config)
        {
            this.keywordsToKeepElements = new HashSet<string>(config?.KeepElementsContaining ?? new string[0], StringComparer.OrdinalIgnoreCase);
            this.keywordsToDropElements = new HashSet<string>(config?.DropElementsContaining ?? new string[0], StringComparer.OrdinalIgnoreCase);
        }

        public bool KeepUnrecognizedObjects { get; set; }

        public void Sort(string firstXmlPath, string secondXmlPath)
        {
            this.knownNames = new HashSet<string>(attributeValuesToRename.Keys, StringComparer.OrdinalIgnoreCase);
            this.unknownNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            this.learningMode = true;
            foreach (var input in new[] { firstXmlPath, secondXmlPath})
            {
                var output = input.Replace(".xml", "-sorted.xml");
                if (File.Exists(output))
                {
                    File.Delete(output);
                }

                using (var file = File.OpenRead(input))
                {
                    var settings = new XmlWriterSettings
                    {
                        Indent = true,
                    };

                    using (var writer = XmlWriter.Create(output, settings))
                    {
                        var input1 = XDocument.Parse(File.ReadAllText(input));
                        var sorted = Sort(input1.Root);
                        sorted.WriteTo(writer);
                    }
                }

                this.learningMode = false;
            }
        }

        private bool SmartFilter(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                if (this.learningMode)
                {
                    this.knownNames.Add(name);
                }
                else
                {
                    bool known = this.knownNames.Contains(name);
                    if (!known)
                    {
                        this.unknownNames.Add(name);
                        return this.KeepUnrecognizedObjects;
                    }
                }
            }

            return true;
        }

        public XElement Sort(XElement element)
        {
            var sorted = new XElement(element.Name);
            foreach (var xa in element.Attributes().
                        Where(xa => !attributesToDrop.Contains(xa.Name.ToString())).
                        OrderByDescending(xa => xa.Name == "Name").
                        ThenBy(xa => xa.Name == "Term").
                        ThenBy(xa => attributesToIgnoreForSorting.Contains(xa.Name.ToString())).
                        ThenBy(xa => xa.Name.ToString()).
                        ThenBy(xa => xa.Value.ToString()))
            {
                string newName;
                if (attributeValuesToRename.TryGetValue(xa.Value.ToString(), out newName))
                {
                    sorted.Add(new XAttribute(xa.Name, newName));
                }
                else
                {
                    sorted.Add(xa);
                }
            }

            foreach (var xe in element.Elements().
                        Where(el => SmartFilter(el.Name.ToString()) && SmartFilter(el.Attribute("Name")?.Value)).
                        Select(xe => Sort(xe)).
                        Where(xe =>
                            xe.Attributes().Any(attr => keywordsToKeepElements.Contains(attr.Value)) ||
                            xe.Elements().SelectMany(el => el.Attributes()).All(attr => !keywordsToDropElements.Contains(attr.Value))).
                        OrderBy(xe => xe.Name.ToString()).
                        ThenBy(xe => PrintAttrs(xe.Attributes())).
                        ThenBy(xe => PrintElems(xe.Elements())))
            {
                sorted.Add(xe);
            }

            foreach (var n in element.Nodes().Where(n=>n.NodeType != XmlNodeType.Element))
            {
                sorted.Add(n);
            }

            return sorted;
        }

        public static string PrintAttrs(IEnumerable<XAttribute> attributes)
        {
            var sb = new StringBuilder();
            foreach (var attr in attributes.Where(attr=> !attributesToIgnoreForSorting.Contains(attr.Name.ToString())))
            {
                sb.Append(attr.Name).Append("=").Append(attr.Value).Append(";");
            }

            return sb.ToString();
        }

        public static string PrintElems(IEnumerable<XElement> elements)
        {
            var sb = new StringBuilder();
            foreach (var el in elements.OrderBy(xe => xe.Name.ToString()).
                        ThenBy(xe => PrintAttrs(xe.Attributes())))
            {
                sb.Append(PrintAttrs(el.Attributes())).Append("|");
            }

            return sb.ToString();
        }
    }
}
