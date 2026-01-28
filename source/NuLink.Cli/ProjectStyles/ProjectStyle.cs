using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Buildalyzer;

namespace NuLink.Cli.ProjectStyles
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

            if (!SdkProjectStyle.IsSdkStyleProject(projectXml))
            {
                throw new Exception(
                    $"Only SDK-style projects are supported. Legacy .NET Framework projects (packages.config) " +
                    $"are no longer supported: {project.ProjectFile.Path}");
            }

            return new SdkProjectStyle(ui, project, projectXml);
        }
    }
}