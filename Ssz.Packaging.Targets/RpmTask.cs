using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Ssz.Packaging.Targets.IO;
using Ssz.Packaging.Targets.Rpm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;

namespace Ssz.Packaging.Targets
{
    /// <summary>
    /// Creates a RPM package (RED OS, ALT Linux, ROSA, Fedora, RHEL, CentOS...).
    /// Has the same set of options as <see cref="DebTask"/>, so both packages can be created from the same input.
    /// </summary>
    public class RpmTask : Task
    {
        /// <summary>
        /// System directories, which belong to the base system (the 'filesystem' package) and must not be owned by this package.
        /// </summary>
        private static readonly HashSet<string> SystemDirectories = new HashSet<string>(StringComparer.Ordinal)
        {
            "/", "/bin", "/boot", "/etc", "/home", "/lib", "/lib64", "/opt", "/root", "/run", "/sbin", "/srv", "/tmp",
            "/usr", "/usr/bin", "/usr/include", "/usr/lib", "/usr/lib64", "/usr/libexec", "/usr/local", "/usr/local/bin",
            "/usr/local/lib", "/usr/local/sbin", "/usr/local/share", "/usr/sbin", "/usr/share", "/usr/share/doc",
            "/var", "/var/cache", "/var/lib", "/var/log", "/var/opt", "/var/run", "/var/tmp",
            "/lib/systemd", "/lib/systemd/system", "/usr/lib/systemd", "/usr/lib/systemd/system",
            "/etc/systemd", "/etc/systemd/system", "/etc/profile.d", "/etc/logrotate.d",
        };

        public RpmTask()
        {
        }

        public RpmTask(ResourceManager taskResources)
            : base(taskResources)
        {
        }

        [Required]
        public string ProgramFiles_SourceDir { get; set; }

        [Required]
        public string ProgramFiles_DestinationDir { get; set; }

        [Required]
        public ITaskItem[] ProgramFiles_Metadata { get; set; }

        public string ProgramData_SourceDir { get; set; }

        public string ProgramData_DestinationDir { get; set; }

        public ITaskItem[] ProgramData_Metadata { get; set; }

        [Required]
        public string RpmPath { get; set; }

        /// <summary>
        /// Gets or sets the path to the temporary CPIO archive (payload). Deleted after the package is created.
        /// </summary>
        [Required]
        public string RpmCpioPath { get; set; }

        /// <summary>
        /// Gets or sets the package version. Must not contain the '-' character (e.g. 1.0.0).
        /// </summary>
        [Required]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the package release (e.g. 202601011200). Must not contain the '-' character.
        /// </summary>
        [Required]
        public string Release { get; set; }

        [Required]
        public string PackageName { get; set; }

        /// <summary>
        /// Gets or sets the package vendor (RPM analog of the Debian 'Maintainer' field).
        /// </summary>
        [Required]
        public string Vendor { get; set; }

        [Required]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the package URL (RPM analog of the Debian 'Homepage' field).
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the runtime identifier for which we are currently building.
        /// Used to determine the target architecture of the package.
        /// </summary>
        public string RuntimeIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the package architecture (x86_64, aarch64, noarch,...). When not set,
        /// the target architecture will be derived based on the <see cref="RuntimeIdentifier"/>
        /// </summary>
        public string RpmPackageArchitecture { get; set; }

        /// <summary>
        /// Gets or sets the path to the app host. This is a native executable which loads
        /// the .NET Core runtime, and invokes the manage assembly. On Linux, it is symlinked
        /// into /usr/local/bin.
        /// </summary>
        public string AppHost { get; set; }

        /// <summary>
        /// Gets or sets a list of empty folders to create when
        /// installing this package.
        /// </summary>
        public ITaskItem[] LinuxFolders { get; set; }

        /// <summary>
        /// Gets or sets a list of RPM packages on which the version of .NET Core
        /// embedded in this RPM package depends.
        /// </summary>
        public ITaskItem[] RpmDotNetDependencies { get; set; }

