#!/bin/bash
# build-release-packages.sh - Build V0.1B Release Packages
# Usage: ./scripts/build-release-packages.sh

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
WIN_PROJECT="src/AnomaliImportTool.UI/AnomaliImportTool.UI.csproj"
UNO_PROJECT="src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/AnomaliImportTool.Uno.csproj"

# Helper functions
print_header() {
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}========================================${NC}"
}

print_step() {
    echo -e "${GREEN}🔧 $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

# Check prerequisites
check_prerequisites() {
    print_step "Checking prerequisites..."
    
    # Check .NET 8 SDK
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK not found. Please install .NET 8 SDK."
        exit 1
    fi
    
    # Check if projects exist
    if [[ ! -f "$WIN_PROJECT" ]]; then
        print_error "Windows project file not found: $WIN_PROJECT"
        exit 1
    fi
    
    if [[ ! -f "$UNO_PROJECT" ]]; then
        print_error "Uno project file not found: $UNO_PROJECT"
        exit 1
    fi
    
    print_success "Prerequisites check passed"
}

# Clean and setup
setup_build_environment() {
    print_step "Setting up build environment..."
    rm -rf "$DIST_DIR"
    mkdir -p "$DIST_DIR"
    print_success "Build environment ready"
}

# Build Windows with WinUI project
build_windows() {
    print_header "🪟 Building Windows Release"
    
    local platforms=("win-x64" "win-x86" "win-arm64")
    
    for platform in "${platforms[@]}"; do
        print_step "Building Windows $platform..."
        local output_dir="$DIST_DIR/windows-$platform"
        
        if dotnet publish "$WIN_PROJECT" \
            --configuration Release \
            --runtime "$platform" \
            --framework net8.0-windows10.0.19041.0 \
            --output "$output_dir" \
            --self-contained true \
            -p:PublishSingleFile=true \
            -p:PublishTrimmed=false \
            -p:IncludeNativeLibrariesForSelfExtract=true \
            --verbosity minimal; then
            
            print_success "Windows $platform build completed"
            
            # Create package
            local package_name="AnomaliImportTool-v$VERSION-windows-$platform"
            cd "$DIST_DIR"
            zip -r "$package_name.zip" "windows-$platform/"
            cd - > /dev/null
            
            print_success "Package created: $package_name.zip"
        else
            print_error "Windows $platform build failed"
        fi
    done
}

# Build cross-platform with Uno project
build_cross_platform() {
    print_header "🌍 Building Cross-Platform Releases"
    
    # Linux builds
    print_step "Building Linux x64..."
    local output_dir="$DIST_DIR/linux-x64"
    
    if dotnet publish "$UNO_PROJECT" \
        --configuration Release \
        --runtime linux-x64 \
        --framework net8.0-desktop \
        --output "$output_dir" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:PublishTrimmed=true \
        --verbosity minimal; then
        
        print_success "Linux x64 build completed"
        
        # Create package
        local package_name="AnomaliImportTool-v$VERSION-linux-x64"
        cd "$DIST_DIR"
        tar -czf "$package_name.tar.gz" "linux-x64/"
        cd - > /dev/null
        
        print_success "Package created: $package_name.tar.gz"
    else
        print_error "Linux x64 build failed"
    fi
    
    # macOS builds
    local macos_platforms=("osx-x64" "osx-arm64")
    
    for platform in "${macos_platforms[@]}"; do
        print_step "Building macOS $platform..."
        local output_dir="$DIST_DIR/macos-$platform"
        
        if dotnet publish "$UNO_PROJECT" \
            --configuration Release \
            --runtime "$platform" \
            --framework net8.0-desktop \
            --output "$output_dir" \
            --self-contained true \
            -p:PublishSingleFile=true \
            -p:PublishTrimmed=true \
            --verbosity minimal; then
            
            print_success "macOS $platform build completed"
            
            # Create package
            local package_name="AnomaliImportTool-v$VERSION-macos-$platform"
            cd "$DIST_DIR"
            tar -czf "$package_name.tar.gz" "macos-$platform/"
            cd - > /dev/null
            
            print_success "Package created: $package_name.tar.gz"
        else
            print_error "macOS $platform build failed"
        fi
    done
}

# Build WebAssembly
build_webassembly() {
    print_header "🌐 Building WebAssembly Release"
    
    print_step "Building WebAssembly..."
    local output_dir="$DIST_DIR/webassembly"
    
    if dotnet publish "$UNO_PROJECT" \
        --configuration Release \
        --framework net8.0-browserwasm \
        --output "$output_dir" \
        --verbosity minimal; then
        
        print_success "WebAssembly build completed"
        
        # Create package
        local package_name="AnomaliImportTool-v$VERSION-webassembly"
        cd "$DIST_DIR"
        zip -r "$package_name.zip" "webassembly/"
        cd - > /dev/null
        
        print_success "Package created: $package_name.zip"
    else
        print_error "WebAssembly build failed"
    fi
}

# Create release summary
create_release_summary() {
    print_step "Creating release summary..."
    
    local summary_file="$DIST_DIR/RELEASE_SUMMARY.md"
    cat > "$summary_file" << EOF
# Anomali Import Tool V$VERSION Release Packages

**Build Date**: $(date)
**Version**: $VERSION

## Available Packages

### Windows (WinUI 3)
- \`AnomaliImportTool-v$VERSION-windows-win-x64.zip\` - Windows x64
- \`AnomaliImportTool-v$VERSION-windows-win-x86.zip\` - Windows x86
- \`AnomaliImportTool-v$VERSION-windows-win-arm64.zip\` - Windows ARM64

### Cross-Platform (Uno Platform)
- \`AnomaliImportTool-v$VERSION-linux-x64.tar.gz\` - Linux x64
- \`AnomaliImportTool-v$VERSION-macos-osx-x64.tar.gz\` - macOS Intel
- \`AnomaliImportTool-v$VERSION-macos-osx-arm64.tar.gz\` - macOS Apple Silicon

### Web Deployment
- \`AnomaliImportTool-v$VERSION-webassembly.zip\` - Browser WebAssembly

## Installation Instructions

### Windows
1. Download the appropriate Windows package for your architecture
2. Extract the ZIP file to your desired location
3. Run \`AnomaliImportTool.UI.exe\`

### Linux/macOS
1. Download the appropriate package for your platform
2. Extract: \`tar -xzf AnomaliImportTool-v$VERSION-[platform].tar.gz\`
3. Run: \`./AnomaliImportTool.Uno\`

### WebAssembly
1. Download the WebAssembly package
2. Extract to a web server directory
3. Serve the \`wwwroot\` folder via HTTP/HTTPS

## System Requirements

- **Windows**: Windows 10 version 19041.0 or later
- **Linux**: Modern Linux distribution with .NET 8.0 runtime
- **macOS**: macOS 10.15 (Catalina) or later
- **Web**: Modern browser with WebAssembly support

## Features Included

- Document processing (PDF, Excel, Word)
- Template management with CRUD operations
- Anomali ThreatStream API integration
- Security and compliance features
- Cross-platform compatibility
- Zero-installation deployment

EOF

    print_success "Release summary created: $summary_file"
}

# Main execution
main() {
    print_header "🚀 Building Anomali Import Tool V$VERSION Release Packages"
    
    check_prerequisites
    setup_build_environment
    
    # Build all platforms
    build_windows
    build_cross_platform
    build_webassembly
    
    create_release_summary
    
    print_header "🎉 Build Complete!"
    print_success "Release packages available in: $DIST_DIR"
    
    # List created packages
    echo ""
    print_step "Created packages:"
    find "$DIST_DIR" -name "*.zip" -o -name "*.tar.gz" | sort | while read -r package; do
        local size=$(du -h "$package" | cut -f1)
        echo "   📦 $(basename "$package") ($size)"
    done
}

# Run main function
main "$@" 