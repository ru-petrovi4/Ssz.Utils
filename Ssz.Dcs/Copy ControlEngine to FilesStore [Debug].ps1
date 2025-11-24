$PSScriptRoot = ($MyInvocation.MyCommand.Path | Split-Path | Resolve-Path).ProviderPath

Remove-Item "C:\CDT.CentralServer\FilesStore\ControlEngine.Bin\*" -Recurse -Force

Copy-Item -Path "$PSScriptRoot\Ssz.Dcs.ControlEngine\bin\Debug\net10.0\*" -Destination "C:\CDT.CentralServer\FilesStore\ControlEngine.Bin" -Recurse

Read-Host -Prompt "Press Enter to exit"