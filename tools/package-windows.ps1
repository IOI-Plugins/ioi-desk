param([string]$IdentityName,[string]$Publisher,[string]$PublisherDisplayName='IOI',[switch]$Preview)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if($Preview){$IdentityName='IOI.IOIDesk.Preview';$Publisher='CN=IOI Desk Preview'}
if(!$IdentityName -or !$Publisher){throw 'Provide the exact identity name and publisher from Partner Center. Preview is for CI validation only.'}
if($IdentityName -notmatch '^[A-Za-z0-9.-]{3,50}$'){throw 'Invalid package identity.'}
$release=Get-Content (Join-Path $root 'release.json') -Raw | ConvertFrom-Json
$version=$release.version+'.'+$release.build
$stage=Join-Path $root 'work/msix-stage'
if(Test-Path -LiteralPath $stage){throw 'Use a clean work/msix-stage directory; existing files preserved.'}
New-Item -ItemType Directory -Path $stage -Force | Out-Null
$built=Join-Path $root 'dist/windows'
if(!(Test-Path -LiteralPath (Join-Path $built 'vendor/deskflow/deskflow-core.exe'))){throw 'Build with the verified Deskflow runtime before packaging.'}
Copy-Item -Path (Join-Path $built '*') -Destination $stage -Recurse
Copy-Item -LiteralPath (Join-Path $root 'third-party') -Destination (Join-Path $stage 'third-party') -Recurse
if(!(Test-Path (Join-Path $stage 'third-party/deskflow-v1.27.0-source.tar.gz'))){throw 'Corresponding Deskflow source archive missing.'}
Copy-Item -LiteralPath (Join-Path $root 'src/browser') -Destination (Join-Path $stage 'BrowserExtension') -Recurse
Add-Type -AssemblyName System.Drawing
$assets=Join-Path $stage 'Assets'
New-Item -ItemType Directory -Path $assets -Force | Out-Null
$original=[Drawing.Image]::FromFile((Join-Path $root 'assets/IOIDesk.png'))
try{
 foreach($size in @(44,50,150)){
  $bitmap=New-Object Drawing.Bitmap($size,$size)
  $graphics=[Drawing.Graphics]::FromImage($bitmap)
  try{$graphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$graphics.DrawImage($original,0,0,$size,$size);$bitmap.Save((Join-Path $assets ('Logo'+$size+'.png')),[Drawing.Imaging.ImageFormat]::Png)}finally{$graphics.Dispose();$bitmap.Dispose()}
 }
}finally{$original.Dispose()}
$name=[Security.SecurityElement]::Escape($IdentityName)
$publisherXml=[Security.SecurityElement]::Escape($Publisher)
$display=[Security.SecurityElement]::Escape($PublisherDisplayName)
$manifest=@"
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10" xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10" xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities" IgnorableNamespaces="uap rescap">
 <Identity Name="$name" Publisher="$publisherXml" Version="$version" ProcessorArchitecture="x64" />
 <Properties><DisplayName>IOI Desk</DisplayName><PublisherDisplayName>$display</PublisherDisplayName><Description>Seu espaço de trabalho integrado.</Description><Logo>Assets\Logo50.png</Logo></Properties>
 <Resources><Resource Language="pt-BR" /></Resources>
 <Dependencies><TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.19041.0" MaxVersionTested="10.0.26100.0" /></Dependencies>
 <Applications><Application Id="IOIDesk" Executable="IOIDesk.exe" EntryPoint="Windows.FullTrustApplication"><uap:VisualElements DisplayName="IOI Desk" Description="Seu espaço de trabalho integrado." Square150x150Logo="Assets\Logo150.png" Square44x44Logo="Assets\Logo44.png" BackgroundColor="transparent" /></Application></Applications>
 <Capabilities><rescap:Capability Name="runFullTrust" /></Capabilities>
</Package>
"@
$manifest | Set-Content -LiteralPath (Join-Path $stage 'AppxManifest.xml') -Encoding UTF8
$sdk=Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/bin'
$makeappx=@(Get-ChildItem -LiteralPath $sdk -Recurse -Filter makeappx.exe | Where-Object {$_.Directory.Name -eq 'x64'} | Sort-Object FullName -Descending)[0].FullName
if(!$makeappx){throw 'Windows SDK MakeAppx required.'}
$output=Join-Path $root ('dist/IOIDesk-'+$version+$(if($Preview){'-preview'}else{'-store'})+'-x64.msix')
& $makeappx pack /d $stage /p $output /o
if($LASTEXITCODE -ne 0){throw 'MakeAppx validation failed.'}
Get-FileHash -LiteralPath $output -Algorithm SHA256 | Format-List
Write-Output "Package generated: $output. Unsigned; no installation or store submission performed."
