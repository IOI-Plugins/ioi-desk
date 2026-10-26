param([string]$DeskflowRoot,[switch]$IncludeRuntime)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$release=Get-Content -LiteralPath (Join-Path $root 'release.json') -Raw | ConvertFrom-Json
$destination=Join-Path $root 'dist/windows'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$versionSource=Join-Path $root 'src/windows/Product.cs'
Set-Content -LiteralPath $versionSource -Value ('static class Product { public const string Name="IOI Desk"; public const string Version="'+$release.version+'"; }') -Encoding UTF8
$sources=@(Get-ChildItem (Join-Path $root 'src/windows') -Filter *.cs | ForEach-Object FullName)+@(Get-ChildItem (Join-Path $root 'tests/windows') -Filter *.cs | ForEach-Object FullName)
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ "/win32icon:$root/assets/IOIDesk.ico" "/win32manifest:$root/assets/app.manifest" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Xml.dll "/out:$destination/IOIDesk.exe" $sources
if($LASTEXITCODE -ne 0){throw 'IOI Desk não compilou.'}
if($IncludeRuntime){
    if(!$DeskflowRoot -or !(Test-Path -LiteralPath (Join-Path $DeskflowRoot 'deskflow-core.exe'))){throw 'Informe -DeskflowRoot para incluir o componente original.'}
    New-Item -ItemType Directory -Path (Join-Path $destination 'vendor') -Force | Out-Null
    $runtimeDestination=Join-Path $destination 'vendor/deskflow'
    New-Item -ItemType Directory -Path $runtimeDestination -Force | Out-Null
    Get-ChildItem -LiteralPath $DeskflowRoot | Copy-Item -Destination $runtimeDestination -Recurse -Force
    $settings=Join-Path $destination 'vendor/deskflow/settings/Deskflow.conf'
    if(Test-Path -LiteralPath $settings){Set-Content -LiteralPath $settings -Value "[log]`nlevel=Info`n" -Encoding UTF8}
}
Copy-Item -LiteralPath (Join-Path $root 'release.json') -Destination $destination -Force
Write-Output "IOI Desk $($release.version): $destination"
