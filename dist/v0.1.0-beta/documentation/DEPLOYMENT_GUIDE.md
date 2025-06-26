# 🚀 Multi-Platform Deployment Guide

## Overview

Thanks to the successful Uno Platform migration, the Anomali Import Tool can now be deployed as **standalone portable applications** across all major operating systems. This guide covers creating distributable binaries for Linux (AppImages), macOS (app bundles), and Windows (executables) that require **NO INSTALLATION**.

---

## 📦 **Supported Deployment Targets**

| Platform | Package Format | Distribution Method | Status |
|----------|---------------|-------------------|---------|
| **Linux** | `AppImage` | Universal portable binary | ✅ Ready |
| **Linux** | `.tar.gz` | Compressed standalone application | ✅ Ready |
| **macOS** | `.app` bundle | Portable app bundle | ✅ Ready |
| **macOS** | `.zip` archive | Compressed portable app | ✅ Ready |
| **Windows** | `.exe` executable | Standalone self-contained binary | ✅ Ready |
| **Windows** | `.zip` archive | Compressed portable application | ✅ Ready |
| **Web** | WebAssembly | Browser-based access (no download) | ✅ Ready |

---

## 🏗️ **Build Prerequisites**

### Required Tools
```bash
# .NET 8 SDK
dotnet --version  # Should be 8.0.0 or later

# Platform-specific tools
sudo apt install dpkg-dev fakeroot  # Linux .deb packaging
brew install create-dmg             # macOS .dmg creation (on macOS)
```

### Runtime Identifiers
```xml
<!-- Supported RIDs for cross-platform deployment -->
<RuntimeIdentifiers>
  linux-x64;linux-arm64;
  osx-x64;osx-arm64;
  win-x64;win-arm64
</RuntimeIdentifiers>
```

---

## 🐧 **Linux Deployment**

### 1. Build Linux Binary
```bash
# Build self-contained Linux application
dotnet publish src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/AnomaliImportTool.Uno.csproj \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained true \
  --output ./dist/linux-x64 \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true
```

### 2. Create .deb Package
```bash
# Create Debian package structure
mkdir -p dist/deb/anomali-import-tool/DEBIAN
mkdir -p dist/deb/anomali-import-tool/usr/bin
mkdir -p dist/deb/anomali-import-tool/usr/share/applications
mkdir -p dist/deb/anomali-import-tool/usr/share/pixmaps

# Copy binary
cp dist/linux-x64/AnomaliImportTool.Uno dist/deb/anomali-import-tool/usr/bin/anomali-import-tool
chmod +x dist/deb/anomali-import-tool/usr/bin/anomali-import-tool

# Create control file
cat > dist/deb/anomali-import-tool/DEBIAN/control << EOF
Package: anomali-import-tool
Version: 1.0.0
Section: utils
Priority: optional
Architecture: amd64
Depends: libc6, libgcc-s1, libstdc++6
Maintainer: Hive Helix Labs <contact@hivehelixlabs.com>
Description: Anomali Threat Bulletin Import Tool
 Professional security document import tool with Living Workspace interface.
 Cross-platform application for processing and importing threat intelligence
 documents into Anomali ThreatStream platform.
EOF

# Create desktop entry
cat > dist/deb/anomali-import-tool/usr/share/applications/anomali-import-tool.desktop << EOF
[Desktop Entry]
Version=1.0
Type=Application
Name=Anomali Import Tool
Comment=Professional security document import tool
Icon=anomali-import-tool
Exec=anomali-import-tool
Categories=Office;Security;
Terminal=false
StartupWMClass=AnomaliImportTool.Uno
EOF

# Build .deb package
dpkg-deb --build dist/deb/anomali-import-tool
mv dist/deb/anomali-import-tool.deb dist/anomali-import-tool_1.0.0_amd64.deb
```

