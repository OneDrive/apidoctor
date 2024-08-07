using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Kibali
{
    internal class ParsingHelpers
    {
        public static void ParseMap<T>(JsonElement node, T permissionsDocument, FixedFieldMap<T> handlers)
        {
            foreach (var element in node.EnumerateObject())
            {
                handlers[element.Name](permissionsDocument, element.Value);
            };
        }

        internal static List<T> GetList<T>(JsonElement v, Func<JsonElement,T> load)
        {
            var list = new List<T>();
            foreach (var item in v.EnumerateArray())
            {
                list.Add(load(item));
            }
            return list;
        }

        internal static Dictionary<string,T> GetMap<T>(JsonElement v, Func<JsonElement, T> load)
        {
            var map = new Dictionary<string,T>();
            foreach (var item in v.EnumerateObject())
            {
                map.Add(item.Name,load(item.Value));
            }
            return map;
        }

        internal static SortedDictionary<string, T> GetOrderedMap<T>(JsonElement v, Func<JsonElement, T> load)
        {
            var map = new SortedDictionary<string, T>();
            foreach (var item in v.EnumerateObject())
            {
                map.Add(item.Name, load(item.Value));
            }
            return map;
        }

        internal static List<string> GetListOfString(JsonElement v)
        {
            var list = new List<string>();
            foreach (var item in v.EnumerateArray())
            {
                list.Add(item.GetString());
            }
            return list;
        }

        internal static HashSet<string> GetHashSetOfString(JsonElement v)
        {
            var hashSet = new HashSet<string>();
            foreach (var item in v.EnumerateArray())
            {
                hashSet.Add(item.GetString());
            }
            return hashSet;
        }

        internal static SortedSet<string> GetOrderedHashSetOfString(JsonElement v)
        {
            var sortedSet = new SortedSet<string>();
            foreach (var item in v.EnumerateArray())
            {
                sortedSet.Add(item.GetString());
            }
            return sortedSet;
        }

        internal static Dictionary<string, List<T>> GetMapOfLists<T>(JsonElement v, Func<JsonElement, T> load)
        {
            var map = new Dictionary<string, List<T>>();
            foreach (var item in v.EnumerateObject())
            {
                var entries = new List<T>();
                foreach (var entry in item.Value.EnumerateArray())
                {
                    entries.Add(load(entry));
                }

                map.Add(item.Name,  entries);
            }
            return map;
        }

        /// <summary>
        /// Parse properties.
        /// </summary>
        /// <param name="context">Name-value pair separated by ';'.</param>
        internal static Dictionary<string,string> ParseProperties(string context)
        {
            var properties = new Dictionary<string, string>();
            foreach (var pair in ParseKey(context))
            {
                properties.Add(pair.Key, pair.Value);
            }

            return properties;
        }

        /// <summary>
        /// Enumerate the key value pairs for the configuration key.
        /// </summary>
        /// <param name="key">Configuration key supplied in the setting.</param>
        /// *<returns>Key value pairs.</returns>
        internal static IEnumerable<KeyValuePair<string, string>> ParseKey(string key)
        {
            foreach (var pair in key.Split(';'))
            {
                if (string.IsNullOrEmpty(pair))
                {
                    continue;
                }

                var index = pair.IndexOf('=');
                if (index == -1)
                {
                    throw new InvalidOperationException($"Unable to parse: {key}. Format is name1=value1;name2=value2;...");
                }

                var keyValue = new KeyValuePair<string, string>(pair.Substring(0, index), pair.Substring(index + 1));
                yield return keyValue;
            }
        }



    }
    public class FixedFieldMap<T> : Dictionary<string, Action<T, JsonElement>>
    {

    }
}
