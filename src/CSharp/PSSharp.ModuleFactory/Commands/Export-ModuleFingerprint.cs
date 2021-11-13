using Microsoft.PowerShell.Commands;
using System.Collections.ObjectModel;
using System.Management.Automation.Provider;
using System.Text.Json;

namespace PSSharp.ModuleFactory.Commands
{
    [Cmdlet(VerbsData.Export, ModuleFingerprintNoun, SupportsShouldProcess = true, RemotingCapability = RemotingCapability.PowerShell)]
    [OutputType(typeof(FileInfo))]
    public sealed class ExportModuleFingerprintCommand : ModuleFactoryCmdlet
    {
        /// <summary>
        /// The module fingerprint to export.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [NoCompletion]
        public ModuleFingerprint Fingerprint { get; set; } = null!;

        /// <summary>
        /// The path to which the fingerprint should be exported.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1)]
        [Alias("FilePath", "OutputPath", "DestinationPath")]
        public string Path { get; set; } = null!;

        /// <summary>
        /// Overwrites an existing file at the provided path.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// Write the fingerprint file to cmdlet output.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            var path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path, out var provider, out _);
            var dirOfPath = System.IO.Path.GetDirectoryName(path);
            var createDir = dirOfPath is not null && !Directory.Exists(dirOfPath);

            if (!Force
                && File.Exists(path))
            {
                var ex = new InvalidOperationException(Resources.FileExists);
                var er = new ErrorRecord(
                    ex,
                    nameof(Resources.FileExists),
                    ErrorCategory.InvalidOperation,
                    path);
                er.ErrorDetails = new ErrorDetails(string.Format(Resources.FileExistsInterpolated, path));
                WriteError(er);
                return;
            }

            var createDirWarning = createDir ? string.Format(Resources.ShouldProcessAndCreateDirectoryWarning, dirOfPath) : string.Empty;
            var createDirDescription = createDir ? string.Format(Resources.ShouldProcessAndCreateDirectoryDescription, dirOfPath) : string.Empty;

            var shouldProcess = ShouldProcess(
                string.Format(Resources.ShouldProcessExportModuleFingerprintDescription, path) + createDirDescription,
                string.Format(Resources.ShouldProcessExportModuleFingerprintWarning, path) + createDirWarning,
                Resources.ShouldProcessExportModuleFingerprintAction);

            if (!shouldProcess) return;

            if (createDir)
            {
                Directory.CreateDirectory(dirOfPath!);
            }
            if (provider.ImplementingType == typeof(FileSystemProvider))
            {
                using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
                JsonSerializer.Serialize(fs, Fingerprint);
            }
            else
            {
                string? json = null;
                Collection<IContentWriter>? writers;
                writers = SessionState.InvokeProvider.Content.GetWriter(path);
                foreach (var writer in writers)
                {
                    if (writer is Stream stream)
                    {
                        JsonSerializer.Serialize(stream, Fingerprint);
                    }
                    else
                    {
                        json ??= JsonSerializer.Serialize(Fingerprint);
                        writer.Write(json.ToCharArray());
                    }
                    writer.Dispose();
                }
            }


            if (PassThru)
            {
                var item = SessionState.InvokeProvider.Item.Get(path);
                WriteObject(item, true);
            }
        }
    }
}
