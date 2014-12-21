using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocumentationTester
{
    static class ExtensionMethods
    {

        public static string ComponentsJoinedByString(this IEnumerable<string> source, string separator)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var component in source)
            {
                if (sb.Length > 0)
                    sb.Append(separator);
                sb.Append(component);
            }
            return sb.ToString();
        }
    }
}
