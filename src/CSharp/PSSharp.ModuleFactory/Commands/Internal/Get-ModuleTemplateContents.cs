namespace PSSharp.ModuleFactory.Commands
{
    /// <summary>
    /// Lists the files used for a module template.
    /// </summary>
    /// <remarks>This cmdlet is internally used by Export-ModuleTemplate and is not intended to be public.</remarks>
    [Cmdlet(VerbsCommon.Get, ModuleTemplateContentsNoun)]
    [OutputType(typeof(ITemplateContents))]
    public sealed class GetModuleTemplateContentsCommand : ModuleFactoryCmdlet
    {
        public const string ModuleTemplateContentsNoun = "ModuleTemplateContents";

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [ModuleTemplateMetadataTransformation(SupportWildcards = true)]
        public ModuleTemplateMetadata[] Template { get; set; } = null!;
        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            foreach (var template in Template)
            {
                WriteObject(Repository.GetTemplateContents(template.TemplateId));
            }
        }
    }
}
