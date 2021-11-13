---
external help file: PSSharp.ModuleFactory-help.xml
Module Name: PSSharp.ModuleFactory
online version:
schema: 2.0.0
---

# Get-ModuleFingerprint

## SYNOPSIS
Identifies the "fingerprint" of a PowerShell module, which represents the public API of the module.

## SYNTAX

### DefaultSet (Default)
```
Get-ModuleFingerprint [-Name] <String[]> [<CommonParameters>]
```

### FullyQualifiedNameSet
```
Get-ModuleFingerprint [-FullyQualifiedName] <ModuleSpecification[]> [<CommonParameters>]
```

### ModuleInfoSet
```
Get-ModuleFingerprint [-Module] <PSModuleInfo[]> [<CommonParameters>]
```

## DESCRIPTION
Generates a "fingerprint" of a PowerShell module, which contains data about the exported commands of the module.
This fingerprint can be used elsewhere to identify what changes were made between two versions or builds of a
module, and is generally intended for use in module development to generate a synopsis on changes between
the most recent release and most recent development build versions of a module.

Note that a fingerprint may only be identified for a module that is currently imported in the PowerShell session,
as otherwise the information available about the module (such as parameters of each command) is incomplete.

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-ModuleFingerprint PSSharp.ModuleFactory

# Result omitted
```

Identifies the fingerprint for the PSSharp.ModuleFactory module that is imported in the current PowerShell session.

### Example 1
```powershell
PS C:\> Get-ModuleFingerprint PSSharp.ModuleFactory | Export-ModuleFingerprint -OutputPath ./pssharp.modulefactory.fingerprint
```

Demonstrates retrieving a ModuleFingerprint from a module imported in the current PowerShell session and storing
the value in a file by piping the fingerprint into the Export-ModuleFingerprint command.

## PARAMETERS

### -FullyQualifiedName
A ModuleSpecification instance, aka fully qualified name, of a module for which to generate a fingerprint. The
module must be imported into the PowerShell session or the command will fail because non-imported modules 
(such as those identified with Get-Module -ListAvailable) do not expose enough information to generate a module
fingerprint.

```yaml
Type: ModuleSpecification[]
Parameter Sets: FullyQualifiedNameSet
Aliases: ModuleSpecification

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### -Module
A PowerShell module instance for which to generate a fingerprint. The module must be imported into the PowerShell
session or the command will fail because non-imported modules (such as those identified with Get-Module
-ListAvailable) do not expose enough information to generate a module fingerprint.

```yaml
Type: PSModuleInfo[]
Parameter Sets: ModuleInfoSet
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Name
The name of a module for which to generate a fingerprint. The module must be imported into the PowerShell session
or the command will fail because non-imported modules (such as those identified with Get-Module -ListAvailable)
do not expose enough information to generate a module fingerprint.

```yaml
Type: String[]
Parameter Sets: DefaultSet
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
    The module name may be piped into this function by property name.

### Microsoft.PowerShell.Commands.ModuleSpecification[]
    A module specification may be piped into this function.

### System.Management.Automation.PSModuleInfo[]
    A PSModuleInfo instance may be piped into this function.

## OUTPUTS

### PSSharp.ModuleFactory.ModuleFingerprint
    The "fingerprint" which contains information about the public API of the PowerShell module.

## NOTES

## RELATED LINKS
[Compare-ModuleFingerprint]()
[Export-ModuleFingerprint]()
[Import-ModuleFingerprint]()
