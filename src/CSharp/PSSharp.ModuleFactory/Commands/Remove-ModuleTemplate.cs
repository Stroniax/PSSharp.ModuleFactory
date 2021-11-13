namespace PSSharp.ModuleFactory.Commands
{
    [Cmdlet(VerbsCommon.Remove, ModuleTemplateNoun,
        DefaultParameterSetName = nameof(Name),
        SupportsShouldProcess = true,
        RemotingCapability = RemotingCapability.PowerShell)]
    [OutputType(typeof(System.Management.Automation.Internal.AutomationNull))]
    public sealed class RemoveModuleTemplateCommand : ModuleFactoryCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = nameof(Name))]
        [SupportsWildcards]
        [ModuleTemplateCompletion]
        public string? Name { get; set; } = null!;

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = nameof(Name))]
        [ModuleTemplateCompletion]
        public Version? Version { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = nameof(TemplateId), ValueFromPipelineByPropertyName = true)]
        [ModuleTemplateCompletion]
        public Guid TemplateId { get; set; }

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ParameterSetName = nameof(ModuleTemplate))]
        [ModuleTemplateCompletion]
        [ModuleTemplateMetadataTransformation]
        public ModuleTemplateMetadata[]? ModuleTemplate { get; set; }

        protected override void ProcessRecord()
        {
            if (ParameterSetName == nameof(Name))
            {
                var templatesToRemove = Repository.ListTemplates(Name ?? "*", true, Version?.ToString());
                if (templatesToRemove.Count == 0 && !WildcardPattern.ContainsWildcardCharacters(Name))
                {
                    var ex = new ItemNotFoundException(Resources.TemplateNotFound);
                    var er = new ErrorRecord(
                        ex,
                        nameof(Resources.TemplateNotFound),
                        ErrorCategory.ObjectNotFound,
                        Name)
                    {
                        ErrorDetails = new ErrorDetails(string.Format(Resources.TemplateNotFoundInterpolated, Name))
                    };
                    WriteError(er);
                }

                foreach (var template in templatesToRemove)
                {
                    if (ShouldProcess(
                        string.Format(Resources.ShouldProcessRemoveModuleTemplateDescription, template, template.TemplateId),
                        string.Format(Resources.ShouldProcessRemoveModuleTemplateWarning, template, template.TemplateId),
                        "Remove module template"))
                    {
                        Repository.DeleteTemplate(template.TemplateId);
                    }
                }
            }
            else if (ParameterSetName == nameof(TemplateId))
            {
                var template = Repository.GetTemplate(TemplateId);
                if (template is null)
                {
                    var ex = new ItemNotFoundException(Resources.TemplateNotFound);
                    var er = new ErrorRecord(
                        ex,
                        nameof(Resources.TemplateNotFound),
                        ErrorCategory.ObjectNotFound,
                        TemplateId)
                    {
                        ErrorDetails = new ErrorDetails(string.Format(Resources.TemplateNotFoundInterpolated, TemplateId))
                    };
                    WriteError(er);
                }
                else if (ShouldProcess(
                        string.Format(Resources.ShouldProcessRemoveModuleTemplateDescription, template, template.TemplateId),
                        string.Format(Resources.ShouldProcessRemoveModuleTemplateWarning, template, template.TemplateId),
                        "Remove module template"))
                {
                    Repository.DeleteTemplate(template.TemplateId);
                }
            }
            else if (ParameterSetName == nameof(ModuleTemplate))
            {
                foreach (var template in ModuleTemplate ?? Array.Empty<ModuleTemplateMetadata>())
                {
                    if (Repository.GetTemplate(template.TemplateId) is null)
                    {
                        var ex = new ItemNotFoundException(Resources.TemplateNotFound);
                        var er = new ErrorRecord(
                            ex,
                            nameof(Resources.TemplateNotFound),
                            ErrorCategory.ObjectNotFound,
                            TemplateId)
                        {
                            ErrorDetails = new ErrorDetails(string.Format(Resources.TemplateNotFoundInterpolated, template.TemplateId))
                        };
                        WriteError(er);
                    }
                    else if (ShouldProcess(
                        string.Format(Resources.ShouldProcessRemoveModuleTemplateDescription, template, template.TemplateId),
                        string.Format(Resources.ShouldProcessRemoveModuleTemplateWarning, template, template.TemplateId),
                        "Remove module template"))
                    {
                        Repository.DeleteTemplate(template.TemplateId);
                    }
                }
            }
            else throw new PSNotImplementedException(string.Format(Resources.ParameterSetNotImplemented, ParameterSetName));
        }
    }
}
