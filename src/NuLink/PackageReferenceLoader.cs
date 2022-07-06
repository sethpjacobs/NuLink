using Buildalyzer;
using NuLink.ProjectStyles;

namespace NuLink
{
    public class PackageReferenceLoader
    {
        private readonly IUserInterface _ui;

        public PackageReferenceLoader(IUserInterface ui)
        {
            _ui = ui;
        }

        public HashSet<PackageReferenceInfo> LoadPackageReferences(IEnumerable<IProjectAnalyzer> projects)
        {
            var results = new HashSet<PackageReferenceInfo>();

            foreach (var project in projects)
            {
                _ui.ReportMedium(() => $"Checking package references: {Path.GetFileName(project.ProjectFile.Path)}");

                var projectStyle = ProjectStyle.Create(_ui, project);
                var projectPackages = projectStyle.LoadPackageReferences();
                
                results.UnionWith(projectPackages);
            }

            return results;
        }
        
    }
}