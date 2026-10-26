#!/bin/sh
set -eu
root=$(CDPATH= cd -- "$(dirname "$0")/.." && pwd)
app="$root/dist/macos/IOI Desk.app"
profile=${IOI_NOTARY_PROFILE:-IOIDesk}
submission=${1:?Supply the Apple submission UUID}
case "$submission" in *[!A-Fa-f0-9-]*|'') echo 'Invalid submission identifier.' >&2; exit 1;; esac
mkdir -p "$root/work/notarization"
xcrun notarytool info "$submission" --keychain-profile "$profile" --output-format json > "$root/work/notarization/result.json"
python3 - "$root/work/notarization/result.json" <<'PY'
import json,sys
result=json.load(open(sys.argv[1]))
if result.get('status')!='Accepted':
    raise SystemExit('Notarization not accepted: '+str(result.get('status')))
PY
codesign --verify --deep --strict "$app"
xcrun stapler staple "$app"
xcrun stapler validate "$app"
spctl --assess --type execute --verbose=2 "$app"
version=$(python3 -c 'import json,sys; r=json.load(open(sys.argv[1])); print(r["version"]+"-"+str(r["build"]))' "$root/release.json")
archive="$root/dist/IOIDesk-$version-macos-arm64.zip"
ditto -c -k --sequesterRsrc --keepParent "$app" "$archive"
shasum -a 256 "$archive" > "$archive.sha256"
echo "Notarized, stapled and Gatekeeper-validated: $archive"
