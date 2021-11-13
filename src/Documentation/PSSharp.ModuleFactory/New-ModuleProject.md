---
external help file: PSSharp.ModuleFactory.dll-Help.xml
Module Name: PSSharp.ModuleFactory
online version:
schema: 2.0.0
---

# New-ModuleProject

## SYNOPSIS
Creates the structure for a new PowerShell module development project based on a template.

## SYNTAX

### TemplateName (Default)
```
New-ModuleProject [-TemplateName] <String> [-TemplateVersion <Version>] [-OutputPath] <String> [-WhatIf]
 [-Confirm] [<CommonParameters>]
```

### TemplateId
```
New-ModuleProject -TemplateId <Guid> [-OutputPath] <String> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Generates files and directories to build a project for developing a PowerShell module based on a module template
available to the current user. To create a new module template, see [New-ModuleTemplate](). To import a template
from another source, see [Import-ModuleTemplate]().

Templates are based on a PowerShell data file ('.psd1') but may contain one or more PowerShell scripts that can
be run when the module project is build. Make sure you trust the author of a template before using it.

Each template may define dynamic parameters which are passed to the template to help pre-build parts of the
module project. See documentation about the template for more information.

## EXAMPLES

### Example 1
```powershell
PS C:\> New-ModuleProject -TemplateName PSSharp-BinaryModule -ModuleName TestFromTemplate -OutputPath ./TestFromTemplate
```

Demonstrates using a template to generate a new PowerShell module project.

### Example 2
```powershell
PS C:\> Get-ModuleTemplate *Binary* | New-ModuleProject -ModuleName TestFromTemplate -OutputPath ./TestFromTemplate
```

Demonstrates using a template that is piped into this command to generate a new PowerShell module project.

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

### -OutputPath
The path at which the template will be expanded to create a module project. All files from the template will go
directly in this or a child directory.

```yaml
Type: String
Parameter Sets: (All)
Aliases: DestinationPath

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TemplateId
The template id of the template to build a project based on.

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

### -TemplateName
The name of the template to build a project based on.

```yaml
Type: String
Parameter Sets: TemplateName
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TemplateVersion
The version of the template to build a project based on. This parameter is for use in conjunction with the
-TemplateName parameter when more than one template is available with the same name. If not provided, the
latest version will be used.

```yaml
Type: Version
Parameter Sets: TemplateName
Aliases:

Required: False
Position: Named
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

### System.Guid
    The -TemplateId parameter accepts pipeline input by property name.

## OUTPUTS

### System.Management.Automation.Internal.AutomationNull
    This command has no output.
## NOTES

## RELATED LINKS
[Get-ModuleTemplate]()
[Import-ModuleTemplate]()