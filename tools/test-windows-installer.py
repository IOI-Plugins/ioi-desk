"""Exercise installer/uninstaller in disposable CI paths, never production."""
import json
import os
from pathlib import Path
import subprocess

root=Path(__file__).resolve().parents[1]
if os.environ.get('GITHUB_ACTIONS')!='true' or os.name!='nt':
    raise SystemExit('Installer smoke test is restricted to a disposable Windows CI runner.')
release=json.loads((root/'release.json').read_text(encoding='utf-8'))
installer=root/('dist/IOIDesk-'+release['version']+'-windows-x64-setup.exe')
temporary=Path(os.environ['RUNNER_TEMP'])
install=temporary/'IOIDeskInstallerSmoke'
data=temporary/'IOIDeskInstallerSmokeData'
if install.exists() or data.exists():
    raise SystemExit('Installer test requires fresh paths.')
data.mkdir()
marker=data/'keep-user-data.txt'
marker.write_text('preserve')
environment=dict(os.environ,IOI_DESK_DATA_ROOT=str(data))
subprocess.run([str(installer),'/S','/NORESTART','/D='+str(install)],check=True,timeout=90,env=environment)
for name in ['IOIDesk.exe','WinSparkle.dll','LICENSE','NOTICE','Uninstall.exe']:
    if not (install/name).is_file():
        raise SystemExit('Installer omitted '+name)
user_file=install/'user-added.txt'
user_file.write_text('preserve')
report=temporary/'ioi-installer-self-test.txt'
subprocess.run([str(install/'IOIDesk.exe'),'--self-test',str(report)],check=True,timeout=30,env=environment)
if not report.read_text(encoding='utf-8-sig').startswith('PASS'):
    raise SystemExit('Installed executable failed self-test.')
subprocess.run([str(install/'Uninstall.exe'),'/S','_?='+str(install)],check=True,timeout=90,env=environment)
if (install/'IOIDesk.exe').exists() or marker.read_text()!='preserve' or user_file.read_text()!='preserve':
    raise SystemExit('Uninstall failed to preserve unrelated/user data.')
print('PASS: standard installer works; uninstall preserves user data and unrelated files.')
