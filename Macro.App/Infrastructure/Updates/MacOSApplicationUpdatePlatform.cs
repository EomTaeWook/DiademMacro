using Dignus.DependencyInjection.Attributes;
using Macro.Infrastructure.Models.Updates;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Macro.Infrastructure.Updates
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton, typeof(IApplicationUpdatePlatform))]
    public class MacOSApplicationUpdatePlatform : IApplicationUpdatePlatform
    {
        private const string _bundleName = "Diadem Macro.app";
        private readonly ApplicationUpdateFiles _files;

        public MacOSApplicationUpdatePlatform(ApplicationUpdateFiles files)
        {
            _files = files;
        }

        public string DownloadFileName => "verified.zip";

        [DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "renamex_np", SetLastError = true)]
        private static extern int RenameWithFlags([MarshalAs(UnmanagedType.LPUTF8Str)] string sourcePath, [MarshalAs(UnmanagedType.LPUTF8Str)] string targetPath, uint flags);

        public bool IsAssetName(string assetName, string releaseTag)
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                return assetName == "Macro-" + releaseTag + "-osx-arm64.zip";
            }
            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                return assetName == "Macro-" + releaseTag + "-osx-x64.zip";
            }
            return false;
        }

        public ApplicationUpdateMessageCode GetInstallBlockedCode(ApplicationUpdateEnvironment environment)
        {
            if (OperatingSystem.IsMacOSVersionAtLeast(14) == false)
            {
                return ApplicationUpdateMessageCode.DeploymentRequired;
            }
            if (RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
            {
                if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
                {
                    return ApplicationUpdateMessageCode.DeploymentRequired;
                }
            }
            if (environment.PublishedSingleFile == false)
            {
                return ApplicationUpdateMessageCode.DeploymentRequired;
            }
            if (Debugger.IsAttached == true)
            {
                return ApplicationUpdateMessageCode.DeploymentRequired;
            }
            try
            {
                ValidateInstallPath(environment.ExecutablePath);
                if (environment.ExecutablePath.Contains("/AppTranslocation/", StringComparison.Ordinal) == true)
                {
                    return ApplicationUpdateMessageCode.DeploymentRequired;
                }
                string executableDirectory = Path.GetDirectoryName(environment.ExecutablePath);
                if (File.Exists(Path.Combine(executableDirectory, "Macro.dll")) == true)
                {
                    return ApplicationUpdateMessageCode.DeploymentRequired;
                }
                if (File.Exists(Path.Combine(executableDirectory, "Macro.deps.json")) == true)
                {
                    return ApplicationUpdateMessageCode.DeploymentRequired;
                }
                return ApplicationUpdateMessageCode.None;
            }
            catch (ApplicationUpdateException)
            {
                return ApplicationUpdateMessageCode.DeploymentRequired;
            }
            catch (IOException)
            {
                return ApplicationUpdateMessageCode.DeploymentRequired;
            }
            catch (UnauthorizedAccessException)
            {
                return ApplicationUpdateMessageCode.DeploymentRequired;
            }
            catch (ArgumentException)
            {
                return ApplicationUpdateMessageCode.DeploymentRequired;
            }
        }

        public string GetHelperPath(string directory)
        {
            return Path.Combine(directory, "helper.app", "Contents", "MacOS", "Macro");
        }

        public void ValidateInstallPath(string executablePath)
        {
            string bundlePath = GetBundlePath(executablePath);
            _files.ValidatePlainFile(executablePath);
            _files.ValidateNoLink(bundlePath);
            _files.ValidateNoLink(Path.Combine(bundlePath, "Contents"));
            _files.ValidateNoLink(Path.GetDirectoryName(bundlePath));
        }

        public async Task PrepareHelperAsync(string executablePath, string directory, CancellationToken cancellationToken)
        {
            string bundlePath = GetBundlePath(executablePath);
            ValidateBundleFiles(bundlePath);
            await RunToolAsync("/usr/bin/codesign", cancellationToken, "--verify", "--deep", "--strict", bundlePath).ConfigureAwait(false);
            await RunToolAsync("/usr/bin/ditto", cancellationToken, bundlePath, Path.Combine(directory, "helper.app")).ConfigureAwait(false);
            _files.ValidatePlainFile(GetHelperPath(directory));
        }

        public async Task<int> InstallAndRestartAsync(ApplicationUpdateInstallManifest manifest, string directory)
        {
            string installedBundle = GetBundlePath(manifest.InstallPath);
            string backupBundle = null;
            bool replaced = false;
            try
            {
                string candidateDirectory = Path.Combine(directory, "candidate");
                string candidateArchive = Path.Combine(directory, DownloadFileName);
                ValidateArchive(candidateArchive, candidateDirectory);
                Directory.CreateDirectory(candidateDirectory);
                _files.ValidateNoLink(candidateDirectory);
                await RunToolAsync("/usr/bin/ditto", CancellationToken.None, "-x", "-k", candidateArchive, candidateDirectory).ConfigureAwait(false);
                string candidateBundle = Path.Combine(candidateDirectory, _bundleName);
                await ValidateCandidateAsync(candidateBundle, manifest.CandidateVersion).ConfigureAwait(false);
                ValidateInstallPath(manifest.InstallPath);
                await _files.VerifyHashAsync(manifest.InstallPath, manifest.InstalledSha256, CancellationToken.None).ConfigureAwait(false);
                backupBundle = Path.Combine(Path.GetDirectoryName(installedBundle), ".macro-update-" + Guid.NewGuid().ToString("N") + ".backup.app");
                if (Directory.Exists(backupBundle) == true)
                {
                    throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
                }
                if (File.Exists(backupBundle) == true)
                {
                    throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
                }
                await RunToolAsync("/usr/bin/ditto", CancellationToken.None, candidateBundle, backupBundle).ConfigureAwait(false);
                await ValidateCandidateAsync(backupBundle, manifest.CandidateVersion).ConfigureAwait(false);
                ValidateInstallPath(manifest.InstallPath);
                await _files.VerifyHashAsync(manifest.InstallPath, manifest.InstalledSha256, CancellationToken.None).ConfigureAwait(false);
                SwapBundles(backupBundle, installedBundle);
                replaced = true;
                await RestartAsync(manifest, directory, ApplicationUpdateMessageCode.None).ConfigureAwait(false);
                TryDeleteBundle(backupBundle);
                backupBundle = null;
                return 0;
            }
            catch (Exception exception)
            {
                ApplicationUpdateHelper.RecordFailure(_files, directory, exception);
                ApplicationUpdateMessageCode failureCode = ApplicationUpdateMessageCode.ReplaceFailed;
                if (replaced == true)
                {
                    failureCode = ApplicationUpdateMessageCode.LaunchFailed;
                    try
                    {
                        SwapBundles(backupBundle, installedBundle);
                        replaced = false;
                    }
                    catch (Exception rollbackException)
                    {
                        ApplicationUpdateHelper.RecordFailure(_files, directory, rollbackException);
                        failureCode = ApplicationUpdateMessageCode.RollbackFailed;
                    }
                }
                if (failureCode != ApplicationUpdateMessageCode.RollbackFailed)
                {
                    try
                    {
                        ValidateInstallPath(manifest.InstallPath);
                        await _files.VerifyHashAsync(manifest.InstallPath, manifest.InstalledSha256, CancellationToken.None).ConfigureAwait(false);
                        await RestartAsync(manifest, directory, failureCode).ConfigureAwait(false);
                    }
                    catch (Exception restartException)
                    {
                        ApplicationUpdateHelper.RecordFailure(_files, directory, restartException);
                    }
                }
                return 1;
            }
            finally
            {
                if (replaced == false)
                {
                    TryDeleteBundle(backupBundle);
                }
            }
        }

        public Task RestartAsync(ApplicationUpdateInstallManifest manifest, string directory, ApplicationUpdateMessageCode failureCode)
        {
            string[] arguments = { "-n", "-a", GetBundlePath(manifest.InstallPath), "--args", "--macro-update-cleanup", directory, Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture) };
            if (failureCode != ApplicationUpdateMessageCode.None)
            {
                Array.Resize(ref arguments, arguments.Length + 1);
                arguments[arguments.Length - 1] = failureCode.ToString();
            }
            return RunToolAsync("/usr/bin/open", CancellationToken.None, arguments);
        }

        private static string GetBundlePath(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath) == true)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
            if (Path.IsPathFullyQualified(executablePath) == false)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
            string fullPath = Path.GetFullPath(executablePath);
            string executableDirectory = Path.GetDirectoryName(fullPath);
            string contentsDirectory = Path.GetDirectoryName(executableDirectory);
            string bundlePath = Path.GetDirectoryName(contentsDirectory);
            if (Path.GetFileName(fullPath) != "Macro")
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
            if (Path.GetFileName(executableDirectory) != "MacOS")
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
            if (Path.GetFileName(contentsDirectory) != "Contents")
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
            if (string.IsNullOrEmpty(bundlePath) == true)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
            if (bundlePath.EndsWith(".app", StringComparison.Ordinal) == false)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
            return bundlePath;
        }

        private void ValidateArchive(string archivePath, string candidateDirectory)
        {
            string candidatePrefix = Path.GetFullPath(candidateDirectory) + Path.DirectorySeparatorChar;
            using ZipArchive archive = ZipFile.OpenRead(archivePath);
            if (archive.Entries.Count > 20000)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
            long expandedSize = 0;
            bool hasExecutable = false;
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string entryName = entry.FullName;
                if (entryName.Contains('\\') == true)
                {
                    throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
                }
                string destination = Path.GetFullPath(Path.Combine(candidateDirectory, entryName));
                if (destination.StartsWith(candidatePrefix, StringComparison.Ordinal) == false)
                {
                    throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
                }
                if (entryName.StartsWith(_bundleName + "/", StringComparison.Ordinal) == false)
                {
                    if (entryName != _bundleName)
                    {
                        if (entryName.StartsWith("__MACOSX/", StringComparison.Ordinal) == false)
                        {
                            throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
                        }
                    }
                }
                foreach (string component in entryName.Split('/'))
                {
                    if (component == "..")
                    {
                        throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
                    }
                    if (component == ".")
                    {
                        throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
                    }
                }
                int fileType = (entry.ExternalAttributes >> 16) & 0xF000;
                if (fileType == 0xA000)
                {
                    throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
                }
                expandedSize += entry.Length;
                if (expandedSize > 2L * 1024 * 1024 * 1024)
                {
                    throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
                }
                if (entryName == _bundleName + "/Contents/MacOS/Macro")
                {
                    hasExecutable = true;
                }
            }
            if (hasExecutable == false)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
        }

        private async Task ValidateCandidateAsync(string bundlePath, string expectedVersion)
        {
            if (OperatingSystem.IsMacOSVersionAtLeast(14) == false)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
            ValidateBundleFiles(bundlePath);
            string propertyListPath = Path.Combine(bundlePath, "Contents", "Info.plist");
            XmlReaderSettings settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null, MaxCharactersInDocument = 1024 * 1024 };
            using XmlReader reader = XmlReader.Create(propertyListPath, settings);
            XDocument propertyList = XDocument.Load(reader);
            XElement dictionary = propertyList.Root?.Element("dict");
            if (dictionary == null)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
            if (ReadBundleProperty(dictionary, "CFBundleIdentifier") != "org.eomtaewook.diademmacro")
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
            if (ReadBundleProperty(dictionary, "CFBundleExecutable") != "Macro")
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
            if (ReadBundleProperty(dictionary, "CFBundlePackageType") != "APPL")
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
            if (ReadBundleProperty(dictionary, "CFBundleShortVersionString") != expectedVersion)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
            if (Version.TryParse(ReadBundleProperty(dictionary, "LSMinimumSystemVersion"), out Version minimumVersion) == false)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
            if (OperatingSystem.IsMacOSVersionAtLeast(minimumVersion.Major, minimumVersion.Minor, Math.Max(0, minimumVersion.Build)) == false)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
            string executablePath = Path.Combine(bundlePath, "Contents", "MacOS", "Macro");
            using (BinaryReader executable = new BinaryReader(File.OpenRead(executablePath)))
            {
                if (executable.ReadUInt32() != 0xFEEDFACF)
                {
                    throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
                }
                int architecture = executable.ReadInt32();
                int expectedArchitecture = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? 0x0100000C : 0x01000007;
                if (architecture != expectedArchitecture)
                {
                    throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
                }
            }
            if ((File.GetUnixFileMode(executablePath) & UnixFileMode.UserExecute) == 0)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
            await RunToolAsync("/usr/bin/codesign", CancellationToken.None, "--verify", "--deep", "--strict", bundlePath).ConfigureAwait(false);
        }

        private static string ReadBundleProperty(XElement dictionary, string propertyName)
        {
            string value = null;
            foreach (XElement element in dictionary.Elements("key"))
            {
                if (element.Value != propertyName)
                {
                    continue;
                }
                if (value != null)
                {
                    throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
                }
                if (element.NextNode is not XElement propertyValue)
                {
                    throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
                }
                if (propertyValue.Name != "string")
                {
                    throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
                }
                value = propertyValue.Value;
            }
            if (string.IsNullOrWhiteSpace(value) == true)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
            return value;
        }

        private void ValidateBundleFiles(string directory)
        {
            _files.ValidateNoLink(directory);
            foreach (string childDirectory in Directory.EnumerateDirectories(directory))
            {
                ValidateBundleFiles(childDirectory);
            }
            foreach (string filePath in Directory.EnumerateFiles(directory))
            {
                _files.ValidateNoLink(filePath);
            }
        }

        private static void SwapBundles(string firstBundle, string secondBundle)
        {
            if (RenameWithFlags(firstBundle, secondBundle, 0x00000002) != 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "The application bundles could not be atomically exchanged.");
            }
        }

        private void TryDeleteBundle(string directory)
        {
            if (directory == null)
            {
                return;
            }
            try
            {
                _files.DeletePlainDirectory(directory);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (ApplicationUpdateException)
            {
            }
        }

        private static async Task RunToolAsync(string executablePath, CancellationToken cancellationToken, params string[] arguments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProcessStartInfo start = new ProcessStartInfo(executablePath) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (string argument in arguments)
            {
                start.ArgumentList.Add(argument);
            }
            using Process process = Process.Start(start);
            if (process == null)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
            Task<string> output = process.StandardOutput.ReadToEndAsync();
            Task<string> error = process.StandardError.ReadToEndAsync();
            // Wait for the owned copy/extraction process before cleaning its files on cancellation.
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            await output.ConfigureAwait(false);
            string errorText = await error.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(Path.GetFileName(executablePath) + " exited with code " + process.ExitCode + ": " + errorText);
            }
        }
    }
}