### 3. Create AppImage (Universal Linux)
```bash
# Download AppImage tools
wget -O appimagetool https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage
chmod +x appimagetool

# Create AppDir structure
mkdir -p dist/AppDir/usr/bin
mkdir -p dist/AppDir/usr/share/applications
mkdir -p dist/AppDir/usr/share/pixmaps

# Copy files
cp dist/linux-x64/AnomaliImportTool.Uno dist/AppDir/usr/bin/
cp dist/deb/anomali-import-tool/usr/share/applications/anomali-import-tool.desktop dist/AppDir/
cp dist/deb/anomali-import-tool/usr/share/applications/anomali-import-tool.desktop dist/AppDir/usr/share/applications/

# Create AppRun script
cat > dist/AppDir/AppRun << 'EOF'
#!/bin/bash
SELF=$(readlink -f "$0")
HERE=${SELF%/*}
export PATH="${HERE}/usr/bin/:${PATH}"
cd "${HERE}/usr/bin"
exec ./AnomaliImportTool.Uno "$@"
EOF
chmod +x dist/AppDir/AppRun

# Build AppImage
./appimagetool dist/AppDir dist/AnomaliImportTool-1.0.0-x86_64.AppImage
```

---

## 🍎 **macOS Deployment**

### 1. Build macOS Binary
```bash
# Build for Intel Macs
dotnet publish src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/AnomaliImportTool.Uno.csproj \
  --configuration Release \
  --runtime osx-x64 \
  --self-contained true \
  --output ./dist/osx-x64 \
  -p:PublishSingleFile=true

# Build for Apple Silicon Macs
dotnet publish src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/AnomaliImportTool.Uno.csproj \
  --configuration Release \
  --runtime osx-arm64 \
  --self-contained true \
  --output ./dist/osx-arm64 \
  -p:PublishSingleFile=true
```

### 2. Create .app Bundle
```bash
# Create universal app bundle
mkdir -p "dist/Anomali Import Tool.app/Contents/MacOS"
mkdir -p "dist/Anomali Import Tool.app/Contents/Resources"

# Create Info.plist
cat > "dist/Anomali Import Tool.app/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>AnomaliImportTool.Uno</string>
    <key>CFBundleIdentifier</key>
    <string>com.hivehelixlabs.anomaliimporttool</string>
    <key>CFBundleName</key>
    <string>Anomali Import Tool</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSHumanReadableCopyright</key>
    <string>© 2024 Hive Helix Labs. All rights reserved.</string>
</dict>
</plist>
EOF

# Create universal binary using lipo (on macOS)
if command -v lipo &> /dev/null; then
    lipo -create \
        dist/osx-x64/AnomaliImportTool.Uno \
        dist/osx-arm64/AnomaliImportTool.Uno \
        -output "dist/Anomali Import Tool.app/Contents/MacOS/AnomaliImportTool.Uno"
else
    # Fallback to x64 binary if lipo not available
    cp dist/osx-x64/AnomaliImportTool.Uno "dist/Anomali Import Tool.app/Contents/MacOS/"
fi

chmod +x "dist/Anomali Import Tool.app/Contents/MacOS/AnomaliImportTool.Uno"
```

### 3. Create .dmg Installer
```bash
# Create DMG (requires macOS)
if command -v create-dmg &> /dev/null; then
    create-dmg \
        --volname "Anomali Import Tool" \
        --volicon "assets/icon.icns" \
        --window-pos 200 120 \
        --window-size 600 400 \
        --icon-size 100 \
        --icon "Anomali Import Tool.app" 175 120 \
        --hide-extension "Anomali Import Tool.app" \
        --app-drop-link 425 120 \
        "dist/AnomaliImportTool-1.0.0.dmg" \
        "dist/Anomali Import Tool.app"
fi
```

---

## 🪟 **Windows Deployment**

### 1. Build Windows Binary
```bash
# Build self-contained Windows application
dotnet publish src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/AnomaliImportTool.Uno.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  --output ./dist/win-x64 \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true

# Build ARM64 version for newer Windows devices
dotnet publish src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/AnomaliImportTool.Uno.csproj \
  --configuration Release \
  --runtime win-arm64 \
  --self-contained true \
  --output ./dist/win-arm64 \
  -p:PublishSingleFile=true
```

