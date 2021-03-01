using System.Collections.Generic;

namespace NugetDownloaderApp.NugetWorker.Model
{
    public class NugetSettings
    {
        #region properties

        public string CSVDirectory { get; set; }
        public bool DisableCache { get; set; }
        public string ExcludeDllRegEx { get; set; }
        public string NugetFolder { get; set; }
        public string NugetPackageRegEx { get; set; }
        public List<NugetRepository> NugetRepositories { get; set; }
        public bool RunningOnwindows { get; set; }

        #endregion
    }
}
