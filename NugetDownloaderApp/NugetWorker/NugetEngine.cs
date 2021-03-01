using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NugetWorker
{
    public class NugetEngine
    {
        #region fields

        public List<DllInfo> dllInfos = new List<DllInfo>();
        private List<Task> packageDowloadTasks = new List<Task>();
        private IList<PackageWrapper> packageWrappers = new List<PackageWrapper>();

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
                Console.WriteLine($"Total Dlls {dllInfos.Count} for rootpackage {packageName}-{version}");
                Console.WriteLine("done with nuget engine!!!! ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR : {ex.Message} | {ex.StackTrace}");
            }
        }

        #endregion
    }
}
