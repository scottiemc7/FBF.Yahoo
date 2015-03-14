using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FBF.Yahoo.Extensions
{
    public static class UriExtensions
    {
        private static Regex QUERYSTRINGREGEX = new Regex("((?:\\?|&)(?<param>[^=]+)=(?<value>[^&]+))");

        public static string ToUriWithSortedQueryString(this Uri uri)
        {
            string sortedQuery = uri.ToUriWithoutQueryString();
            if (!String.IsNullOrEmpty(uri.Query))
            {
                SortedDictionary<string, string> q = new SortedDictionary<string, string>();
                foreach (Match m in QUERYSTRINGREGEX.Matches(uri.Query))
                    q.Add(m.Groups["param"].Value, m.Groups["value"].Value);
                sortedQuery += "?";
                foreach (string key in q.Keys)
                    sortedQuery += key + "=" + q[key] + "&";
                sortedQuery = sortedQuery.TrimEnd('&');
            }
            return sortedQuery;
        }

        public static string ToUriWithoutQueryString(this Uri uri)
        {
            return String.Format("{0}://{1}{2}", uri.Scheme, uri.Host, uri.LocalPath);
        }
    }
}
