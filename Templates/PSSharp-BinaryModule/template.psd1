@{
    'Name'        = 'PSSharp-BinaryModule'
    'Version'     = '1.0.0'
    'Description' = '(Default) PowerShell binary module project template.'

    'DynamicParameters'  = @(
        @{
            'Name'       = 'ModuleName'
            'TypeName'   = 'System.String'
            'Attributes' = @(
                @{
                    'TypeName' = 'System.Management.Automation.ParameterAttribute'
                    'Properties' = @{ 'Mandatory' = $true }
                }
            )
        }
    )
    'Files'       = @(
        # Advanced file definition schema; provide this hashtable or a string
        # @{ Name = ''; Directory = ''; Template = ''; InterpolateVariables = $true; Content = $null }

        # Files in the root directory
        '.gitignore'

        # VS Code project files
        '.vscode/launch.json'
        '.vscode/tasks.json'

        # Build/release files
        'Build/build.psd1'
        'Build/build.ps1'

        # PowerShell module project files
        'src/PowerShell/{{ModuleName}}.types.ps1xml'
        'src/PowerShell/{{ModuleName}}.format.ps1xml'

        # Pester tests
        'Tests/{{ModuleName}}.Tests.ps1'
    )
    'BeforeExecute' = {
            dotnet new classlib --name $ModuleName --output "./src/$ModuleName"
            dotnet new xunit --name "$ModuleName.Tests" --output "./src/$ModuleName.Tests"
            dotnet new sln --name $ModuleName
            dotnet sln add "./src/$ModuleName"
            dotnet sln add "./src/$ModuleName.Tests"
            dotnet add "./src/$ModuleName.Tests" reference "./src/$ModuleName"
            dotnet add "./src/$ModuleName" package PowerShellStandard.Library --framework netstandard2.0
            dotnet add "./src/$ModuleName" package System.Management.Automation

            [xml]$csproj = Get-Content "./src/$ModuleName/$ModuleName.csproj"
            $csproj.project.itemgroup.PackageReference
            | Where-Object Include -eq 'System.Management.Automation'
            | ForEach-Object { $_.SetAttribute('PrivateAssets', 'all') ; $_.SetAttribute("Condition", '''$(TargetFramework)'' != ''netstandard2.0''') }
            $csproj.project.itemgroup.PackageReference
            | Where-Object Include -eq 'PowerShellStandard.Library'
            | ForEach-Object { $_.SetAttribute('PrivateAssets', 'all') }
            $csproj.Save(($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath("./src/$ModuleName/$ModuleName.csproj")))

            New-Item -ItemType Directory -Path "./src/PowerShell/"
            New-Item -ItemType Directory -Path "./src/PowerShell/"
            New-Item -ItemType Directory -Path "./src/PowerShell/Public"
            New-Item -ItemType Directory -Path "./src/PowerShell/Private"
            New-Item -ItemType Directory -Path "./src/PowerShell/Classes"
            New-Item -ItemType Directory -Path "./src/Documentation"
            if (Get-Module -ListAvailable -Name PlatyPS) {
                New-MarkdownAboutHelp -AboutName $ModuleName -OutputFolder './src/Documentation'
            }
        }
    'AfterExecute' = @(
    )
}