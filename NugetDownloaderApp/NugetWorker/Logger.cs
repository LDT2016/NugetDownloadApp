using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository.Hierarchy;
using NuGet.Common;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NugetDownloaderApp.NugetWorker
{
    public class Logger : ILogger
    {
        private StringBuilder _logData = new StringBuilder();

        #region constructors

        public Logger(string logpath)
        {
            //read the log4net config and create logger instance form log4net
            var configLoader = new XmlDocument();
            configLoader.Load(File.OpenRead("log4net.config"));
            var repo = LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(Hierarchy));
            XmlConfigurator.Configure(repo, configLoader["log4net"]);

            var appender = (FileAppender)repo.GetAppenders()
                                             .Where(x => x.Name == "LogFileAppender")
                                             .FirstOrDefault();
            appender.File = logpath; 

            appender.ActivateOptions();
            _log = LogManager.GetLogger(typeof(Logger));
        }

        #endregion

        #region properties

        private ILog _log { get; }

        public StringBuilder LogData
        {
            get => _logData;
            set => _logData = value;
        }

        #endregion

        #region methods

        public void Log(LogLevel level, string data)
        {
            Console.WriteLine(data);
            _log.Info(data);
            _logData.AppendLine(data);
        }

        public void Log(ILogMessage message)
        {
            Console.WriteLine(message);
            _log.Info(message.Message);
            _logData.AppendLine(message.Message);
        }

        public Task LogAsync(LogLevel level, string data)
        {
            Console.WriteLine(data);
            _log.Info(data);
            _logData.AppendLine(data);
            return null;
        }

        public Task LogAsync(ILogMessage message)
        {
            Console.WriteLine(message);
            _log.Info(message.Message);
            _logData.AppendLine(message.Message);

            return null;
        }

        public void LogDebug(string data)
        {
            Console.WriteLine(data);
            _log.Debug(data);
            _logData.AppendLine(data);
        }

        public void LogError(string data)
        {
            Console.WriteLine(data);
            _log.Error(data);
            _logData.AppendLine(data);
        }

        public void LogInformation(string data)
        {
            Console.WriteLine(data);
            _log.Info(data);
            _logData.AppendLine(data);
        }

        public void LogInformationSummary(string data)
        {
            Console.WriteLine(data);
            _log.Info(data);
            _logData.AppendLine(data);
        }

        public void LogMinimal(string data)
        {
            Console.WriteLine(data);
            _log.Info(data);
            _logData.AppendLine(data);
        }

        public void LogVerbose(string data)
        {
            Console.WriteLine(data);
            _log.Debug(data);
            _logData.AppendLine(data);
        }

        public void LogWarning(string data)
        {
            Console.WriteLine(data);
            _log.Warn(data);
            _logData.AppendLine(data);
        }

        #endregion
    }
}
