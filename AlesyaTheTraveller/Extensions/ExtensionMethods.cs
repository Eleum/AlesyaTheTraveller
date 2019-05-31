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

        public static string ReplaceForEach(this string str, string[] values, string replacement = "")
        {
            foreach(var value in values)
            {
                str = str.Replace(value, replacement);
            }

            return str;
        }

        public static string ReplaceForEach(this string str, string formattedString)
        {
            if (string.IsNullOrWhiteSpace(formattedString))
                return str;

            var parts = formattedString.Split(";");
            if (parts.Length == 0)
                return str;

            foreach(var part in parts)
            {
                var innerParts = part.Split("|");
                if (innerParts.Length == 0)
                    continue;

                if (string.IsNullOrWhiteSpace(innerParts[1]))
                    continue;

                str = str.Replace(innerParts[0].ToLower(), innerParts[1]);
            }

            return str;
        }
    }
}
