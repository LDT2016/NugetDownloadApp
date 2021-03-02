using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NugetDownloaderApp.NugetWorker.Model;
using NugetDownloaderApp.NugetWorker.Utility;

namespace NugetDownloaderApp.NugetWorker
{
    public class PackageFinder
    {
        #region fields

        public List<DllInfo> dllInfos = new List<DllInfo>();
        public object islocked = new object();
        private readonly List<Task> _packageDowloadTasks = new List<Task>();
        private readonly PackageDownloder packageDownloder;
        private List<PackageWrapper> packageWrappers = new List<PackageWrapper>();

        #endregion

        #region constructors

        public PackageFinder()
        {
            TargetFramwork = NugetHelper.Instance.GetTargetFramwork();
            Logger = NugetHelper.Instance.logger;
            SourceRepos = NugetHelper.Instance.GetSourceRepos();
            packageDownloder = new PackageDownloder();
        }

        #endregion

        #region properties

        public ILogger Logger { get; set; }
        public IEnumerable<SourceRepository> SourceRepos { get; set; }
        public string TargetFramwork { get; set; }

        #endregion

        #region methods

        public List<PackageWrapper> GetListOfPackageIdentities(string packageName, string version)
        {
            GetListOfPackageIdentitiesRecursive(packageName, version);

            Task.WhenAll(_packageDowloadTasks)
                .Wait();

            packageWrappers = packageWrappers.DistinctBy(x => x.packageName)
                                             .ToList();

            dllInfos = packageDownloder.downloadedDllPaths.DistinctBy(x => x.path)
                                       .ToList();

            return packageWrappers;
        }

        public PackageWrapper GetPackageByExactSearch(string packageName, string version)
        {
            var packageFound = false;
            PackageWrapper packageWrapper = null;

            #region processing

            foreach (var sourceRepository in SourceRepos)
            {
                if (!packageFound)
                {
                    //extact search 
                    var packageMetadataResource = sourceRepository.GetResourceAsync<PackageMetadataResource>()
                                                                  .Result;

                    var sourceCacheContext = new SourceCacheContext();

                    ////below will slow down search as it is disabling search
                    if (NugetHelper.Instance.GetNugetSettings()
                                   .DisableCache)
                    {
                        sourceCacheContext.NoCache = true;
                        sourceCacheContext.DirectDownload = true;
                    }

                    IPackageSearchMetadata rootPackage;

                    //if user has mentioned version , then search specifcially for that version only , else get latest version
                    if (!string.IsNullOrWhiteSpace(version))
                    {
                        rootPackage = GetPackageFromRepoWithVersion(packageName, version, packageMetadataResource, sourceCacheContext, sourceRepository);
                    }
                    else
                    {
                        rootPackage = GetPackageFromRepoWithoutVersion(packageName, packageMetadataResource, sourceCacheContext, sourceRepository);
                    }

                    if (rootPackage == null)
                    {
                        Logger.LogDebug(" No Package found in Repo " + $"{sourceRepository.PackageSource.Source} for package : {packageName} | {version}");

                        //as we have not found package , there is no need to process further ,look for next repo by continue
                        continue;
                    }

                    packageWrapper = new PackageWrapper();
                    packageWrapper.rootPackageIdentity = rootPackage.Identity;
                    packageWrapper.packageName = packageWrapper.rootPackageIdentity.Id;

                    packageWrapper.version = packageWrapper.rootPackageIdentity.Version;

                    //save the repo infor as well so that during install it doesnt need to search on all repos
                    packageWrapper.sourceRepository = sourceRepository;

                    //load child package identities
                    packageWrapper.childPackageIdentities = NugetHelper.Instance.GetChildPackageIdentities(rootPackage);

                    Logger.LogDebug($"Latest Package form Exact Search : {packageWrapper.packageName}" + $"| {packageWrapper.version} in Repo {sourceRepository.PackageSource.Source}");

                    packageFound = true;
                }
            }

            #endregion

            return packageWrapper;
        }

        public IPackageSearchMetadata GetPackageFromRepoWithoutVersion(string packageName, PackageMetadataResource packageMetadataResource, SourceCacheContext sourceCacheContext, SourceRepository sourceRepository)
        {
            IPackageSearchMetadata rootPackage = null;

            var exacactsearchMetadata = packageMetadataResource.GetMetadataAsync(packageName, true, true, sourceCacheContext, Logger, CancellationToken.None)
                                                               .Result;

            if (exacactsearchMetadata.Count() == 0)
            {
                Logger.LogDebug("GetPackageFromRepoWithoutVersion - No Package & any version  found in Repo " + $"{sourceRepository.PackageSource.Source} for package : {packageName}");
            }
            else //select latest version
            {
                rootPackage = exacactsearchMetadata.OrderByDescending(x => x.Identity.Version)
                                                   .FirstOrDefault();
            }

            return rootPackage;
        }

        public IPackageSearchMetadata GetPackageFromRepoWithVersion(string packageName,
                                                                    string version,
                                                                    PackageMetadataResource packageMetadataResource,
                                                                    SourceCacheContext sourceCacheContext,
                                                                    SourceRepository sourceRepository)
        {
            IPackageSearchMetadata rootPackage = null;


            if (NuGetVersion.TryParse(version, out var _nugetversion))
            {
                var packageIdentity = new PackageIdentity(packageName, NuGetVersion.Parse(version));

                var exacactsearchMetadata = packageMetadataResource.GetMetadataAsync(packageIdentity, sourceCacheContext, Logger, CancellationToken.None)
                                                                   .Result;

                if (exacactsearchMetadata == null)
                {
                    Logger.LogDebug("GetPackageFromRepoWithVersion - No Package found in Repo " + $"{sourceRepository.PackageSource.Source} for package : {packageName}  with version  {version}");
                }
                else
                {
                    rootPackage = exacactsearchMetadata;
                }
            }

            return rootPackage;
        }

        private void GetListOfPackageIdentitiesRecursive(string packageName, string version)
        {
            var packageWrapper = GetPackageByExactSearch(packageName, version);
            if (packageWrapper == null)
            {
                return;
            }
            if (packageWrapper.childPackageIdentities.Count > 0)
            {
                Parallel.ForEach(packageWrapper.childPackageIdentities, childPackageIdentity =>
                 {
                     if (!packageWrappers.ToList().Any(x => x.packageName.ToLower() == childPackageIdentity.Id.ToLower()))
                     {
                         GetListOfPackageIdentitiesRecursive(childPackageIdentity.Id,
                         childPackageIdentity.Version.Version.ToString());
                     }
                 });
            }

            _packageDowloadTasks.Add(packageDownloder.DownloadPackage(packageWrapper));

            lock (islocked)
            {
                packageWrappers.Add(packageWrapper);
            }

        }

        #endregion
    }
}
