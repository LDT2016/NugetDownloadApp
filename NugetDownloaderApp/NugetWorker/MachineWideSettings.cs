using System;
using System.Collections.Generic;
using NuGet.Common;
using NuGet.Configuration;

namespace NugetWorker
{
    public class MachineWideSettings : IMachineWideSettings
    {
        #region fields

        private readonly Lazy<IEnumerable<Settings>> _settings;

        #endregion

        #region constructors

        public MachineWideSettings()
        {
            var baseDirectory = NuGetEnvironment.GetFolderPath(NuGetFolderPath.MachineWideConfigDirectory);
            _settings = new Lazy<IEnumerable<Settings>>(() => NuGet.Configuration.Settings.LoadMachineWideSettings(baseDirectory));
        }

        #endregion

        #region properties

        public IEnumerable<Settings> Settings => _settings.Value;

        #endregion
    }
}
