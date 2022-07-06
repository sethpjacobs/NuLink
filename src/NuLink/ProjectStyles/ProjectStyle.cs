using System.Xml.Linq;
using Buildalyzer;

namespace NuLink.ProjectStyles
{
    public abstract class ProjectStyle
    {
        public const string MsbuildNamespaceUri = "http://schemas.microsoft.com/developer/msbuild/2003";
        
        protected ProjectStyle(IUserInterface ui, IProjectAnalyzer project, XElement projectXml)
        {
            this.UI = ui;
            this.Project = project;
            this.ProjectXml = projectXml;
        }

        public abstract IEnumerable<PackageReferenceInfo> LoadPackageReferences();

        protected IUserInterface UI { get; }

        protected IProjectAnalyzer Project { get; }

        protected XElement ProjectXml { get; }

        public static ProjectStyle Create(IUserInterface ui, IProjectAnalyzer project)
        {
            var projectXml = XElement.Load(project.ProjectFile.Path);

            var isSdkStyle = SdkProjectStyle.IsSdkStyleProject(projectXml);
            var isOldStyle = OldProjectStyle.IsOldStyleProject(projectXml);

            if (isSdkStyle && !isOldStyle)
            {
                return new SdkProjectStyle(ui, project, projectXml);
            }

            if (isOldStyle && !isSdkStyle)
            {
                return new OldProjectStyle(ui, project, projectXml);
            }
                
            throw new Exception($"Error: could not recognize project style: {project.ProjectFile.Path}");
        }
    }
}