### 2. Create Windows Installer (.exe)
```bash
# Using NSIS (requires Windows or Wine)
# First, create installer script
cat > dist/installer.nsi << 'EOF'
!define APPNAME "Anomali Import Tool"
!define COMPANYNAME "Hive Helix Labs"
!define DESCRIPTION "Professional security document import tool"
!define VERSIONMAJOR 1
!define VERSIONMINOR 0
!define VERSIONBUILD 0

RequestExecutionLevel admin
InstallDir "$PROGRAMFILES64\${COMPANYNAME}\${APPNAME}"
Name "${APPNAME}"
Icon "assets\icon.ico"
outFile "dist\AnomaliImportTool-Setup-1.0.0.exe"

Section "install"
    SetOutPath $INSTDIR
    File "dist\win-x64\AnomaliImportTool.Uno.exe"
    
    # Create uninstaller
    WriteUninstaller "$INSTDIR\uninstall.exe"
    
    # Create start menu shortcut
    CreateDirectory "$SMPROGRAMS\${COMPANYNAME}"
    CreateShortcut "$SMPROGRAMS\${COMPANYNAME}\${APPNAME}.lnk" "$INSTDIR\AnomaliImportTool.Uno.exe"
    
    # Registry information for add/remove programs
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayName" "${APPNAME}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "UninstallString" "$\"$INSTDIR\uninstall.exe$\""
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayVersion" "${VERSIONMAJOR}.${VERSIONMINOR}.${VERSIONBUILD}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "Publisher" "${COMPANYNAME}"
SectionEnd

Section "uninstall"
    Delete "$INSTDIR\AnomaliImportTool.Uno.exe"
    Delete "$INSTDIR\uninstall.exe"
    RMDir $INSTDIR
    Delete "$SMPROGRAMS\${COMPANYNAME}\${APPNAME}.lnk"
    RMDir "$SMPROGRAMS\${COMPANYNAME}"
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"
SectionEnd
EOF

# Build installer (requires NSIS)
# makensis dist/installer.nsi
```

### 3. Create MSI Package
```bash
# Using WiX Toolset (requires Windows)
# Create Product.wxs
cat > dist/Product.wxs << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
    <Product Id="*" Name="Anomali Import Tool" Language="1033" Version="1.0.0" 
             Manufacturer="Hive Helix Labs" UpgradeCode="12345678-1234-1234-1234-123456789012">
        <Package InstallerVersion="200" Compressed="yes" InstallScope="perMachine" />
        
        <MajorUpgrade DowngradeErrorMessage="A newer version is already installed." />
        <MediaTemplate />
        
        <Feature Id="ProductFeature" Title="Anomali Import Tool" Level="1">
            <ComponentGroupRef Id="ProductComponents" />
        </Feature>
    </Product>
    
    <Fragment>
        <Directory Id="TARGETDIR" Name="SourceDir">
            <Directory Id="ProgramFiles64Folder">
                <Directory Id="INSTALLFOLDER" Name="Anomali Import Tool" />
            </Directory>
        </Directory>
    </Fragment>
    
    <Fragment>
        <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
            <Component Id="ProductComponent">
                <File Source="dist\win-x64\AnomaliImportTool.Uno.exe" />
            </Component>
        </ComponentGroup>
    </Fragment>
</Wix>
EOF

# Build MSI (requires WiX Toolset)
# candle dist/Product.wxs -out dist/Product.wixobj
# light dist/Product.wixobj -out dist/AnomaliImportTool-1.0.0.msi
```

---

## 🌐 **Web Deployment**

### Build WebAssembly Application
```bash
# Build for web deployment
dotnet publish src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/AnomaliImportTool.Uno.csproj \
  --configuration Release \
  --framework net8.0-browserwasm \
  --output ./dist/web

# The output can be hosted on any web server
# Files will be in dist/web/wwwroot/
```

