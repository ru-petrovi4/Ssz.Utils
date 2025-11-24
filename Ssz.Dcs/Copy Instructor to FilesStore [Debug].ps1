$PSScriptRoot = ($MyInvocation.MyCommand.Path | Split-Path | Resolve-Path).ProviderPath

Remove-Item "C:\Dcs.CentralServer\FilesStore\Instructor.Bin\*" -Recurse -Force

Copy-Item -Path "$PSScriptRoot\Ssz.Dcs.Instructor\bin\Debug\net10.0-windows\*" -Destination "C:\Dcs.CentralServer\FilesStore\Instructor.Bin" -Recurse

Read-Host -Prompt "Press Enter to exit"