namespace NugetDownloaderApp.NugetWorker.Model
{
    public class DllInfo
    {
        #region properties

        public string framework { get; set; }
        public string name { get; set; }
        public string path { get; set; }
        public string processor { get; set; }
        public string rootPackage { get; set; }

        #endregion
    }
}
