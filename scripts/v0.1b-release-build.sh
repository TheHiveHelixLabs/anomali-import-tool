#!/bin/bash

# V0.1B Release Build Script
# Simplified build for initial release with compilation fixes applied

set -e

echo "🚀 Building Anomali Import Tool V0.1B Release"
echo "=============================================="

# Configuration
VERSION="0.1.0-beta"
PROJECT_DIR="/home/ash/AnomaliImportTool"
DIST_DIR="$PROJECT_DIR/dist/v0.1b"
UNO_PROJECT="$PROJECT_DIR/src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/AnomaliImportTool.Uno.csproj"
UI_PROJECT="$PROJECT_DIR/src/AnomaliImportTool.UI/AnomaliImportTool.UI.csproj"

# Create distribution directory
echo "📁 Creating distribution directory..."
rm -rf "$DIST_DIR"
mkdir -p "$DIST_DIR"

# Build Windows Desktop (using UI project which is simpler)
echo "🪟 Building Windows Desktop..."
dotnet publish "$UI_PROJECT" \
    --configuration Release \
    --runtime win-x64 \
    --self-contained true \
    --output "$DIST_DIR/windows-x64" \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true \
    --verbosity minimal

# Build Linux Desktop (using UNO desktop framework)
echo "🐧 Building Linux Desktop..."
dotnet publish "$UNO_PROJECT" \
    --configuration Release \
    --framework net8.0-desktop \
    --runtime linux-x64 \
    --self-contained true \
    --output "$DIST_DIR/linux-x64" \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true \
    --verbosity minimal

# Build macOS Desktop (using UNO desktop framework)
echo "🍎 Building macOS Desktop..."
dotnet publish "$UNO_PROJECT" \
    --configuration Release \
    --framework net8.0-desktop \
    --runtime osx-x64 \
    --self-contained true \
    --output "$DIST_DIR/macos-x64" \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true \
    --verbosity minimal

# Build WebAssembly (using UNO WASM framework)
echo "🌐 Building WebAssembly..."
dotnet publish "$UNO_PROJECT" \
    --configuration Release \
    --framework net8.0-browserwasm \
    --output "$DIST_DIR/webassembly" \
    --verbosity minimal

# Create release packages
echo "📦 Creating release packages..."

# Windows ZIP
if [ -d "$DIST_DIR/windows-x64" ]; then
    cd "$DIST_DIR"
    zip -r "AnomaliImportTool-v$VERSION-windows-x64.zip" windows-x64/
    echo "✅ Windows package: AnomaliImportTool-v$VERSION-windows-x64.zip"
fi

# Linux TAR.GZ
if [ -d "$DIST_DIR/linux-x64" ]; then
    cd "$DIST_DIR"
    tar -czf "AnomaliImportTool-v$VERSION-linux-x64.tar.gz" linux-x64/
    echo "✅ Linux package: AnomaliImportTool-v$VERSION-linux-x64.tar.gz"
fi

# macOS TAR.GZ
if [ -d "$DIST_DIR/macos-x64" ]; then
    cd "$DIST_DIR"
    tar -czf "AnomaliImportTool-v$VERSION-macos-x64.tar.gz" macos-x64/
    echo "✅ macOS package: AnomaliImportTool-v$VERSION-macos-x64.tar.gz"
fi

# WebAssembly ZIP
if [ -d "$DIST_DIR/webassembly" ]; then
    cd "$DIST_DIR"
    zip -r "AnomaliImportTool-v$VERSION-webassembly.zip" webassembly/
    echo "✅ WebAssembly package: AnomaliImportTool-v$VERSION-webassembly.zip"
fi

echo ""
echo "🎉 V0.1B Release Build Complete!"
echo "📁 Release files available in: $DIST_DIR"
echo "📊 Package Summary:"
ls -la "$DIST_DIR"/*.{zip,tar.gz} 2>/dev/null || echo "   (Package files will be listed here)"

echo ""
echo "🚀 Ready for GitHub Release!"
echo "   Version: $VERSION"
echo "   Target Platforms: Windows, Linux, macOS, WebAssembly"
echo "   Features: Core document processing, template management, Anomali API integration" 