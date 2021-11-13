$script:ExportModuleTemplatJsonSerializerOptions = [System.Text.Json.JsonSerializerOptions]::new([System.Text.Json.JsonSerializerDefaults]::General)

function Export-ModuleTemplate {
    [CmdletBinding(RemotingCapability = 'PowerShell', DefaultParameterSetName = 'Name')]
    [OutputType([System.IO.FileInfo])]
    param(
        [Parameter(Mandatory, ParameterSetName = 'Name', Position = 0)]
        [PSSharp.ModuleFactory.ModuleTemplateCompletion()]
        [SupportsWildcards()]
        [string]
        $Name,

        [Parameter(Mandatory, ParameterSetName = 'TemplateId', ValueFromPipelineByPropertyName)]
        [PSSharp.ModuleFactory.ModuleTemplateCompletion()]
        [Guid]
        $TemplateId,

        [Parameter(Mandatory)]
        [string]
        [Alias('DestinationPath')]
        $OutputPath,

        [Parameter()]
        [switch]
        $PassThru
    )
    process {
        $GetTemplateParameters = @{}
        if ($PSCmdlet.ParameterSetName -eq 'Name') {
            $GetTemplateParameters['Name'] = $Name
        }
        else {
            $GetTemplateParameters['TemplateId'] = $TemplateId
        }
        $Template = Get-ModuleTemplate @GetTemplateParameters

        if ($Template.Count -gt 1) {
            $ex = [System.Reflection.AmbiguousMatchException]::new('The module template name is ambiguous.')
            $er = [System.Management.Automation.ErrorRecord]::new(
                $ex,
                'AmbiguousModuleTemplate',
                [System.Management.Automation.ErrorCategory]::InvalidResult,
                $Name ?? $TemplateId
            )
            $PSCmdlet.WriteError($er)
            return
        }

        if (!$Template) { return; }

        $TemplateContents = Get-ModuleTemplateContents -Template $Template


        try {
            $TempArchive = New-TemporaryFile

            $CompressArchiveParameters = @{
                LiteralPath      = $TemplateContents.TemplateDirectory
                DestinationPath  = $TempArchive.FullName
                PassThru         = $true
                CompressionLevel = 'Optimal'
                Force            = $true
            }
            [void](Compress-Archive @CompressArchiveParameters)

            Use-DisposableObject ($FileStream = [System.IO.FileStream]::new($OutputPath, 'Create', 'Write')) {
                Use-DisposableObject ($ArchiveStream = [System.IO.FileStream]::new($TempArchive.FullName, 'Open', 'Read')) {
                    $MetadataBytes = [System.Text.Json.JsonSerializer]::SerializeToUtf8Bytes($Template, [PSSharp.ModuleFactory.ModuleTemplateMetadata], $script:ExportModuleTemplatJsonSerializerOptions)
                    $MetadataByteSize = $MetadataBytes.Count
                    $MetadataByteSizeBytes = [BitConverter]::GetBytes($MetadataByteSize)

                    $FileStream.Write($MetadataByteSizeBytes, 0, $MetadataByteSizeBytes.Count)
                    $FileStream.Write($MetadataBytes, 0, $MetadataBytes.Count)
                    $ArchiveStream.CopyTo($FileStream)
                }
            }
            if ($PassThru) {
                Get-Item -LiteralPath $OutputPath
            }
        }
        finally {
            if (Test-Path $TempArchive.FullName) {
                Remove-Item $TempArchive.FullName
            }
        }
    }
}