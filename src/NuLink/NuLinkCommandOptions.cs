namespace NuLink
{
    public class NuLinkCommandOptions
    {
        public NuLinkCommandOptions(
            string consumerProjectPath, 
            string packageId = null, 
            bool dryRun = false, 
            bool bareUI = false,
            string localProjectPath = null)
        {
            ConsumerProjectPath = consumerProjectPath;
            PackageId = packageId;
            DryRun = dryRun;
            BareUI = bareUI;
            LocalProjectPath = localProjectPath;
            ProjectIsSolution = ConsumerProjectPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase);
        }

        public string ConsumerProjectPath { get; }
        public bool ProjectIsSolution { get; }
        public string PackageId { get; }
        public string LocalProjectPath { get; }
        public bool DryRun { get; }
        public bool BareUI { get; }
    }
}