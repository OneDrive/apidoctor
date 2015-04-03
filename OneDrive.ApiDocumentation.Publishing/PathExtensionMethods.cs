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
            if (null == path) return null;
            var parts = path.Split(new char[] { '\\', '/' });
            if (parts.Length > 0)
            {
                return parts[parts.Length - 1];
            }
            return null;
        }

        public static string FirstPathComponent(this string path)
        {
            if (null == path) return null;

            var parts = path.Split(new char[] { '\\', '/' });
            if (parts.Length > 0)
            {
                return parts[0];
            }
            return null;
        }
    }
}
