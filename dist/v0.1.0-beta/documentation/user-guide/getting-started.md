# 🚀 Getting Started Guide - Standalone Deployment

The Anomali Import Tool is a **completely portable, standalone application** that requires **NO INSTALLATION**. Simply download, unzip, and run directly from any location. This guide covers getting started across all supported platforms.

## 🖥️ System Requirements

### Minimum Requirements
| Platform | Version | RAM | Storage | Additional |
|----------|---------|-----|---------|------------|
| **Windows** | 10 (1903+) | 4GB | 100MB | .NET 8 (embedded) |
| **macOS** | 10.15+ | 4GB | 100MB | Intel & Apple Silicon |
| **Linux** | Ubuntu 20.04+ | 4GB | 100MB | GUI desktop environment |
| **Web** | Chrome 90+ | 2GB | - | Modern browser |

### Recommended Requirements
| Platform | Version | RAM | Storage | Additional |
|----------|---------|-----|---------|------------|
| **Windows** | 11 | 8GB | 500MB | SSD storage |
| **macOS** | 12.0+ | 8GB | 500MB | SSD storage |
| **Linux** | Ubuntu 22.04+ | 8GB | 500MB | SSD storage |
| **Web** | Chrome 100+ | 4GB | - | Stable internet |

## 🪟 Windows - Standalone Deployment

### 🚫 **NO INSTALLATION REQUIRED** - Simply Download and Run

The Anomali Import Tool is completely portable and requires no installation, administrative privileges, or system modifications.

### Method 1: Standalone Executable (Recommended)

1. **Download the Portable Application**
   ```
   File: AnomaliImportTool-1.0.0-win-x64.exe
   Size: ~33MB
   Format: Self-contained executable with embedded .NET runtime
   ```

2. **Run Directly from Any Location**
   - **USB Drives**: Copy to USB drive and run directly
   - **Network Shares**: Run from UNC paths without copying
   - **Local Folders**: Run from Downloads, Desktop, or any directory
   - **Cloud Storage**: Works from OneDrive, Dropbox, etc.

3. **Launch the Application**
   - Double-click the `.exe` file to launch instantly
   - **No system changes**: Application leaves no traces
   - **Settings**: Stored in `config/` folder next to executable
   - **Logs**: Stored in `logs/` folder next to executable

### Method 2: ZIP Archive Distribution

1. **Download the ZIP Package**
   ```
   File: AnomaliImportTool-1.0.0-win-x64.zip
   Size: ~30MB
   Format: Compressed portable application
   ```

2. **Extract and Run**
   - Extract to any folder (USB drive, network share, local directory)
   - Run `AnomaliImportTool.exe` from the extracted folder
   - All dependencies included - no external requirements

### ✅ **Enterprise Deployment Benefits**
- **Zero IT Support**: No installation processes or administrative requirements
- **Compliance Friendly**: No system modifications or registry changes
- **Easy Distribution**: Copy files to network shares or distribute via email
- **Version Control**: Multiple versions can coexist in different folders

## 🍎 macOS - Standalone Deployment

### 🚫 **NO INSTALLATION REQUIRED** - Simply Download and Run

The Anomali Import Tool is completely portable and requires no installation to the Applications folder or system modifications.

### Method 1: Portable App Bundle (Recommended)

1. **Download the Portable App**
   ```
   File: AnomaliImportTool-1.0.0-osx.app.zip
   Size: ~30MB
   Format: Self-contained macOS app bundle with embedded .NET runtime
   ```

2. **Run Directly from Any Location**
   - **External Drives**: Copy to USB drives, external SSDs, or SD cards
   - **Network Shares**: Run from AFP, SMB, or NFS mounted volumes
   - **Local Folders**: Run from Downloads, Desktop, or any directory
   - **Cloud Storage**: Works from iCloud Drive, Dropbox, Google Drive

3. **Launch the Application**
   - Unzip the file anywhere (no Applications folder needed)
   - Double-click **"Anomali Import Tool.app"** to launch instantly
   - **No system changes**: Application leaves no traces
   - **Settings**: Stored in `config/` folder inside the app bundle
   - **Logs**: Stored in `logs/` folder inside the app bundle

### Method 2: DMG Distribution (Optional)

1. **Download the DMG Package**
   ```
   File: AnomaliImportTool-1.0.0.dmg
   Size: ~32MB
   Format: Disk image with portable app
   ```

