[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', '', Scope='Function', Target='*')]
param()

Describe 'Compare-ModuleFingerprint' {
    BeforeAll {
        $HashtableForParameterFingerprint = @{
            Name = 'MyParameter'
            TypeName = 'System.String'
            Position = 0
        }
        $HashtableForParameterSetFingerprint = @{
            Name = 'MyParameterSet'
            Parameters = @( $HashtableForParameterFingerprint )
        }
        $HashtableForCommandFingerprint = @{
            Name = 'Test-MyCommand'
            ParameterSets = @( $HashtableForParameterSetFingerprint )
        }
        $HashtableForModuleFingerprint = @{
            Commands = @( $HashtableForCommandFingerprint )
        }
        $Fingerprint = [PSSharp.ModuleFactory.ModuleFingerprint]$HashtableForModuleFingerprint
        $FingerprintCopy = [PSSharp.ModuleFactory.ModuleFingerprint]$HashtableForModuleFingerprint
    }
    Context 'Input' {
        It 'acccepts fingerprints positionally' {
            { Compare-ModuleFingerprint $Fingerprint $FingerprintCopy } | Should -Not -Throw
        }
        It 'transforms a path into a fingerprint' {
            $File = New-TemporaryFile
            try {
                $Fingerprint | Export-ModuleFingerprint -Path $File.FullName | Out-Null
                {
                    Compare-ModuleFingerprint -Initial $Fingerprint -Current $File.FullName
                } | Should -Not -Throw
            }
            finally {
                Remove-Item $File.FullName
            }
        }
        Context 'VersionStepParameterSet' {
            It 'allows the -VersionStep parameter' {
                {
                    Compare-ModuleFingerprint -Initial $Fingerprint -Current $Fingerprint -VersionStep
                } | Should -Not -Throw
            }
            It 'fails with -VersionStep:$false' {
                {
                    Compare-ModuleFingerprint -Initial $Fingerprint -Current $Fingerprint -VersionStep:$false
                } | Should -Throw -Exception ([System.Management.Automation.ParameterBindingException])
            }
            It 'has dynamic parameter MajorChange' {
                {
                    Compare-ModuleFingerprint -Initial $Fingerprint -Current $Fingerprint -VersionStep -MajorChange MandatoryParameterAdded
                } | Should -Not -Throw -Exception ([System.Management.Automation.ParameterBindingException])
            }
            It 'has dynamic parameter MinorChange' {
                {
                    Compare-ModuleFingerprint -Initial $Fingerprint -Current $Fingerprint -VersionStep -MinorChange MandatoryParameterAdded
                } | Should -Not -Throw -Exception ([System.Management.Automation.ParameterBindingException])
            }
            It 'has dynamic parameter PatchChange' {
                {
                    Compare-ModuleFingerprint -Initial $Fingerprint -Current $Fingerprint -VersionStep -PatchChange MandatoryParameterAdded
                } | Should -Not -Throw -Exception ([System.Management.Automation.ParameterBindingException])
            }
        }
    }
    Context 'Output' {
        It 'returns one item' {
            Compare-ModuleFingerprint $Fingerprint $Fingerprint | Should -HaveCount 1
        }
        It 'identifies no changes for the same object' {
            Compare-ModuleFingerprint $Fingerprint $Fingerprint | Should -Be ([PSSharp.ModuleFactory.ModuleFingerprintChange]::None)
        }
        It 'identifies no changes for a copy of the object' {
            Compare-ModuleFingerprint $Fingerprint $FingerprintCopy | Should -Be ([PSSharp.ModuleFactory.ModuleFingerprintChange]::None)
        }
        It 'identifies an added/removed command' {
            $copy = $HashtableForModuleFingerprint.Clone()
            $command = $HashtableForCommandFingerprint.Clone()
            $command['Name'] = 'NewCommandName'
            $copy['Commands'] += $command

            $Result = Compare-ModuleFingerprint $Fingerprint $copy
            $Result.HasFlag([PSSharp.ModuleFactory.ModuleFingerprintChange]::CommandAdded) | Should -BeTrue

            $InverseResult = Compare-ModuleFingerprint $copy $Fingerprint
            $InverseResult.HasFlag([PSSharp.ModuleFactory.ModuleFingerprintChange]::CommandRemoved) | Should -BeTrue
        }
        It 'identifies an added/removed parameter set' {
            $copy = $HashtableForModuleFingerprint.Clone()
            $parameterSet = $HashtableForParameterSetFingerprint.Clone()
            $parameterSet['Name'] = 'SecondParameterSet'
            $copy['Commands'][0]['ParameterSets'] += $parameterSet

            $Result = Compare-ModuleFingerprint $Fingerprint $Copy
            $Result.HasFlag([PSSharp.ModuleFactory.ModuleFingerprintChange]::ParameterSetAdded) | Should -BeTrue

            $InverseResult = Compare-ModuleFingerprint $Copy $Fingerprint
            $InverseResult.HasFlag([PSSharp.ModuleFactory.ModuleFingerprintChange]::ParameterSetRemoved) | Should -BeTrue
        }
        It 'identifies an added/removed parameter' {
            $copy = $HashtableForModuleFingerprint.Clone()
            $parameter = $HashtableForParameterFingerprint.Clone()
            $parameter['Name'] = 'NewParameter'
            $copy['Commands'][0]['ParameterSets'][0]['Parameters'] += $parameter

            $Result = Compare-ModuleFingerprint $Fingerprint $copy
            $Result.HasFlag([PSSharp.ModuleFactory.ModuleFingerprintChange]::NonMandatoryParameterAdded) | Should -BeTrue

            $InverseResult = Compare-ModuleFingerprint $copy $Fingerprint
            $InverseResult.HasFlag([PSSharp.ModuleFactory.ModuleFingerprintChange]::NonMandatoryParameterRemoved) | Should -BeTrue

            $parameter['IsMandatory'] = $true
            $SecondResult = Compare-ModuleFingerprint $Fingerprint $copy
            $SecondResult.HasFlag([PSSharp.ModuleFactory.ModuleFingerprintChange]::MandatoryParameterAdded) | Should -BeTrue

            $InverseSecondResult = Compare-ModuleFingerprint $copy $Fingerprint
            $InverseSecondResult.HasFlag([PSSharp.ModuleFactory.ModuleFingerprintChange]::MandatoryParameterRemoved) | Should -BeTrue
        }
        It 'identifies a modified parameter' {
            $copy = $HashtableForModuleFingerprint.Clone()
            $parameter = $copy['Commands'][0]['ParameterSets'][0]['Parameters'][0]
            $parameter.Position += 1

            $Result = Compare-ModuleFingerprint $Fingerprint $Copy
            $Result | Should -Be ([PSSharp.ModuleFactory.ModuleFingerprintChange]::ParameterPositionChanged)
            $InverseResult = Compare-ModuleFingerprint $Copy $Fingerprint
            $InverseResult | Should -Be ([PSSharp.ModuleFactory.ModuleFingerprintChange]::ParameterPositionChanged)

            $Parameter.Position -= 1
            $Parameter.IsMandatory = !$Parameter.IsMandatory
            $SecondResult = Compare-ModuleFingerprint $Fingerprint $Copy
            $SecondResult | Should -Be ([PSSharp.ModuleFactory.ModuleFingerprintChange]::ParameterBecameMandatory)
            $InverseSecondResult = Compare-ModuleFingerprint $Copy $Fingerprint
            $InverseSecondResult | Should -Be ([PSSharp.ModuleFactory.ModuleFingerprintChange]::ParameterBecameNonMandatory)

            $Parameter.IsMandatory = !$Parameter.IsMandatory
            $Parameter.TypeName = 'pssharp.modulefactory.pseudotype'
            $ThirdResult = Compare-ModuleFingerprint $Fingerprint $Copy
            $ThirdResult | Should -Be ([PSSharp.ModuleFactory.ModuleFingerprintChange]::ParameterTypeChanged)
            $InverseThirdResult = Compare-ModuleFingerprint $Copy $Fingerprint
            $InverseThirdResult | Should -Be ([PSSharp.ModuleFactory.ModuleFingerprintChange]::ParameterTypeChanged)
        }
    }
}