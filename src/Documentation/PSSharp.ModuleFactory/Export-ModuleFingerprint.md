---
external help file: PSSharp.ModuleFactory.dll-Help.xml
Module Name: PSSharp.ModuleFactory
online version:
schema: 2.0.0
---

# Export-ModuleFingerprint

## SYNOPSIS
Exports a ModuleFingerprint to a file.

## SYNTAX

```
Export-ModuleFingerprint [-Fingerprint] <ModuleFingerprint> [-Path] <String> [-Force] [-PassThru] [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Exports a serialized version of a ModuleFingerprint which can be reimported later. The ModuleFingerprint
represents the API of a module at a current point in time and may be used to compare with a later version
of the module to identify changes made to the public commands of the module.

The module fingerprint is serialized as JSON but uses logic that prevents it from being properly serialized
and deserialized with the Convert[To|From]-Json commands.

## EXAMPLES

### Example 1
```powershell
PS C:\> $Fingerprint = Get-ModuleFingerprint PSSharp.ModuleFactory
PS C:\> $Path = './pssharp.modulefactory.fingerprint'
PS C:\> Export-ModuleFingerprint -Fingerprint $Fingerprint -Path $Path -PassThru

    Directory: C:\Users\myself

Mode                 LastWriteTime         Length Name
----                 -------------         ------ ----
-a---          11/12/2021  9:28 PM           7005 psharp.modulefactory.fingerprint
```

Demonstrates retrieving a ModuleFingerprint from a module imported in the current PowerShell session and storing
the value in a file.

### Example 1
```powershell
PS C:\> Get-ModuleFingerprint PSSharp.ModuleFactory | Export-ModuleFingerprint -Path '.\pssharp.modulefactory.fingerprint'
```

Demonstrates retrieving a ModuleFingerprint from a module imported in the current PowerShell session and storing
the value in a file by piping the fingerprint into this command.

## PARAMETERS

### -Confirm
Prompts you for confirmation before running the cmdlet.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Fingerprint
A ModuleFingerprint instance to be exported to the file.

```yaml
Type: ModuleFingerprint
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Force
Overwrite the file if it already exists.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Write the file to the pipeline after exporting the module fingerprint.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
The path to which the fingerprint should be exported.

```yaml
Type: String
Parameter Sets: (All)
Aliases: FilePath

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf
Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### PSSharp.ModuleFactory.ModuleFingerprint
    A ModuleFingerprint instance obtained using Import-ModuleFingerprint or Get-ModuleFingerprint, which
    represents the public API of a PowerShell module.

## OUTPUTS

### System.IO.FileInfo
    The file to which the fingerprint was exported, if the -PassThru parameter is present.

## NOTES

## RELATED LINKS
[Compare-ModuleFingerprint]()
[Get-ModuleFingerprint]()
[Import-ModuleFingerprint]()
