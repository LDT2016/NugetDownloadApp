using System;
using System.Xml.Linq;
using NuGet.Packaging;
using NuGet.ProjectManagement;
using NugetDownloaderApp.NugetWorker.Utility;

namespace NugetDownloaderApp.NugetWorker.Model
{
    public class ProjectContext : INuGetProjectContext
    {
        #region properties

        public NuGetActionType ActionType { get; set; }
        public ExecutionContext ExecutionContext => null;
        public Guid OperationId { get; set; }
        public XDocument OriginalPackagesConfig { get; set; }
        public PackageExtractionContext PackageExtractionContext { get; set; }
        public ISourceControlManagerProvider SourceControlManagerProvider => null;

        #endregion

        #region methods

        public void Log(MessageLevel level, string message, params object[] args)
        {
            // Do your logging here...
            NugetHelper.Instance.logger.LogDebug(message);
        }

        public void ReportError(string message)
        {
            NugetHelper.Instance.logger.LogDebug(message);
        }

        public FileConflictAction ResolveFileConflict(string message)
        {
            return FileConflictAction.Ignore;
        }

        #endregion
    }
}
