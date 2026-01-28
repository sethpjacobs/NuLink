using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

namespace NuLink.Cli.ProjectStyles
{
    public class DirectoryPackagesPropsLoader(string projectDirectory, IUserInterface ui)
    {
        public bool ProjectUsesCpm(XElement projectXml)
        {
            // Check for explicit ManagePackageVersionsCentrally property in the project
            var manageVersionsElement = projectXml.XPathSelectElement("//ManagePackageVersionsCentrally");
            if (manageVersionsElement != null)
            {
                return string.Equals(manageVersionsElement.Value, "true", StringComparison.OrdinalIgnoreCase);
            }

            // If not explicitly set, check if Directory.Packages.props exists (implicit CPM)
            var propsPath = FindDirectoryPackagesPropsPath();
            return propsPath != null;
        }

        public string FindDirectoryPackagesPropsPath()
        {
            var directory = projectDirectory;

            while (!string.IsNullOrEmpty(directory))
            {
                var propsPath = Path.Combine(directory, "Directory.Packages.props");
                if (File.Exists(propsPath))
                {
                    ui.ReportLow(() => $"Found Directory.Packages.props at: {propsPath}");
                    return propsPath;
                }

                var parent = Directory.GetParent(directory);
                directory = parent?.FullName;
            }

            return null;
        }

        public Dictionary<string, string> LoadPackageVersions(string propsPath)
        {
            var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(propsPath) || !File.Exists(propsPath))
            {
                ui.ReportWarning(() => $"Directory.Packages.props not found at: {propsPath}");
                return versions;
            }

            try
            {
                var propsXml = XElement.Load(propsPath);
                var packageVersions = propsXml.XPathSelectElements("//PackageVersion");

                foreach (var element in packageVersions)
                {
                    var packageId = element.Attribute("Include")?.Value;
                    var version = element.Attribute("Version")?.Value;

                    if (!string.IsNullOrWhiteSpace(packageId) && !string.IsNullOrWhiteSpace(version))
                    {
                        versions[packageId] = version;
                        ui.ReportLow(() => $"CPM: {packageId} = {version}");
                    }
                }
            }
            catch (Exception ex)
            {
                ui.ReportWarning(() => $"Failed to load Directory.Packages.props: {ex.Message}");
            }

            return versions;
        }
    }
}
