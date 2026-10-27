param([string]$DeskflowRoot,[switch]$IncludeRuntime)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$release=Get-Content -LiteralPath (Join-Path $root 'release.json') -Raw | ConvertFrom-Json
$updates=Get-Content -LiteralPath (Join-Path $root 'assets/updates.json') -Raw | ConvertFrom-Json
if([Convert]::FromBase64String($updates.publicKey).Length -ne 32){throw 'Invalid updater public key.'}
$destination=Join-Path $root 'dist/windows'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$versionSource=Join-Path $root 'src/windows/Product.cs'
Set-Content -LiteralPath $versionSource -Value ('static class Product { public const string Name="IOI Desk"; public const string Version="'+$release.version+'"; }') -Encoding UTF8
$config='static class UpdateConfig { public const string Build="'+$release.build+'"; public const string PublicKey="'+$updates.publicKey+'"; public const string WindowsFeed="'+$updates.windowsFeed+'"; }'
Set-Content -LiteralPath (Join-Path $root 'src/windows/UpdateConfig.cs') -Value $config -Encoding UTF8
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
foreach($notice in @('LICENSE','NOTICE','TRADEMARKS.md')){Copy-Item -LiteralPath (Join-Path $root $notice) -Destination $destination -Force}
New-Item -ItemType Directory -Path (Join-Path $destination 'assets') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'assets/BRAND.md') -Destination (Join-Path $destination 'assets/BRAND.md') -Force
$updater=Join-Path $root 'vendor/winsparkle'
if(Test-Path -LiteralPath (Join-Path $updater 'WinSparkle.dll')){
    Copy-Item -LiteralPath (Join-Path $updater 'WinSparkle.dll') -Destination $destination -Force
    $notices=Join-Path $destination 'third-party/WinSparkle'
    New-Item -ItemType Directory -Path $notices -Force | Out-Null
    foreach($notice in @('COPYING','COPYING.expat')){Copy-Item -LiteralPath (Join-Path $updater $notice) -Destination $notices -Force}
}elseif($IncludeRuntime){throw 'Run tools/fetch-updater.py before packaging a release.'}
Write-Output "IOI Desk $($release.version): $destination"
