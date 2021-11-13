# PSSharp.ModuleFactory

This is a module desgined for module templating and build/release pipelines. I generally expect to be the only
user of this module.


## Commands

### Build-ModuleProject
Builds a PowerShell module using the configuratoin defined in a project definition file (a .psd1 file that by
convention is named project.psd1).

```PowerShell
PS:\> Build-ModuleProject -Module 'PSSharp.ModuleFactory' -Configuration 'Debug'
```

### New-ModuleProject
Creates a new module project based on a predefined template. This module ships with two templates: PSSharp-BinaryModule
and PSSharp-ScriptModule.

```PowerShell
PS:\> $NewModuleProjectParameters = @{
    Template    = 'PSSharp-ScriptModule'
    Path        = '~\Documents\PowerShell\Develop\Modules\TestScriptModule'
    Name        = 'TestScriptModule'
}
PS:\> New-ModuleProject @NewModuleProjectParameters
```

### Get-ModuleProject
Lists PSModuleProjects that are available to the caller. This function may be passed paths to search for module projects,
otherwise it will check in the current directory for a 'project.psd1' file which it will attempt to parse.

```PowerShell
PS:\> Get-ModuleProject

Name                    BuildConfigurations
---------------------   -------------------
PSSharp.ModuleFactory   { Debug, Release }

PS:\> Get-ModuleProject -Path '~\Documents\PowerShell\Develop\Modules\TestScriptModule\project.psd1'

Name                BuildConfigurations
----------------    -------------------
TestScriptModule    { Default }
```

### Get-ModuleTemplate
Lists the module project templates that are currently installed on the system.

### New-ModuleTemplate
Creates a new module project template from source template files.

### Import-ModuleTemplate
Creates a new module project template from a template that was previously exported.

### Export-ModuleTemplate
Exports a module project template.

## Special stuff
These are some things that I'd like to implement later but will take a lot more effort to get working.

I would like to consider an entire project-based powershell module solution with such commands as the following:
- Get-ModuleProject
    Lists all module projects in the current context or under a provided path.
    A module project is source code that can be compiled into a PowerShell project. This may consist of
    content such as scripts, type and format files, clixml files, or dotnet binary projects.
- Get-PSModuleSolution
    Lists all module solutions in the current context or under a provided path.
    A module solution is a logical collection of module projects.
- Build-ModuleProject
    Builds a module project that may be tested or released.
- Publish-ModuleProject
    Publishes a built version of a module project.

I also like the idea of managing these centrally on a device, for example a global $PSModuleProjectPath to which
a user may register one or more paths that will be included by default in searches for module projects.

The entire system would be managed through PowerShell data files with the ".psd1" extension. For example:
```PowerShell
# File: .\MyProject.psd1
@{
    Name = 'MyProject-ScriptModule'
    BuildConfiguration = @(
        @{
            Configuration = 'Default'
            Files = @(
                    '.\src\PowerShell\MyProject\**\*.psm1'
                    '.\src\PowerShell\MyProject\**\*.ps1'
                )
            Manifest = @{
                ModuleVersion = '1.0.0'
                Author = 'John Deere', 'John Appleseed', 'John Doe', 'John Hancock'
            }
            # ...
        }
    )
}
# File: .\sln.psd1
@{
    # File names to external .psd1 files to load project information from.
    # Name is not constrained to a specific schema, but the file must be a .psd1 file.
    ExternalProjects = @(
        '.\MyProject.psd1'
    )
    Projects = @(
        @{
            Name = 'SecondProject'
            BuildConfiguration = @(
                @{
                    Configuration = 'Debug'
                    NestedProjects = @( @{ ProjectName = 'MyProject-ScriptModule' ; Configuration = 'Default' })
                    # ...
                }
                @{
                    Configuration = 'Release'
                    # ...
                }
            )
        }
    )
}
```



# Projects

## PSSharp.ModuleFactory
[ CSharp | PowerShell ]
PowerShell module for common build tasks such as compiling multiple script files into a single output script module file
(commonly referred to as a "monolith" file), or generating and using module project templates.

### Build Requirements
- None

To build the project, run the .\Build\build.ps1 script file.

## PSSharp.MSBuild
[ CSharp ]
PowerShell API for the MSBuild tool set as exposed through the Microsoft.Build API. Because this module does not wrap the
msbuild executable but uses the underlying framework, these commands are able to produce rich object objects that can be
used in other commands. This module also includes additional commands to work with MSBuild project files and tasks.

### Build Requirements
- None

To build the project, run 'dotnet build' on the '.\src\CSharp\PSSharp.MSBuild' directory. This project is currently a
shell for future content.