using System;
using System.Collections.Generic;
using System.Linq;

namespace NugetDownloaderApp.NugetWorker.Utility
{
    public static class NugetExtension
    {
        #region methods

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var knownKeys = new HashSet<TKey>();

            return source.Where(element => knownKeys.Add(keySelector(element)));
        }

        public static string GetProcessor(this string folder)
        {
            if (folder.ToLower()
                      .Contains("x86"))
            {
                return "x86";
            }

            if (folder.ToLower()
                      .Contains("x64"))
            {
                return "x64";
            }

            return "NA";
        }

        #endregion
    }
}
