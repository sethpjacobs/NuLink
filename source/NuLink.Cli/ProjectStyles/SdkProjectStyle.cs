using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Buildalyzer;

namespace NuLink.Cli.ProjectStyles
{
    public class SdkProjectStyle : ProjectStyle
    {
        public SdkProjectStyle(IUserInterface ui, IProjectAnalyzer project, XElement projextXml)
            : base(ui, project, projextXml)
        {
        }

        public override IEnumerable<PackageReferenceInfo> LoadPackageReferences()
        {
            var packagesRootFolder = GetPackagesRootFolder();
            var projectDirectory = Path.GetDirectoryName(Project.ProjectFile.Path);

            // Initialize CPM loader and check if project uses Central Package Management
            var cpmLoader = new DirectoryPackagesPropsLoader(projectDirectory, UI);
            var usesCpm = cpmLoader.ProjectUsesCpm(ProjectXml);

            Dictionary<string, string> centralVersions = null;
            if (usesCpm)
            {
                var propsPath = cpmLoader.FindDirectoryPackagesPropsPath();
                centralVersions = cpmLoader.LoadPackageVersions(propsPath);
                UI.ReportMedium(() => $"Project uses Central Package Management with {centralVersions.Count} package version(s)");
            }

            var packages = GetPackages(centralVersions);
            return packages.Where(p => p != null);
        }

        private IEnumerable<PackageReferenceInfo> GetPackages(Dictionary<string, string> centralVersions)
        {
            var packagesRootFolder = GetPackagesRootFolder();
            var elements = ProjectXml.XPathSelectElements("//PackageReference");

            foreach (var element in elements)
            {
                var packageId = element.Attribute("Include")?.Value;
                if (string.IsNullOrWhiteSpace(packageId))
                    continue;

                // Version resolution order:
                // 1. VersionOverride attribute (CPM escape hatch)
                // 2. Version attribute on PackageReference
                // 3. Central version from Directory.Packages.props
                var version = element.Attribute("VersionOverride")?.Value
                           ?? element.Attribute("Version")?.Value;

                if (string.IsNullOrWhiteSpace(version) && centralVersions != null)
                {
                    centralVersions.TryGetValue(packageId, out version);
                }

                if (string.IsNullOrWhiteSpace(version))
                {
                    UI.ReportWarning(() => $"Could not determine version for package {packageId}");
                    continue;
                }

                var folder = GetPackageFolder(packagesRootFolder, packageId, version);
                yield return new PackageReferenceInfo(
                    packageId,
                    version,
                    rootFolderPath: folder,
                    libSubfolderPath: "lib");
            }
        }

        private string GetPackageFolder(string packagesRootFolder, string packageId, string version)
        {
            return Path.Combine(
                packagesRootFolder,
                packageId.ToLower(),
                version.ToLower());
        }
    
        private string GetPackagesRootFolder()
        {
            var ns = new XmlNamespaceManager(new NameTable());
            ns.AddNamespace("msb", MsbuildNamespaceUri);
    
            var nugetPropsXml = XElement.Load(GetNuGetPropsFilePath());
            var result = 
                nugetPropsXml.XPathSelectElement("//msb:NuGetPackageRoot", ns)?.Value
                ?? throw new Exception("Could not find NuGetPackageRoot property");
    
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var userProfilePath = Environment.GetEnvironmentVariable("UserProfile");
                UI.ReportLow(() => $"Detected Windows: $(UserProfile)=[{userProfilePath}]");
                result = result.Replace("$(UserProfile)", userProfilePath, StringComparison.InvariantCultureIgnoreCase);
            }
                
            return result;
        }
    
        private string GetNuGetPropsFilePath()
        {
            var filePath = Path.Combine(
                Path.GetDirectoryName(Project.ProjectFile.Path),
                "obj",
                $"{Path.GetFileName(Project.ProjectFile.Path)}.nuget.g.props");
    
            return filePath;
        }

        public static bool IsSdkStyleProject(XElement projectXml)
        {
            var sdkValue = projectXml.Attribute("Sdk")?.Value;
            return !String.IsNullOrEmpty(sdkValue);
        }
    }
}