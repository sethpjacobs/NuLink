using System;
using System.IO;
using System.Linq.Expressions;
using System.Xml.Linq;
using AwesomeAssertions;
using NuLink.Cli;
using NuLink.Cli.ProjectStyles;
using Xunit;

namespace NuLink.Tests.Integration;

public class DirectoryPackagesPropsLoaderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly TestUserInterface _ui;

    public DirectoryPackagesPropsLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"nulink-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _ui = new TestUserInterface();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void FindDirectoryPackagesPropsPath_WhenFileExists_ReturnsPath()
    {
        var propsPath = Path.Combine(_tempDir, "Directory.Packages.props");
        File.WriteAllText(propsPath, "<Project></Project>");

        var loader = new DirectoryPackagesPropsLoader(_tempDir, _ui);
        var result = loader.FindDirectoryPackagesPropsPath();

        result.Should().Be(propsPath);
    }

    [Fact]
    public void FindDirectoryPackagesPropsPath_WhenFileInParentDir_ReturnsParentPath()
    {
        var childDir = Path.Combine(_tempDir, "child", "grandchild");
        Directory.CreateDirectory(childDir);
        var propsPath = Path.Combine(_tempDir, "Directory.Packages.props");
        File.WriteAllText(propsPath, "<Project></Project>");

        var loader = new DirectoryPackagesPropsLoader(childDir, _ui);
        var result = loader.FindDirectoryPackagesPropsPath();

        result.Should().Be(propsPath);
    }

    [Fact]
    public void FindDirectoryPackagesPropsPath_WhenFileNotFound_ReturnsNull()
    {
        var loader = new DirectoryPackagesPropsLoader(_tempDir, _ui);
        var result = loader.FindDirectoryPackagesPropsPath();

        result.Should().BeNull();
    }

    [Fact]
    public void ProjectUsesCpm_WhenExplicitlyEnabled_ReturnsTrue()
    {
        var projectXml = XElement.Parse(@"
                <Project>
                    <PropertyGroup>
                        <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                    </PropertyGroup>
                </Project>");

        var loader = new DirectoryPackagesPropsLoader(_tempDir, _ui);
        var result = loader.ProjectUsesCpm(projectXml);

        result.Should().BeTrue();
    }

    [Fact]
    public void ProjectUsesCpm_WhenExplicitlyDisabled_ReturnsFalse()
    {
        var projectXml = XElement.Parse(@"
                <Project>
                    <PropertyGroup>
                        <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
                    </PropertyGroup>
                </Project>");

        var loader = new DirectoryPackagesPropsLoader(_tempDir, _ui);
        var result = loader.ProjectUsesCpm(projectXml);

        result.Should().BeFalse();
    }

    [Fact]
    public void ProjectUsesCpm_WhenPropsFileExists_ReturnsTrue()
    {
        var projectXml = XElement.Parse("<Project></Project>");
        var propsPath = Path.Combine(_tempDir, "Directory.Packages.props");
        File.WriteAllText(propsPath, "<Project></Project>");

        var loader = new DirectoryPackagesPropsLoader(_tempDir, _ui);
        var result = loader.ProjectUsesCpm(projectXml);

        result.Should().BeTrue();
    }

    [Fact]
    public void ProjectUsesCpm_WhenNoPropsFile_ReturnsFalse()
    {
        var projectXml = XElement.Parse("<Project></Project>");

        var loader = new DirectoryPackagesPropsLoader(_tempDir, _ui);
        var result = loader.ProjectUsesCpm(projectXml);

        result.Should().BeFalse();
    }

    [Fact]
    public void LoadPackageVersions_ReturnsVersions()
    {
        var propsContent = @"
                <Project>
                    <ItemGroup>
                        <PackageVersion Include=""Newtonsoft.Json"" Version=""13.0.3"" />
                        <PackageVersion Include=""xunit"" Version=""2.6.1"" />
                    </ItemGroup>
                </Project>";
        var propsPath = Path.Combine(_tempDir, "Directory.Packages.props");
        File.WriteAllText(propsPath, propsContent);

        var loader = new DirectoryPackagesPropsLoader(_tempDir, _ui);
        var versions = loader.LoadPackageVersions(propsPath);

        versions.Should().HaveCount(2);
        versions["Newtonsoft.Json"].Should().Be("13.0.3");
        versions["xunit"].Should().Be("2.6.1");
    }

    [Fact]
    public void LoadPackageVersions_IsCaseInsensitive()
    {
        var propsContent = @"
                <Project>
                    <ItemGroup>
                        <PackageVersion Include=""Newtonsoft.Json"" Version=""13.0.3"" />
                    </ItemGroup>
                </Project>";
        var propsPath = Path.Combine(_tempDir, "Directory.Packages.props");
        File.WriteAllText(propsPath, propsContent);

        var loader = new DirectoryPackagesPropsLoader(_tempDir, _ui);
        var versions = loader.LoadPackageVersions(propsPath);

        versions["newtonsoft.json"].Should().Be("13.0.3");
        versions["NEWTONSOFT.JSON"].Should().Be("13.0.3");
    }

    [Fact]
    public void LoadPackageVersions_WhenPathNull_ReturnsEmptyDictionary()
    {
        var loader = new DirectoryPackagesPropsLoader(_tempDir, _ui);
        var versions = loader.LoadPackageVersions(null);

        versions.Should().BeEmpty();
    }

    [Fact]
    public void LoadPackageVersions_WhenFileNotFound_ReturnsEmptyDictionary()
    {
        var loader = new DirectoryPackagesPropsLoader(_tempDir, _ui);
        var versions = loader.LoadPackageVersions("/nonexistent/path");

        versions.Should().BeEmpty();
    }

    private class TestUserInterface : IUserInterface
    {
        public void Report(VerbosityLevel level, Expression<Func<string>> message, params ConsoleColor[] paramColors)
        {
            // No-op for tests
        }
    }
}