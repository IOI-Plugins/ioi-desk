"""Repository hygiene checks. Complements, never replaces, Gitleaks and SAST."""
from pathlib import Path, PurePosixPath
import re
import subprocess
import sys

root=Path(__file__).resolve().parents[1]

def violations(name, content):
    path=PurePosixPath(name)
    errors=[]
    if path.parts[0] in {'dist','work','vendor','.ssh'}:
        errors.append('generated or private directory is tracked')
    if path.suffix.lower() in {'.p12','.pfx','.pem','.key','.p8','.keychain','.keychain-db'}:
        errors.append('key or credential container is tracked')
    if path.name=='.env' or (path.name.startswith('.env.') and not path.name.endswith(('.example','.sample'))):
        errors.append('environment credentials file is tracked')
    if path.name in {'authorized_keys','known_hosts','.gitleaksignore','.gitleaks.toml'}:
        errors.append('private SSH data or unreviewed secret-scanner override')
    if re.search(r'-----BEGIN (?:RSA |EC |OPENSSH |ENCRYPTED )?PRIVATE KEY-----',content):
        errors.append('private key material')
    if path.suffix.lower() in {'.cs','.swift','.py','.ps1','.sh','.js'}:
        if re.search(r'(?i)(?:C:[/\\]Users[/\\][^/\\\s]+|/Users/[A-Za-z][^/\s]+/|192\.168\.\d+\.\d+)',content):
            errors.append('personal workstation path or fixed LAN address in source')
    if name.startswith('.github/workflows/'):
        if re.search(r'^\s*pull_request_target\s*:',content,re.M):
            errors.append('privileged pull_request_target workflow')
        if not re.search(r'^permissions:\s*\n\s+contents:\s*read\s*$',content,re.M):
            errors.append('workflow must default to contents: read')
        for ref in re.findall(r'\buses:\s*[\x27\x22]?([^\s\x27\x22]+)',content):
            if not ref.startswith('./') and not re.fullmatch(r'[A-Za-z0-9_.-]+/[A-Za-z0-9_./-]+@[a-f0-9]{40}',ref):
                errors.append('action not pinned to a full commit SHA')
        if re.search(r'\$\{\{\s*github\.event\.(?:issue|pull_request|comment|discussion)\.',content):
            errors.append('untrusted event text embedded directly in workflow')
        if re.search(r'persist-credentials:\s*true',content):
            errors.append('checkout credentials persisted')
    return errors

def test():
    assert violations('signing/cert.p12','')
    assert violations('work/config.json','')
    assert violations('.env','')
    assert violations('src/leak.txt','-----BEGIN '+'PRIVATE KEY-----')
    assert not violations('assets/Entitlements.plist','public metadata')
    assert violations('.github/workflows/bad.yml','permissions:\n  contents: read\njobs:\n  x:\n    uses: actions/checkout@main\n')
    assert not violations('.github/workflows/good.yml','permissions:\n  contents: read\njobs:\n  x:\n    uses: actions/checkout@'+'a'*40+'\n')
    print('PASS: security policy rejects private material and unsafe action references')

if '--self-test' in sys.argv:
    test()
    raise SystemExit(0)
files=subprocess.check_output(['git','ls-files','-z'],cwd=root).decode().split('\0')
failed=[]
for name in filter(None,files):
    path=root/name
    if not path.is_file():
        continue
    try:
        content=path.read_text(encoding='utf-8-sig')
    except UnicodeError:
        content=''
    failed.extend((name,error) for error in violations(name,content))
for name,error in failed:
    # Never include matching content; filenames and rule descriptions only.
    print('FAIL: '+name+': '+error)
if failed:
    raise SystemExit(1)
print('PASS: tracked repository files satisfy publication hygiene policy')
