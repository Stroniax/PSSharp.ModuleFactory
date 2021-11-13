Import-Module "$PSScriptRoot\PSSharp.ModuleFactory.dll"

$Templates = @(Get-ModuleTemplate)
if ($Templates.Count -eq 0) {
    Write-Verbose "Imoprting default initial module templates."
    Get-ChildItem -Path "$PSScriptRoot\DefaultTemplates" | Import-ModuleTemplate
}

$ExecutionContext.SessionState.Module.OnRemove = {
    Set-ModuleTemplateRepository -TemplateRepository $null
}