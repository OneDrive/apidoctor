using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation.Utility
{
    internal static class ExtensionMethods
    {

        public static void AddToList<TKey, TValue>(this Dictionary<TKey,List<TValue>> dict, TKey key, params TValue[] values)
        {
            lock (dict)
            {
                List<TValue> existingList = null;
                if (dict.TryGetValue(key, out existingList))
                {
                    existingList.AddRange(values);
                }
                else
                {
                    var list = new List<TValue>();
                    list.AddRange(values);
                    dict.Add(key, list);
                }
            }
        }

    }
}
