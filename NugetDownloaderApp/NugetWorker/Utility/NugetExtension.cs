﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NugetWorker
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

        public static void LogDllPathstoCSV(this List<DllInfo> dllInfos, string filename = "dlls.csv")
        {
            using (var sw = new StreamWriter(Path.Combine(NugetHelper.Instance.GetNugetSettings()
                                                                     .CSVDirectory,
                                                          filename)))
            {
                sw.AutoFlush = true;
                sw.WriteLine("packageName ,Path ," + "DllName ");

                foreach (var dllinfo in dllInfos)
                {
                    sw.WriteLine($"{dllinfo.rootPackage} " + $",{dllinfo.path} ," + $"{dllinfo.name} ");
                }
            }
        }

        public static void LogPackagestoCSV(this List<PackageWrapper> packageWrappers)
        {
            using (var sw = new StreamWriter(@"C:\Users\ppatidar\.nuget\packages.csv"))
            {
                sw.AutoFlush = true;
                sw.WriteLine("packageName ,version ," + "childPackageIdentitiesCount ");

                foreach (var packageWrapper in packageWrappers)
                {
                    sw.WriteLine($"{packageWrapper.packageName} ,{packageWrapper.version} ," + $"{packageWrapper.childPackageIdentities.Count} ");
                }
            }
        }

        #endregion
    }
}
