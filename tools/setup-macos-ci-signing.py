"""Apple notarytool and disposable CI Keychain; secret output is never logged."""
import base64
import os
from pathlib import Path
import secrets
import shutil
import subprocess

root=Path(__file__).resolve().parents[1]
required=['MACOS_CERTIFICATE_BASE64','MACOS_CERTIFICATE_PASSWORD','APPLE_ID','APPLE_APP_PASSWORD']
if any(not os.environ.get(name) for name in required):
    raise SystemExit('Release signing secrets are not configured; no unsigned release produced.')
temporary=Path(os.environ['RUNNER_TEMP'])
certificate=temporary/'ioi-signing.p12'
certificate.write_bytes(base64.b64decode(os.environ['MACOS_CERTIFICATE_BASE64'],validate=True))
certificate.chmod(0o600)
keychain=temporary/'ioi-release.keychain-db'
keychain_password=secrets.token_urlsafe(36)
# This password protects only the temporary CI keychain, not an Apple account.
def command(args):
    try:
        result=subprocess.run(args,capture_output=True,text=True,timeout=60)
    except subprocess.TimeoutExpired:
        raise SystemExit('Release signing command timed out; secret output withheld.') from None
    if result.returncode:
        raise SystemExit('Release signing setup failed at '+Path(args[0]).name)
command(['security','create-keychain','-p',keychain_password,str(keychain)])
command(['security','set-keychain-settings','-lut','21600',str(keychain)])
command(['security','unlock-keychain','-p',keychain_password,str(keychain)])
command(['security','list-keychains','-d','user','-s',str(keychain)])
command(['security','default-keychain','-d','user','-s',str(keychain)])
# Standard GitHub recipe: only the disposable runner keychain permits tools.
command(['security','import',str(certificate),'-P',os.environ['MACOS_CERTIFICATE_PASSWORD'],'-A','-t','cert','-f','pkcs12','-k',str(keychain)])
command(['security','set-key-partition-list','-S','apple-tool:,apple:','-k',keychain_password,str(keychain)])
certificate.unlink()
probe=temporary/'ioi-signing-probe'
shutil.copy2('/usr/bin/true',probe)
command(['codesign','--force','--timestamp','--sign','01888205A54DCB53AE5FF7F6437F6F95893005B7',str(probe)])
command(['codesign','--verify','--strict',str(probe)])
probe.unlink()
print('Noninteractive codesign authorization verified.',flush=True)
# Apple's supported noninteractive authentication, on an ephemeral runner.
# Read the password from GitHub Secrets, never print the command or its output.
print('Developer ID imported into disposable CI keychain.',flush=True)
try:
    result=subprocess.run(['xcrun','notarytool','store-credentials','IOIDesk',
        '--apple-id',os.environ['APPLE_ID'],'--team-id','5723C964BL',
        '--password',os.environ['APPLE_APP_PASSWORD'],'--keychain',str(keychain)],
        capture_output=True,text=True,timeout=180)
except subprocess.TimeoutExpired:
    raise SystemExit('Apple credential validation timed out; secret output withheld.') from None
if result.returncode:
    reason='Apple rejected the credentials' if '401' in result.stdout+result.stderr else 'Apple credential validation failed'
    raise SystemExit(reason+'; secret output withheld.')
print('Notarization credentials validated in disposable CI keychain.')
