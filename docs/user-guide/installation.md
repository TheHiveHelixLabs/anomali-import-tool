# 💻 Installation Guide

This guide covers installation of the Anomali Import Tool across all supported platforms. Choose your platform below for detailed instructions.

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

## 🪟 Windows Installation

### Method 1: Installer (Recommended)

1. **Download the Installer**
   ```
   File: AnomaliImportTool-Setup-1.0.0.exe
   Size: ~35MB
   Format: NSIS Installer
   ```

2. **Run the Installer**
   - Double-click the downloaded `.exe` file
   - Click **"Yes"** if prompted by Windows Defender
   - Follow the installation wizard:
     - ✅ Accept license agreement
     - ✅ Choose installation directory (default: `C:\Program Files\Anomali Import Tool`)
     - ✅ Create desktop shortcut (recommended)
     - ✅ Add to Start Menu (recommended)

3. **Launch the Application**
   - From Start Menu: **Start > Anomali Import Tool**
   - From Desktop: Double-click the desktop shortcut
   - From Run: Press `Win+R`, type `anomali-import-tool`

### Method 2: MSI Package (Enterprise)

For enterprise deployments with Group Policy:

```powershell
# Silent installation
msiexec /i AnomaliImportTool-1.0.0.msi /quiet /norestart

# Installation with logging
msiexec /i AnomaliImportTool-1.0.0.msi /quiet /l*v install.log

# Custom installation directory
msiexec /i AnomaliImportTool-1.0.0.msi INSTALLDIR="C:\Tools\AnomaliImportTool" /quiet
```

### Method 3: Portable Version

For USB drives or network shares:

1. **Download Portable**
   ```
   File: AnomaliImportTool-1.0.0-win-x64.exe
   Size: ~33MB
   Format: Self-contained executable
   ```

2. **Run Directly**
   - No installation required
   - Double-click to run
   - All settings stored in same directory

### Method 4: Microsoft Store

1. **Search the Store**
   - Open Microsoft Store
   - Search for "Anomali Import Tool"
   - Click **"Get"** to install

2. **Launch from Start Menu**
   - Application appears in Start Menu automatically

### Method 5: Package Managers

```powershell
# Chocolatey (when available)
choco install anomali-import-tool

# Winget (when available)
winget install HiveHelixLabs.AnomaliImportTool

# Scoop (when available)
scoop bucket add extras
scoop install anomali-import-tool
```

## 🍎 macOS Installation

### Method 1: DMG Installer (Recommended)

1. **Download the DMG**
   ```
   File: AnomaliImportTool-1.0.0.dmg
   Size: ~32MB
   Format: Apple Disk Image
   ```

2. **Install the Application**
   - Double-click the downloaded `.dmg` file
   - Drag **"Anomali Import Tool.app"** to the **Applications** folder
   - Eject the disk image when complete

3. **Launch the Application**
   - From Applications: Open **Applications** folder, double-click **Anomali Import Tool**
   - From Launchpad: Click **Launchpad**, find **Anomali Import Tool**
   - From Spotlight: Press `Cmd+Space`, type "Anomali"

### Method 2: Homebrew

```bash
# Add the Hive Helix Labs tap (when available)
brew tap hivehelixlabs/tools

# Install the application
brew install --cask anomali-import-tool

# Launch from terminal
open -a "Anomali Import Tool"
```

### Method 3: Mac App Store

1. **Search the App Store**
   - Open App Store
   - Search for "Anomali Import Tool"
   - Click **"Get"** to install

2. **Launch from Applications**
   - Application appears in Applications folder automatically

### Method 4: Portable Version

For network shares or external drives:

1. **Download Portable**
   ```
   File: AnomaliImportTool-1.0.0-osx.app.zip
   Size: ~30MB
   Format: Portable app bundle
   ```

2. **Extract and Run**
   - Unzip the downloaded file
   - Double-click **"Anomali Import Tool.app"**
   - No installation required

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

## 🐧 Linux Installation

### Method 1: DEB Package (Ubuntu/Debian)

1. **Download the Package**
   ```
   File: anomali-import-tool_1.0.0_amd64.deb
   Size: ~9.3MB
   Format: Debian package
   ```

2. **Install via APT**
   ```bash
   # Download and install
   sudo dpkg -i anomali-import-tool_1.0.0_amd64.deb
   
   # Install any missing dependencies
   sudo apt-get install -f
   ```

3. **Install via Software Center**
   - Double-click the `.deb` file
   - Click **"Install"** in the Software Center
   - Enter your password when prompted

4. **Launch the Application**
   ```bash
   # From terminal
   anomali-import-tool
   
   # From applications menu
   # Look for "Anomali Import Tool" in your applications
   ```

### Method 2: AppImage (Universal)

1. **Download AppImage**
   ```
   File: AnomaliImportTool-1.0.0-x86_64.AppImage
   Size: ~35MB
   Format: Portable Linux application
   ```

