using System.Collections.Generic;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugetDownloaderApp.NugetWorker.Model
{
    public class PackageWrapper
    {
        #region properties

        public List<PackageIdentity> childPackageIdentities { get; set; }
        public string packageName { get; set; }
        public string PossibleFolder => $"{packageName}.{version.Version}";
        public PackageIdentity rootPackageIdentity { get; set; }
        public SourceRepository sourceRepository { get; set; }
        public NuGetVersion version { get; set; }

        #endregion
    }
}
