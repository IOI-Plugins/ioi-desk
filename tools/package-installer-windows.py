"""Build the original NSIS installer format consumed by WinSparkle."""
import json
import os
from pathlib import Path
import shutil
import subprocess

root=Path(__file__).resolve().parents[1]
release=json.loads((root/'release.json').read_text(encoding='utf-8-sig'))
payload=root/'work/windows-installer-payload'
if payload.exists():
    raise SystemExit('Use a clean work/windows-installer-payload directory.')
built=root/'dist/windows'
for required in ['IOIDesk.exe','WinSparkle.dll','vendor/deskflow/deskflow-core.exe']:
    if not (built/required).is_file():
        raise SystemExit('Missing release component: '+required)
shutil.copytree(built,payload)
shutil.copytree(root/'third-party',payload/'third-party',dirs_exist_ok=True)
shutil.copytree(root/'src/browser',payload/'BrowserExtension')
shutil.copyfile(root/'assets/IOIDesk.ico',payload/'assets/IOIDesk.ico')
nsis_license=root/'vendor/nsis/nsis-3.12/COPYING'
if nsis_license.is_file():
    shutil.copyfile(nsis_license,payload/'third-party/NSIS-COPYING')
if not (payload/'third-party/deskflow-v1.27.0-source.tar.gz').is_file():
    raise SystemExit('Corresponding Deskflow source missing.')
output=root/('dist/IOIDesk-'+release['version']+'-windows-x64-setup.exe')
compiler=shutil.which('makensis')
bundled=root/'vendor/nsis/nsis-3.12/Bin/makensis.exe'
if os.name=='nt' and bundled.is_file():
    compiler=str(bundled)
if not compiler:
    raise SystemExit('NSIS makensis compiler required.')
manifest=root/'work/windows-uninstall-files.nsh'
def nsis_path(path):
    relative=path.relative_to(payload).as_posix().replace('/','\\')
    if any(x in relative for x in ['$', '"', '\n', '\r']):
        raise SystemExit('Unsupported installer filename.')
    return '$INSTDIR\\'+relative
lines=['Delete "'+nsis_path(p)+'"' for p in sorted(payload.rglob('*')) if p.is_file()]
lines+=['RMDir "'+nsis_path(p)+'"' for p in sorted(payload.rglob('*'),key=lambda x:len(x.parts),reverse=True) if p.is_dir()]
manifest.write_text('\n'.join(lines)+'\n',encoding='utf-8')
define='/D' if os.name=='nt' else '-D'
subprocess.run([compiler,define+'OUTPUT='+str(output),define+'PAYLOAD='+str(payload),define+'DELETE_MANIFEST='+str(manifest),define+'VERSION='+release['version'],str(root/'tools/installer-windows.nsi')],check=True)
print('Generated installer:',output)
