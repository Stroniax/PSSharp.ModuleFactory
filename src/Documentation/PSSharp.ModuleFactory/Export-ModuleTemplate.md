---
external help file: PSSharp.ModuleFactory-help.xml
Module Name: PSSharp.ModuleFactory
online version:
schema: 2.0.0
---

# Export-ModuleTemplate

## SYNOPSIS
Exports a registered module template to a file to allow it to be shared with another device or user.

## SYNTAX

### Name (Default)
```
Export-ModuleTemplate [-Name] <String> -OutputPath <String> [<CommonParameters>]
```

### TemplateId
```
Export-ModuleTemplate -TemplateId <Guid> -OutputPath <String> [<CommonParameters>]
```

## DESCRIPTION
Creates an export file that contains information about a module template stored in the Module Template repository
on the local machine. The exported template may be re-imported later or shared to another device or user to be
imported so that they also may use it.

This command is useful when sharing a module template across multiple workspaces so that the template may be
referenced with the same TemplateId.

## EXAMPLES

### Example 1
```powershell
PS C:\> Export-ModuleTemplate -Name PSSharp-BinaryProject -OutputPath '.\PSSharp-BinaryProject-Template'
```

Creates an export of the PSSharp-BinaryProject module template.

### Example 2
```powershell
PS C:\> Get-ModuleTemplate | Export-ModuleTemplate -OutputPath {$_.Name + '-template'}
```

Exports all module templates to a file with the name of the template proceeded with '-template'.

## PARAMETERS

### -Name
The name of the ModuleTemplate to export.

```yaml
Type: String
Parameter Sets: Name
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -OutputPath
The file path where the template should be exported to.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TemplateId
The unique identifier of the module template to export.

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


### -PassThru
Write the file to the pipeline after exporting the module template.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Guid
    The TemplateId parameter can be filled by a TemplateId member of an input object passed through the pipeline,
    such as a ModuleTemplate instance identified by Get-ModuleTemplate.

## OUTPUTS

### System.IO.FileInfo
    The file to which the template was exported, if the -PassThru parameter is present.

## NOTES

## RELATED LINKS
[Get-ModuleTemplate]()
[Import-ModuleTemplate]()
[New-ModuleProject]()
[New-ModuleTemplate]()