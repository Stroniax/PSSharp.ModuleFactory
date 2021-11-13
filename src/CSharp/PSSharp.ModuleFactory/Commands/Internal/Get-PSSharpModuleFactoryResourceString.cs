namespace PSSharp.ModuleFactory.Commands
{
    [Cmdlet(VerbsCommon.Get, "PSSharpModuleFactoryResourceString")]
    [OutputType(typeof(string))]
    public sealed class GetPSSharpModuleFactoryResourceStringCommand : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        [Alias("ResourceId")]
        public string Name { get; set; } = null!;

        [Parameter(ValueFromRemainingArguments = true)]
        public object?[] ArgumentList { get; set; } = Array.Empty<object>();

        [Parameter]
        [Alias("ResourceBaseName")]
        [PSDefaultValue(Value = ModuleFactoryCmdlet.ResourceBaseName)]
        public string BaseName { get; set; } = ModuleFactoryCmdlet.ResourceBaseName;

        protected override void ProcessRecord()
        {
            var resourceString = GetResourceString(BaseName, Name);
            if (resourceString is not null)
            {
                WriteObject(string.Format(resourceString, args: ArgumentList));
            }
        }
    }
}
