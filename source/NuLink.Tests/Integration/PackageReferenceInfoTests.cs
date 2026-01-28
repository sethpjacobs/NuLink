using System;
using System.IO;
using AwesomeAssertions;
using Murphy.SymbolicLink;
using NuLink.Cli;
using Xunit;

namespace NuLink.Tests.Integration
{
    public class PackageReferenceInfoTests : IDisposable
    {
        private readonly string _tempDir;

        public PackageReferenceInfoTests()
        {
            // Use resolved path to avoid macOS /var -> /private/var symlink issues
            var rawTempDir = Path.Combine(Path.GetTempPath(), $"nulink-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(rawTempDir);
            _tempDir = SymbolicLink.resolve(rawTempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        [Fact]
        public void CheckStatus_WhenLibFolderDoesNotExist_ReturnsNotLinkable()
        {
            var packageRoot = Path.Combine(_tempDir, "package.1.0.0");
            Directory.CreateDirectory(packageRoot);

            var info = new PackageReferenceInfo("Package", "1.0.0", packageRoot, "lib");
            var status = info.CheckStatus();

            status.LibFolderExists.Should().BeFalse();
            status.IsLinkable.Should().BeFalse();
            status.IsLibFolderLinked.Should().BeFalse();
        }

        [Fact]
        public void CheckStatus_WhenLibFolderExists_ReturnsLinkable()
        {
            var packageRoot = Path.Combine(_tempDir, "package.1.0.0");
            var libFolder = Path.Combine(packageRoot, "lib");
            Directory.CreateDirectory(libFolder);

            var info = new PackageReferenceInfo("Package", "1.0.0", packageRoot, "lib");
            var status = info.CheckStatus();

            status.LibFolderExists.Should().BeTrue();
            status.IsLinkable.Should().BeTrue();
            status.IsLibFolderLinked.Should().BeFalse();
            status.LibBackupFolderExists.Should().BeFalse();
        }

        [Fact]
        public void CheckStatus_WhenLibFolderIsSymlink_ReturnsLinked()
        {
            var packageRoot = Path.Combine(_tempDir, "package.1.0.0");
            var libFolder = Path.Combine(packageRoot, "lib");
            var targetFolder = Path.Combine(_tempDir, "target-lib");
            Directory.CreateDirectory(packageRoot);
            Directory.CreateDirectory(targetFolder);

            SymbolicLink.create(targetFolder, libFolder);

            var info = new PackageReferenceInfo("Package", "1.0.0", packageRoot, "lib");
            var status = info.CheckStatus();

            status.LibFolderExists.Should().BeTrue();
            status.IsLibFolderLinked.Should().BeTrue();
            status.LibFolderLinkTargetPath.Should().Be(targetFolder);
        }

        [Fact]
        public void CheckStatus_WhenBackupFolderExists_ReportsIt()
        {
            var packageRoot = Path.Combine(_tempDir, "package.1.0.0");
            var libFolder = Path.Combine(packageRoot, "lib");
            var backupFolder = Path.Combine(packageRoot, "nulink-backup.lib");
            Directory.CreateDirectory(libFolder);
            Directory.CreateDirectory(backupFolder);

            var info = new PackageReferenceInfo("Package", "1.0.0", packageRoot, "lib");
            var status = info.CheckStatus();

            status.LibBackupFolderExists.Should().BeTrue();
        }

        [Fact]
        public void CheckStatus_WhenLinkedWithBackup_IsNotCorrupt()
        {
            var packageRoot = Path.Combine(_tempDir, "package.1.0.0");
            var libFolder = Path.Combine(packageRoot, "lib");
            var backupFolder = Path.Combine(packageRoot, "nulink-backup.lib");
            var targetFolder = Path.Combine(_tempDir, "target-lib");
            Directory.CreateDirectory(packageRoot);
            Directory.CreateDirectory(backupFolder);
            Directory.CreateDirectory(targetFolder);

            SymbolicLink.create(targetFolder, libFolder);

            var info = new PackageReferenceInfo("Package", "1.0.0", packageRoot, "lib");
            var status = info.CheckStatus();

            status.IsLibFolderLinked.Should().BeTrue();
            status.LibBackupFolderExists.Should().BeTrue();
            status.IsCorrupt.Should().BeFalse();
        }

        [Fact]
        public void CheckStatus_WhenLinkedWithoutBackup_IsCorrupt()
        {
            var packageRoot = Path.Combine(_tempDir, "package.1.0.0");
            var libFolder = Path.Combine(packageRoot, "lib");
            var targetFolder = Path.Combine(_tempDir, "target-lib");
            Directory.CreateDirectory(packageRoot);
            Directory.CreateDirectory(targetFolder);

            SymbolicLink.create(targetFolder, libFolder);

            var info = new PackageReferenceInfo("Package", "1.0.0", packageRoot, "lib");
            var status = info.CheckStatus();

            status.IsLibFolderLinked.Should().BeTrue();
            status.LibBackupFolderExists.Should().BeFalse();
            status.IsCorrupt.Should().BeTrue();
        }

        [Fact]
        public void CheckStatus_WhenNotLinkedWithBackup_IsCorrupt()
        {
            var packageRoot = Path.Combine(_tempDir, "package.1.0.0");
            var libFolder = Path.Combine(packageRoot, "lib");
            var backupFolder = Path.Combine(packageRoot, "nulink-backup.lib");
            Directory.CreateDirectory(libFolder);
            Directory.CreateDirectory(backupFolder);

            var info = new PackageReferenceInfo("Package", "1.0.0", packageRoot, "lib");
            var status = info.CheckStatus();

            status.IsLibFolderLinked.Should().BeFalse();
            status.LibBackupFolderExists.Should().BeTrue();
            status.IsCorrupt.Should().BeTrue();
        }

        [Fact]
        public void PackageReferenceInfo_EqualsCorrectly()
        {
            var info1 = new PackageReferenceInfo("Package", "1.0.0", "/path/to/package", "lib");
            var info2 = new PackageReferenceInfo("Package", "1.0.0", "/path/to/package", "lib");
            var info3 = new PackageReferenceInfo("Package", "2.0.0", "/path/to/package", "lib");

            info1.Should().Be(info2);
            info1.Should().NotBe(info3);
        }
    }
}
