---
external help file: PSSharp.ModuleFactory-help.xml
Module Name: PSSharp.ModuleFactory
online version:
schema: 2.0.0
---

# Import-ModuleTemplate

## SYNOPSIS
Imports a pre-build module template from a file.

## SYNTAX

### Path (Default)
```
Import-ModuleTemplate [-Path] <String> [<CommonParameters>]
```

### LiteralPath
```
Import-ModuleTemplate -LiteralPath <String> [<CommonParameters>]
```

## DESCRIPTION
Imports a preexisting module template that was previously generated using the Export-ModuleTemplate command.
The module template will be registered for the current user and can be used to generate a PowerShell module
project for module development.

A PowerShell module template cannot be imported if a template already exists with the same name and version,
or with the same template id.

## EXAMPLES

### Example 1
```powershell
PS C:\> Import-ModuleTemplate -Path '.\PSSharp-BinaryProject-Template'

# Result omitted
```

Imports a module template from the provided path where a template was previously exported to.

## PARAMETERS

### -LiteralPath
Path to the module template file to add to the user's available templates. The -LiteralPath parameter treats all
paths exactly as provided and does not expand wildcards.

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
Path to the module template file to add to the user's available templates. The -Path parameter does support
wildcards.

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

### PSSharp.ModuleFactory.ModuleTemplateMetadata
    An object that contains information about the newly imported module template such as the name, version, and
    template id.

## NOTES

## RELATED LINKS
[Export-ModuleTemplate]()
[Get-ModuleTemplate]()
[New-ModuleProject]()
[New-ModuleTemplate]()