using System;
using Newtonsoft.Json;

namespace NugetWorker.Utility
{
    public sealed class ObjectConverter
    {
        #region fields

        private static readonly Lazy<ObjectConverter> lazy = new Lazy<ObjectConverter>(() => new ObjectConverter());

        #endregion

        #region constructors

        private ObjectConverter() { }

        #endregion

        #region properties

        public static ObjectConverter Instance => lazy.Value;

        #endregion

        #region methods

        public NugetSettings GetWatcherSettingsFromJson(string json)
        {
            return JsonConvert.DeserializeObject<NugetSettings>(json);
        }

        #endregion
    }
}
