$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$vendor=Join-Path $root 'vendor'
New-Item -ItemType Directory -Path $vendor -Force | Out-Null
$archive=Join-Path $vendor 'deskflow-windows.zip'
Invoke-WebRequest 'https://github.com/deskflow/deskflow/releases/download/v1.27.0/deskflow-1.27.0-win-x64-portable.zip' -OutFile $archive
if((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne '8D19004CC3625623489D96580C1B58FD13EE78185689907FD4478595F93FAB9A'){throw 'Deskflow checksum mismatch.'}
$destination=Join-Path $vendor 'windows-original'
if(Test-Path -LiteralPath $destination){throw 'Use a clean vendor/windows-original directory; existing data preserved.'}
Expand-Archive -LiteralPath $archive -DestinationPath $destination
$cores=@(Get-ChildItem -LiteralPath $destination -Recurse -Filter deskflow-core.exe)
if($cores.Count -ne 1){throw 'Unexpected Deskflow archive structure.'}
$cores[0].DirectoryName | Set-Content -LiteralPath (Join-Path $vendor 'windows-runtime-path.txt') -Encoding UTF8
