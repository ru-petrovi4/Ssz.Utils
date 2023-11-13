$PSScriptRoot = ($MyInvocation.MyCommand.Path | Split-Path | Resolve-Path).ProviderPath

Remove-Item "$PSScriptRoot\Packages\Ssz.DataGrpc.Client" -Recurse -ErrorAction Ignore
Copy-Item -Path "$PSScriptRoot\..\Ssz.Utils\Ssz.DataGrpc.Client\bin\Release" -Destination "$PSScriptRoot\Packages\Ssz.DataGrpc.Client" -Recurse

Remove-Item "$PSScriptRoot\Packages\Ssz.Utils" -Recurse -ErrorAction Ignore
Copy-Item -Path "$PSScriptRoot\..\Ssz.Utils\Ssz.Utils\bin\Release" -Destination "$PSScriptRoot\Packages\Ssz.Utils" -Recurse

Remove-Item "$PSScriptRoot\Packages\Ssz.Utils.Wpf" -Recurse -ErrorAction Ignore
Copy-Item -Path "$PSScriptRoot\..\Ssz.Utils\Ssz.Utils.Wpf\bin\Release" -Destination "$PSScriptRoot\Packages\Ssz.Utils.Wpf" -Recurse

Remove-Item "$PSScriptRoot\Packages\Ssz.Xi.Client" -Recurse -ErrorAction Ignore
Copy-Item -Path "$PSScriptRoot\..\Ssz.Utils\Ssz.Xi.Client\bin\Release" -Destination "$PSScriptRoot\Packages\Ssz.Xi.Client" -Recurse

dotnet-outdated -u


Read-Host -Prompt "Press Enter to exit"