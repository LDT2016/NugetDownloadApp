using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NugetDownloaderApp.NugetWorker.Model;
using NugetDownloaderApp.NugetWorker.Utility;

namespace NugetDownloaderApp.NugetWorker
{
    public class NugetEngine
    {
        #region fields

        public List<DllInfo> dllInfos = new List<DllInfo>();

        #endregion

        #region methods

        public async Task GetPackage(string packageName, string version = "")
        {
            try
            {
                var packageFinder = new PackageFinder();
                var packageWrappers = packageFinder.GetListOfPackageIdentities(packageName, version);

                if (packageWrappers == null || packageWrappers.Count < 1)
                {
                    throw new Exception("enable to locate package!!!");
                }

                dllInfos.AddRange(packageFinder.dllInfos);
                NugetHelper.Instance.logger.LogDebug($"Total Dlls {dllInfos.Count} for rootpackage {packageName}-{version}");
            }
            catch (Exception ex)
            {
                NugetHelper.Instance.logger.LogDebug($"ERROR : {ex.Message} | {ex.StackTrace}");
            }
        }

        #endregion
    }
}
