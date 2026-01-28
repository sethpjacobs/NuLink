using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;

namespace NuLink.Cli
{
    class Program
    {
        // Define options as static fields so they can be accessed in handlers
        private static readonly Option<string> ConsumerOption = new Option<string>(
            new[] { "--consumer", "-c" },
            HelpText.ProjectOption)
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        private static readonly Option<string> PackageOption = new Option<string>(
            new[] { "--package", "-p" },
            HelpText.PackageOption)
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        private static readonly Option<string> LocalProjectOption = new Option<string>(
            new[] { "--local", "-l" },
            HelpText.LocalProjectOption)
        {
            Arity = ArgumentArity.ExactlyOne
        };

        private static readonly Option<bool> DryRunOption = new Option<bool>(
            new[] { "--dry-run", "-d" },
            HelpText.DryRunOption)
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        private static readonly Option<bool> QuietOption = new Option<bool>(
            new[] { "--quiet", "-q" },
            HelpText.QuietOption)
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        static int Main(string[] args)
        {
            var rootCommand = BuildCommandLine();
            return rootCommand.Invoke(args);
        }

        private static RootCommand BuildCommandLine()
        {
            var rootCommand = new RootCommand("NuLink - The missing LINK feature of NuGet");

            // Status command
            var statusCommand = new Command("status", HelpText.StatusCommand);
            statusCommand.AddOption(ConsumerOption);
            statusCommand.AddOption(PackageOption);
            statusCommand.AddOption(QuietOption);
            statusCommand.SetHandler(HandleStatus);
            rootCommand.AddCommand(statusCommand);

            // Link command
            var linkCommand = new Command("link", HelpText.LinkCommand);
            linkCommand.AddOption(ConsumerOption);
            linkCommand.AddOption(PackageOption);
            linkCommand.AddOption(LocalProjectOption);
            linkCommand.AddOption(DryRunOption);
            linkCommand.SetHandler(HandleLink);
            rootCommand.AddCommand(linkCommand);

            // Unlink command
            var unlinkCommand = new Command("unlink", HelpText.UnlinkCommand);
            unlinkCommand.AddOption(ConsumerOption);
            unlinkCommand.AddOption(PackageOption);
            unlinkCommand.AddOption(DryRunOption);
            unlinkCommand.SetHandler(HandleUnlink);
            rootCommand.AddCommand(unlinkCommand);

            return rootCommand;
        }

        private static void HandleStatus(InvocationContext context)
        {
            var consumer = context.ParseResult.GetValueForOption(ConsumerOption);
            var package = context.ParseResult.GetValueForOption(PackageOption);
            var quiet = context.ParseResult.GetValueForOption(QuietOption);

            var options = new NuLinkCommandOptions(
                ValidateConsumerProject(consumer),
                package,
                bareUI: quiet);

            context.ExitCode = ExecuteCommand(
                "status",
                options,
                options.ConsumerProjectPath != null);
        }

        private static void HandleLink(InvocationContext context)
        {
            var consumer = context.ParseResult.GetValueForOption(ConsumerOption);
            var package = context.ParseResult.GetValueForOption(PackageOption);
            var local = context.ParseResult.GetValueForOption(LocalProjectOption);
            var dryRun = context.ParseResult.GetValueForOption(DryRunOption);

            var options = new NuLinkCommandOptions(
                ValidateConsumerProject(consumer),
                package,
                localProjectPath: ValidateTargetProject(local),
                dryRun: dryRun);

            context.ExitCode = ExecuteCommand(
                "link",
                options,
                options.ConsumerProjectPath != null && options.LocalProjectPath != null && options.PackageId != null);
        }

        private static void HandleUnlink(InvocationContext context)
        {
            var consumer = context.ParseResult.GetValueForOption(ConsumerOption);
            var package = context.ParseResult.GetValueForOption(PackageOption);
            var dryRun = context.ParseResult.GetValueForOption(DryRunOption);

            var options = new NuLinkCommandOptions(
                ValidateConsumerProject(consumer),
                package,
                dryRun: dryRun);

            context.ExitCode = ExecuteCommand(
                "unlink",
                options,
                options.ConsumerProjectPath != null && options.PackageId != null);
        }

        private static int ExecuteCommand(
            string commandName,
            NuLinkCommandOptions options,
            bool isValid)
        {
            if (!isValid)
            {
                return 1;
            }

            try
            {
                var command = CreateCommand(commandName, options);
                command.Execute(options);
                return 0;
            }
            catch (Exception e)
            {
                FullUI.FatalError(() => $"Fatal error: {e.Message}\nException: {e}");
                return 100;
            }
        }

        private static string ValidateConsumerProject(string filePath)
        {
            if (filePath != null)
            {
                if (File.Exists(filePath))
                {
                    return filePath;
                }

                FullUI.FatalError(() => $"Error: File does not exist: {filePath}");
                return null;
            }

            filePath = Directory
                .GetFiles(Directory.GetCurrentDirectory(), "*.*")
                .FirstOrDefault(path =>
                    path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase));

            if (filePath == null)
            {
                FullUI.FatalError(() => $"Error: No .sln/.csproj file found in current directory, and {"--consumer"} was not specified");
                return null;
            }

            return filePath;
        }

        private static string ValidateTargetProject(string filePath)
        {
            if (File.Exists(filePath))
            {
                return Path.GetFullPath(filePath);
            }

            FullUI.FatalError(() => $"Error: File does not exist: {filePath}");
            return null;
        }

        private static INuLinkCommand CreateCommand(string name, NuLinkCommandOptions options)
        {
            var ui = (options.BareUI ? new BareUI() : new FullUI() as IUserInterface);

            switch (name)
            {
                case "status":
                    return new StatusCommand(ui);
                case "link":
                    return new LinkCommand(ui);
                case "unlink":
                    return new UnlinkCommand(ui);
                default:
                    throw new Exception($"Command not supported: {name}.");
            }
        }

        private static class HelpText
        {
            public const string StatusCommand =
                "Check status of packages in consumer project/solution";
            public const string LinkCommand =
                "Link package to a directory in local file system, machine-wide";
            public const string UnlinkCommand =
                "Unlink package from local file system, machine-wide";
            public const string ProjectOption =
                "Path to consumer .csproj or .sln (default: first .csproj or .sln in current directory)";
            public const string PackageOption =
                "Package id (default: all packages in consumer project/solution)";
            public const string LocalProjectOption =
                "Full path to package .csproj in local file system";
            public const string DryRunOption =
                "If specified, list intended actions without executing them";
            public const string QuietOption =
                "If specified, suppresses all output except data (useful for scripting)";
        }
    }
}
