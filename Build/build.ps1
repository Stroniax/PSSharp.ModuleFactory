using namespace System;
using namespace System.IO;
using namespace System.Text;
using namespace System.Management.Automation;
using namespace System.Collections.Generic;

[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingPositionalParameters', '')]
param(
    [Parameter()]
    [string]
    $ModuleName = 'PSSharp.ModuleFactory',

    [Parameter()]
    [Version]
    $Version = '0.0.2'
)

$BuildConfig = Import-PowerShellDataFile -Path (Join-Path $PSScriptRoot 'build-config.psd1')
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$CSharpSource = Join-Path $ProjectRoot 'src' 'CSharp' $ModuleName
$PowerShellSource = Join-Path $ProjectRoot 'src' 'PowerShell' $ModuleName
$DocumentationSource = Join-Path $ProjectRoot 'src' 'Documentation' $ModuleName

# First we'll check for changes to see if we need to re-build the binary project(s)
$Rebuild = @{
    'CSharp'        = $true
    'PowerShell'    = $true
    'Documentation' = $true
}

# If nothing needs rebuilt, we'll return here


if ($BuildConfig[$ModuleName]['UseIncrementalVersioning'] -and
    !$PSBoundParameters.ContainsKey('Version')) {
    $VersionPath = Join-Path $PSScriptRoot '.version'
    if (Test-Path $VersionPath) {
        [version]$FormerVersion = Get-Content -Path $VersionPath
        $Version = [version]::new($FormerVersion.Major, $FormerVersion.Minor, $FormerVersion.Build, $FormerVersion.Revision + 1)
        Set-Content -Path $VersionPath -Value $Version.ToString()
    }
    else {
        $File = New-Item -ItemType File -Path $VersionPath -Value $Version.ToString()
        $File.Attributes = $File.Attributes -bor [FileAttributes]::Hidden
    }
}

$Output = Join-Path $PSScriptRoot $ModuleName $Version
if (Test-Path $Output) {
    Remove-Item $Output -Force -Recurse
}
New-Item $Output -ItemType Directory

if ($Rebuild['CSharp']) {
    if ($BuildConfig[$ModuleName]['Manifest']['CompatiblePSEditions'].Count -gt 1) {
        dotnet publish "$CSharpSource" --output "$Output\Core" --framework net6.0 --configuration release /p:DebugType=None /p:Version="$Version" /p:AssemblyVersion="$Version" /p:AssemblyFileVersion="$Version"
        dotnet publish "$CSharpSource" --output "$Output\Desktop" --framework netstandard2.0 --configuration release /p:DebugType=None /p:Version="$Version" /p:AssemblyVersion="$Version" /p:AssemblyFileVersion="$Version"
    }
    elseif ($BuildConfig[$ModuleName]['Manifest']['CompatiblePSEditions'] -eq 'Core') {
        dotnet publish "$CSharpSource" --output "$Output" --framework net6.0 --configuration release /p:DebugType=None /p:Version="$Version" /p:AssemblyVersion="$Version" /p:AssemblyFileVersion="$Version"
    }
    else {
        dotnet publish "$CSharpSource" --output "$Output" --framework netstandard2.0 --configuration release /p:DebugType=None /p:Version="$Version" /p:AssemblyVersion="$Version" /p:AssemblyFileVersion="$Version"
    }
}

if ($Rebuild['PowerShell']) {
    # Script Files (.psm1 & .ps1 functions & classes files)
    $PublicFunctions = Get-ChildItem -Path (Join-Path $ProjectRoot 'src' 'PowerShell' $ModuleName 'Public') -Include '*.psm1', '*.ps1' -Recurse -ErrorAction Ignore
    $PrivateFunctions = Get-ChildItem -Path (Join-Path $ProjectRoot 'src' 'PowerShell' $ModuleName 'Private') -Include '*.psm1', '*.ps1' -Recurse -ErrorAction Ignore
    $PSClassDefinitions = Get-ChildItem -Path (Join-Path $ProjectRoot 'src' 'PowerShell' $ModuleName 'Class') -Include '*.psm1', '*.ps1' -Recurse -ErrorAction Ignore

    $UsingNamespaces = [HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $TempFile = New-TemporaryFile
    @($PSClassDefinitions; $PrivateFunctions; $PublicFunctions)
    | Get-Content
    | ForEach-Object {
        if ($_ -like 'using namespace*') {
            [void]$UsingNamespaces.Add($_)
        }
        else {
            $_
        }
    }
    | Out-File $TempFile -Append

    $psm1 = Join-Path $PowerShellSource "$ModuleName.psm1"
    if (Test-Path $psm1) {
        Get-Content $psm1
        | ForEach-Object {
            if ($_ -like 'using namespace*') {
                [void]$UsingNamespaces.Add($_)
            }
            else {
                $_
            }
        }
        | Out-File $TempFile -Append
    }

    $fromStream = [FileStream]::new($TempFile.FullName, 'Open', 'Read')
    $newPsm1 = Join-Path $Output "$ModuleName.psm1"
    $toStream = [FileStream]::new($newPsm1, 'Create', 'ReadWrite')

    $UsingNamespaceBytes = [Encoding]::Default.GetBytes(($UsingNamespaces -join "`n`r"))
    $toStream.Write($UsingNamespaceBytes, 0, $UsingNamespaceBytes.Length)
    $fromStream.CopyTo($toStream)

    $toStream.Flush()

    $fromStream.Dispose()
    $toStream.Dispose()
    Remove-Item -Path $TempFile.FullName
}

[string[]]$FunctionsToExport = $PublicFunctions.BaseName

# All other files in the PowerShell source directory
Get-ChildItem -Path $PowerShellSource
| Where-Object { $_.Name -notin @('Public', 'Private', 'Class', "$ModuleName.psm1") }
| Copy-Item -Destination $Output -Recurse

# Documentation
New-ExternalHelp -Path $DocumentationSource -OutputPath $Output

# Using a job to run this in a separate process. This way I'm still able to pass variables to the session.
$UpdateManifestScript = {
    param(
        [Parameter(Mandatory)]
        [Version]$Version,

        [Parameter()]
        [string[]]
        $FunctionsToExport,

        [hashtable]
        $Manifest
    )

    $Manifest['ModuleVersion'] = $Version
    # The manifest will not exist yet. Attempting to import the module will import an old version.
    New-ModuleManifest -Path "$using:PSScriptRoot/$using:ModuleName/$Version/$using:ModuleName.psd1" @Manifest
    $ModuleToUpdate = Import-Module "$using:PSScriptRoot/$using:ModuleName" -PassThru
    [string[]]$CmdletsToExport = $ModuleToUpdate.ExportedCmdlets.Keys

    $AliasesToExport = $FunctionsToExport | ForEach-Object { Get-Alias -Definition $_ -ErrorAction Ignore }

    $Files = Get-ChildItem $using:Output -Recurse -File | ForEach-Object { $_.FullName.Replace($using:Output, '', [StringComparison]::OrdinalIgnoreCase).Trim('/\') }
    $NestedModules = $Files | Where-Object { $_.EndsWith('.psm1', [StringComparison]::OrdinalIgnoreCase) }

    $Manifest['Path'] = Join-Path $using:Output "$using:ModuleName.psd1"
    $Manifest['FileList'] = $Files
    $Manifest['CmdletsToExport'] ??= $CmdletsToExport
    $Manifest['FunctionsToExport'] ??= $FunctionsToExport
    $Manifest['AliasesToExport'] ??= $AliasesToExport
    $Manifest['NestedModules'] ??= $NestedModules

    foreach ($key in [string[]]$Manifest.Keys) {
        if (!$Manifest[$key]) { [void]$Manifest.Remove($Key) }
    }

    Update-ModuleManifest @Manifest
}
$StartJobParameters = @{
    Command      = $UpdateManifestScript
    ArgumentList = @(
        $Version,
        $FunctionsToExport,
        $BuildConfig[$ModuleName]['Manifest']
    )
}
Start-Job @StartJobParameters | Receive-Job -Wait -AutoRemoveJob



### IDEAL BUILD SCRIPT
return

<#
[CmdletBinding(DefaultParameterSetName = 'IncrementBuild')]
param(
    [Parameter(Mandatory)]
    [string]
    $ModuleName,

    [Parameter(Mandatory, ParameterSetName = 'Version')]
    [Version]
    $Version,

    [Parameter(Mandatory, ParameterSetName = 'IncrementBuild')]
    [ValidateSet('Major', 'Minor', 'Build', 'Revision')]
    [string]
    $IncrementVersionStep,

    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]
    $Configuration,

    [Parameter()]
    [string]
    $OutputPath
    )
#>

# Configure env/vars
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$BuildConfiguration = Import-PowerShellDataFile (Join-Path $ProjectRoot 'Build' 'BuildConfiguration.psd1')
$ModuleManifestParameters = $BuildConfiguration[$ModuleName]['Manifest']
$BuildMetadata = Import-Clixml (Join-Path $ProjectRoot 'Build' 'metadata.json')
if ($Version) {
    if ($Version -lt $BuildMetadata.$ModuleName.$Version) {
        Write-Warning 'Detected module version downgrade. Continue with build?' -WarningAction Inquire
    }
    $BuildMetadata.$ModuleName.Version = $Version
}
else {
    if ($IncrementVersionStep -eq 'Major') {
        $Version = [Version]::new(
            $BuildMetadata.$ModuleName.Version.Major + 1,
            0,
            0,
            0
        );
    }
    elseif ($IncrementVersionStep -eq 'Minor') {
        $Version = [Version]::new(
            $BuildMetadata.$ModuleName.Version.Major,
            $BuildMetadata.$ModuleName.Version.Minor + 1,
            0,
            0
        );
    }
    elseif ($IncrementVersionStep -eq 'Build') {
        $Version = [Version]::new(
            $BuildMetadata.$ModuleName.Version.Major,
            $BuildMetadata.$ModuleName.Version.Minor,
            $BuildMetadata.$ModuleName.Version.Build + 1,
            0
        );
    }
    elseif ($IncrementVersionStep -eq 'Revision') {
        $Version = [Version]::new(
            $BuildMetadata.$ModuleName.Version.Major,
            $BuildMetadata.$ModuleName.Version.Minor,
            $BuildMetadata.$ModuleName.Version.Build,
            $BuildMetadata.$ModuleName.Version.Revision + 1
        );
    }
    $ModuleManifestParameters['Version'] = $BuildMetadata.$ModuleName.Version = $Version
}
$OutputDirectory = $OutputPath ?? (Join-Path $ProjectRoot 'Build' $ModuleName $Version)

# Build source files
$MSBuildParameters = @{
    Path     = Join-Path $ProjectRoot 'src' 'CSharp' $ModuleName
    Property = @{
        Output    = $OutputDirectory
        Framework = 'net5.0'
        Version   = $Version
    }
}
$MSBuildResult = Invoke-MSBuild @MSBuildParameters

$ScriptBuildParameters = @{
    Path            = Get-ChildItem (Join-Path $ProjectRoot 'src' 'PowerShell' $ModuleName) -Recurse -Include '*.psm1', '*.ps1'
    DestinationPath = Join-Path $OutputDirectory "$ModuleName.psm1"
}
$ScriptBuildResult = Build-ScriptModule @ScriptBuildParameters

$NestedModuleData = @()
foreach ($NestedModule in $BuildConfiguration.$ModuleName.NestedModules) {
    $NestedModuleBuildParams = @{}
    foreach ($key in $PSBoundParameters.Keys) {
        $NestedModuleBuildParams[$key] = $PSBoundParameters[$key]
    }
    $NestedModuleBuildParams['ModuleName'] = $NestedModule

    $NestedModuleData += & $PSScriptRoot @NestedModuleBuildParams
}

$ModuleManifestParameters['Path'] = Join-Path $OutputDirectory "$ModuleName.psd1"
$ModuleManifestParameters['ModuleVersion'] = $AssemblyVersion
if ($BuildScriptModule) { $ModuleManifestParameters['RootModule'] = $ScriptBuildResult.Path }
$ModuleManifestParameters['FunctionsToExport'] = (Get-ChildItem -Path "$ProjectRoot/src/PowerShell/$ModuleName/Public" -Recurse -Include '*.psm1', '*.ps1').BaseName
$ModuleManifestParameters['FileList'] = Get-ChildItem "$PSScriptRoot/$ModuleName/$ModuleVersion" -Recurse | Select-Object -ExpandProperty FullName
$ModuleManifestParameters['RequiredAssemblies'] = @($ScriptBuildResult.RequiredAssemblies) + @($NestedModuleData.RequriedAssemblies) + $MSBuildResult.Assemblies | Select-Object -Unique
$ModuleManifestParameters['RequiredModules'] = @($ScriptBuildResult.RequiredModules) + $NestedModuleData.RequiredModules | Select-Object -Unique
$ModuleManifestParameters['TypesToProcess'] = Get-ChildItem -Path $OutputDirectory -Filter '*.types.ps1xml' | Select-Object -ExpandProperty FullName | Where-Object { $_ -notin $NestedModuleData.TypesToProcess }
$ModuleManifestParameters['RequiredPSEditions'] = $ScriptBuildResult.RequiredPSEditions
$ModuleManifestParameters['PowerShellVersion'] = $ScriptBuildResult.PowerShellVersion, $ModuleManifestParameters['PowerShellVersion'] | Sort-Object -Descending | Select-Object -First 1

New-ModuleManifest @ModuleManifestParameters