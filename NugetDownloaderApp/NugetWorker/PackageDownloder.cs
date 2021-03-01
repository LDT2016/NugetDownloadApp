using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.ProjectManagement;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NugetDownloaderApp.NugetWorker.Model;
using NugetDownloaderApp.NugetWorker.Utility;

namespace NugetDownloaderApp.NugetWorker
{
    public class PackageDownloder
    {
        #region fields

        public List<DllInfo> downloadedDllPaths = new List<DllInfo>();

        #endregion

        #region constructors

        public PackageDownloder()
        {
            _targetFramwork = NugetHelper.Instance.GetTargetFramwork();
            _logger = NugetHelper.Instance.logger;

            //_sourceRepos = NugetHelper.Instance.GetSourceRepos();
        }

        #endregion

        #region properties

        private ILogger _logger { get; }
        private IList<SourceRepository> _sourceRepos { get; set; }
        private string _targetFramwork { get; }

        #endregion

        #region methods

        public async Task DownloadPackage(PackageWrapper packageWrapper)
        {
            try
            {
                //this will prevent install to look in all repos
                _sourceRepos = new List<SourceRepository>();
                _sourceRepos.Add(packageWrapper.sourceRepository);

                var packageIdentity = packageWrapper.rootPackageIdentity;
                var providers = new List<Lazy<INuGetResourceProvider>>();
                providers.AddRange(Repository.Provider.GetCoreV3()); // Add v3 API s

                var rootPath = NugetHelper.Instance.GetNugetSettings()
                                          .NugetFolder;

                //var settings = Settings.LoadDefaultSettings(@"C:\Program Files (x86)\NuGet\Config", "Microsoft.VisualStudio.Offline.config", new MachineWideSettings());
                //var machineWideSettings = new MachineWideSettings();
                var settings = new Settings(rootPath);
                var packageSourceProvider = new PackageSourceProvider(settings);
                var sourceRepositoryProvider = new SourceRepositoryProvider(packageSourceProvider, providers);
                var project = new FolderNuGetProject(rootPath);

                var packageManager = new NuGetPackageManager(sourceRepositoryProvider, settings, rootPath)
                                     {
                                         PackagesFolderNuGetProject = project
                                     };

                var allowPrereleaseVersions = true;
                var allowUnlisted = false;
                INuGetProjectContext projectContext = new ProjectContext();

                var resolutionContext = new ResolutionContext(DependencyBehavior.Lowest, allowPrereleaseVersions, allowUnlisted, VersionConstraints.None);

                if (NugetHelper.Instance.GetNugetSettings()
                               .DisableCache)
                {
                    resolutionContext.SourceCacheContext.NoCache = true;
                    resolutionContext.SourceCacheContext.DirectDownload = true;
                }

                var downloadContext = new PackageDownloadContext(resolutionContext.SourceCacheContext, rootPath, resolutionContext.SourceCacheContext.DirectDownload);

                var packageAlreadyExists = packageManager.PackageExistsInPackagesFolder(packageIdentity, PackageSaveMode.None);

                if (!packageAlreadyExists)
                {
                    await packageManager.InstallPackageAsync(project, packageIdentity, resolutionContext, projectContext, downloadContext, _sourceRepos, new List<SourceRepository>(), CancellationToken.None);

                    var packageDeps = packageManager.GetInstalledPackagesDependencyInfo(project, CancellationToken.None, true);
                    _logger.LogDebug($"Package {packageIdentity.Id} is got Installed at  | {project.GetInstalledPath(packageIdentity)} ");
                }
                else
                {
                    var packageDeps = packageManager.GetInstalledPackagesDependencyInfo(project, CancellationToken.None, true);
                    _logger.LogDebug($"Package {packageIdentity.Id} is Already Installed at  | {project.GetInstalledPath(packageIdentity)} " + " | skipping instalation !!");
                }

                #region GetDll paths

                var dllstoAdd = NugetHelper.Instance.GetInstallPackagesDllPath(packageWrapper, ref project);

                if (dllstoAdd.Count > 0)
                {
                    downloadedDllPaths.AddRange(dllstoAdd);
                }

                ////now iterate for child identities , but as we have alreayd written login for recursive install , check if this
                //is now really required or not ?

                //if (packageWrapper.childPackageIdentities != null && packageWrapper.childPackageIdentities.Count > 0)
                //{
                //    foreach (var childPackageIdentity in packageWrapper.childPackageIdentities)
                //    {

                //        var _dllstoAdd = NugetHelper.Instance.GetInstallPackagesDllPath(packageWrapper., ref project);
                //        if (_dllstoAdd.Count > 0)
                //        {
                //            downloadedDllPaths.AddRange(_dllstoAdd);
                //        }

                //    }
                //}

                #endregion

                _logger.LogDebug($"done for package {packageIdentity.Id} , with total Dlls {downloadedDllPaths.Count}");
            }
            catch (Exception e)
            {
                NugetHelper.Instance.logger.LogDebug(e.Message);

                throw;
            }
        }

        #endregion
    }
}
