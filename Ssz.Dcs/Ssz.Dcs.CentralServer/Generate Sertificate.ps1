$PSScriptRoot = ($MyInvocation.MyCommand.Path | Split-Path | Resolve-Path).ProviderPath

$SelfSignCert=New-SelfSignedCertificate -dnsname deltasim.simcode.com
$certFile = Export-Certificate -Cert $SelfSignCert -FilePath $PSScriptRoot\Ssz_Dcs_CentralServer.cer
Import-Certificate -CertStoreLocation Cert:\LocalMachine\AuthRoot -FilePath $certFile.FullName

Read-Host -Prompt "Press Enter to exit"