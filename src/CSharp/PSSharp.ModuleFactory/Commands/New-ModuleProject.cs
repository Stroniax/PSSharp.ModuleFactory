using Microsoft.PowerShell.Commands;
using System.Management.Automation.Language;
using System.Reflection;

namespace PSSharp.ModuleFactory.Commands
{
    [Cmdlet(VerbsCommon.New, ModuleProjectNoun,
        SupportsShouldProcess = true,
        DefaultParameterSetName = TemplateNamePathSet,
        RemotingCapability = RemotingCapability.PowerShell)]
    [OutputType(typeof(System.Management.Automation.Internal.AutomationNull))]
    public sealed class NewModuleProjectCommand : ModuleFactoryCmdlet, IDynamicParameters
    {
        public const string TemplateNamePathSet = "TemplateName";
        public const string TemplateIdPathSet = "TemplateId";


        [Parameter(Mandatory = true, Position = 0, ParameterSetName = TemplateNamePathSet)]
        [ModuleTemplateCompletion]
        public string? TemplateName { get; set; }

        [Parameter(ParameterSetName = TemplateNamePathSet)]
        [ModuleTemplateCompletion]
        public Version? TemplateVersion { get; set; }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = TemplateIdPathSet)]
        [ModuleTemplateCompletion]
        public Guid TemplateId { get; set; }

        [Parameter(Mandatory = true, Position = 1, ParameterSetName = TemplateNamePathSet)]
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = TemplateIdPathSet)]
        [Alias("DestinationPath")]
        public string OutputPath { get; set; } = null!;

        public object? GetDynamicParameters()
        {
            string templateId;
            if (TemplateName is not null)
            {
                var templates = Repository.ListTemplates(TemplateName, true, null);
                if (templates.Count == 1)
                {
                    templateId = templates[0].TemplateId.ToString();
                }
                else
                {
                    return _dynamicParameters = null;
                }
            }
            else if (MyInvocation.BoundParameters.ContainsKey(nameof(TemplateId)))
            {
                templateId = TemplateId.ToString();
            }
            else
            {
                return _dynamicParameters = null;
            }
            var templateIdWc = Guid.TryParse(templateId, out _)
                ? WildcardPattern.Get(templateId, WildcardOptions.IgnoreCase)
                : WildcardPattern.Get(templateId + "*", WildcardOptions.IgnoreCase);
            var template = Repository.ListTemplates().Find(i => templateIdWc.IsMatch(i.TemplateId.ToString()));
            if (template is null) return null;
            using var contents = Repository.GetTemplateContents(template.TemplateId);
            if (contents is null) return null;
            var templateFilePath = System.IO.Path.Combine(contents.TemplateDirectory, "template.psd1");
            if (!File.Exists(templateFilePath)) return _dynamicParameters = null;
            var templateFileAst = Parser.ParseFile(templateFilePath, out _, out var parseErrors);
            if (parseErrors.Length > 0) return _dynamicParameters = null;
            var hashtableAst = templateFileAst.Find<HashtableAst>(false);
            if (hashtableAst is null) return _dynamicParameters = null;
            object hashtable;
            try
            {
                hashtable = hashtableAst.SafeGetValue();
            }
            catch
            {
                return _dynamicParameters = null;
            }
            if (!LanguagePrimitives.TryConvertTo<ConfigurationModuleTemplate>(hashtable, out var templateFile))
            {
                return _dynamicParameters = null;
            }
            return _dynamicParameters = templateFile.GetDynamicParameters();
        }
        private RuntimeDefinedParameterDictionary? _dynamicParameters;

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            ModuleTemplateMetadata? template;
            if (ParameterSetName == TemplateNamePathSet)
            {
                var matchingTemplates = Repository.ListTemplates(TemplateName!, false, TemplateVersion?.ToString());
                if (matchingTemplates.Count == 0)
                {
                    matchingTemplates = Repository.ListTemplates(TemplateName!, true, TemplateVersion?.ToString());
                }
                if (matchingTemplates.Count == 0)
                {
                    var ex = new ItemNotFoundException(Resources.TemplateNotFound);
                    var er = new ErrorRecord(
                        ex,
                        nameof(Resources.TemplateNotFound),
                        ErrorCategory.ObjectNotFound,
                        TemplateName)
                    {
                        ErrorDetails = new ErrorDetails(string.Format(Resources.TemplateNotFoundInterpolated, TemplateName))
                    };
                    WriteError(er);
                    return;
                }
                else if (matchingTemplates.Count > 1)
                {
                    var ex = new AmbiguousMatchException(Resources.AmbiguousTemplateName);
                    var er = new ErrorRecord(
                        ex,
                        nameof(Resources.AmbiguousTemplateName),
                        ErrorCategory.InvalidArgument,
                        TemplateName)
                    {
                        ErrorDetails = new ErrorDetails(string.Format(Resources.AmbiguousTemplateNameInterpolated, TemplateName))
                    };
                    WriteError(er);
                    return;
                }
                else
                {
                    template = matchingTemplates[0];
                }
            }
            else if (ParameterSetName == TemplateIdPathSet)
            {
                template = Repository.GetTemplate(TemplateId);
                if (template is null)
                {
                    var ex = new ItemNotFoundException(Resources.TemplateNotFound);
                    var er = new ErrorRecord(
                        ex,
                        nameof(Resources.TemplateNotFound),
                        ErrorCategory.ObjectNotFound,
                        TemplateName)
                    {
                        ErrorDetails = new ErrorDetails(string.Format(Resources.TemplateNotFoundInterpolated, TemplateId))
                    };
                    WriteError(er);
                    return;
                }
            }
            else throw new PSNotImplementedException(string.Format(Resources.ParameterSetNotImplemented, ParameterSetName));

            using var templateContents = Repository.GetTemplateContents(template.TemplateId);
            if (templateContents is null)
            {
                // because we just retrieved the template from the repository, theoretically this should never happen
                var ex = new ItemNotFoundException(Resources.TemplateNotFound);
                var er = new ErrorRecord(
                    ex,
                    nameof(Resources.TemplateNotFound),
                    ErrorCategory.ObjectNotFound,
                    template.TemplateId)
                {
                    ErrorDetails = new ErrorDetails(string.Format(Resources.TemplateNotFoundInterpolated, template.TemplateId))
                };
                WriteError(er);
                return;
            }
            var templateManifestPath = System.IO.Path.Combine(templateContents.TemplateDirectory, "template.psd1");
            if (!File.Exists(templateManifestPath))
            {
                var ex = new InvalidDataException(Resources.TemplateDataFileNotFound);
                var er = new ErrorRecord(
                    ex,
                    nameof(Resources.TemplateDataFileNotFound),
                    ErrorCategory.InvalidData,
                    template)
                {
                    ErrorDetails = new ErrorDetails(string.Format(Resources.TemplateDataFileNotFoundInterpolated, templateContents.TemplateDirectory))
                };
                WriteError(er);
                return;
            }
            var manifestMany = ImportPowerShellDataFiles<ConfigurationModuleTemplate>(templateManifestPath, true, out var templateManifestErrors, false);
            if (templateManifestErrors)
            {
                return;
            }

            if (manifestMany.Length != 1)
            {
                var ex = new InvalidCastException(Resources.DataFileCastError);
                var er = new ErrorRecord(
                    ex,
                    nameof(Resources.DataFileCastError),
                    ErrorCategory.InvalidData,
                    manifestMany)
                {
                    ErrorDetails = new ErrorDetails(string.Format(Resources.DataFileCastErrorInterpolated, templateManifestPath, typeof(ConfigurationModuleTemplate)))
                };
                WriteError(er);
                return;
            }

            var manifest = manifestMany[0];

            var destinationPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(OutputPath, out var provider, out _);
            if (provider.ImplementingType != typeof(FileSystemProvider))
            {
                var ex = new ArgumentException(Resources.ExpectedFileSystemPath);
                var er = new ErrorRecord(
                    ex,
                    nameof(Resources.ExpectedFileSystemPath),
                    ErrorCategory.InvalidArgument,
                    OutputPath
                    )
                {
                    ErrorDetails = new ErrorDetails(string.Format(Resources.ExpectedFileSystemPathInterpolated, OutputPath))
                };
                WriteError(er);
                return;
            }

            var shouldProcess = ShouldProcess(
                string.Format(Resources.ShouldProcessNewModuleProjectDescription, destinationPath, template),
                string.Format(Resources.ShouldProcessNewModuleProjectWarning, destinationPath, template),
                Resources.ShouldProcessNewModuleProjectAction);

            if (shouldProcess)
            {
                foreach (var command in manifest.BeforeExecute) command.ImportedFromDataFile = true;
                foreach (var command in manifest.AfterExecute) command.ImportedFromDataFile = true;

                SessionState.PSVariable.Set(ConfigurationModuleTemplate.TemplateDynamicParameters, _dynamicParameters);
                SessionState.PSVariable.Set(ConfigurationModuleTemplate.TemplateDestinationVariable, destinationPath);
                SessionState.PSVariable.Set(ConfigurationModuleTemplate.TemplatePathVariable, templateManifestPath);
                manifest.Invoke(SessionState);
            }
        }
    }
}
