param(
    [Parameter(Mandatory=$true)][string]$Path,
    [Parameter(Mandatory=$true)][string]$ExpectedPublisher
)
$ErrorActionPreference='Stop'
if([string]::IsNullOrWhiteSpace($ExpectedPublisher)){throw 'Expected publisher must be configured.'}
$file=Get-Item -LiteralPath $Path
if($file.PSIsContainer){throw 'Select one signed file, not a directory.'}
$signature=Get-AuthenticodeSignature -LiteralPath $file.FullName
if($signature.Status -ne 'Valid'){throw ('Authenticode rejected '+$file.Name+': '+$signature.Status)}
if(!$signature.SignerCertificate -or $signature.SignerCertificate.Subject -cne $ExpectedPublisher){throw ('Unexpected signing publisher for '+$file.Name)}
if(!$signature.TimeStamperCertificate){throw ('Trusted timestamp missing from '+$file.Name)}
Write-Output ('PASS: trusted Authenticode signature, expected publisher and timestamp: '+$file.Name)
