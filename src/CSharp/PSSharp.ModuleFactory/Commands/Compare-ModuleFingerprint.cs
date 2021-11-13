namespace PSSharp.ModuleFactory.Commands
{
    [Cmdlet(VerbsData.Compare, ModuleFactoryCmdlet.ModuleFingerprintNoun, DefaultParameterSetName = DefaultSet, RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(ModuleFingerprintChange), ParameterSetName = new[] { DefaultSet })]
    [OutputType(typeof(ModuleVersionStep), ParameterSetName = new[] { VersionStepSet })]
    public sealed class CompareModuleFingerprintCommand : Cmdlet, IDynamicParameters
    {
        private const string CmdletName = VerbsData.Compare + "-" + ModuleFactoryCmdlet.ModuleFingerprintNoun;
        public const string DefaultSet = "DefaultSet";
        public const string VersionStepSet = "VersionStep";

        [Parameter(Mandatory = true, Position = 0,
            ParameterSetName = DefaultSet,
            HelpMessageBaseName = ModuleFactoryCmdlet.ResourceBaseName,
            HelpMessageResourceId = nameof(Resources.HelpMessageCompareModuleFingerprintInitial))]
        [Parameter(Mandatory = true, Position = 0,
            ParameterSetName = VersionStepSet,
            HelpMessageBaseName = ModuleFactoryCmdlet.ResourceBaseName,
            HelpMessageResourceId = nameof(Resources.HelpMessageCompareModuleFingerprintInitial))]
        [FilePathToModuleFingerprintTransformation]
        [Alias("Previous")]
        public ModuleFingerprint Initial { get; set; } = null!;
        [Parameter(Mandatory = true, Position = 1,
            ParameterSetName = DefaultSet,
            HelpMessageBaseName = ModuleFactoryCmdlet.ResourceBaseName,
            HelpMessageResourceId = nameof(Resources.HelpMessageCompareModuleFingerprintCurrent))]
        [Parameter(Mandatory = true, Position = 1,
            ParameterSetName = VersionStepSet,
            HelpMessageBaseName = ModuleFactoryCmdlet.ResourceBaseName,
            HelpMessageResourceId = nameof(Resources.HelpMessageCompareModuleFingerprintCurrent))]
        [FilePathToModuleFingerprintTransformation]
        public ModuleFingerprint Current { get; set; } = null!;

        [Parameter(Mandatory = true, ParameterSetName = VersionStepSet), ValidateSet("true")]
        public SwitchParameter VersionStep { get; set; }

        public object? GetDynamicParameters()
            => VersionStep ? _dynamicParameters : null;
        private readonly CompareModuleFingerprintCommandDynamicParameters _dynamicParameters = new();
        private sealed class CompareModuleFingerprintCommandDynamicParameters
        {
            [Parameter]
            public ModuleFingerprintChange MajorChange { get; set; }
                = ModuleFingerprintChange.MandatoryParameterAdded
                | ModuleFingerprintChange.MandatoryParameterRemoved
                | ModuleFingerprintChange.NonMandatoryParameterRemoved
                | ModuleFingerprintChange.CommandRemoved
                | ModuleFingerprintChange.ParameterSetRemoved
                | ModuleFingerprintChange.ParameterBecameMandatory
                | ModuleFingerprintChange.ParameterTypeChanged
                | ModuleFingerprintChange.ParameterPositionChanged;

            [Parameter]
            public ModuleFingerprintChange MinorChange { get; set; }
                = ModuleFingerprintChange.NonMandatoryParameterAdded
                | ModuleFingerprintChange.CommandAdded
                | ModuleFingerprintChange.ParameterBecameNonMandatory
                | ModuleFingerprintChange.ParameterSetAdded
                | ModuleFingerprintChange.ParameterSetNameChanged;

            [Parameter]
            public ModuleFingerprintChange PatchChange { get; set; }
                = ModuleFingerprintChange.None;
        }

        protected override void ProcessRecord()
        {
            var changes = Current.GetChangesFrom(Initial);
            if (VersionStep)
            {
                WriteInformation(new InformationRecord(string.Format(Resources.CompareModuleFingerprintInformation, changes), CmdletName));
                var step = ModuleVersionStep.None;
                foreach (var val in Enum.GetValues<ModuleFingerprintChange>())
                {
                    if (changes.HasFlag(val))
                    {
                        if (_dynamicParameters.MajorChange.HasFlag(val))
                        {
                            step = ModuleVersionStep.Major;
                            WriteObject(step);
                            return;
                        }
                        else if (_dynamicParameters.MinorChange.HasFlag(val))
                        {
                            step = ModuleVersionStep.Minor;
                        }
                        else if (_dynamicParameters.PatchChange.HasFlag(val))
                        {
                            step = step > ModuleVersionStep.Patch ? step : ModuleVersionStep.Patch;
                        }
                    }
                }
                WriteObject(changes);
            }
            else
            {
                WriteObject(changes);
            }
        }
    }
}
