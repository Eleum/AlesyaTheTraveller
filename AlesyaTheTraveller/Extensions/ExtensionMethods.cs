using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlesyaTheTraveller.Extensions
{
    public static class ExtensionMethods
    {
        public static string GetAndRemove(this Dictionary<string, string> dict, string key)
        {
            var item = dict[key];
            dict.Remove(key);

            return item;
        }
    }
}
