using System;
using System.IO;
using System.Linq;

namespace NuLink.Tests.Acceptance
{
    public abstract class AcceptanceTestTarget
    {
        public abstract string ConsumerSolutionFolder { get; }
        public abstract string ConsumerSolutionFile { get; }
        public abstract string PackageSolutionFolder(string packageId);
        public abstract string PackageProjectFolder(string packageId);
        public abstract string PackageProjectFile(string packageId);
        public abstract string PackageIdSuffix { get; }
        public abstract string PackagesRootFolder(string solutionFolder);
        public abstract string PackageNugetFolder(string solutionFolder, string packageId, string version);
        public abstract string PackageNugetLibFolder(string solutionFolder, string packageId, string version);
        public abstract void BuildPackageProjectIn(string projectFolder); 
        public abstract void RunTestProjectIn(string testProjectFolder); 
        public abstract void RestoreSolutionPackagesIn(string solutionFolder);

        public string PackageId(string packageName)
        {
            return packageName + PackageIdSuffix;
        }
        
        public static readonly AcceptanceTestTarget NetCore = new NetCoreTestTarget();

        public static AcceptanceTestTarget Get(TargetProjectKind projectKind)
        {
            switch (projectKind)
            {
                case TargetProjectKind.NetCore:
                    return NetCore;
            }

            throw new NotSupportedException(projectKind.ToString());
        }

        public void LogName()
        {
            Console.WriteLine($"--- acceptance test target: {ToString()} ---");
        }
    }

    public class NetCoreTestTarget : AcceptanceTestTarget
    {
        public override string ConsumerSolutionFolder =>
            Path.Combine(TestEnvironment.DemoFolder, "NuLink.TestCase.Consumer");
        public override string ConsumerSolutionFile => 
            Path.Combine(ConsumerSolutionFolder, "NuLink.TestCase.Consumer.sln");
        public override string PackageSolutionFolder(string packageId) => Path.Combine(
            TestEnvironment.DemoFolder, 
            packageId);
        public override string PackageProjectFolder(string packageId) => Path.Combine(
            PackageSolutionFolder(packageId),
            packageId);
        public override string PackageProjectFile(string packageId) =>
            Path.Combine(PackageProjectFolder(packageId), $"{packageId}.csproj");
        public override string PackageIdSuffix => string.Empty;
        public override string PackagesRootFolder(string solutionFolder) => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget",
            "packages");
        public override string PackageNugetFolder(string solutionFolder, string packageId, string version) => Path.Combine(
            PackagesRootFolder(solutionFolder),
            packageId.ToLower());
        public override string PackageNugetLibFolder(string solutionFolder, string packageId, string version) => Path.Combine(
            PackageNugetFolder(solutionFolder, packageId, version),
            version,
            "lib");
        public override void BuildPackageProjectIn(string projectFolder)
        {
            ExternalProgram.ExecIn(projectFolder, "dotnet", "build", "-c", "Debug");
        }
        public override void RunTestProjectIn(string testProjectFolder)
        {
            ExternalProgram.Exec("dotnet", "test", testProjectFolder);
        }
        public override void RestoreSolutionPackagesIn(string solutionFolder)
        {
            ExternalProgram.ExecIn(
                solutionFolder, 
                "dotnet", 
                "restore",
                "--force");
        }
        public override string ToString()
        {
            return "NetCore";
        }
    }
}
