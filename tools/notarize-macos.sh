#!/bin/sh
set -eu
root=$(CDPATH= cd -- "$(dirname "$0")/.." && pwd)
app="$root/dist/macos/IOI Desk.app"
profile=${IOI_NOTARY_PROFILE:-IOIDesk}
mkdir -p "$root/work/notarization"
codesign --verify --deep --strict "$app"
signature=$(codesign -d --verbose=2 "$app" 2>&1)
printf '%s' "$signature" | grep -q 'flags=.*runtime' || { echo 'Hardened Runtime ausente.' >&2; exit 1; }
printf '%s' "$signature" | grep -q 'TeamIdentifier=5723C964BL' || { echo 'Equipe de assinatura inesperada.' >&2; exit 1; }
# Read credentials through the macOS Keychain only. Never accept passwords in argv.
xcrun notarytool history --keychain-profile "$profile" --output-format json > "$root/work/notarization/history.json"
archive="$root/work/notarization/IOIDesk.zip"
ditto -c -k --sequesterRsrc --keepParent "$app" "$archive"
if [ "${IOI_NOTARY_WAIT:-0}" = 1 ]; then
  xcrun notarytool submit "$archive" --keychain-profile "$profile" --wait --timeout 20m --output-format json > "$root/work/notarization/submission.json"
else
  xcrun notarytool submit "$archive" --keychain-profile "$profile" --output-format json > "$root/work/notarization/submission.json"
fi
echo "Enviado. Consulte work/notarization/submission.json; não instalado."
