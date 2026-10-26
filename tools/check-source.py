from pathlib import Path
import json

root=Path(__file__).resolve().parent.parent
release=json.loads((root/'release.json').read_text())
mac='\n'.join(p.read_text(encoding='utf-8-sig') for p in (root/'src/macos').glob('*.swift'))
assert mac.count('NSApplicationDelegate')==1,'Multiple macOS application owners'
assert 'Contents/Helpers/Camadas.app' not in mac,'Legacy second application referenced at runtime'
assert 'com.camadas.desktop' not in mac,'Legacy process launch remains'
assert 'EngineWindow' not in mac,'Duplicate connection UI remains'
assert 'app.delegate=controller' in mac,'Single application entry missing'
assert (root/'assets/Info.plist').is_file()
assert release['macBundleIdentifier']=='com.ioi.desk.settings','Primary identity changed unexpectedly'
assert not list((root/'src/windows').glob('*.xml')),'Personal configuration in source tree'
assert not any(p.suffix in {'.p12','.pfx','.pem','.key'} for p in (root/'src').rglob('*')),'Signing or connection keys in source'
for file in (root/'src/windows').glob('*.cs'):
 if file.name not in {'AppPaths.cs','ComputerSharing.cs'}:
  assert 'AppDomain.CurrentDomain.BaseDirectory' not in file.read_text(encoding='utf-8-sig'),f'Application-local mutable settings remain: {file.name}'
print(f"PASS: {release['product']} {release['version']}; one macOS application, centralized data paths and no private configuration in source")
