& git pull
Remove-Item ".\Ssz.Utils\Bin" -Recurse -ErrorAction Ignore
Remove-Item ".\Ssz.Utils.Wpf\Bin" -Recurse -ErrorAction Ignore
Remove-Item ".\Ssz.Xi.Client\Bin" -Recurse -ErrorAction Ignore
Remove-Item ".\Ssz.WindowsAPICodePack\Bin" -Recurse -ErrorAction Ignore
Remove-Item ".\Ssz.Xceed.Wpf.Toolkit\Bin" -Recurse -ErrorAction Ignore
$msBuildExe = 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\msbuild.exe'
& "$($msBuildExe)" Ssz.Utils.sln /t:Build /m /property:Configuration=Release
Read-Host -Prompt "Press Enter to exit"