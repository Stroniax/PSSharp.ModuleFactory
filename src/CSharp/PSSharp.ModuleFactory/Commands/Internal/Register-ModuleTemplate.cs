namespace PSSharp.ModuleFactory.Commands
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// This cmdlet is internal and is only used by the Import-ModuleTemplate function
    /// after expanding the compressed template archive.
    /// </remarks>
    [Cmdlet(VerbsLifecycle.Register, ModuleTemplateNoun,
        SupportsShouldProcess = true,
        RemotingCapability = RemotingCapability.PowerShell,
        DefaultParameterSetName = nameof(Path))]
    public sealed class RegisterModuleTemplateCommand : ModuleFactoryCmdlet
    {
        private bool _isLiteralPath;

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = nameof(Path))]
        public string Path { get; set; } = null!;
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = nameof(LiteralPath))]
        public string LiteralPath
        {
            get => Path;
            set => (Path, _isLiteralPath) = (value, true);
        }
        [Parameter(Mandatory = true, Position = 1)]
        public ModuleTemplateMetadata Metadata { get; set; } = null!;

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            var fileSystemPaths = ResolvePath(Path, _isLiteralPath, out var hadErrors, allowMultipleFiles: false, requireFileSystemPath: true);
            if (hadErrors) return;

            var fileSystemPath = fileSystemPaths.Single();
            if (ShouldProcess(
                string.Format(Resources.ShouldProcessRegisterModuleTemplateDescription, Metadata, Metadata.TemplateId),
                string.Format(Resources.ShouldProcessRegisterModuleTemplateWarning, Metadata, Metadata.TemplateId),
                Resources.ShouldProcessRegisterModuleTemplateAction,
                out var isWhatIf
                ))
            {
                if (Repository.TryCreateModuleTemplate(fileSystemPath, Metadata, this))
                {
                    WriteObject(Metadata);
                }
            }
            if (isWhatIf == ShouldProcessReason.WhatIf)
            {
                WriteDebug("Detected WhatIf. Checking if template registration would have errors. The template will not be registered.");
                // confirm that no conflicting template exists
                var conflictById = Repository.GetTemplate(Metadata.TemplateId);
                if (conflictById is not null)
                {
                    var ex = new InvalidOperationException(Resources.RepositoryTemplateIdConflict);
                    var er = new ErrorRecord(
                        ex,
                        nameof(Resources.RepositoryTemplateIdConflict),
                        ErrorCategory.InvalidData,
                        Metadata)
                    {
                        ErrorDetails = new ErrorDetails(string.Format(Resources.RepositoryTemplateIdConflictInterpolated, Metadata.TemplateId))
                    };
                    WriteError(er);
                    return;
                }
                var conflictByName = Repository.ListTemplates(Metadata.Name ?? string.Empty, false, Metadata.Version?.ToString());
                if (conflictByName.Count > 0)
                {
                    var ex = new InvalidOperationException(Resources.TemplateVersionConflict);
                    var er = new ErrorRecord(
                        ex,
                        nameof(Resources.TemplateVersionConflict),
                        ErrorCategory.InvalidData,
                        Metadata)
                    {
                        ErrorDetails = new ErrorDetails(string.Format(Resources.TemplateVersionConflictInterpolated, Metadata.Name, Metadata.Version))
                    };
                    WriteError(er);
                    return;
                }
                // confirm that the template.psd1 file exists
                var dataFilePath = System.IO.Path.Combine(fileSystemPath, "template.psd1");
                if (!File.Exists(dataFilePath))
                {
                    var inner = new FileNotFoundException(null, dataFilePath);
                    var ex = new ItemNotFoundException(Resources.TemplateDataFileNotFound, inner);
                    var er = new ErrorRecord(
                        ex,
                        nameof(Resources.TemplateDataFileNotFound),
                        ErrorCategory.ObjectNotFound,
                        dataFilePath)
                    {
                        ErrorDetails = new ErrorDetails(string.Format(Resources.TemplateDataFileNotFoundInterpolated, dataFilePath))
                    };
                    WriteError(er);
                    return;
                }
            }
        }
    }
}
