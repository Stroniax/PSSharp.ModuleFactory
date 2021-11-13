---
external help file: PSSharp.ModuleFactory.dll-Help.xml
Module Name: PSSharp.ModuleFactory
online version:
schema: 2.0.0
---

# Compare-ModuleFingerprint

## SYNOPSIS
Compares two ModuleFingerprint objects to identify changes in the API of a module between two versions
or builds.

## SYNTAX

### DefaultSet (Default)
```
Compare-ModuleFingerprint [-Initial] <ModuleFingerprint> [-Current] <ModuleFingerprint> [<CommonParameters>]
```

### VersionStep
```
Compare-ModuleFingerprint [-Initial] <ModuleFingerprint> [-Current] <ModuleFingerprint> [-VersionStep]
[-MajorChange <ModuleFingerprintChange>] [-MinorChange <ModuleFingerprintChange>] [-PatchChange <ModuleFingerprintChange>]
[<CommonParameters>]
```

## DESCRIPTION
Compares two ModuleFingerprint objects to identify changes in the API of a module between two versions
or builds. A module fingerprint can be obtained using the Get-ModuleFingerprint command, or from a previously
stored fingerprint using Import-ModuleFingerprint.

## EXAMPLES

### Example 1
```powershell
PS C:\> $CurrentFingerprint = Get-ModuleFingerprint 'PSSharp.ModuleFactory'
PS C:\> $StoredFingerprint = Import-ModuleFingerprint '.\pssharp.modulefactory.fingerprint'
PS C:\> Compare-ModuleFingerprint -Initial $StoredFingerprint -Current $CurrentFingerprint
NonMandatoryParameterAdded
```

This example demonstrates comparing a module fingerprint from a module imported into the current session with
a fingerprint stored on the current machine. The result indicates that the only difference between the two
versions of the module is that a non-mandatory parameter was added to a command in the new ("current") version.

### Example 2
```powershell
PS C:\> $CurrentFingerprint = Import-Module PSSharp.ModuleFactory -RequiredVersion 0.0.2 -Force -PassThru | Get-ModuleFingerprint
PS C:\> $PreviousFingerprint = Import-Module PSSharp.ModuleFactory -RequiredVersion 0.0.2 -Force -PassThru | Get-ModuleFingerprint
PS C:\> Compare-ModuleFingerprint -Initial $PreviousFingerprint -Current $CurrentFingerprint
CommandAdded
```

This example demonstrates comparing module fingerprints from two modules that are currently loaded into the
session. The result indicates that the only change between the two versions of the module is that one or
more commands were added.

### Example 3
```powershell
PS C:\> $CurrentFingerprint = Get-ModuleFingerprint PSSharp.ModuleFactory
PS C:\> Compare-ModuleFingerprint -Initial '.\pssharp.modulefactory.fingerprint' -Current $CurrentFingerprint
NonMandatoryParameterAdded, NonMandatoryParameterRemoved, ParameterSetAdded
```

This example demonstrates utilizing the transformation on the -Initial parameter available by passing a filepath
to the parameter. The fingerprint that was exported to the provided path is imported and compared to the current
version of the PSSharp.ModuleFactory module in the PowerShell session, and the result indicates that at least one
non-mandatory parameter was added, at least one non-mandatory parameter was removed, and at least one parameter
set was added to a command.

### Example 4
```powershell
PS C:\> Compare-ModuleFingerprint -Initial $InitialFingerprint -Current $CurrentFingerprint -VersionStep
Minor
```

This example demonstrates using the -VersionStep parameter to indicate that instead of returning the changes
between the fingerprints, the cmdlet should identify what portion of a version specification should be
incremented based on the API changes identified between two fingerprints.

### Example 5
```powershell
PS C:\> $CompareModuleFingerprintParameters = @{
    Initial = $InitialFingerprint
    Current = $CurrentFingerprint
    VersionStep = $true
    MajorChange = 'ParameterSetRemoved', 'CommandRemoved'
    MinorChange = 'ParameterSetAdded, CommandAdded, NonMandatoryParameterRemoved, MandatoryParameterRemoved'
    PatchChange = ([Enum]::GetValues([PSSharp.ModuleFactory.Module]))
}
PS C:\> Compare-ModuleFingerprint @CompareModuleFingerprintParameters
Patch
```

This example demonstrates using the -VersionStep parameter and the additional dynamic -MajorChange, -MinorChange,
and -PatchChange parameters. This parameter set indicated by the -VersionStep parameter will identify what part
of a version should be incremented based on the severity of the changes to the module API. By default the -*Change
parameters will be given generally acceptable values to indicate which changes are classified as worthy of
incrementing the major vs minor vs patch portion of a version: this example demonstrates how to override those
values.
When a change type is not included in any of the -*Change parameters, if that change is detected between the
fingerprints it will have no effect on the resulting ModuleVersionStep value. PatchChange is given all
ModuleFingerprintChange values to indicate that any change to the fingerprint not specified in another priority
should be identified as an increment to the patch number of the version.

