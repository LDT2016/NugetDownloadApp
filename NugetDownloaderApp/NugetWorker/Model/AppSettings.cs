using System.Collections.Generic;

namespace NugetDownloaderApp.NugetWorker.Model
{
    public class NugetSettings
    {
        #region properties

        public bool DisableCache { get; set; }
        public string NugetFolder { get; set; }
        public bool RunningOnwindows { get; set; }
        public List<NugetRepository> NugetRepositories { get; set; }
        #endregion
    }
}
