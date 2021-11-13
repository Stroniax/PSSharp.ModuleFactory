namespace PSSharp.ModuleFactory.Commands
{
    [Cmdlet(VerbsCommon.Set, ModuleTemplateRepositoryNoun,
        SupportsShouldProcess = true,
        RemotingCapability = RemotingCapability.PowerShell)]
    public sealed class SetModuleTemplateRepositoryCommand : ModuleFactoryCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [Alias("Repository")]
        [AllowNull]
        public IModuleTemplateRepository? TemplateRepository { get; set; }

        protected override void ProcessRecord()
        {
            var currentRepository = IsRepositoryLoaded ? Repository.ToString() : "null";
            var providedRepository = TemplateRepository is null ? "null (reset to default)" : TemplateRepository.ToString();
            if (ShouldProcess(
                string.Format(Resources.ShouldProcessSetModuleTemplateRepositoryDescription, providedRepository, currentRepository),
                string.Format(Resources.ShouldProcessSetModuleTemplateRepositoryWarning, providedRepository, currentRepository),
                Resources.ShouldProcessSetModuleTemplateRepositoryAction))
            {
                Repository = TemplateRepository;
            }
        }
    }
}
