#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
if [[ "$(uname -s)" != Darwin ]]; then echo 'This build requires macOS and Xcode Command Line Tools.' >&2; exit 1; fi
xcrun --find swift >/dev/null
swift test
stamp="$(date +%Y%m%d%H%M%S)-$$"
output="$PWD/dist/$stamp"
mkdir -p "$output/ROW.app/Contents/MacOS" "$output/ROW.app/Contents/Resources"
for arch in arm64 x86_64; do
  swift build -c release --arch "$arch" --product ROW
  bin="$(swift build -c release --arch "$arch" --show-bin-path)"
  cp "$bin/ROW" "$output/ROW-$arch"
done
lipo -create "$output/ROW-arm64" "$output/ROW-x86_64" -output "$output/ROW.app/Contents/MacOS/ROW"
chmod +x "$output/ROW.app/Contents/MacOS/ROW"
cat > "$output/ROW.app/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleExecutable</key><string>ROW</string>
<key>CFBundleIdentifier</key><string>com.johncao3.row</string>
<key>CFBundleName</key><string>ROW</string>
<key>CFBundleIconFile</key><string>ROW</string>
<key>CFBundleDisplayName</key><string>ROW</string>
<key>CFBundleVersion</key><string>106</string>
<key>CFBundleShortVersionString</key><string>1.6.0</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>LSMinimumSystemVersion</key><string>13.0</string>
<key>LSUIElement</key><true/>
<key>NSHighResolutionCapable</key><true/>
</dict></plist>
PLIST
if [[ -f ../assets/ROW.png ]]; then
  mkdir -p "$output/ROW.iconset"
  for size in 16 32 128 256 512; do
    sips -z "$size" "$size" ../assets/ROW.png --out "$output/ROW.iconset/icon_${size}x${size}.png" >/dev/null
    double=$((size * 2))
    sips -z "$double" "$double" ../assets/ROW.png --out "$output/ROW.iconset/icon_${size}x${size}@2x.png" >/dev/null
  done
  iconutil -c icns "$output/ROW.iconset" -o "$output/ROW.app/Contents/Resources/ROW.icns"
fi
# Local ad-hoc signature only. This is NOT a Developer ID signature or notarization.
codesign --force --sign - "$output/ROW.app"
codesign --verify --deep --strict "$output/ROW.app"
plutil -lint "$output/ROW.app/Contents/Info.plist"
lipo "$output/ROW.app/Contents/MacOS/ROW" -verify_arch arm64 x86_64
cp README.md "$output/README.md"
ditto -c -k --sequesterRsrc --keepParent "$output/ROW.app" "$output/ROW-1.06-macOS-preview.zip"
(cd "$output" && shasum -a 256 ROW-1.06-macOS-preview.zip > SHA256SUMS.txt)
printf 'Built local, ad-hoc signed, NOT notarized preview:\n%s\n' "$output/ROW-1.06-macOS-preview.zip"
