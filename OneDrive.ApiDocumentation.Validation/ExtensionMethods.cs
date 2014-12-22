namespace OneDrive.ApiDocumentation.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

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
    }
}