---

## 🤖 **Automated Build Script**

Create a comprehensive build script for all platforms:

```bash
#!/bin/bash
# build-all.sh - Build for all platforms

set -e

echo "🚀 Building Anomali Import Tool for all platforms..."

# Clean previous builds
rm -rf dist/
mkdir -p dist

# Build Linux
echo "🐧 Building Linux binaries..."
dotnet publish src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/AnomaliImportTool.Uno.csproj \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained true \
  --output ./dist/linux-x64 \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true

# Build macOS
echo "🍎 Building macOS binaries..."
dotnet publish src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/AnomaliImportTool.Uno.csproj \
  --configuration Release \
  --runtime osx-x64 \
  --self-contained true \
  --output ./dist/osx-x64 \
  -p:PublishSingleFile=true

dotnet publish src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/AnomaliImportTool.Uno.csproj \
  --configuration Release \
  --runtime osx-arm64 \
  --self-contained true \
  --output ./dist/osx-arm64 \
  -p:PublishSingleFile=true

# Build Windows
echo "🪟 Building Windows binaries..."
dotnet publish src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/AnomaliImportTool.Uno.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  --output ./dist/win-x64 \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true

# Build WebAssembly
echo "🌐 Building WebAssembly..."
dotnet publish src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/AnomaliImportTool.Uno.csproj \
  --configuration Release \
  --framework net8.0-browserwasm \
  --output ./dist/web

echo "✅ Build complete! Binaries available in dist/ directory"
echo "📦 Next: Run packaging scripts for your target platform"
```

---

## 📋 **Running the Application**

### Linux (AppImage - No Installation)
```bash
# Make executable and run directly
chmod +x AnomaliImportTool-1.0.0-x86_64.AppImage
./AnomaliImportTool-1.0.0-x86_64.AppImage
```

### Linux (Binary Archive - No Installation)
```bash
# Extract and run
tar -xzf AnomaliImportTool-1.0.0-linux-x64.tar.gz
cd AnomaliImportTool-1.0.0-linux-x64
./AnomaliImportTool
```

### macOS (Portable App Bundle - No Installation)
```bash
# Unzip and run directly (no Applications folder needed)
unzip AnomaliImportTool-1.0.0-osx.app.zip
open "Anomali Import Tool.app"
```

### Windows (Standalone Executable - No Installation)
```powershell
# Run directly - no installation required
AnomaliImportTool-1.0.0-win-x64.exe

# Or extract from ZIP and run
Expand-Archive AnomaliImportTool-1.0.0-win-x64.zip
.\AnomaliImportTool-1.0.0-win-x64\AnomaliImportTool.exe
```

---

## 🔧 **Distribution Channels**

### Direct Download
- **GitHub Releases**: Automated builds via GitHub Actions
- **Company Website**: Direct download links
- **Enterprise Portal**: Internal distribution

### Portable Distribution
- **Direct Copy**: Copy executable/app bundle to target systems
- **File Sharing**: Distribute via network shares, email, or file transfer
- **Cloud Storage**: Share via OneDrive, Dropbox, Google Drive, etc.

### Enterprise Distribution
- **Network Shares**: Deploy to shared network locations for easy access
- **USB Distribution**: Copy to USB drives for air-gapped environments
- **Cloud Distribution**: Host on internal file servers or cloud storage

---

## 🔒 **Code Signing & Security**

### Platform Requirements
- **Windows**: Authenticode certificate for .exe/.msi
- **macOS**: Apple Developer ID for .app/.dmg notarization
- **Linux**: GPG signing for .deb packages

### Security Considerations
- All binaries are self-contained (no external dependencies)
- Trimmed assemblies reduce attack surface
- Digital signatures verify authenticity
- Regular security updates through update mechanisms

---

*🎯 Multi-platform deployment ready - From a single Uno Platform codebase to native applications across all major operating systems!* 