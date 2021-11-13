@{
    'BuildConfigurations' = @(
        @{
            'ModuleName' = 'PSSharp.ModuleFactory'
            'Configuration' = 'Debug'
            'DotNetProjects' = @( 'src/CSharp/PSSharp.ModuleFactory')
            'ScriptProjects' = @( 'src/PowerShell/PSSharp.ModuleFactory' )
            'TypeFiles' = @( 'src/PowerShell/PSSharp.ModuleFactory/*.types.ps1xml' )
            'FormatFiles' = @( 'src/PowerShell/PSSharp.ModuleFactory/*.format.ps1xml' )
            'Files' = @(
                @{ 'Path' = 'src/PowerShell/PSSharp.ModuleFactory' ; 'Recurse' = $true; 'Exclude' = '*.psm1', '*.ps1' }
            )
            'OutputDirectory' = 'Build' #/PSSharp.ModuleFactory/{Version}
        }
    )
}