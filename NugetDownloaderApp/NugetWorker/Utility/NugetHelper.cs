using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NugetDownloaderApp.NugetWorker.Model;

namespace NugetDownloaderApp.NugetWorker.Utility
{
    public sealed class NugetHelper
    {
        #region fields

        private static readonly Lazy<NugetHelper> lazy = new Lazy<NugetHelper>(() => new NugetHelper());
        private static ILogger _logger = new Logger("log.log");
        private NugetSettings _nugetSettings;

        #endregion

        #region constructors

        private NugetHelper() { }

        #endregion

        #region properties

        public static NugetHelper Instance => lazy.Value;

        public ILogger logger
        {
            get => _logger;
            set => _logger = value;
        }

        #endregion

        #region methods

        public List<PackageIdentity> GetChildPackageIdentities(IPackageSearchMetadata rootPackage)
        {
            var childPackageIdentities = new List<PackageIdentity>();

            //
            //load child package identity , if there is any dependency set
            if (rootPackage.DependencySets != null && rootPackage.DependencySets.Count() > 0)
            {
                //check specific dependency set for this version only  //note our _targetFramwork is .NETCoreApp,Version=v2.0
                //while the deps may be for .NETStandard,Version=v2.0 or .NETCoreApp,Version = v2.0 as well.

                var nugetFramwork = NuGetFramework.ParseFrameworkName(Instance.GetTargetFramwork(), new DefaultFrameworkNameProvider());
                var mostCompatibleFramework = Instance.GetMostCompatibleFramework(nugetFramwork, rootPackage.DependencySets);

                if (rootPackage.DependencySets.Any(x => x.TargetFramework == mostCompatibleFramework))
                {
                    var depsForTargetFramwork = rootPackage.DependencySets.Where(x => x.TargetFramework == mostCompatibleFramework)
                                                           .FirstOrDefault();

                    if (depsForTargetFramwork.Packages.Count() > 0)
                    {
                        foreach (var package in depsForTargetFramwork.Packages)
                        {
                            var _identity = new PackageIdentity(package.Id, package.VersionRange.MinVersion);

                            if (_identity != null)
                            {
                                childPackageIdentities.Add(_identity);
                            }
                        }
                    }
                }
            }

            //
            return childPackageIdentities;
        }

        public List<DllInfo> GetInstallPackagesDllPath(PackageWrapper packageWrapper, ref FolderNuGetProject project)
        {
            var dllinfo = new List<DllInfo>();

            var packageIdentity = packageWrapper.rootPackageIdentity;
            var packageFilePath = project.GetInstalledPackageFilePath(packageIdentity);

            if (!string.IsNullOrWhiteSpace(packageFilePath))
            {
                _logger.LogDebug(packageFilePath);

                var archiveReader = new PackageArchiveReader(packageFilePath);
                var nugetFramwork = NuGetFramework.ParseFrameworkName(Instance.GetTargetFramwork(), new DefaultFrameworkNameProvider());
                var referenceGroup = Instance.GetMostCompatibleGroup(nugetFramwork, archiveReader.GetReferenceItems());

                if (referenceGroup != null)
                {
                    foreach (var group in referenceGroup.Items)
                    {
                        var installedPackagedFolder = project.GetInstalledPath(packageIdentity);
                        var installedDllPath = string.Empty;

                        if (Instance.GetNugetSettings()
                                    .RunningOnwindows)
                        {
                            installedDllPath = Path.Combine(installedPackagedFolder, group.Replace("/", "\\"));
                        }
                        else
                        {
                            installedDllPath = Path.Combine(installedPackagedFolder, group);
                        }

                        var installedDllFolder = Path.GetDirectoryName(installedDllPath);
                        var dllName = Path.GetFileName(installedDllPath);

                        var extension = Path.GetExtension(installedDllPath)
                                            .ToLower();
                        var processor = group.GetProcessor();

                        _logger.LogDebug($"dll Path: {installedDllPath}");

                        //check if file path exist , then only add
                        if (File.Exists(installedDllPath) && extension == ".dll")
                        {
                            dllinfo.Add(new DllInfo
                                        {
                                            name = dllName,
                                            path = installedDllPath,
                                            framework = referenceGroup.TargetFramework.DotNetFrameworkName,
                                            processor = processor,
                                            rootPackage = packageIdentity.Id
                                        });
                        }

                        //also , try to cross refer this with expected folder name to avoid version mismatch
                    }
                }
            }

            return dllinfo;
        }

