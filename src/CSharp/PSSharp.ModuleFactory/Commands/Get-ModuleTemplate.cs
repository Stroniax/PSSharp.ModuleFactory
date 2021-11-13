namespace PSSharp.ModuleFactory.Commands
{
    /// <summary>
    /// Retrieves module templates that are available on the current system.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, ModuleTemplateNoun,
        DefaultParameterSetName = nameof(Name),
        RemotingCapability = RemotingCapability.PowerShell)]
    [OutputType(typeof(ModuleTemplateMetadata))]
    public sealed class GetModuleTemplateCommand : ModuleFactoryCmdlet
    {
        /// <summary>
        /// The name of the template(s) to list.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = nameof(Name))]
        [ModuleTemplateCompletion]
        [ValidateNotNullOrEmpty]
        [SupportsWildcards]
        public string? Name
        {
            get => _name;
            set
            {
                _name = value;
                _wildcardPattern = WildcardPattern.Get(value ?? "", WildcardOptions.IgnoreCase);
            }
        }
        /// <summary>
        /// The <see cref="ModuleTemplateMetadata.TemplateId"/> of the template to retrieve.
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = nameof(TemplateId))]
        [ModuleTemplateCompletion]
        public Guid TemplateId { get; set; }

        private string? _name;
        private WildcardPattern _wildcardPattern = WildcardPattern.Get("*", WildcardOptions.IgnoreCase);
        protected override void ProcessRecord()
        {
            var templates = Repository.ListTemplates();
            if (Name is null && ParameterSetName == nameof(Name))
            {
                WriteObject(templates, true);
                return;
            }
            var yieldAny = false;
            foreach (var template in templates)
            {
                switch (ParameterSetName)
                {
                    case nameof(Name):
                        {
                            if (_wildcardPattern.IsMatch(template.Name))
                            {
                                yieldAny = true;
                                WriteObject(template);
                            }
                        }
                        break;
                    case nameof(TemplateId):
                        {
                            if (TemplateId == template.TemplateId)
                            {
                                yieldAny = true;
                                WriteObject(template);
                            }
                        }
                        break;
                }
            }
            if (!yieldAny && !WildcardPattern.ContainsWildcardCharacters(Name))
            {
                var ex = new ItemNotFoundException(Resources.TemplateNotFound);
                var er = new ErrorRecord(
                    ex,
                    nameof(Resources.TemplateNotFound),
                    ErrorCategory.ObjectNotFound,
                    Name ?? TemplateId as object);
                WriteError(er);
            }
        }
    }
}