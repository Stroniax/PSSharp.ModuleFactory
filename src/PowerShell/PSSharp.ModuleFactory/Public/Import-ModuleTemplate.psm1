$script:ImportModuleTemplateTempDirId = 0

function Import-ModuleTemplate {
    [CmdletBinding(RemotingCapability = 'PowerShell', DefaultParameterSetName = 'Path')]
    [OutputType([PSSharp.ModuleFactory.ModuleTemplateMetadata])]
    [System.Diagnostics.CodeAnalysis.SuppressMessage('PSUseDeclaredVarsMoreThanAssignments', '', Justification = 'Variable referenced out of analyzed scope.')]
    param(
        [Parameter(Mandatory, ParameterSetName = 'Path', Position = 0)]
        [Alias('FilePath')]
        [string]
        $Path,

        [Parameter(Mandatory, ParameterSetName = 'LiteralPath', ValueFromPipelineByPropertyName)]
        [string]
        [Alias('PSPath')]
        $LiteralPath
    )
    process {
        do {
            $TempDirId = $script:ImportModuleTemplateTempDirId++
            $TempDirPath = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "ImportModuleTemplate_$TempDirId")
        }
        while ( Test-Path $TempDirPath )


        $TempArchivePath = New-TemporaryFile

        $ExpandArchiveParameters = @{
            DestinationPath = $TempDirPath
            LiteralPath     = $TempArchivePath.FullName
            Force           = $true
        }

        $ResolvePathParameters = @{}
        if ($PSCmdlet.ParameterSetName -eq 'Path') { 
            $ResolvedPaths = @( Resolve-Path -Path $Path )
            if ($ResolvedPaths.Count -ne 1) {
                Write-Debug 'Could not resolve single path.'
                return;
            }
            else{
                $ResolvedPath = $ResolvedPaths[0].Path
            }
        }
        else {
            $ResolvedPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($LiteralPath)
        }
        if (!(Test-Path $ResolvedPath)) {
            $exMessage = Get-PSSharpModuleFactoryResourceString -Name 'FileNotFound'
            $ex = [System.Management.Automation.ItemNotFoundException]::new($exMessage)
            $er = [System.Management.Automation.ErrorRecord]::new(
                $ex,
                'FileNotFound',
                [System.Management.Automation.ErrorCategory]::ObjectNotFound,
                $LiteralPath
            )
            $er.ErrorDetails = Get-PSSharpModuleFactoryResourceString 'FileNotFoundInterpolated' $LiteralPath
            $PSCmdlet.WriteError($er)
            return
        }

        Write-Debug "Importing template from path [$(${ResolvedPath}?.GetType() ?? '(null)')] '$ResolvedPath'."
        try {
            Use-DisposableObject ($TemplateStream = [System.IO.FileStream]::new($ResolvedPath, 'Open', 'Read')) {
                Use-DisposableObject ($ArchiveStream = [System.IO.FileStream]::new($TempArchivePath.FullName, 'Create', 'ReadWrite')) {
                    $SizeOfMetadataBytes = [byte[]]::new(4)
                    [void]$TemplateStream.Read($SizeOfMetadataBytes, 0, $SizeOfMetadataBytes.Count)
                    $SizeOfMetadata = [System.BitConverter]::ToInt32($SizeOfMetadataBytes)
                    $MetadataBytes = [byte[]]::new($SizeOfMetadata)
                    [void]$TemplateStream.Read($MetadataBytes, 0, $SizeOfMetadata)
                    $Metadata = [System.Text.Json.JsonSerializer]::Deserialize($MetadataBytes, [PSSharp.ModuleFactory.ModuleTemplateMetadata])
                    # The rest of the file is the archive
                    $TemplateStream.CopyTo($ArchiveStream)
                }
            }
            Expand-Archive @ExpandArchiveParameters

            # Register-ModuleTemplate writes the metadata to command output
            Register-ModuleTemplate -LiteralPath (Join-Path $TempDirPath $Metadata.TemplateId) -Metadata $Metadata
        }
        finally {
            if (Test-Path $TempArchivePath.FullName) {
                Remove-Item $TempArchivePath.FullName
            }
            if (Test-Path $TempDirPath) {
                Remove-Item $TempDirPath -Force -Recurse
            }
        }
    }
}