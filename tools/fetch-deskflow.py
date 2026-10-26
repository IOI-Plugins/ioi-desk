"""Fetch the unmodified, SHA-256 pinned macOS arm64 Deskflow dependency."""
import hashlib
import pathlib
import platform
import subprocess
import tempfile
import urllib.request

root = pathlib.Path(__file__).resolve().parents[1]
if platform.system() != 'Darwin' or platform.machine() != 'arm64':
    raise SystemExit('This dependency is for macOS arm64 only.')
url = 'https://github.com/deskflow/deskflow/releases/download/v1.27.0/deskflow-1.27.0-macos-arm64.dmg'
sha = 'dd21e79e566314394214906d3338c505d337af536fc580c022ee3fadffad0c97'
(root/'vendor').mkdir(exist_ok=True)
with tempfile.TemporaryDirectory(prefix='ioi-desk-dependency-') as tmp:
    tmp = pathlib.Path(tmp)
    dmg = tmp/'deskflow.dmg'
    urllib.request.urlretrieve(url, dmg)
    if hashlib.sha256(dmg.read_bytes()).hexdigest() != sha:
        raise SystemExit('Deskflow checksum mismatch; dependency rejected.')
    mount = tmp/'mount'
    mount.mkdir()
    subprocess.run(['hdiutil','attach','-readonly','-nobrowse','-mountpoint',str(mount),str(dmg)], input='Y\n', text=True, check=True)
    try:
        apps = list(mount.glob('*.app'))
        if len(apps) != 1 or apps[0].name != 'Deskflow.app':
            raise SystemExit('Unexpected vendor disk image contents.')
        subprocess.run(['ditto',str(apps[0]),str(root/'vendor/Deskflow.app')],check=True)
    finally:
        subprocess.run(['hdiutil','detach',str(mount)],check=True)