2. **Extract and Run (No Installation)**
   - Double-click the `.dmg` file to mount
   - **Optional**: Drag to Applications folder (traditional approach)
   - **Portable**: Copy "Anomali Import Tool.app" to any location
   - Run directly from any folder - no system integration required

### ✅ **Enterprise Deployment Benefits**
- **Zero IT Support**: No installation processes or admin privileges required
- **Compliance Friendly**: No system modifications or preference changes
- **Easy Distribution**: Copy app bundles to network shares or distribute via file sharing
- **Version Control**: Multiple versions can coexist in different folders

### Security Notice (macOS)

On first launch, macOS may show a security warning:

1. **Gatekeeper Warning**
   ```
   "Anomali Import Tool" cannot be opened because it is from an unidentified developer.
   ```

2. **Override (if needed)**
   - Right-click the app and select **"Open"**
   - Click **"Open"** in the confirmation dialog
   - Or go to **System Preferences > Security & Privacy** and click **"Open Anyway"**

## 🐧 Linux - Standalone Deployment

### 🚫 **NO INSTALLATION REQUIRED** - Simply Download and Run

The Anomali Import Tool is completely portable and requires no installation, package management, or root privileges.

### Method 1: AppImage Portable (Recommended)

1. **Download the Portable AppImage**
   ```
   File: AnomaliImportTool-1.0.0-x86_64.AppImage
   Size: ~35MB
   Format: Self-contained Linux application with embedded .NET runtime
   ```

2. **Run Directly from Any Location**
   ```bash
   # Make executable
   chmod +x AnomaliImportTool-1.0.0-x86_64.AppImage
   
   # Run directly
   ./AnomaliImportTool-1.0.0-x86_64.AppImage
   ```

3. **Deployment Options**
   - **USB Drives**: Copy to external storage and run on any Linux system
   - **Network Shares**: Run from NFS, CIFS/SMB mounted volumes
   - **Local Folders**: Run from home directory, Downloads, or any location
   - **Multiple Versions**: Keep different versions in separate folders

### Method 2: Binary Package Distribution

1. **Download the Standalone Binary**
   ```
   File: AnomaliImportTool-1.0.0-linux-x64.tar.gz
   Size: ~30MB
   Format: Compressed standalone application
   ```

2. **Extract and Run**
   ```bash
   # Extract anywhere
   tar -xzf AnomaliImportTool-1.0.0-linux-x64.tar.gz
   
   # Run from extracted folder
   cd AnomaliImportTool-1.0.0-linux-x64
   ./AnomaliImportTool
   ```

### ✅ **Enterprise Deployment Benefits**
- **Zero Dependencies**: No package installation or root access required
- **Universal Compatibility**: Runs on any Linux distribution with GUI support
- **Easy Distribution**: Copy files to network shares or distribute via file transfer
- **Version Control**: Multiple versions can coexist without conflicts

### Optional: Desktop Integration (No Installation)

```bash
# Create desktop entry (no sudo required)
cat > ~/.local/share/applications/anomali-import-tool.desktop << EOF
[Desktop Entry]
Name=Anomali Import Tool
Exec=/path/to/AnomaliImportTool-1.0.0-x86_64.AppImage
Icon=application-x-executable
Type=Application
Categories=Office;Security;
Comment=Portable security document import tool
EOF
```

# Launch from terminal
anomali-import-tool

# Or from applications menu
```

### Method 4: Flatpak

```bash
# Add Flathub repository (if not already added)
flatpak remote-add --if-not-exists flathub https://flathub.org/repo/flathub.flatpakrepo

# Install application (when available)
flatpak install flathub com.hivehelixlabs.AnomaliImportTool

# Launch application
flatpak run com.hivehelixlabs.AnomaliImportTool
```

### Method 5: From Source

```bash
# Clone repository
git clone https://github.com/HiveHelixLabs/AnomaliImportTool.git
cd AnomaliImportTool

# Build application
dotnet build --configuration Release

# Publish self-contained
dotnet publish src/AnomaliImportTool.Uno/AnomaliImportTool.Uno.csproj \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained true \
  --output ./dist/linux-x64

