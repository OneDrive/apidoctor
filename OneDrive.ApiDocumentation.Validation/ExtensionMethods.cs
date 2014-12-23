namespace OneDrive.ApiDocumentation.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public static class ExtensionMethods
    {

        public static string ComponentsJoinedByString(this IEnumerable<string> source, string separator, int startIndex = 0)
        {
            StringBuilder sb = new StringBuilder();
            int index = 0;
            foreach (var component in source)
            {
                if (index < startIndex)
                {
                    index++;
                    continue;
                }

                if (sb.Length > 0)
                    sb.Append(separator);
                sb.Append(component);

                index++;
            }
            return sb.ToString();
        }

        public static bool TryGetPropertyValue<T>(this Newtonsoft.Json.Linq.JContainer container, string propertyName, out T value )
        {
            try
            {
                Newtonsoft.Json.Linq.JValue storedValue = (Newtonsoft.Json.Linq.JValue)container[propertyName];
                if (storedValue == null)
                {
                    value = default(T);
                    return false;
                }
                else
                {
                    value = (T)storedValue.Value;
                    return true;
                }
            }
            catch 
            {
                value = default(T);
                return false;
            }
        }

        public static T PropertyValue<T>(this Newtonsoft.Json.Linq.JContainer container, string propertyName, T defaultValue)
        {
            T storedValue;
            if (container.TryGetPropertyValue(propertyName, out storedValue))
                return storedValue;
            else
                return defaultValue;
        }

        public static string TopLineOnly(this string input)
        {
            System.IO.StringReader reader = new System.IO.StringReader(input);
            return reader.ReadLine();
        }
    }
}
