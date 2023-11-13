$PSScriptRoot = ($MyInvocation.MyCommand.Path | Split-Path | Resolve-Path).ProviderPath

Remove-Item "C:\Dcs.CentralServer\ControlEngine.Bin\DcsCentralServer.db" -Recurse -Force -ErrorAction Ignore

Copy-Item -Path "$PSScriptRoot\Ssz.Dcs.CentralServer\DcsCentralServer.db" -Destination "C:\Dcs.CentralServer\" -Recurse

Read-Host -Prompt "Press Enter to exit"