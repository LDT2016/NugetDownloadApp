using System;
using System.Reflection;

namespace NugetDownloaderApp.Reflction
{
    public class Loader : MarshalByRefObject
    {
        #region fields

        private Assembly _assembly;

        #endregion

        #region methods

        public object ExecuteStaticMethod(string typeName, string methodName, params object[] parameters)
        {
            var type = _assembly.GetType(typeName);

            var method = type.GetMethod(methodName);

            return method?.Invoke(null, parameters);
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void LoadAssembly(string path)
        {
            _assembly = Assembly.Load(AssemblyName.GetAssemblyName(path));
        }

        public void LoadAssembly(byte[] rawAssembly)
        {
            _assembly = Assembly.Load(rawAssembly);
        }

        #endregion
    }
}
