$PSScriptRoot = ($MyInvocation.MyCommand.Path | Split-Path | Resolve-Path).ProviderPath

Remove-Item "C:\Program Files\Ssz\Dcs.Launcher\*" -Recurse -Force

Copy-Item -Path "$PSScriptRoot\Ssz.Dcs.Launcher\bin\Release\net7.0-windows\*" -Destination "C:\Program Files\Ssz\Dcs.Launcher" -Recurse

#Read-Host -Prompt "Press Enter to exit"