# Run application
./dist/linux-x64/AnomaliImportTool.Uno
```

## 🌐 WebAssembly Version

### 🚫 **NO INSTALLATION REQUIRED** - Browser-Based Access

The WebAssembly version runs directly in your browser with no downloads, plugins, or extensions required.

### Method 1: Direct Browser Access

1. **Navigate to Web Application**
   ```
   URL: https://your-deployment-url.com
   Browser: Chrome 90+, Firefox 88+, Safari 14+, Edge 90+
   ```

2. **Instant Access**
   - Application loads directly in browser
   - No downloads or installations required
   - Full functionality available immediately
   - Cross-platform browser compatibility

### Method 2: Progressive Web App (Optional)

1. **"Install" as PWA (Browser Feature)**
   - Click the **"Install"** button in the address bar
   - Or go to browser menu > **"Install Anomali Import Tool"**
   - Creates a desktop app-like experience (browser-managed, not system installation)

2. **PWA Benefits**
   - Runs like a native app
   - Offline functionality for document processing
   - No actual system installation - managed by browser

### Browser Requirements

| Browser | Minimum Version | Recommended |
|---------|----------------|-------------|
| **Chrome** | 90 | 100+ |
| **Firefox** | 88 | 95+ |
| **Safari** | 14 | 15+ |
| **Edge** | 90 | 100+ |

## 🔧 First Launch Setup

After downloading and running the standalone application:

### 1. First Launch Configuration

```
✅ Accept terms and conditions
✅ Choose portable data storage location (always relative to application)
✅ Configure basic security settings
✅ Set up Anomali ThreatStream connection
```

### 2. Verify Application

```bash
# Check version (from application folder)
./AnomaliImportTool --version

# Test basic functionality
./AnomaliImportTool --test-config

# View help
./AnomaliImportTool --help
```

### 3. Optional: Portable PATH Setup

For convenience, create aliases or shortcuts:

```bash
# Windows: Create batch file in PATH
echo @"%~dp0AnomaliImportTool.exe" %* > anomali.bat

# macOS/Linux: Create alias in shell profile
alias anomali="/path/to/AnomaliImportTool"

# Or create symbolic link (optional)
ln -s "/path/to/AnomaliImportTool" /usr/local/bin/anomali
```

## 🗑️ Removing the Application

Since this is a standalone portable application with no installation:

### Simple Removal Process
```bash
# Windows: Delete the folder/executable
rmdir /s AnomaliImportTool-folder
# or just delete the .exe file

# macOS: Delete the app bundle
rm -rf "Anomali Import Tool.app"

# Linux: Delete the AppImage or extracted folder
rm AnomaliImportTool-1.0.0-x86_64.AppImage
# or rm -rf AnomaliImportTool-folder
```

### Optional: Remove Configuration Data
```bash
# All platforms: Remove config folder (if preserving settings not needed)
rm -rf config/
rm -rf logs/

# Note: Configuration is stored relative to the application
# No system-wide files or registry entries to clean
```

### No System Impact
- ✅ No registry entries to remove
- ✅ No system files to clean up
- ✅ No administrative privileges required
- ✅ Complete removal by simply deleting files

## 🚨 Troubleshooting Startup Issues

### Common Issues

#### Windows
```
❌ "Windows Defender blocked this app"
✅ Click "More info" → "Run anyway" (standalone apps are safe)

❌ "Application won't start"
✅ Check if antivirus is blocking execution

❌ "Missing dependencies"
✅ Download the self-contained version (includes .NET runtime)
```

#### macOS
```
❌ "App is damaged and can't be opened"
✅ Download again or disable Gatekeeper temporarily

❌ "Permission denied"
✅ Right-click app → Open → Confirm (first run only)

❌ "Apple cannot check for malicious software"
✅ System Preferences → Security → Allow (first run only)
```

#### Linux
```
❌ "AppImage won't run"
✅ chmod +x filename.AppImage (make executable)

❌ "Missing GUI libraries"
✅ Install desktop environment packages if running on server

❌ "Permission denied"
✅ Ensure executable permissions: chmod +x AnomaliImportTool
```

## 📞 Getting Started Support

Need help getting started?

- **Application Issues**: [support@hivehelixlabs.com](mailto:support@hivehelixlabs.com)
- **Platform-Specific Help**: [Community Forum](https://github.com/HiveHelixLabs/AnomaliImportTool/discussions)
- **Enterprise Deployment**: [enterprise@hivehelixlabs.com](mailto:enterprise@hivehelixlabs.com)

---

**Next Step**: Once the application is running, continue with the [**Quick Start Guide**](quick-start.md) to configure your first import! 