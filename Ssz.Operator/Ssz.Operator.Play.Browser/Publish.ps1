# Publishes Ssz.Operator.Play.Browser into one folder and reports where the files are.
#
# The folder is passed to MSBuild explicitly rather than taken from FolderProfile.pubxml: the
# WebAssembly SDK silently rewrites the value the Visual Studio publish wizard writes there
# (workaround for dotnet/sdk#12114), which is why publishing from the IDE can leave the folder
# named in the profile empty. A value given on the command line is a global property and is not
# rewritten.

[CmdletBinding()]
param(
    # Where to put the published files. Default: bin\<Configuration>\net10.0-browser\publish
    [string] $PublishDir,

    [string] $Configuration = 'Release',

    # Keep what is already in the folder. By default it is emptied first, so that files left over
    # from an earlier publish cannot reach the server.
    [switch] $KeepExisting,

    # Do not open the folder in Explorer when done.
    [switch] $NoOpen,

    # Do not wait for Enter at the end. For calling this script from another one.
    [switch] $NoPause,

    # Show the whole build log. By default only errors are shown: a publish of this project
    # produces several hundred trimming warnings that would bury the result.
    [switch] $ShowBuildOutput
)

$ErrorActionPreference = 'Stop'

$projectFile = Join-Path $PSScriptRoot 'Ssz.Operator.Play.Browser.csproj'
if (-not $PublishDir) {
    $PublishDir = Join-Path $PSScriptRoot "bin\$Configuration\net10.0-browser\publish"
}

if (-not (Test-Path $projectFile)) {
    throw "Не найден файл проекта: $projectFile"
}

Write-Host "Проект:  $projectFile"
Write-Host "Каталог: $PublishDir"
Write-Host ''

if ((Test-Path $PublishDir) -and -not $KeepExisting) {
    Write-Host 'Очистка каталога...'
    Remove-Item -Path (Join-Path $PublishDir '*') -Recurse -Force
}

Write-Host 'Публикация, это займёт несколько минут...'
Write-Host ''
$started = Get-Date

$publishArgs = @(
    $projectFile
    '--configuration', $Configuration
    '-p:PublishProfile=FolderProfile'
    "-p:PublishDir=$PublishDir"
    '--nologo'
    '--verbosity', $(if ($ShowBuildOutput) { 'minimal' } else { 'quiet' })
)

if ($ShowBuildOutput) {
    dotnet publish @publishArgs
}
else {
    # Even at quiet verbosity the linker prints several hundred IL trim warnings, which would
    # bury the result. Errors are not filtered.
    dotnet publish @publishArgs 2>&1 | Where-Object { $_ -notmatch 'warning IL[0-9]+' }
}

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish завершился с кодом $LASTEXITCODE"
}

# The site root, not the publish root: index.html and _framework live one level down.
$siteDir = Join-Path $PublishDir 'wwwroot'
if (-not (Test-Path (Join-Path $siteDir 'index.html'))) {
    throw "Публикация завершилась, но $siteDir\index.html не найден. Смотрите вывод выше."
}

$files = Get-ChildItem -Path $PublishDir -Recurse -File
$sizeMb = [Math]::Round((($files | Measure-Object -Property Length -Sum).Sum) / 1MB, 1)
$seconds = [int]((Get-Date) - $started).TotalSeconds

Write-Host ''
Write-Host "Готово за $seconds с. Файлов: $($files.Count), объём: $sizeMb МБ" -ForegroundColor Green
Write-Host ''
Write-Host "Опубликовано: $PublishDir"
Write-Host "Корень сайта: $siteDir"
Write-Host ''

if (-not $NoOpen) {
    Start-Process explorer.exe $PublishDir
}

if (-not $NoPause) {
    Read-Host -Prompt 'Нажмите Enter для выхода'
}