## PARAMETERS

### -Current
The "current" fingerprint indicating the API of a current version of a module. For example, this may be the
ModuleFingerprint from the latest development build of a module.

This parameter also accepts a file path to a stored module fingerprint, which will be automatically imported
through the parameter binding process.

```yaml
Type: ModuleFingerprint
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Initial
The "initial" fingerprint indicating a snapshot of a module's API which should be compared to. For example,
this may be the ModuleFingerprint from the previous release build of a module.

This parameter also accepts a file path to a stored module fingerprint, which will be automatically imported
through the parameter binding process.

```yaml
Type: ModuleFingerprint
Parameter Sets: (All)
Aliases: Previous

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -VersionStep
Indicates that instead of identifying the changes between the fingerprints, the cmdlet should compare changes
to specifications available through the -MajorChange, -MinorChange, and -PatchChange parameters to determine
the portion of a version to increment due to the identified changes.
This parameter cannot be manually set to $false as it identifies the parameter set and enables the
-MajorChange, -MinorChange, and -PatchChange dynamic parameters.

```yaml
Type: SwitchParameter
Parameter Sets: VersionStep
Aliases:
Accepted values: true

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MajorChange
Enumeration flags indicating which ModuleFingerprintChange types should be counted as worthy of incrementing the
"Major" portion of a version, for example the '1' in '1.X.X.X'.

If a change type is not identified in any of the -*Change parameters, changes of that type between two fingerprint
versions will be ignored.

This is a dynamic parameter which is only available when the -VersionStep switch parameter is set to $true.

```yaml
Type: ModuleFingerprintChange
Parameter Sets: VersionStep
Aliases:
Accepted values: None, CommandAdded, CommandRemoved, ParameterSetNameChanged, MandatoryParameterRemoved, MandatoryParameterAdded, NonMandatoryParameterAdded, NonMandatoryParameterRemoved, ParameterTypeChanged, ParameterPositionChanged, ParameterBecameMandatory, ParameterBecameNonMandatory, ParameterSetAdded, ParameterSetRemoved

Required: True
Position: Named
Default value: MandatoryParameterAdded, MandatoryParameterRemoved, NonMandatoryParameterRemoved, CommandRemoved, ParameterSetRemoved, ParameterBecameMandatory, ParameterTypeChanged, ParameterPositionChanged
Accept pipeline input: False
Accept wildcard characters: False
```

### -MinorChange
Enumeration flags indicating which ModuleFingerprintChange types should be counted as worthy of incrementing the
"Minor" portion of a version, for example the '1' in 'X.1.X.X'.

If a change type is not identified in any of the -*Change parameters, changes of that type between two fingerprint
versions will be ignored.

This is a dynamic parameter which is only available when the -VersionStep switch parameter is set to $true.

```yaml
Type: ModuleFingerprintChange
Parameter Sets: VersionStep
Aliases:
Accepted values: None, CommandAdded, CommandRemoved, ParameterSetNameChanged, MandatoryParameterRemoved, MandatoryParameterAdded, NonMandatoryParameterAdded, NonMandatoryParameterRemoved, ParameterTypeChanged, ParameterPositionChanged, ParameterBecameMandatory, ParameterBecameNonMandatory, ParameterSetAdded, ParameterSetRemoved

Required: True
Position: Named
Default value: NonMandatoryParameterAdded, CommandAdded, ParameterBecameNonMandatory, ParameterSetAdded, ParameterSetNameChanged
Accept pipeline input: False
Accept wildcard characters: False
```

### -MinorChange
Enumeration flags indicating which ModuleFingerprintChange types should be counted as worthy of incrementing the
"Patch" portion of a version, for example the '1' in 'X.X.1.X'.

If a change type is not identified in any of the -*Change parameters, changes of that type between two fingerprint
versions will be ignored.

This is a dynamic parameter which is only available when the -VersionStep switch parameter is set to $true.

```yaml
Type: ModuleFingerprintChange
Parameter Sets: VersionStep
Aliases:
Accepted values: None, CommandAdded, CommandRemoved, ParameterSetNameChanged, MandatoryParameterRemoved, MandatoryParameterAdded, NonMandatoryParameterAdded, NonMandatoryParameterRemoved, ParameterTypeChanged, ParameterPositionChanged, ParameterBecameMandatory, ParameterBecameNonMandatory, ParameterSetAdded, ParameterSetRemoved

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### PSSharp.ModuleFactory.ModuleFingerprintChange
    The changes detected between two ModuleFingerprint instances.

### PSSharp.ModuleFactory.ModuleVersionStep
    A ModuleVersionStep value indicating the portion of a version to increment based on changes
    to the API of the module between the time of the two fingerprints.
## NOTES

## RELATED LINKS
[Export-ModuleFingerprint]()
[Get-ModuleFingerprint]()
[Import-ModuleFingerprint]()