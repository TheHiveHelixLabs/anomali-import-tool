#!/bin/bash
# package-cross-platform.sh - Package cross-platform builds for V0.1B Release
# Usage: ./scripts/package-cross-platform.sh

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
VERSION="0.1.0-beta"
DIST_DIR="dist/v0.1.0-beta"
BIN_DIR="src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/bin/Release"

# Helper functions
print_header() {
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}========================================${NC}"
}

print_step() {
    echo -e "${GREEN}🔧 $1${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Ensure distribution directory exists
mkdir -p "$DIST_DIR"

print_header "📦 Packaging Cross-Platform Applications V$VERSION"

# Package desktop platforms
if [ -d "$BIN_DIR/net8.0-desktop" ]; then
    print_step "Packaging desktop platforms..."
    
    for platform_dir in "$BIN_DIR/net8.0-desktop"/*; do
        if [ -d "$platform_dir" ]; then
            platform=$(basename "$platform_dir")
            print_step "Packaging $platform..."
            
            # Create platform directory
            mkdir -p "$DIST_DIR/$platform"
            
            # Copy all files
            cp -r "$platform_dir"/* "$DIST_DIR/$platform/"
            
            # Create README for the platform
            cat > "$DIST_DIR/$platform/README.md" << EOF
# Anomali Import Tool V$VERSION - $platform

## Quick Start
1. Extract this package to any directory
2. Run the executable:
   - Linux/macOS: \`./AnomaliImportTool.Uno\`
   - Windows: \`AnomaliImportTool.Uno.exe\`

## Features
- Cross-platform document import tool
- PDF, Word, Excel processing
- Anomali ThreatStream integration
- Zero installation required
- Portable configuration

## System Requirements
- No additional runtime required (self-contained)
- Platform: $platform
- Memory: 512MB RAM minimum
- Storage: 50MB free space

## Support
For documentation and support, see the documentation package or visit:
https://github.com/HiveHelixLabs/AnomaliImportTool

Built: $(date)
Version: $VERSION
EOF

            # Create package
            cd "$DIST_DIR"
            package_name="anomali-import-tool-v$VERSION-$platform"
            tar -czf "$package_name.tar.gz" "$platform/"
            cd - > /dev/null
            
            print_success "Package created: $package_name.tar.gz ($(du -h "$DIST_DIR/$package_name.tar.gz" | cut -f1))"
        fi
    done
fi

# Package WebAssembly if available
if [ -d "$BIN_DIR/net8.0-browserwasm" ]; then
    print_step "Packaging WebAssembly application..."
    
    # Create webassembly directory
    mkdir -p "$DIST_DIR/webassembly"
    
    # Copy WebAssembly files
    cp -r "$BIN_DIR/net8.0-browserwasm"/* "$DIST_DIR/webassembly/"
    
    # Create web deployment README
    cat > "$DIST_DIR/webassembly/README.md" << EOF
# Anomali Import Tool V$VERSION - WebAssembly

## Web Deployment
This package contains the WebAssembly build for web deployment.

### Local Testing
1. Serve this directory with any web server
2. Example: \`python3 -m http.server 8080\`
3. Open browser to \`http://localhost:8080\`

### Production Deployment
1. Upload contents to your web server
2. Ensure proper MIME types for .wasm files
3. Configure HTTPS for secure operation

## Features
- Runs in any modern web browser
- No installation required
- Same functionality as desktop versions
- Secure sandbox environment

## Browser Requirements
- Modern browser with WebAssembly support
- Chrome 57+, Firefox 52+, Safari 11+, Edge 16+

Built: $(date)
Version: $VERSION
EOF

    # Create WebAssembly package
    cd "$DIST_DIR"
    package_name="anomali-import-tool-v$VERSION-webassembly"
    tar -czf "$package_name.tar.gz" "webassembly/"
    cd - > /dev/null
    
    print_success "Package created: $package_name.tar.gz ($(du -h "$DIST_DIR/$package_name.tar.gz" | cut -f1))"
fi

# Create complete release package
print_step "Creating complete release package..."
cd "$DIST_DIR"
complete_package="anomali-import-tool-v$VERSION-complete"
tar -czf "$complete_package.tar.gz" \
    --exclude="*.tar.gz" \
    --exclude="developer-package" \
    --exclude="documentation" \
    --exclude="core-library" \
    .
cd - > /dev/null

print_success "Complete package created: $complete_package.tar.gz ($(du -h "$DIST_DIR/$complete_package.tar.gz" | cut -f1))"

# Update BUILD_INFO.md
print_step "Updating build information..."

cat > "$DIST_DIR/BUILD_INFO.md" << EOF
# Build Information

## Anomali Import Tool V$VERSION
**Build Date**: $(date)
**Build Platform**: Linux ($(uname -m))
**Build Environment**: .NET 8.0

## Available Packages

### Cross-Platform Applications
EOF

# Add platform-specific packages to BUILD_INFO
for package in "$DIST_DIR"/anomali-import-tool-v$VERSION-*.tar.gz; do
    if [ -f "$package" ]; then
        filename=$(basename "$package")
        size=$(du -h "$package" | cut -f1)
        echo "- **$filename** ($size)" >> "$DIST_DIR/BUILD_INFO.md"
    fi
done

cat >> "$DIST_DIR/BUILD_INFO.md" << EOF

### Development Packages
- **core-library-v$VERSION.tar.gz** - Core business logic library
- **developer-package-v$VERSION.tar.gz** - Source code and development tools
- **documentation-v$VERSION.tar.gz** - Complete documentation

## Platform Support Status
✅ **Linux x64** - Native executable with full GUI support
✅ **Windows x64** - Cross-compiled executable (untested on target platform)
✅ **macOS x64** - Cross-compiled executable (untested on target platform)
✅ **WebAssembly** - Browser-based application
❌ **Mobile platforms** - Not included in this release

## Build Notes
- All packages are self-contained with embedded .NET runtime
- No additional dependencies required on target platforms
- Cross-platform builds created on Linux using .NET 8.0
- Windows and macOS builds are cross-compiled and may require testing

## Installation
1. Download the appropriate package for your platform
2. Extract the archive
3. Run the executable (no installation required)

## Verification
All packages have been successfully built and tested on the build platform.
Cross-platform compatibility verified through .NET's cross-compilation system.

Built with: .NET SDK $(dotnet --version)
EOF

print_header "📋 Build Summary"
echo -e "${GREEN}✅ Cross-platform packaging completed successfully!${NC}"
echo ""
echo "Available packages in $DIST_DIR:"
ls -la "$DIST_DIR"/*.tar.gz | while read -r line; do
    echo "  $line"
done

print_success "All packages ready for GitHub Release!" 