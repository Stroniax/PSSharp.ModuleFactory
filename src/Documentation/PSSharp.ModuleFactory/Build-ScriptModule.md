---
external help file: PSSharp.ModuleFactory.dll-Help.xml
Module Name: PSSharp.ModuleFactory
online version:
schema: 2.0.0
---

# Build-ScriptModule

## SYNOPSIS
Compiles a single script module (.psm1) file from one or more source script or script module (.ps1, psm1) files.

## SYNTAX

### Path (Default)
```
Build-ScriptModule [-Path] <String[]> [-OutputPath] <String> [-FunctionSourceTrace <SourceFileTraceLevel>]
 [-ClassSourceTrace <SourceFileTraceLevel>] [-StatementSourceTrace <SourceFileTraceLevel>] [-Force]
 [-NoClobber] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### LiteralPath
```
Build-ScriptModule -LiteralPath <String[]> [-OutputPath] <String> [-FunctionSourceTrace <SourceFileTraceLevel>]
 [-ClassSourceTrace <SourceFileTraceLevel>] [-StatementSourceTrace <SourceFileTraceLevel>] [-Force]
 [-NoClobber] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Generates a full .psm1 script module file with from one or more source files.

If the output file was modified later than the last modify date of all source files, by default no action will be
run (though a result object based on the existing script module file will be generated).

This command returns an object that contains information about functions, classes, and script requirements defined
in the source files.

## EXAMPLES

### Example 1
```powershell
PS C:\> Build-ScriptModule -Path .\source.ps1 -OutputPath .\MyModule.psm1
```

Generates a module from a single source file. This file will generally be the same as the input file.

### Example 2
```powershell
PS C:\> Build-ScriptModule -Path .\source.ps1, .\classes.ps1 -OutputPath .\MyModule.psm1
```

Combines two source files into a single script module at path '.\MyModule.psm1'.

### Example 3
```powershell
PS C:\> Get-ChildItem -Path .\pssrc -Recurse -Include '*.ps1', '*.psm1' | Build-ScriptModule -OutputPath .\MyModule.psm1
```

Identifies all script and module files in the '.\pssrc' directory and uses them to generate a script module at
'.\MyModule.psm1'.

### Example 4
```powershell
PS C:\> Build-ScriptModule -Path .\*.ps1 -OutputPath .\MyModule.psm1 -FunctionSourceTrace FilePathLineNumber
```

Identifies all script files in the current directory and uses them to generate a script module at '.\MyModule.psm1'.
Comments will be included before each function definition that indicate the full file path and line number of the source
file for the function.

## PARAMETERS

### -ClassSourceTrace
Determines the comments included before each class definition in the output file that indicate the source file and/or line
that the class was initially defined in.

```yaml
Type: SourceFileTraceLevel
Parameter Sets: (All)
Aliases:
Accepted values: None, FileName, FilePath, FileNameLineNumber, FilePathLineNumber

Required: False
Position: Named
Default value: FileNameLineNumber
Accept pipeline input: False
Accept wildcard characters: False
```

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

### -Force
Overwrite the existing file even if source files were not modified later than the current destination file's last
modified timestamp.

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

### -FunctionSourceTrace
Determines the comments included before each function definition in the output file that indicate the source file and/or line
that the class was initially defined in.

```yaml
Type: SourceFileTraceLevel
Parameter Sets: (All)
Aliases:
Accepted values: None, FileName, FilePath, FileNameLineNumber, FilePathLineNumber

Required: False
Position: Named
Default value: FileNameLineNumber
Accept pipeline input: False
Accept wildcard characters: False
```

### -LiteralPath
Path to one or more source files from which the module should be built. The -LiteralPath parameter treats all
paths exactly as provided and does not expand wildcards.

```yaml
Type: String[]
Parameter Sets: LiteralPath
Aliases: PSPath

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -NoClobber
Indicates that the command should fail with an error if a file exists at the output path. Default behavior is to
update the destination file if source files were modified after the last write time on the destination file.

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

### -OutputPath
The path that the resultant script module will be created at.

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

### -Path
Path to one or more source files from which the module should be built. The -Path parameter does support wildcards.

```yaml
Type: String[]
Parameter Sets: Path
Aliases: FilePath

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### -StatementSourceTrace
Determines the comments included before each statement (not a function or type definition) in the output file that
indicate the source file and/or line that the class was initially defined in. Statements include lines such as
variable assignments or function calls outside of the function itself.

```yaml
Type: SourceFileTraceLevel
Parameter Sets: (All)
Aliases:
Accepted values: None, FileName, FilePath, FileNameLineNumber, FilePathLineNumber

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

### System.String[]
    The -LiteralPath parameter accepts pipeline input by property name.

## OUTPUTS

### PSSharp.ModuleFactory.Commands.BuildScriptModuleCommand+BuildScriptModuleResult

An object representing the functions, aliases, and script requirements of the source files.

## NOTES

## RELATED LINKS