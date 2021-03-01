using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugetWorker
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
            _targetFramwork = NugetHelper.Instance.GetTargetFramwork();
            _logger = NugetHelper.Instance.logger;

            _sourceRepos = NugetHelper.Instance.GetSourceRepos();

             packageDownloder = new PackageDownloder();
        }

        #endregion

        #region properties

        public ILogger _logger { get; set; }
        public IEnumerable<SourceRepository> _sourceRepos { get; set; }
        public string _targetFramwork { get; set; }

        #endregion

        #region methods

        //public List<PackageWrapper> GetListOfPackageIdentities(string packageName, string version)
        //{

        //    var packageWrapper=GetPackageByExact( packageName,  version);
        //    foreach(var childPackageIdentity in packageWrapper.childPackageIdentities)
        //    {
        //        packageWrappers.AddRange(GetListOfPackageIdentities(childPackageIdentity.Id, childPackageIdentity.Version.Version.ToString()));
        //    }
        //   // packageWrappers.Add(packageWrapper);
        //    return packageWrappers;
        //}

        public List<PackageWrapper> GetListOfPackageIdentities(string packageName, string version)
        {
            //thats a recursive loop , this will give list of root as well as all dependency as root during recursion 
            GetListOfPackageIdentitiesRecursive(packageName, version);
            Task.WhenAll(_packageDowloadTasks).Wait();
            packageWrappers = packageWrappers.DistinctBy(x => x.packageName).ToList();
            dllInfos = packageDownloder.downloadedDllPaths.DistinctBy(x=>x.path).ToList();
            
             return packageWrappers;
        }

        public PackageWrapper GetPackageByExactSearch(string packageName, string version)
        {
            var packageFound = false;
            PackageWrapper packageWrapper = null;

            #region processing 
            foreach (var sourceRepository in _sourceRepos)
            {
                if (!packageFound)
                {
                    // _logger.LogInformation("###################################################################");
                    //_logger.LogInformation($"Looking in Repo {sourceRepository.PackageSource.Source} for package : {packageName}");

                    //extact search 
                    var packageMetadataResource = sourceRepository
                                                  .GetResourceAsync<PackageMetadataResource>().Result;

                    var sourceCacheContext = new SourceCacheContext();

                    ////below will slow down search as it is disabling search
                    if(NugetHelper.Instance.GetNugetSettings().DisableCache)
                    {
                        sourceCacheContext.NoCache = true;
                        sourceCacheContext.DirectDownload = true;
                    }

                    IPackageSearchMetadata rootPackage = null;

                    //if user has mentioned version , then search specifcially for that version only , else get latest version
                    if (!string.IsNullOrWhiteSpace(version))
                    {
                         rootPackage = GetPackageFromRepoWithVersion(packageName, version,
                         packageMetadataResource, sourceCacheContext, sourceRepository);
                    }
                    else
                    {
                        rootPackage = GetPackageFromRepoWithoutVersion(packageName, 
                         packageMetadataResource, sourceCacheContext, sourceRepository);
                    }

                    if (rootPackage == null)
                    {
                        _logger.LogInformation(" No Package found in Repo " +
                            $"{sourceRepository.PackageSource.Source} for package : {packageName} | {version}");

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

                    _logger.LogInformation($"Latest Package form Exact Search : {packageWrapper.packageName }" +
                                           $"| {packageWrapper.version } in Repo {sourceRepository.PackageSource.Source}");

                    packageFound = true;
                    //as package is found , we can break loop here for this package, but keeping above bool as well for testing
                     
                    _logger.LogInformation("---------------------------------------------------------------------");
                    break;
                }

            }

            #endregion 

            return packageWrapper;
        }

        /// <summary>
        /// Obsolute : Find app matching package with that keyword , but implemented C# code  is not same as GetPackageByExact
        /// If you want to use it  , then change internal implementation as well
        /// </summary>
        /// <returns></returns>
        /// 
        public PackageWrapper GetPackageByFullSearch(string packageName, string version)
        {
            var packageFound = false;
            PackageWrapper packageWrapper = null;

            #region processing
            _logger.LogInformation($"Target frameworkName : {_targetFramwork}");


            //create search criteria
            var filter = new SearchFilter(true, SearchFilterType.IsLatestVersion);
            filter.SupportedFrameworks = new List<string>
                                         { _targetFramwork };
            

            foreach (var sourceRepository in _sourceRepos)
            {
                if (!packageFound)
                {
                    // _logger.LogInformation("###################################################################");
                    //_logger.LogInformation($"Looking in Repo {sourceRepository.PackageSource.Source} for package : {packageName}");
                    
                    //create package source resource - Full Search  - all matching keywords
                    var searchResource = sourceRepository.
                                         GetResourceAsync<PackageSearchResource>().Result;
                    var FullsearchResults = searchResource
                                            .SearchAsync(packageName, filter, 0, 20, _logger, CancellationToken.None).Result;

                    if (FullsearchResults.Count() == 0 || 
                        !FullsearchResults.Any(x=>x.Identity.Id.ToLower()==packageName.ToLower()))
                    {
                        _logger.LogInformation($"Full Search - No Package found in Repo {sourceRepository.PackageSource.Source} for package : {packageName}");
                    }
                    else
                    {
                        //only list relavant package
                        FullsearchResults = FullsearchResults.Where(x => x.Identity.Id.ToLower() == packageName.ToLower());

                        packageFound = true;
                        //got list of matching package , iterate throug it
                        //foreach (var pgkdata in FullsearchResults)
                        //{
                        //    _logger.LogInformation($"Full search Package Located: {pgkdata.Title} | { pgkdata.Identity.Version}");
                        //    //identity = pgkdata.Identity;
                        //}

                        //initilize wrapper
                        packageWrapper = new PackageWrapper();
                        IPackageSearchMetadata rootPackage;
                        //if version number is present , select that specific package , else select latest
                        if (!string.IsNullOrWhiteSpace(version))
                        {
                            //not checking against Identity.Version & Identity.Version.Version is diffrent
                            //Identity.Version.Version is more accurate but may result in diffrent packages
                            rootPackage = FullsearchResults.Where(x => x.Identity.HasVersion
                            && x.Identity.Version.Version.ToString() == version).LastOrDefault();

                            //it is possible that no matching version found , then fallback to latest 
                            if (rootPackage == null)
                            {
                                rootPackage = FullsearchResults.OrderByDescending(x => x.Identity.Version)
                                .FirstOrDefault();
                            }
                        }
                        else //select latest version
                        {
                            rootPackage = FullsearchResults.OrderByDescending(x => x.Identity.Version)
                             .FirstOrDefault();
                        }
                        packageWrapper.rootPackageIdentity = rootPackage.Identity;

                        packageWrapper.packageName = packageWrapper.rootPackageIdentity.Id;

                        packageWrapper.version = packageWrapper.rootPackageIdentity.Version;
                        //save the repo infor as well so that during install it doesnt need to search on all repos
                        packageWrapper.sourceRepository = sourceRepository;

                        //load child package identities
                        packageWrapper.childPackageIdentities = NugetHelper.Instance.GetChildPackageIdentities(rootPackage);

                        _logger.LogInformation("Exact Package form Full Search : " +
                            $"{packageWrapper.packageName }| {packageWrapper.version }");
                    }

                }

            }

            #endregion

            return packageWrapper;
        }

        public IPackageSearchMetadata GetPackageFromRepoWithoutVersion(string packageName,
                                                                       PackageMetadataResource packageMetadataResource, SourceCacheContext sourceCacheContext,
                                                                       SourceRepository sourceRepository)
        {
            IPackageSearchMetadata rootPackage = null;
            var ExacactsearchMetadata = packageMetadataResource
                                        .GetMetadataAsync(packageName, true, true, sourceCacheContext, _logger, CancellationToken.None).Result;

            if (ExacactsearchMetadata.Count()==0)
            {
                _logger.LogInformation("GetPackageFromRepoWithoutVersion - No Package & any version  found in Repo " +
                    $"{sourceRepository.PackageSource.Source} for package : {packageName}");
            }
            else //select latest version
            {
                rootPackage = ExacactsearchMetadata.OrderByDescending(x => x.Identity.Version)
                   .FirstOrDefault();
            }

            return rootPackage;
        }

        public IPackageSearchMetadata GetPackageFromRepoWithVersion(string packageName, string version , 
                                                                    PackageMetadataResource packageMetadataResource, SourceCacheContext sourceCacheContext,
                                                                    SourceRepository sourceRepository)
        {
            IPackageSearchMetadata rootPackage = null;

            //if version is there , then first try to get version specific 
            //this one will ignore version number and if same package with another version is present , 
            // then it will take that existing version instead of requested 
            //IEnumerable<IPackageSearchMetadata> ExacactsearchMetadata = packageMetadataResource
            //    .GetMetadataAsync(packageName, true, true, sourceCacheContext, _logger, CancellationToken.None).Result;

            PackageIdentity packageIdentity = null;
            NuGetVersion nugetversion=null;
            if(NuGetVersion.TryParse(version,out nugetversion))
            {
                packageIdentity = new PackageIdentity(packageName, NuGetVersion.Parse(version));
                var ExacactsearchMetadata = packageMetadataResource
                                            .GetMetadataAsync(packageIdentity, sourceCacheContext, _logger, CancellationToken.None).Result;

                if (ExacactsearchMetadata == null)
                {
                    _logger.LogInformation("GetPackageFromRepoWithVersion - No Package found in Repo " +
                        $"{sourceRepository.PackageSource.Source} for package : {packageName}  with version  {version}");
                    
                    //need to discuss if fallback  should be here as well
                }
                else
                {
                    rootPackage = ExacactsearchMetadata;
                }
            }
            
            return rootPackage;
        }

        private void GetListOfPackageIdentitiesRecursive(string packageName, string version)
        {

          
            var  packageWrapper = GetPackageByExactSearch(packageName, version);
            if(packageWrapper==null)
            {
                return;
            }
            if(packageWrapper.childPackageIdentities.Count>0)
            {
                Parallel.ForEach(packageWrapper.childPackageIdentities, childPackageIdentity =>
                 {
                     if (!packageWrappers.ToList().Any(x => x.packageName.ToLower() == childPackageIdentity.Id.ToLower()))
                     {
                         GetListOfPackageIdentitiesRecursive(childPackageIdentity.Id,
                         childPackageIdentity.Version.Version.ToString());
                     }
                 });
                //foreach (var childPackageIdentity in packageWrapper.childPackageIdentities)
                //{
                //    if(!packageWrappers.ToList().Any(x=>x.packageName.ToLower()==childPackageIdentity.Id.ToLower()))
                //    {
                //        GetListOfPackageIdentitiesRecursive(childPackageIdentity.Id,
                //        childPackageIdentity.Version.Version.ToString());
                //    }
             
                //}
              
            }

            //packageDownloder.DownloadPackage(packageWrapper);
            _packageDowloadTasks.Add(packageDownloder.DownloadPackage(packageWrapper));
            //packageDownloder.DownloadPackage(packageWrapper);
            
            lock (islocked)
            {
                packageWrappers.Add(packageWrapper);
            }
            
        }

        #endregion
    }
}
