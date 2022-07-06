using Buildalyzer;

namespace NuLink
{
    public static class WorkspaceLoader
    {
        public static IEnumerable<IProjectAnalyzer> LoadProjects(string filePath, bool isSolution)
        {
            var analyzerManager = (isSolution 
                ? new AnalyzerManager(filePath) 
                : new AnalyzerManager());

            return isSolution 
                ? analyzerManager.Projects.Values 
                : new[] { analyzerManager.GetProject(filePath) };
        }
    }
}
