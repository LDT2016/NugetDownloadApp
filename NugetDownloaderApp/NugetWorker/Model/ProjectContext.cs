using System;
using System.Xml.Linq;
using NuGet.Packaging;
using NuGet.ProjectManagement;

namespace NugetWorker
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
            Console.WriteLine(message);
        }

        public void ReportError(string message)
        {
            Console.WriteLine(message);
        }

        public FileConflictAction ResolveFileConflict(string message)
        {
            return FileConflictAction.Ignore;
        }

        #endregion
    }
}
