---
external help file: PSSharp.ModuleFactory.dll-Help.xml
Module Name: PSSharp.ModuleFactory
online version:
schema: 2.0.0
---

# Import-ModuleFingerprint

## SYNOPSIS
Imports a module fingerprint from a file which it was previously exported to.

## SYNTAX

### Path (Default)
```
Import-ModuleFingerprint [-Path] <String> [<CommonParameters>]
```

### LiteralPath
```
Import-ModuleFingerprint -LiteralPath <String> [<CommonParameters>]
```

## DESCRIPTION
Retrieves a JSON-serialized module fingerprint instance from a file it was previously exported to using the
Export-ModuleFingerprint cmdlet. This cmdlet is used to properly generate the module fingerprint from serialized
json as the default Convert[From|To]-Json commands are not entirely compatible with the ModuleFingerprint type.

## EXAMPLES

### Example 1
```powershell
PS C:\> Import-ModuleFingerprint -Path ./pssharp.modulefactory.fingerprint

# Result omitted
```

Demonstrates loading a ModuleFingerprint from a file it was previously exported to.

### Example 2
```powershell
PS C:\> $Fingerprint = Get-ModuleFingerprint PSSharp.ModuleFactory
PS C:\> $Copy = $Fingerprint | Export-ModuleFingerprint -Path ./pssharp.modulefactory.fingerprint -PassThru | Import-ModuleFingerprint
PS C:\> $Fingerprint -eq $Copy

# Result: $true
```

This example demonstrates identifying and exporting a module fingerprint, and then reimporting it and comparing
the fingerprint to indicate that the serialized and initial values are considered equal.

## PARAMETERS

### -LiteralPath
Path to the exported module fingerprint. The -LiteralPath parameter treats all paths exactly as input and does
not expand wildcards.

```yaml
Type: String
Parameter Sets: LiteralPath
Aliases: PSPath

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
Path to the exported module fingerprint. The -Path parameter does support wildcards.

```yaml
Type: String
Parameter Sets: Path
Aliases: FilePath

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String
    The -LiteralPath parameter accepts pipeline input by property name.

## OUTPUTS

### PSSharp.ModuleFactory.ModuleFingerprint
    The deserialized "fingerprint" which contains information about the public API of the PowerShell module.
## NOTES

## RELATED LINKS
[Compare-ModuleFingerprint]()
[Export-ModuleFingerprint]()
[Get-ModuleFingerprint]()