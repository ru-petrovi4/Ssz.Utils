Get-ChildItem -Path C:\Dev\Ssz.Utils -Include *.nupkg -File -Recurse | foreach { $_.Delete()}
Read-Host -Prompt "Press Enter to exit"