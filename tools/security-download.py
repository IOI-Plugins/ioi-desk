"""Install only checksum-pinned scanner binaries; no remote installer scripts."""
import hashlib
import io
import json
from pathlib import Path
import platform
import tarfile
import urllib.request

root=Path(__file__).resolve().parents[1]
if platform.system()!='Linux' or platform.machine() not in {'x86_64','amd64'}:
    raise SystemExit('Pinned security tools require Linux x86_64.')
destination=root/'work/security-tools'
destination.mkdir(parents=True,exist_ok=True)
for name,tool in json.loads((root/'tools/security-tools.json').read_text()).items():
    with urllib.request.urlopen(tool['url'],timeout=60) as response:
        data=response.read(100_000_001)
    if len(data)>100_000_000 or hashlib.sha256(data).hexdigest()!=tool['sha256']:
        raise SystemExit('Rejected scanner download: '+name)
    with tarfile.open(fileobj=io.BytesIO(data),mode='r:gz') as archive:
        members=[m for m in archive if m.isfile() and m.name==name]
        if len(members)!=1:
            raise SystemExit('Unexpected scanner archive: '+name)
        file=archive.extractfile(members[0])
        if file is None:
            raise SystemExit('Scanner binary missing: '+name)
        binary=destination/name
        binary.write_bytes(file.read())
        binary.chmod(0o755)
    print('Verified '+name+' '+tool['version'])
