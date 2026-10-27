"""Generate standard Sparkle/WinSparkle appcasts with the official signer."""
import base64
from datetime import datetime, timezone
from email.utils import format_datetime
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[1]
release=json.loads((root/'release.json').read_text(encoding='utf-8'))
updates=json.loads((root/'assets/updates.json').read_text(encoding='utf-8'))
directory=Path(sys.argv[1]).resolve()
version=release['version']
base='https://github.com/'+updates['repository']+'/releases/download/v'+version+'/'
sparkle=root/'vendor/sparkle/bin'
signer=sparkle/'sign_update'
private=os.environ.get('SPARKLE_SIGNING_KEY_FILE')
key_args=['--ed-key-file',private] if private else ['--account','IOIDeskUpdater']
mac=directory/('IOIDesk-'+version+'-'+str(release['build'])+'-macos-arm64.zip')
windows=directory/('IOIDesk-'+version+'-windows-x64-setup.exe')
if not mac.is_file() or not windows.is_file():
    raise SystemExit('Both platform release packages are required.')
feed_directory=root/'work/mac-appcast-archives'
feed_directory.mkdir(parents=True,exist_ok=True)
shutil.copyfile(mac,feed_directory/mac.name)
subprocess.run([str(sparkle/'generate_appcast'),*key_args,'--download-url-prefix',base,'--maximum-deltas','0','-o',str(directory/'appcast-macos.xml'),str(feed_directory)],check=True)
namespace='http://www.andymatuschak.org/xml-namespaces/sparkle'
ET.register_namespace('sparkle',namespace)
def sign(path):
    signature=subprocess.check_output([str(signer),*key_args,'-p',str(path)],text=True).strip()
    if len(base64.b64decode(signature,validate=True))!=64:
        raise SystemExit('Invalid signature returned by official Sparkle signer.')
    subprocess.run([str(signer),*key_args,'--verify',str(path),signature],check=True)
    return signature
document=ET.Element('rss',{'version':'2.0'})
channel=ET.SubElement(document,'channel')
ET.SubElement(channel,'title').text='IOI Desk · Windows'
ET.SubElement(channel,'link').text='https://github.com/'+updates['repository']
ET.SubElement(channel,'description').text='Atualizações oficiais do IOI Desk'
item=ET.SubElement(channel,'item')
ET.SubElement(item,'title').text='IOI Desk '+version
ET.SubElement(item,'pubDate').text=format_datetime(datetime.now(timezone.utc))
ET.SubElement(item,'{'+namespace+'}version').text=str(release['build'])
ET.SubElement(item,'{'+namespace+'}shortVersionString').text=version
ET.SubElement(item,'{'+namespace+'}minimumSystemVersion').text='10.0'
ET.SubElement(item,'enclosure',{'url':base+windows.name,'length':str(windows.stat().st_size),'type':'application/octet-stream','{'+namespace+'}os':'windows-x64','{'+namespace+'}edSignature':sign(windows),'{'+namespace+'}installerArguments':'/S'})
windows_feed=directory/'appcast-windows.xml'
ET.ElementTree(document).write(windows_feed,encoding='utf-8',xml_declaration=True)
# Sparkle signs the feed itself as well as the Mac archive. Never modify later.
subprocess.run([str(signer),*key_args,str(directory/'appcast-macos.xml')],check=True)
subprocess.run([str(signer),*key_args,'--verify',str(directory/'appcast-macos.xml')],check=True)
for feed in [directory/'appcast-macos.xml',windows_feed]:
    parsed=ET.parse(feed)
    enclosures=parsed.findall('.//enclosure')
    if not enclosures or any(not e.get('{'+namespace+'}edSignature') or not e.get('url','').startswith(base) for e in enclosures):
        raise SystemExit('Unsigned or unexpected release enclosure.')
files=[mac,windows,directory/'appcast-macos.xml',windows_feed]
(directory/'SHA256SUMS').write_text(''.join(hashlib.sha256(p.read_bytes()).hexdigest()+'  '+p.name+'\n' for p in files))
print('Verified signed appcasts and SHA256SUMS generated for both platforms.')
