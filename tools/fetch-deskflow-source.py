"""Include the exact corresponding original source in distribution packages."""
import hashlib
from pathlib import Path
import urllib.request

root=Path(__file__).resolve().parents[1]
source=root/'third-party/deskflow-v1.27.0-source.tar.gz'
expected='dfe4327f5d0631ead9bacee743a96ee58024af11b5cc080af89b4c740b18e893'
if not source.exists():
    urllib.request.urlretrieve('https://github.com/deskflow/deskflow/archive/refs/tags/v1.27.0.tar.gz',source)
if hashlib.sha256(source.read_bytes()).hexdigest()!=expected:
    raise SystemExit('Corresponding source checksum mismatch; packaging blocked.')
print('Deskflow corresponding source verified.')
