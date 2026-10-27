"""Fetch original updater frameworks after verifying their pinned checksums."""
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import platform
import subprocess
import sys
import tarfile
import urllib.request
import zipfile

root=Path(__file__).resolve().parents[1]
name='sparkle' if platform.system()=='Darwin' else 'winsparkle'
dependency=json.loads((root/'tools/updater-dependencies.json').read_text(encoding='utf-8'))[name]
destination=root/'vendor'/name
destination.mkdir(parents=True,exist_ok=True)
archive=destination/('original.tar.xz' if name=='sparkle' else 'original.zip')
if not archive.exists():
    urllib.request.urlretrieve(dependency['url'],archive)
if hashlib.sha256(archive.read_bytes()).hexdigest()!=dependency['sha256']:
    raise SystemExit('Updater checksum mismatch; download rejected.')
if name=='sparkle':
    with tarfile.open(archive) as source:
        for member in source.getmembers():
            path=PurePosixPath(member.name)
            if path.is_absolute() or '..' in path.parts:
                raise SystemExit('Unsafe updater archive path.')
            if member.issym() or member.islnk():
                target=Path(member.name).parent/member.linkname
                resolved=(destination/target).resolve()
                if not resolved.is_relative_to(destination.resolve()):
                    raise SystemExit('Unsafe updater archive link.')
    # Native tar preserves the original framework's symlinks and executable bits.
    subprocess.run(['/usr/bin/tar','-xf',str(archive),'-C',str(destination)],check=True)
else:
    with zipfile.ZipFile(archive) as source:
        prefix='WinSparkle-'+dependency['version']+'/'
        mapping={prefix+'x64/Release/WinSparkle.dll':'WinSparkle.dll',
                 prefix+'COPYING':'COPYING',prefix+'COPYING.expat':'COPYING.expat'}
        for original,target in mapping.items():
            (destination/target).write_bytes(source.read(original))
print('Verified original '+name+' '+dependency['version'])
if '--installer' in sys.argv:
    nsis=json.loads((root/'tools/updater-dependencies.json').read_text(encoding='utf-8'))['nsis']
    target=root/'vendor/nsis'
    target.mkdir(parents=True,exist_ok=True)
    package=target/'original.zip'
    if not package.exists():
        urllib.request.urlretrieve(nsis['url'],package)
    if hashlib.sha256(package.read_bytes()).hexdigest()!=nsis['sha256']:
        raise SystemExit('NSIS checksum mismatch.')
    with zipfile.ZipFile(package) as source:
        for member in source.infolist():
            path=PurePosixPath(member.filename)
            if path.is_absolute() or '..' in path.parts:
                raise SystemExit('Unsafe NSIS package path.')
        source.extractall(target)
    print('Verified NSIS '+nsis['version'])
