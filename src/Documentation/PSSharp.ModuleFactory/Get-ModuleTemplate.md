---
external help file: PSSharp.ModuleFactory.dll-Help.xml
Module Name: PSSharp.ModuleFactory
online version:
schema: 2.0.0
---

# Get-ModuleTemplate

## SYNOPSIS
Lists the PowerShell module templates available to the current user.

## SYNTAX

### Name (Default)
```
Get-ModuleTemplate [[-Name] <String>] [<CommonParameters>]
```

### TemplateId
```
Get-ModuleTemplate -TemplateId <Guid> [<CommonParameters>]
```

## DESCRIPTION
Lists the PowerShell module templates available to the current user. Each template contains data to create a
module project for developing a PowerShell module. The template can be constructed using the
New-ModuleProject cmdlet.

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-ModuleTemplate
```

Lists all module templates available to the current user. By default, the PSSharp.ModuleFactory module comes with
one module tempalte for developing a binary module.

### Example 2
```powershell
PS C:\> Get-ModuleTemplate PSSharp*
```

Lists all module templates available to the current user with a name that begins with 'PSSharp'.

### Example 3
```powershell
PS C:\> Get-ModuleTemplate PSSharp-BinaryModule | New-ModuleProject -ModuleName TestFromTemplate -OutputPath ./TestFromTemplate
```

Identifies the PSSharp-BinaryModule template and creates a new module project using the template at the path './TestFromTemplate'.

## PARAMETERS

### -Name
The name of the template to retrieve, or a wildcard expression to match templates to list.

```yaml
Type: String
Parameter Sets: Name
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: True
```

### -TemplateId
The unique TemplateId of the template to retrieve.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String
    The -Name parameter accepts pipeline input by property name.

### System.Guid
    The -TemplateId parameter accepts pipeline input by property name.

## OUTPUTS

### PSSharp.ModuleFactory.ModuleTemplateMetadata
    An object that contains information about the module template such as the name, version, and template id.

## NOTES

## RELATED LINKS
[Import-ModuleTemplate]()
[New-ModuleProject]()
[New-ModuleTempalte]()