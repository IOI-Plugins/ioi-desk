param([string]$Executable)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(!$Executable){$Executable=Join-Path $root 'dist/windows/IOIDesk.exe'}
$reports=Join-Path $root 'work/test-results/windows'
New-Item -ItemType Directory -Path $reports -Force | Out-Null
$previousData=$env:IOI_DESK_DATA_ROOT
$env:IOI_DESK_DATA_ROOT=Join-Path $root 'work/test-data/windows'
$results=@()
try {
 foreach($name in @('data-migration-test','layout-persistence-test','self-test','profile-test','grid-test','picker-test','drag-test','presentation-test','zone-test','input-safety-test')) {
  $report=Join-Path $reports ($name+'.txt')
  $process=Start-Process -FilePath $Executable -ArgumentList ('--'+$name),('"'+$report+'"') -PassThru -WindowStyle Hidden
  if(!$process.WaitForExit(15000)){throw "Teste $name não terminou em 15 segundos; processo próprio $($process.Id)."}
  $text=if(Test-Path -LiteralPath $report){Get-Content -LiteralPath $report -Raw}else{''}
  $results+=@{name=$name;passed=($process.ExitCode -eq 0 -and $text.StartsWith('PASS'));exitCode=$process.ExitCode;report=$text}
 }
} finally {$env:IOI_DESK_DATA_ROOT=$previousData}
$results | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $reports 'results.json') -Encoding UTF8
$failures=@($results | Where-Object {!$_.passed})
foreach($failure in $failures){Write-Output ("FAIL: "+$failure.name+"; exitCode="+$failure.exitCode+"; "+$failure.report)}
if($failures.Count){throw 'Há falhas de teste. Consulte work/test-results/windows/results.json.'}
Write-Output "PASS: $($results.Count) testes Windows em diretório de dados isolado."

