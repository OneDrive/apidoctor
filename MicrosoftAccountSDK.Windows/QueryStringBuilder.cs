using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicrosoftAccountSDK
{
    public class QueryStringBuilder
    {
        private readonly Dictionary<string, string> parameters = new Dictionary<string, string>();
        public QueryStringBuilder()
        {

        }

        public QueryStringBuilder(string key, string value)
        {
            this[key] = value;
        }

        public void Clear()
        {
            parameters.Clear();
        }

        public bool HasKeys
        {
            get { return parameters.Count > 0; }
        }

        public string this[string key]
        {
            get
            {
                if (parameters.ContainsKey(key))
                    return parameters[key];
                else
                    return null;
            }
            set
            {
                parameters[key] = value;
            }
        }

        public bool ContainsKey(string key)
        {
            return parameters.ContainsKey(key);
        }

        public string[] Keys
        {
            get { return parameters.Keys.ToArray(); }
        }

        public void Add(string key, string value)
        {
            parameters[key] = value;
        }

        public void Remove(string key)
        {
            parameters.Remove(key);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var param in parameters)
            {
                if (sb.Length > 0 && sb[sb.Length - 1] != '?')
                    sb.Append("&");
                sb.Append(param.Key);
                sb.Append("=");
                sb.Append(Uri.EscapeDataString(param.Value));
            }
            return sb.ToString();
        }
    }
}
