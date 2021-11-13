namespace PSSharp.ModuleFactory.Commands
{
    [Cmdlet(VerbsCommon.New, ModuleTemplateNoun,
        DefaultParameterSetName = nameof(Path),
        RemotingCapability = RemotingCapability.PowerShell,
        SupportsShouldProcess = true)]
    [OutputType(typeof(ModuleTemplateMetadata))]
    public sealed class NewModuleTemplateCommand : ModuleFactoryCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = nameof(Path))]
        public string Path { get; set; } = null!;
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = nameof(LiteralPath))]
        public string LiteralPath
        {
            get => Path;
            set => (_isLiteralPath, Path) = (true, value);
        }
        private bool _isLiteralPath;

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            var templatePath = System.IO.Path.Combine(Path, "template.psd1");
            var templates = ImportPowerShellDataFiles<ConfigurationModuleTemplate>(templatePath, _isLiteralPath, out var templateErrors, false);
            if (templateErrors) return;

            var template = templates.Single();

            if (string.IsNullOrWhiteSpace(template.Name))
            {
                var ex = new InvalidDataException(Resources.TemplateNameRequired);
                var er = new ErrorRecord(
                    ex,
                    nameof(Resources.TemplateNameRequired),
                    ErrorCategory.InvalidData,
                    Path)
                {
                    ErrorDetails = new ErrorDetails(string.Format(Resources.TemplateNameRequiredInterpolated, Path))
                };
                WriteError(er);
                return;
            }

            var metadata = new ModuleTemplateMetadata
            {
                Name = template.Name,
                Version = template.Version ?? new Version(1, 0, 0),
                Description = template.Description,
                TemplateId = Guid.NewGuid()
            };

            var templateDirectory = ResolvePath(Path, _isLiteralPath, out _, false, true)[0];

            var existingTemplateMetadata = Repository.ListTemplates();
            foreach (var templateMetadata in existingTemplateMetadata)
            {
                if (metadata.Name.Equals(templateMetadata.Name, StringComparison.OrdinalIgnoreCase)
                    && metadata.Version == templateMetadata.Version)
                {
                    var ex = new InvalidOperationException(Resources.TemplateVersionConflict);
                    var er = new ErrorRecord(
                        ex,
                        nameof(Resources.TemplateVersionConflict),
                        ErrorCategory.InvalidOperation,
                        templateMetadata)
                    {
                        ErrorDetails = new ErrorDetails(string.Format(Resources.TemplateVersionConflictInterpolated, metadata.Name, metadata.Version, Path))
                        {
                            RecommendedAction = Resources.TemplateVersionConflictRecommendedAction
                        }
                    };
                    WriteError(er);
                    return;
                }
            }

            if (ShouldProcess(
                string.Format(Resources.ShouldProcessNewModuleTemplateDescription, metadata.Name, metadata.Version, templateDirectory),
                string.Format(Resources.ShouldProcessNewModuleTemplateWarning, metadata.Name, metadata.Version, templateDirectory),
                "Create Module Template"
                ))
            {
                if (Repository.TryCreateModuleTemplate(templateDirectory, metadata, this))
                {
                    WriteObject(metadata);
                }
            }
        }
    }
}
