using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository.Hierarchy;
using NuGet.Common;

namespace NugetWorker
{
    public class Logger : ILogger
    {
        #region constructors

        public Logger(string logpath)
        {
            //read the log4net config and create logger instance form log4net
            var ConfigLoader = new XmlDocument();
            ConfigLoader.Load(File.OpenRead("log4net.config"));
            var repo = LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(Hierarchy));
            XmlConfigurator.Configure(repo, ConfigLoader["log4net"]);

            var appender = (FileAppender)repo.GetAppenders()
                                             .Where(x => x.Name == "RollingLogFileAppender")
                                             .FirstOrDefault();
            appender.File = logpath; // $"NugetWorker/log/{DateTime.Now.ToString("yyyy_MM_dd")}_{Guid.NewGuid().ToString()}_.log";

            appender.ActivateOptions();
            _ILog = LogManager.GetLogger(typeof(Logger));
        }

        #endregion

        #region properties

        private ILog _ILog { get; }

        #endregion

        #region methods

        public void Log(LogLevel level, string data)
        {
            Console.WriteLine(data);
            _ILog.Info(data);
        }

        public void Log(ILogMessage message)
        {
            Console.WriteLine(message);
            _ILog.Info(message.Message);
        }

        public Task LogAsync(LogLevel level, string data)
        {
            Console.WriteLine(data);
            _ILog.Info(data);

            return null;
        }

        public Task LogAsync(ILogMessage message)
        {
            Console.WriteLine(message);
            _ILog.Info(message.Message);

            return null;
        }

        public void LogDebug(string data)
        {
            Console.WriteLine(data);
            _ILog.Debug(data);
        }

        public void LogError(string data)
        {
            Console.WriteLine(data);
            _ILog.Error(data);
        }

        public void LogInformation(string data)
        {
            Console.WriteLine(data);
            _ILog.Info(data);
        }

        public void LogInformationSummary(string data)
        {
            Console.WriteLine(data);
            _ILog.Info(data);
        }

        public void LogMinimal(string data)
        {
            Console.WriteLine(data);
            _ILog.Info(data);
        }

        public void LogVerbose(string data)
        {
            Console.WriteLine(data);
            _ILog.Debug(data);
        }

        public void LogWarning(string data)
        {
            Console.WriteLine(data);
            _ILog.Warn(data);
        }

        #endregion
    }
}
