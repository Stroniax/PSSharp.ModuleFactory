function Get-ModuleFingerprint {
    [CmdletBinding(
        DefaultParameterSetName = 'DefaultSet',
        RemotingCapability = [System.Management.Automation.RemotingCapability]::PowerShell
    )]
    [OutputType([PSSharp.ModuleFactory.ModuleFingerprint])]
    param(
        [Parameter(
            Position = 0,
            Mandatory,
            ValueFromPipelineByPropertyName,
            ParameterSetName = 'DefaultSet')]
        [PSSharp.ModuleFactory.ModuleNameCompletion()]
        [SupportsWildcards()]
        [string[]]
        $Name,

        [Parameter(
            Position = 0,
            Mandatory,
            ValueFromPipelineByPropertyName,
            ValueFromPipeline,
            ParameterSetName = 'FullyQualifiedNameSet'
        )]
        [Microsoft.PowerShell.Commands.ModuleSpecification[]]
        [PSSharp.ModuleFactory.NoCompletion()]
        [Alias('ModuleSpecification')]
        $FullyQualifiedName,

        [Parameter(
            Position = 0,
            Mandatory,
            ValueFromPipeline,
            ParameterSetName = 'ModuleInfoSet')]
        [PSSharp.ModuleFactory.NoCompletion()]
        [psmoduleinfo[]]
        $Module
    )
    begin {
        $PSModuleAutoLoadingPreference = [System.Management.Automation.PSModuleAutoLoadingPreference]::None
    }
    process {
        # Identify the module
        if ($PSCmdlet.ParameterSetName -eq 'DefaultSet') {
            $InvokeAsModule = $PSCmdlet.SessionState.Module
            if (-not $InvokeAsModule) {
                $InvokeAsModule = [psmoduleinfo]::new($false)
                $InvokeAsModule.SessionState = $PSCmdlet.SessionState
            }

            $Module = & ($InvokeAsModule) { Get-Module -Name $args[0] } $Name
        }
        elseif ($PSCmdlet.ParameterSetName -eq 'FullyQualifiedNameSet') {
            $InvokeAsModule = $PSCmdlet.SessionState.Module
            if (-not $InvokeAsModule) {
                $InvokeAsModule = [psmoduleinfo]::new($false)
                $InvokeAsModule.SessionState = $PSCmdlet.SessionState
            }

            $Module = & ($InvokeAsModule) { Get-Module -FullyQualifiedName $args[0] } $FullyQualifiedName
        }
        elseif ($PSCmdlet.ParameterSetName -ne 'ModuleInfoSet') {
            $ex = [System.NotImplementedException]::new()
            $er = [ErrorRecord]::new(
                $ex,
                'ParameterSetNotImplemented',
                [System.Management.Automation.ErrorCategory]::NotImplemented,
                $PSCmdlet.ParameterSetName
            )
            $er.ErrorDetails = Get-PSSharpModuleFactoryResourceString ParameterSetNotImplemented $PSCmdlet.ParameterSetName
            $er.ErrorDetails.RecommendedAction = "Contact the module author for support. $(PSCmdlet.SessionState.Module.RepositorySourceLocation)"
            $PSCmdlet.WriteError($er)
            return
        }

        # If the module is imported, we can easily create a new fingerprint from the module metadata. However, if
        # the module is not imported we do not get descriptive CommandInfo objects so the fingerprint will be
        # inaccurate.
        $ImportedModules = @(Get-Module)

        foreach ($m in $Module) {
            $ModuleIsImported = $ImportedModules -contains $m
            if ($ModuleIsImported) {
                $Fingerprint = [PSSharp.ModuleFactory.ModuleFingerprint]::new($m)
                $Fingerprint.PSObject.Properties.Add([psnoteproperty]::new('PSModuleName', $m.Name))
                $Fingerprint
            }
            else {
                $ex = [System.InvalidOperationException]::new((Get-PSSharpModuleFactoryResourceString ModuleNotImported))
                $er = [System.Management.Automation.ErrorRecord]::new(
                    $ex,
                    'ModuleNotImported',
                    [System.Management.Automation.ErrorCategory]::InvalidArgument,
                    $m
                )
                $er.ErrorDetails = Get-PSSharpModuleFactoryResourceString ModuleNotImportedInterpolated $m.Name
                $er.ErrorDetails.RecommendedAction = Get-PSSharpModuleFactoryResourceString ModuleNotImportedRecommendedAction
                $PSCmdlet.WriteError($er)
                continue;
            }
        }
    }
}