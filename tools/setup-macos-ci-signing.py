"""Documented Keychain + notarytool flow, with secrets kept out of logs/argv."""
import base64
import os
from pathlib import Path
import pty
import secrets
import select
import subprocess
import termios
import time

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
    result=subprocess.run(args,capture_output=True,text=True)
    if result.returncode:
        raise SystemExit('Release signing setup failed at '+Path(args[0]).name)
command(['security','create-keychain','-p',keychain_password,str(keychain)])
command(['security','set-keychain-settings','-lut','21600',str(keychain)])
command(['security','unlock-keychain','-p',keychain_password,str(keychain)])
command(['security','list-keychains','-d','user','-s',str(keychain)])
command(['security','default-keychain','-d','user','-s',str(keychain)])
command(['swift',str(root/'tools/import-macos-ci-identity.swift'),str(certificate),str(keychain)])
command(['security','set-key-partition-list','-S','apple-tool:,apple:','-s','-k',keychain_password,str(keychain)])
certificate.unlink()
# notarytool explicitly supports a secure password prompt when Apple ID and Team
# are supplied. A PTY lets us answer that prompt without a password CLI argument.
master,slave=pty.openpty()
process=subprocess.Popen(['xcrun','notarytool','store-credentials','IOIDesk','--apple-id',os.environ['APPLE_ID'],'--team-id','5723C964BL','--keychain',str(keychain)],stdin=slave,stdout=slave,stderr=slave,close_fds=True)
sent=False
buffer=b''
deadline=time.monotonic()+120
try:
    while process.poll() is None and time.monotonic()<deadline:
        if not select.select([master],[],[],1)[0]:
            continue
        try:
            buffer=(buffer+os.read(master,4096))[-16384:]
        except OSError:
            break
        if not sent and b'password' in buffer.lower():
            # Do not send credentials while terminal echo is enabled.
            if termios.tcgetattr(slave)[3]&termios.ECHO:
                continue
            os.write(master,os.environ['APPLE_APP_PASSWORD'].encode()+b'\n')
            sent=True
    if process.poll() is None:
        process.terminate()
    status=process.wait(timeout=10)
    if status or not sent:
        raise SystemExit('Notarization credential validation failed; secret output withheld.')
finally:
    os.close(master)
    os.close(slave)
print('Developer ID imported and notarization credentials validated in disposable CI keychain.')
