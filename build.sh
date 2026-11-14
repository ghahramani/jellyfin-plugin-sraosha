#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
JELLYFIN_PLUGINS_DIR="${JELLYFIN_PLUGINS_DIR:-/mnt/programming-ssd/projects/exploring/jellyfin-all/config/jellyfin/data/plugins}"
VERSION="1.0.0.0"
TARGET_DIR="${JELLYFIN_PLUGINS_DIR}/Sraosha_${VERSION}"
DIST_DIR="${SCRIPT_DIR}/dist"
BUILD_NET10="${BUILD_NET10:-false}"

echo "================================================="
echo "🛡️  Building Jellyfin.Plugin.Sraosha v${VERSION}"
echo "================================================="

if [ "${BUILD_NET10}" = "true" ]; then
    echo "Target: .NET 10.0 (Jellyfin 12.0 Preview)"
    if command -v docker &>/dev/null; then
        docker run --rm \
          -v "${SCRIPT_DIR}:/src" \
          -w /src/Jellyfin.Plugin.Sraosha \
          mcr.microsoft.com/dotnet/sdk:10.0-preview \
          dotnet build -c Release -p:NET10=true
        BUILD_OUTPUT="${SCRIPT_DIR}/Jellyfin.Plugin.Sraosha/bin/Release/net10.0"
    else
        dotnet build "${SCRIPT_DIR}/Jellyfin.Plugin.Sraosha" -c Release -p:NET10=true
        BUILD_OUTPUT="${SCRIPT_DIR}/Jellyfin.Plugin.Sraosha/bin/Release/net10.0"
    fi
else
    echo "Target: .NET 8.0 (Jellyfin 10.9 / 10.10)"
    if [ -x "$HOME/.dotnet/dotnet" ]; then
        "$HOME/.dotnet/dotnet" build "${SCRIPT_DIR}/Jellyfin.Plugin.Sraosha" -c Release
    elif [ -x "/tmp/dotnet-sdk/dotnet" ]; then
        DOTNET_ROOT="/tmp/dotnet-sdk" /tmp/dotnet-sdk/dotnet build "${SCRIPT_DIR}/Jellyfin.Plugin.Sraosha" -c Release
    elif dotnet --list-sdks 2>/dev/null | grep -q '8\.'; then
        dotnet build "${SCRIPT_DIR}/Jellyfin.Plugin.Sraosha" -c Release
    elif command -v docker &>/dev/null; then
        docker run --rm \
          -v "${SCRIPT_DIR}:/src" \
          -w /src/Jellyfin.Plugin.Sraosha \
          mcr.microsoft.com/dotnet/sdk:8.0 \
          dotnet build -c Release
    else
        dotnet build "${SCRIPT_DIR}/Jellyfin.Plugin.Sraosha" -c Release
    fi
    BUILD_OUTPUT="${SCRIPT_DIR}/Jellyfin.Plugin.Sraosha/bin/Release/net8.0"
fi

# Package distribution archive
mkdir -p "${DIST_DIR}"
PACKAGE_DIR="/tmp/sraosha-pkg"
rm -rf "${PACKAGE_DIR}"
mkdir -p "${PACKAGE_DIR}"

cp "${BUILD_OUTPUT}/Jellyfin.Plugin.Sraosha.dll" "${PACKAGE_DIR}/"
cp "${SCRIPT_DIR}/Jellyfin.Plugin.Sraosha/meta.json" "${PACKAGE_DIR}/"
cp "${SCRIPT_DIR}/Jellyfin.Plugin.Sraosha/icon.png" "${PACKAGE_DIR}/"

ZIP_NAME="Jellyfin.Plugin.Sraosha_${VERSION}.zip"
(
  cd "${PACKAGE_DIR}"
  zip -r "${DIST_DIR}/${ZIP_NAME}" ./*
)

MD5_CHECKSUM=$(md5sum "${DIST_DIR}/${ZIP_NAME}" | awk '{print $1}')
echo ""
echo "=== Build Complete ==="
echo "Archive:  ${DIST_DIR}/${ZIP_NAME}"
echo "MD5 Hash: ${MD5_CHECKSUM}"

# Optional install to local Jellyfin instance
if [ "${1:-}" = "--install" ] || [ "${INSTALL_LOCAL:-false}" = "true" ]; then
    if [ -d "${JELLYFIN_PLUGINS_DIR}" ]; then
        echo ""
        echo "=== Installing plugin into local Jellyfin ==="
        mkdir -p "${TARGET_DIR}"
        cp -r "${PACKAGE_DIR}"/* "${TARGET_DIR}/"
        echo "Installed to: ${TARGET_DIR}"
        echo "Restart Jellyfin container to load Sraosha."
    else
        echo "Local Jellyfin plugins directory not found at: ${JELLYFIN_PLUGINS_DIR}"
    fi
fi

rm -rf "${PACKAGE_DIR}"
