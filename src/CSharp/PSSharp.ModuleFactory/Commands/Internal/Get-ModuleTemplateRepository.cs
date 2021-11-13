namespace PSSharp.ModuleFactory.Commands
{
    /// <summary>
    /// This cmdlet exposes the <see cref="ModuleTemplateRepository"/> to the internal functions within
    /// the module while allowing us to keep the repository as an internal type.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, ModuleTemplateRepositoryNoun, RemotingCapability = RemotingCapability.PowerShell)]
    public sealed class GetModuleTemplateRepositoryCommand : ModuleFactoryCmdlet
    {
        protected override void ProcessRecord()
        {
            WriteObject(Repository);
        }
    }
}
