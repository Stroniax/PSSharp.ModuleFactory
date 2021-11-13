@{
    'PSSharp.ModuleFactory' = @{
        'UseIncrementalVersioning' = $false
        'Manifest' = @{
            'RequiredAssemblies' = 'PSSharp.ModuleFactory.dll'
            'RootModule' = 'PSSharp.ModuleFactory.psm1'
            'Author' = 'Caleb Frederickson'
            'Company' = 'PSSharp'
            'Copyright' = 'Copyright 2021 Caleb Frederickson'
            'Description' = 'Build and template tools for PowerShell module development.'
            'CompatiblePSEditions' = 'Core'
            'PowerShellVersion' = '7.2.0'
            # Manually defined to avoid importing template files
            'NestedModules' = @()
            'CmdletsToExport' = @(
                'Build-ModuleProject'
                'Build-ScriptModule'
                'New-ModuleProject'
                'Get-ModuleProject'
                'Get-ModuleTemplate'
                'New-ModuleTemplate'
                'Remove-ModuleTemplate'
                'Import-ModuleFingerprint'
                'Export-ModuleFingerprint'
                'Compare-ModuleFingerprint'
            )
            'TypesToProcess' = 'PSSharp.ModuleFactory.types.ps1xml'
            'FormatsToProcess' = 'PSSharp.ModuleFactory.format.ps1xml'
        }
    }
}