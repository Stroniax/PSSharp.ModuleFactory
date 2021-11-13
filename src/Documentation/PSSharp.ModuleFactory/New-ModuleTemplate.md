---
external help file: PSSharp.ModuleFactory.dll-Help.xml
Module Name: PSSharp.ModuleFactory
online version:
schema: 2.0.0
---

# New-ModuleTemplate

## SYNOPSIS
Creates a new PowerShell module template and registers it for the current user.

## SYNTAX

### Path (Default)
```
New-ModuleTemplate [-Path] <String> [-WhatIf] [-Confirm] [<CommonParameters>]
```

### LiteralPath
```
New-ModuleTemplate -LiteralPath <String> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Creates a new PowerShell module template and registers it for the current user. A module template must have a file
named 'template.psd1' in the root, which must be able to be cast to
[PSSharp.ModuleFactory.ConfigurationModuleTemplate] which is used to identify the execution operations of the
template.

View the members of the [PSSharp.ModuleFactory.ConfigurationModuleTemplate] type to see what is available to
templates.

## EXAMPLES

### Example 1
```powershell
PS C:\> New-ModuleTemplate -Path ./MyTemplate

# Result omitted.
```

Creates a PowerShell module template using the contents of the ./MyTemplate directory. A file must exist at the
path './MyTemplate/Template.psd1' for this operation to succeed. The template may then be exported or used to
create one or more module projects using the New-ModuleProject cmdlet.

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

### -LiteralPath
Path to the directory containing the template contents. The -LiteralPath parameter treats all paths exactly as
provided and does not expand wildcards.

```yaml
Type: String
Parameter Sets: LiteralPath
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Path
Path to the directory containing the template contents. The -Path parameter does support wildcards.

```yaml
Type: String
Parameter Sets: Path
Aliases:

Required: True
Position: 0
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

### System.String
    The -LiteralPath parameter accepts pipeline input by property name.

## OUTPUTS

### PSSharp.ModuleFactory.ModuleTemplateMetadata
    An object that contains information about the newly created module template such as the name, version, and
    template id.

## NOTES

## RELATED LINKS
[Get-ModuleTemplate]()
[Import-ModuleTemplate]()
[New-ModuleProject]()