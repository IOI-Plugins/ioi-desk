#!/bin/sh
set -eu
root=$(CDPATH= cd -- "$(dirname "$0")/.." && pwd)
cd "$root"
. "$root/tools/signing.sh"
ioi_signing_preflight
framework="$root/vendor/sparkle/Sparkle.framework"
[ -d "$framework" ] || { echo 'Run python3 tools/fetch-updater.py first.' >&2; exit 1; }
app="$root/dist/macos/IOI Desk.app"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources" "$app/Contents/Helpers" "$root/work"
python3 - "$root" <<'PY'
import json,sys,pathlib,plistlib
root=pathlib.Path(sys.argv[1]);release=json.loads((root/'release.json').read_text());updates=json.loads((root/'assets/updates.json').read_text())
(root/'src/macos/Product.swift').write_text('enum Product { static let name="IOI Desk"; static let version='+json.dumps(release['version'])+' }\n')
info=plistlib.loads((root/'assets/Info.plist').read_bytes());info.update(CFBundleShortVersionString=release['version'],CFBundleVersion=str(release['build']),CFBundleIdentifier=release['macBundleIdentifier'])
info.update(SUFeedURL=updates['macosFeed'],SUPublicEDKey=updates['publicKey'],SUEnableAutomaticChecks=True,SUAutomaticallyUpdate=True,SUEnableSystemProfiling=False,SUVerifyUpdateBeforeExtraction=True,SURequireSignedFeed=True)
(root/'dist/macos/IOI Desk.app/Contents/Info.plist').write_bytes(plistlib.dumps(info))
PY
sdk=$(xcrun --show-sdk-path)
maps="$(xcode-select -p)/usr/include/swift"
if [ -f "$maps/module.modulemap" ] && [ -f "$maps/bridging.modulemap" ]; then
  printf '// Duplicate module map masked for this build only.\n' > work/empty.modulemap
  python3 - "$maps/module.modulemap" "$root" <<'PY'
import json,sys
json.dump({'version':0,'roots':[{'type':'file','name':sys.argv[1],'external-contents':sys.argv[2]+'/work/empty.modulemap'}]},open('work/swift-overlay.json','w'))
PY
  set -- -vfsoverlay "$root/work/swift-overlay.json"
else set --; fi
swiftc -swift-version 5 -sdk "$sdk" "$@" -module-cache-path "$root/work/swift-modules" -O -F "$root/vendor/sparkle" -framework Sparkle -Xlinker -rpath -Xlinker @executable_path/../Frameworks -framework AppKit -framework ApplicationServices -framework ServiceManagement src/macos/*.swift tests/macos/*.swift -o "$app/Contents/MacOS/IOIDesk"
if [ -n "${IOI_ICON_FILE:-}" ]; then cp "$IOI_ICON_FILE" "$app/Contents/Resources/IOIDesk.icns"
else
  mkdir -p work/IOIDesk.iconset
  for size in 16 32 128 256 512; do
    sips -z "$size" "$size" assets/IOIDesk.png --out "work/IOIDesk.iconset/icon_${size}x${size}.png" >/dev/null
    double=$((size*2));sips -z "$double" "$double" assets/IOIDesk.png --out "work/IOIDesk.iconset/icon_${size}x${size}@2x.png" >/dev/null
  done
  iconutil -c icns work/IOIDesk.iconset -o "$app/Contents/Resources/IOIDesk.icns"
fi
if [ -z "${IOI_DESKFLOW_APP:-}" ] || [ ! -d "$IOI_DESKFLOW_APP" ]; then
  echo 'Informe IOI_DESKFLOW_APP com o componente Deskflow original.' >&2; exit 1
fi
ditto "$IOI_DESKFLOW_APP" "$app/Contents/Helpers/Deskflow.app"
cp release.json "$app/Contents/Resources/release.json"
for notice in LICENSE NOTICE TRADEMARKS.md; do
  cp "$root/$notice" "$app/Contents/Resources/$notice"
done
mkdir -p "$app/Contents/Resources/assets"
cp "$root/assets/BRAND.md" "$app/Contents/Resources/assets/BRAND.md"
ditto "$root/third-party" "$app/Contents/Resources/ThirdParty"
ditto "$root/src/browser" "$app/Contents/Resources/BrowserExtension"
mkdir -p "$app/Contents/Frameworks"
ditto "$framework" "$app/Contents/Frameworks/Sparkle.framework"
cp "$root/vendor/sparkle/LICENSE" "$app/Contents/Resources/ThirdParty/Sparkle-LICENSE"
signed_framework="$app/Contents/Frameworks/Sparkle.framework"
for component in "$signed_framework/Versions/B/XPCServices/Downloader.xpc" "$signed_framework/Versions/B/XPCServices/Installer.xpc" "$signed_framework/Versions/B/Autoupdate" "$signed_framework/Versions/B/Updater.app" "$signed_framework"; do
  [ ! -e "$component" ] || ioi_sign_app "$component" "$root/assets/UpdaterEntitlements.plist"
done
ioi_sign_app "$app" "$root/assets/Entitlements.plist"
"$app/Contents/MacOS/IOIDesk" --self-test
"$app/Contents/MacOS/IOIDesk" --layout-test
"$app/Contents/MacOS/IOIDesk" --migration-test
"$app/Contents/MacOS/IOIDesk" --update-test
echo "Build concluído: $app. Nenhum aplicativo instalado foi substituído."
