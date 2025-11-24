$PSScriptRoot = ($MyInvocation.MyCommand.Path | Split-Path | Resolve-Path).ProviderPath
$serviceName = "Ssz.IdentityServer"

stop-service $serviceName
$maxRepeat = 100
$status = "Running" # change to Stopped if you want to wait for services to start
do 
{
    $count = (Get-Service $serviceName | ? {$_.status -eq $status}).count
    $maxRepeat--
    sleep -Milliseconds 600
} until ($count -eq 0 -or $maxRepeat -eq 0)
Start-Sleep -Seconds 5


Remove-Item "C:\Program Files\Ssz\IdentityServer\*" -Recurse -Force

Copy-Item -Path "$PSScriptRoot\Ssz.IdentityServer\bin\Release\net10.0\*" -Destination "C:\Program Files\Ssz\IdentityServer" -Recurse


start-service $serviceName

Read-Host -Prompt "Press Enter to exit"