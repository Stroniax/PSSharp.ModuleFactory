---
external help file: PSSharp.ModuleFactory.dll-Help.xml
Module Name: PSSharp.ModuleFactory
online version:
schema: 2.0.0
---

# Remove-ModuleTemplate

## SYNOPSIS
Removes a PowerShell module template from the template repository for the current user.

## SYNTAX

### Name (Default)
```
Remove-ModuleTemplate [-Name] <String> [[-Version] <Version>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### TemplateId
```
Remove-ModuleTemplate -TemplateId <Guid> [-WhatIf] [-Confirm] [<CommonParameters>]
```

### ModuleTemplate
```
Remove-ModuleTemplate [-ModuleTemplate] <ModuleTemplateMetadata[]> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Removes a PowerShell module template from the template repository for the current user. If no templates exist on
for the user, default template(s) included with this module will be automatically re-imported into the template
repository.

## EXAMPLES

### Example 1
```powershell
PS C:\> Remove-ModuleTemplate PSSharp-BinaryModule
```

This example demonstrates removing the PSSharp-BinaryModule template. When removed, the template is no
longer available and cannot be used to create a new module project, and will not interfere with importing
or creating a new template with the same name and version.

### Example 2
```powershell
PS C:\> Remove-ModuleTemplate PSSharp*
```

This example demonstrates removing all module templates where the name begins with the text 'PSSharp', which
by default will include all templates initially generated when importing the PSSharp.ModuleFactory module.

### Example 3
```powershell
PS C:\> Get-ModuleTemplate | Remove-ModuleTemplate
PS C:\> Import-Module PSSharp.ModuleFactory -Force
```

This example demonstrates removing all module templates and re-importing the PSSharp.ModuleFactory module, which
will reinitialize the template repository with only the default module template(s).

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

### -ModuleTemplate
The module template to be removed. A value for this parameter can be obtained using the Get-ModuleTemplate cmdlet.

```yaml
Type: ModuleTemplateMetadata[]
Parameter Sets: ModuleTemplate
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Name
The name of the module template to be removed.

```yaml
Type: String
Parameter Sets: Name
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -TemplateId
The template  id of the module template to be removed.

```yaml
Type: Guid
Parameter Sets: TemplateId
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Version
The version of the template to remove. This parameter is for use in conjunction with the -Name parameter when more
than one template is available with the same name. If not provided, all templates with the name provided will be
removed.

```yaml
Type: Version
Parameter Sets: Name
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName)
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
    The -Name parameter accepts pipeline input by property name.
### System.Version
    The -Version parameter accepts pipeline input by property name.

### System.Guid
    The -TemplateId parameter accepts pipeline input by property name.

### PSSharp.ModuleFactory.ModuleTemplateMetadata[]
    The -TemplateId parameter accepts pipeline input.

## OUTPUTS

### System.Management.Automation.Internal.AutomationNull
    This command has no output.
## NOTES

## RELATED LINKS
[Get-ModuleTemplate]()
[New-ModuleTemplate]()
[Import-ModuleTemplate]()