2. **Make Executable and Run**
   ```bash
   # Make executable
   chmod +x AnomaliImportTool-1.0.0-x86_64.AppImage
   
   # Run directly
   ./AnomaliImportTool-1.0.0-x86_64.AppImage
   ```

3. **Optional: Integrate with Desktop**
   ```bash
   # Move to /opt for system-wide access
   sudo mv AnomaliImportTool-1.0.0-x86_64.AppImage /opt/
   
   # Create desktop entry
   cat > ~/.local/share/applications/anomali-import-tool.desktop << EOF
   [Desktop Entry]
   Name=Anomali Import Tool
   Exec=/opt/AnomaliImportTool-1.0.0-x86_64.AppImage
   Icon=anomali-import-tool
   Type=Application
   Categories=Office;Security;
   EOF
   ```

### Method 3: Snap Package

```bash
# Install from Snap Store (when available)
sudo snap install anomali-import-tool

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

## 🌐 Web Version

### Access the Web Application

1. **Navigate to Web URL**
   ```
   URL: https://your-deployment-url.com
   Browser: Chrome 90+, Firefox 88+, Safari 14+, Edge 90+
   ```

2. **Progressive Web App Installation**
   - Click the **"Install"** button in the address bar
   - Or go to browser menu > **"Install Anomali Import Tool"**
   - Creates a desktop app-like experience

3. **Offline Support**
   - The web version works offline for most features
   - Document processing available without internet
   - Sync occurs when connection is restored

### Browser Requirements

| Browser | Minimum Version | Recommended |
|---------|----------------|-------------|
| **Chrome** | 90 | 100+ |
| **Firefox** | 88 | 95+ |
| **Safari** | 14 | 15+ |
| **Edge** | 90 | 100+ |

## 🔧 Post-Installation Setup

After installation on any platform:

### 1. First Launch Configuration

```
✅ Accept terms and conditions
✅ Choose data storage location (portable vs. system)
✅ Configure basic security settings
✅ Set up Anomali ThreatStream connection
```

### 2. Verify Installation

```bash
# Check version
anomali-import-tool --version

# Test basic functionality
anomali-import-tool --test-config

# View help
anomali-import-tool --help
```

### 3. Optional: Command Line Tools

For power users, install command line utilities:

```bash
# Add to PATH (varies by platform)
# Windows: Installer handles this automatically
# macOS: ln -s "/Applications/Anomali Import Tool.app/Contents/MacOS/anomali-import-tool" /usr/local/bin/
# Linux: Already in PATH with package installation
```

## 🗑️ Uninstallation

### Windows
```powershell
# Via Add/Remove Programs
appwiz.cpl

# Via MSI (if installed via MSI)
msiexec /x AnomaliImportTool-1.0.0.msi /quiet

# Via Chocolatey
choco uninstall anomali-import-tool

# Via Winget
winget uninstall HiveHelixLabs.AnomaliImportTool
```

### macOS
```bash
# Delete application
rm -rf "/Applications/Anomali Import Tool.app"

# Remove user data (optional)
rm -rf "~/Library/Application Support/AnomaliImportTool"

# Via Homebrew
brew uninstall --cask anomali-import-tool
```

### Linux
```bash
# Via APT
sudo apt remove anomali-import-tool

# AppImage
rm AnomaliImportTool-1.0.0-x86_64.AppImage

# Snap
sudo snap remove anomali-import-tool

# Flatpak
flatpak uninstall com.hivehelixlabs.AnomaliImportTool
```

## 🚨 Troubleshooting Installation Issues

### Common Issues

#### Windows
```
❌ "Windows Defender blocked this app"
✅ Click "More info" → "Run anyway" (safe to proceed)

❌ "Installation failed"
✅ Run installer as Administrator

❌ ".NET runtime missing"
✅ Download standalone version (includes .NET)
```

#### macOS
```
❌ "App is damaged and can't be opened"
✅ Download again or check security settings

❌ "Permission denied"
✅ Right-click app → Open → Confirm

❌ "Apple cannot check for malicious software"
✅ System Preferences → Security → Allow
```

#### Linux
```
❌ "Package dependency issues"
✅ sudo apt-get install -f

❌ "AppImage won't run"
✅ chmod +x filename.AppImage

❌ "Missing libraries"
✅ Install: libglib2.0-0 libgtk-3-0 libnss3
```

## 📞 Installation Support

Need help with installation?

- **Installation Issues**: [support@hivehelixlabs.com](mailto:support@hivehelixlabs.com)
- **Platform-Specific Help**: [Community Forum](https://github.com/HiveHelixLabs/AnomaliImportTool/discussions)
- **Enterprise Deployment**: [enterprise@hivehelixlabs.com](mailto:enterprise@hivehelixlabs.com)

---

**Next Step**: Once installed, continue with the [**Quick Start Guide**](quick-start.md) to configure your first import! 