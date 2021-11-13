using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace PSSharp.ModuleFactory.Commands
{
    [Cmdlet(VerbsData.Import, ModuleFingerprintNoun,
        DefaultParameterSetName = nameof(Path),
        RemotingCapability = RemotingCapability.PowerShell)]
    [OutputType(typeof(ModuleFingerprint))]
    public sealed class ImportModuleFingerprintCommand : ModuleFactoryCmdlet
    {
        private bool _isLiteralPath;
        private string _path = null!;
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = nameof(Path))]
        [Alias("FilePath")]
        public string Path
        {
            get => _path;
            set => (_path, _isLiteralPath) = (value, false);
        }
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = nameof(LiteralPath))]
        [Alias("PSPath")]
        public string LiteralPath
        {
            get => _path;
            set => (_path, _isLiteralPath) = (value, true);
        }

        protected override void ProcessRecord()
        {
            var paths = ResolvePath(_path, _isLiteralPath, out var resolvePathErrors, allowMultipleFiles: false, requireFileSystemPath: true);
            if (resolvePathErrors) return;
            var path = paths[0];


            var errors = new ConcurrentQueue<ErrorRecord>();
            ModuleFingerprint? fingerprint;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using var textReader = new StreamReader(fs);
                using var jsonReader = new JsonTextReader(textReader);
                var serializer = new JsonSerializer();

                serializer.Error += (a, b) =>
                {
                    b.ErrorContext.Handled = true;
                    var inner = b.ErrorContext.Error;
                    var targetObject = new PSObject();
                    targetObject.Properties.Add(new PSNoteProperty("FilePath", path));
                    targetObject.Properties.Add(new PSNoteProperty("Member", b.ErrorContext.Member));
                    targetObject.Properties.Add(new PSNoteProperty("OriginalObject", b.ErrorContext.OriginalObject));
                    targetObject.Properties.Add(new PSNoteProperty("CurrentObject", b.CurrentObject));
                    targetObject.Properties.Add(new PSNoteProperty("JsonPath", b.ErrorContext.Path));

                    var er = new ErrorRecord(
                        b.ErrorContext.Error,
                        "InvalidJson",
                        ErrorCategory.ParserError,
                        targetObject);
                    errors.Enqueue(er);
                };

                fingerprint = serializer.Deserialize<ModuleFingerprint>(jsonReader);
            }
            if (fingerprint is null || !errors.IsEmpty)
            {
                while (errors.TryDequeue(out var errorRecord))
                {
                    WriteError(errorRecord);
                }
            }
            else
            {
                var pso = PSObject.AsPSObject(fingerprint);
                pso.Properties.Add(new PSNoteProperty("FilePath", path));
                WriteObject(pso);
            }
        }
    }
}