        /// <summary>
        /// Gets or sets a list of RPM packages on which this RPM package depends.
        /// Optional 'Version' metadata means 'greater or equal'.
        /// </summary>
        public ITaskItem[] RpmDependencies { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to create a Linux
        /// user and group when installing the package.
        /// </summary>
        public bool CreateUser { get; set; }

        /// <summary>
        /// Gets or sets the name of the Linux user and group to create.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to install
        /// and launch as systemd service when installing the package.
        /// </summary>
        public bool InstallService { get; set; }

        /// <summary>
        /// Gets or sets the name of the SystemD service to create.
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets an additional pre-installation script to execute.
        /// </summary>
        /// <remarks>
        /// This variable must contain the script itself, and not a path to a file
        /// which contains the script. $1 is 1 on install and 2+ on upgrade.
        /// </remarks>
        public string PreInstallScript { get; set; }

        /// <summary>
        /// Gets or sets an additional post-installation script to execute.
        /// </summary>
        /// <remarks>
        /// This variable must contain the script itself, and not a path to a file
        /// which contains the script. $1 is 1 on install and 2+ on upgrade.
        /// </remarks>
        public string PostInstallScript { get; set; }

        /// <summary>
        /// Gets or sets an additional pre-removal script to execute.
        /// </summary>
        /// <remarks>
        /// This variable must contain the script itself, and not a path to a file
        /// which contains the script. $1 is 0 on removal and 1+ on upgrade.
        /// </remarks>
        public string PreRemoveScript { get; set; }

        /// <summary>
        /// Gets or sets an additional post-removal script to execute.
        /// </summary>
        /// <remarks>
        /// This variable must contain the script itself, and not a path to a file
        /// which contains the script. $1 is 0 on removal and 1+ on upgrade.
        /// </remarks>
        public string PostRemoveScript { get; set; }

        /// <summary>
        /// Derives the package architecture from a .NET runtime identiifer.
        /// </summary>
        /// <param name="runtimeIdentifier">
        /// The runtime identifier.
        /// </param>
        /// <returns>
        /// The equivalent package architecture.
        /// </returns>
        public static string GetPackageArchitecture(string runtimeIdentifier)
        {
            // Some architectures are listed here:
            // - https://wiki.centos.org/Download
            // - https://docs.fedoraproject.org/ro/Fedora_Draft_Documentation/0.1/html/RPM_Guide/ch01s03.html
            RuntimeIdentifiers.ParseRuntimeId(runtimeIdentifier, out _, out _, out Architecture? architecture, out _);

            if (architecture != null)
            {
                switch (architecture.Value)
                {
                    case Architecture.Arm:
                        return "armhfp";

                    case Architecture.Arm64:
                        return "aarch64";

                    case Architecture.X64:
                        return "x86_64";

                    case Architecture.X86:
                        return "i386";
                }
            }

            return "noarch";
        }

        public override bool Execute()
        {
            if (this.Version.Contains("-") || this.Release.Contains("-"))
            {
                this.Log.LogError(
                    "RPM Version ('{0}') and Release ('{1}') must not contain the '-' character.",
                    this.Version,
                    this.Release);
                return false;
            }

            this.Log.LogMessage(
                MessageImportance.High,
                "Creating RPM package '{0}' from folder '{1}'",
                this.RpmPath,
                this.ProgramFiles_SourceDir);

            // The package is signed with a throw-away key: rpm reports 'NOKEY', but checks the digests.
            var krgen = PgpSigner.GenerateKeyRingGenerator("dotnet", "dotnet");
            var secretKeyRing = krgen.GenerateSecretKeyRing();
            var privateKey = secretKeyRing.GetSecretKey().ExtractPrivateKey("dotnet".ToCharArray());

            using (var targetStream = File.Open(this.RpmPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            using (var cpioStream = File.Open(this.RpmCpioPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                ArchiveBuilder archiveBuilder = new ArchiveBuilder()
                {
                    Log = this.Log,
                };

                var archiveEntries = archiveBuilder.FromDirectory(
                    this.ProgramFiles_SourceDir,
                    this.AppHost,
                    this.ProgramFiles_DestinationDir,
                    this.ProgramFiles_Metadata);

                archiveEntries.AddRange(archiveBuilder.FromDirectory(
                    this.ProgramData_SourceDir,
                    null,
                    this.ProgramData_DestinationDir,
                    this.ProgramData_Metadata));

                archiveEntries.AddRange(archiveBuilder.FromLinuxFolders(this.LinuxFolders));

                AddOwnedDirectories(
                    archiveEntries,
                    archiveBuilder,
                    new[] { this.ProgramFiles_DestinationDir, this.ProgramData_DestinationDir });

                // Same order as rpmbuild uses (strcmp of the full path).
                archiveEntries = archiveEntries
                    .OrderBy(e => e.TargetPathWithoutFinalSlash, StringComparer.Ordinal)
                    .ToList();

                CpioFileCreator cpioCreator = new CpioFileCreator();
                cpioCreator.FromArchiveEntries(
                    archiveEntries,
                    cpioStream);
                cpioStream.Position = 0;

                // Prepare the list of dependencies
                List<PackageDependency> dependencies = new List<PackageDependency>();

                if (this.RpmDotNetDependencies != null)
                {
                    dependencies.AddRange(
                        this.RpmDotNetDependencies.Select(
                            d => GetPackageDependency(d)));
                }

                if (this.RpmDependencies != null)
                {
                    dependencies.AddRange(
                        this.RpmDependencies.Select(
                            d => GetPackageDependency(d)));
                }

                RpmPackageCreator rpmCreator = new RpmPackageCreator();
                rpmCreator.CreatePackage(
                    archiveEntries,
                    cpioStream,
                    this.PackageName,
                    this.Version,
                    !string.IsNullOrWhiteSpace(this.RpmPackageArchitecture) ? this.RpmPackageArchitecture : GetPackageArchitecture(this.RuntimeIdentifier),
                    this.Release,
                    this.CreateUser,
                    this.UserName,
                    this.InstallService,
                    this.ServiceName,
                    this.Vendor,
                    this.Description,
                    this.Url,
                    this.ProgramFiles_DestinationDir,
                    this.PreInstallScript,
                    this.PostInstallScript,
                    this.PreRemoveScript,
                    this.PostRemoveScript,
                    dependencies,
                    null,
                    privateKey,
                    targetStream);
            }

            try
            {
                File.Delete(this.RpmCpioPath);
            }
            catch
            {
            }

            this.Log.LogMessage(
                MessageImportance.High,
                "Created RPM package '{0}' from folder '{1}'",
                this.RpmPath,
                this.ProgramFiles_SourceDir);

            return true;
        }

        /// <summary>
        /// Adds directory entries for the directories which belong to this package (destination directories and
        /// their subdirectories), so rpm removes them when the package is uninstalled.
        /// System directories (/, /usr, /etc...) belong to the base system and are never added
        /// (owning them with different attributes may cause file conflicts with the 'filesystem' package).
        /// </summary>
        internal static void AddOwnedDirectories(List<ArchiveEntry> entries, ArchiveBuilder archiveBuilder, IEnumerable<string> roots)
        {
            var ownedRoots = roots
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => "/" + r.Replace('\\', '/').Trim('/'))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            bool IsOwned(string dirPath)
            {
                if (SystemDirectories.Contains(dirPath))
                {
                    return false;
                }

                return ownedRoots.Any(r => dirPath == r || dirPath.StartsWith(r == "/" ? "/" : r + "/", StringComparison.Ordinal));
            }

            var dirs = new HashSet<string>(
                entries.Where(e => e.Mode.HasFlag(LinuxFileMode.S_IFDIR)).Select(e => e.TargetPathWithoutFinalSlash),
                StringComparer.Ordinal);

            var toAdd = new List<ArchiveEntry>();

            foreach (var entry in entries)
            {
                string path = entry.TargetPathWithoutFinalSlash;
                int index = path.LastIndexOf('/');
                while (index > 0)
                {
                    path = path.Substring(0, index);
                    if (IsOwned(path) && dirs.Add(path))
                    {
                        toAdd.Add(new ArchiveEntry()
                        {
                            Mode = LinuxFileMode.S_IXOTH | LinuxFileMode.S_IROTH | LinuxFileMode.S_IXGRP |
                                   LinuxFileMode.S_IRGRP | LinuxFileMode.S_IXUSR | LinuxFileMode.S_IWUSR |
                                   LinuxFileMode.S_IRUSR | LinuxFileMode.S_IFDIR,
                            FileSize = 0x1000,
                            Modified = DateTimeOffset.UtcNow,
                            Group = "root",
                            Owner = "root",
                            TargetPath = path,
                            LinkTo = string.Empty,
                            Sha256 = Array.Empty<byte>(),
                            Inode = archiveBuilder.Inode++,
                        });
                    }

                    index = path.LastIndexOf('/');
                }
            }

            entries.AddRange(toAdd);
        }

        private static PackageDependency GetPackageDependency(ITaskItem dependency)
        {
            if (dependency == null)
            {
                return null;
            }

            string version = dependency.GetVersion();
            if (string.IsNullOrEmpty(version))
            {
                return new PackageDependency(dependency.ItemSpec, RpmSense.RPMSENSE_ANY, string.Empty);
            }

            return new PackageDependency(
                dependency.ItemSpec,
                RpmSense.RPMSENSE_EQUAL | RpmSense.RPMSENSE_GREATER,
                version);
        }
    }
}
