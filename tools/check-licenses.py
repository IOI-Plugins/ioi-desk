"""Check the declared licensing boundary and notices needed by packaging."""
from pathlib import Path
import json

root=Path(__file__).resolve().parents[1]
required=['LICENSE','NOTICE','TRADEMARKS.md','assets/BRAND.md',
          'third-party/Deskflow-GPL-2.0.txt',
          'third-party/Deskflow-OpenSSL-Exception.txt','third-party/ORIGEM.md']
for name in required:
    path=root/name
    if not path.is_file() or len(path.read_text(encoding='utf-8-sig').strip())<80:
        raise SystemExit('Missing or empty legal notice: '+name)
license=(root/'LICENSE').read_text(encoding='utf-8-sig')
if not license.startswith('MIT License\n') or 'Adenis R. Sá' not in license or 'THE SOFTWARE IS PROVIDED "AS IS"' not in license:
    raise SystemExit('Own-code license declaration is incomplete.')
origin=(root/'third-party/ORIGEM.md').read_text(encoding='utf-8-sig')
release=json.loads((root/'release.json').read_text(encoding='utf-8-sig'))
if release['deskflowVersion'] not in origin or 'GPL-2.0-only WITH LicenseRef-OpenSSL-Exception' not in origin:
    raise SystemExit('Deskflow version/license provenance mismatch.')
for script in ['tools/build-windows.ps1','tools/build-macos.sh']:
    text=(root/script).read_text(encoding='utf-8-sig')
    if not all(name in text for name in ['LICENSE','NOTICE','TRADEMARKS.md']):
        raise SystemExit('Packaging must preserve own legal notices: '+script)
print('PASS: MIT code, reserved IOI identity and separate Deskflow notices declared')