        public NuGetFramework GetMostCompatibleFramework(NuGetFramework projectTargetFramework, IEnumerable<PackageDependencyGroup> itemGroups)
        {
            var reducer = new FrameworkReducer();
            var mostCompatibleFramework = reducer.GetNearest(projectTargetFramework, itemGroups.Select(i => i.TargetFramework));

            return mostCompatibleFramework;
        }

        public FrameworkSpecificGroup GetMostCompatibleGroup(NuGetFramework projectTargetFramework, IEnumerable<FrameworkSpecificGroup> itemGroups)
        {
            var reducer = new FrameworkReducer();
            var mostCompatibleFramework = reducer.GetNearest(projectTargetFramework, itemGroups.Select(i => i.TargetFramework));

            if (mostCompatibleFramework != null)
            {
                var mostCompatibleGroup = itemGroups.FirstOrDefault(i => i.TargetFramework.Equals(mostCompatibleFramework));

                if (IsValid(mostCompatibleGroup))
                {
                    return mostCompatibleGroup;
                }
            }

            return null;
        }

        public NugetSettings GetNugetSettings()
        {
            if (_nugetSettings == null)
            {
                var watcherSettingsjson = GetNugetSettingsJson();
                _nugetSettings = ObjectConverter.Instance.GetWatcherSettingsFromJson(watcherSettingsjson);
            }

            return _nugetSettings;
        }

        public IEnumerable<SourceRepository> GetSourceRepos()
        {
            var providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3()); // Add v3 API s

            IList<SourceRepository> sourcerepos = new List<SourceRepository>();

            foreach (var nugetRepository in Instance.GetNugetSettings()
                                                    .NugetRepositories.OrderBy(x => x.Order))
            {
                var packageSource = new PackageSource(nugetRepository.Source, nugetRepository.Name);

                if (nugetRepository.IsPrivate)
                {
                    packageSource.Credentials = new PackageSourceCredential(nugetRepository.Name, nugetRepository.Username, nugetRepository.Password, nugetRepository.IsPasswordClearText);
                }

                var sourceRepository = new SourceRepository(packageSource, providers);
                sourcerepos.Add(sourceRepository);
            }

            if (sourcerepos.Count < 1)
            {
                throw new Exception("No Source Repository found!!");
            }

            return sourcerepos;
        }

        public string GetTargetFramwork()
        {
            var frameworkName = Assembly.GetExecutingAssembly()
                                        .GetCustomAttributes(true)
                                        .OfType<TargetFrameworkAttribute>()
                                        .Select(x => x.FrameworkName)
                                        .FirstOrDefault();

            var currentFramework = frameworkName == null
                                       ? NuGetFramework.AnyFramework
                                       : NuGetFramework.ParseFrameworkName(frameworkName, new DefaultFrameworkNameProvider());

            return frameworkName;
        }

        private string GetNugetSettingsJson()
        {
            var baselocation = AppDomain.CurrentDomain.BaseDirectory;
            var FileLocation = baselocation + "nugetSettings.json";

            return File.ReadAllText(FileLocation);
        }

        private bool IsValid(FrameworkSpecificGroup frameworkSpecificGroup)
        {
            if (frameworkSpecificGroup != null)
            {
                return frameworkSpecificGroup.HasEmptyFolder || frameworkSpecificGroup.Items.Any() || !frameworkSpecificGroup.TargetFramework.Equals(NuGetFramework.AnyFramework);
            }

            return false;
        }

        #endregion
    }
}
