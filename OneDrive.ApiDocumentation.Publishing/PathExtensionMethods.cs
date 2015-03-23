using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDrive.ApiDocumentation.Publishing
{
    internal static class PathExtensionMethods
    {

        public static string LastPathComponent(this string path)
        {
            var parts = path.Split(new char[] { '\\', '/' });
            return parts[parts.Length - 1];
        }

    }
}
