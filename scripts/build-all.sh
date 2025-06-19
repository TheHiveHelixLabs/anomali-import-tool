#!/bin/bash
# build-all.sh - Build Anomali Import Tool for all platforms
# Usage: ./scripts/build-all.sh [--clean] [--package]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROJECT_NAME="AnomaliImportTool.Uno"
PROJECT_PATH="src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/AnomaliImportTool.Uno.csproj"
DIST_DIR="dist"
VERSION="1.0.0"

# Parse command line arguments
CLEAN=false
PACKAGE=false
while [[ $# -gt 0 ]]; do
    case $1 in
        --clean)
            CLEAN=true
            shift
            ;;
        --package)
            PACKAGE=true
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [--clean] [--package]"
            echo "  --clean   Clean previous builds before building"
            echo "  --package Create distribution packages (.deb, .dmg, etc.)"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

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
    
    local dotnet_version=$(dotnet --version)
    if [[ ! $dotnet_version =~ ^8\. ]]; then
        print_warning "Expected .NET 8.x, found: $dotnet_version"
    fi
    
    # Check if project exists
    if [[ ! -f "$PROJECT_PATH" ]]; then
        print_error "Project file not found: $PROJECT_PATH"
        exit 1
    fi
    
    print_success "Prerequisites check passed"
}

# Clean previous builds
clean_builds() {
    if [[ $CLEAN == true ]]; then
        print_step "Cleaning previous builds..."
        rm -rf "$DIST_DIR"
        print_success "Previous builds cleaned"
    fi
}

# Create distribution directory
setup_dist_dir() {
    print_step "Setting up distribution directory..."
    mkdir -p "$DIST_DIR"
    print_success "Distribution directory ready: $DIST_DIR"
}

# Build for specific platform
build_platform() {
    local platform=$1
    local runtime=$2
    local framework=${3:-"net8.0-desktop"}
    local output_dir="$DIST_DIR/$platform"
    
    print_step "Building for $platform ($runtime)..."
    
    local publish_args=(
        "$PROJECT_PATH"
        --configuration Release
        --output "$output_dir"
        -p:PublishSingleFile=true
        -p:PublishTrimmed=true
        --verbosity minimal
    )
    
    if [[ $framework != "net8.0-browserwasm" ]]; then
        publish_args+=(--runtime "$runtime" --self-contained true)
    else
        publish_args+=(--framework "$framework")
    fi
    
    if dotnet publish "${publish_args[@]}"; then
        print_success "✅ $platform build completed"
        
        # Display binary info
        if [[ $framework != "net8.0-browserwasm" ]]; then
            local binary_path="$output_dir/$PROJECT_NAME"
            if [[ $runtime == win-* ]]; then
                binary_path="$binary_path.exe"
            fi
            
            if [[ -f "$binary_path" ]]; then
                local size=$(du -h "$binary_path" | cut -f1)
                echo "   📁 Binary: $binary_path ($size)"
            fi
        else
            echo "   🌐 WebAssembly output: $output_dir/wwwroot/"
        fi
    else
        print_error "❌ $platform build failed"
        return 1
    fi
}

# Main build function
build_all_platforms() {
    print_header "🚀 Building Anomali Import Tool v$VERSION for All Platforms"
    
    # Linux builds
    print_header "🐧 Linux Builds"
    build_platform "linux-x64" "linux-x64"
    build_platform "linux-arm64" "linux-arm64"
    
    # macOS builds  
    print_header "🍎 macOS Builds"
    build_platform "osx-x64" "osx-x64"
    build_platform "osx-arm64" "osx-arm64"
    
    # Windows builds
    print_header "🪟 Windows Builds"
    build_platform "win-x64" "win-x64"
    build_platform "win-arm64" "win-arm64"
    
    # WebAssembly build
    print_header "🌐 WebAssembly Build"
    build_platform "web" "browser-wasm" "net8.0-browserwasm"
}

# Create Linux .deb package
create_deb_package() {
    if [[ ! -d "$DIST_DIR/linux-x64" ]]; then
        print_warning "Linux x64 build not found, skipping .deb package creation"
        return 1
    fi
    
    print_step "Creating .deb package..."
    
    local deb_dir="$DIST_DIR/deb/anomali-import-tool"
    mkdir -p "$deb_dir"/{DEBIAN,usr/bin,usr/share/applications,usr/share/pixmaps}
    
    # Copy binary
    cp "$DIST_DIR/linux-x64/$PROJECT_NAME" "$deb_dir/usr/bin/anomali-import-tool"
    chmod +x "$deb_dir/usr/bin/anomali-import-tool"
    
    # Create control file
    cat > "$deb_dir/DEBIAN/control" << EOF
Package: anomali-import-tool
Version: $VERSION
Section: utils
Priority: optional
Architecture: amd64
Depends: libc6, libgcc-s1, libstdc++6
Maintainer: Hive Helix Labs <contact@hivehelixlabs.com>
Description: Anomali Threat Bulletin Import Tool
 Professional security document import tool with Living Workspace interface.
 Cross-platform application for processing and importing threat intelligence
 documents into Anomali ThreatStream platform.
 .
 Features:
  - Cross-platform compatibility (Linux, Windows, macOS, Web)
  - Professional ReactiveUI interface
  - Document processing and threat bulletin creation
  - Integration with Anomali ThreatStream
EOF
    
    # Create desktop entry
    cat > "$deb_dir/usr/share/applications/anomali-import-tool.desktop" << EOF
[Desktop Entry]
Version=1.0
Type=Application
Name=Anomali Import Tool
Comment=Professional security document import tool
Icon=anomali-import-tool
Exec=anomali-import-tool
Categories=Office;Security;Utility;
Terminal=false
StartupWMClass=AnomaliImportTool.Uno
EOF
    
    # Build package
    if command -v dpkg-deb &> /dev/null; then
        dpkg-deb --build "$deb_dir"
        mv "$DIST_DIR/deb/anomali-import-tool.deb" "$DIST_DIR/anomali-import-tool_${VERSION}_amd64.deb"
        print_success "📦 .deb package created: $DIST_DIR/anomali-import-tool_${VERSION}_amd64.deb"
    else
        print_warning "dpkg-deb not found, skipping .deb package creation"
    fi
}

# Create macOS app bundle
create_macos_app() {
    if [[ ! -d "$DIST_DIR/osx-x64" ]] && [[ ! -d "$DIST_DIR/osx-arm64" ]]; then
        print_warning "macOS builds not found, skipping .app bundle creation"
        return 1
    fi
    
    print_step "Creating macOS .app bundle..."
    
    local app_dir="$DIST_DIR/Anomali Import Tool.app"
    mkdir -p "$app_dir/Contents"/{MacOS,Resources}
    
    # Create Info.plist
    cat > "$app_dir/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>$PROJECT_NAME</string>
    <key>CFBundleIdentifier</key>
    <string>com.hivehelixlabs.anomaliimporttool</string>
    <key>CFBundleName</key>
    <string>Anomali Import Tool</string>
    <key>CFBundleVersion</key>
    <string>$VERSION</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSHumanReadableCopyright</key>
    <string>© 2024 Hive Helix Labs. All rights reserved.</string>
    <key>LSApplicationCategoryType</key>
    <string>public.app-category.productivity</string>
</dict>
</plist>
EOF
    
    # Create universal binary if both architectures are available
    if [[ -f "$DIST_DIR/osx-x64/$PROJECT_NAME" ]] && [[ -f "$DIST_DIR/osx-arm64/$PROJECT_NAME" ]] && command -v lipo &> /dev/null; then
        print_step "Creating universal binary..."
        lipo -create \
            "$DIST_DIR/osx-x64/$PROJECT_NAME" \
            "$DIST_DIR/osx-arm64/$PROJECT_NAME" \
            -output "$app_dir/Contents/MacOS/$PROJECT_NAME"
        print_success "Universal binary created"
    elif [[ -f "$DIST_DIR/osx-x64/$PROJECT_NAME" ]]; then
        cp "$DIST_DIR/osx-x64/$PROJECT_NAME" "$app_dir/Contents/MacOS/"
        print_success "x64 binary copied to app bundle"
    elif [[ -f "$DIST_DIR/osx-arm64/$PROJECT_NAME" ]]; then
        cp "$DIST_DIR/osx-arm64/$PROJECT_NAME" "$app_dir/Contents/MacOS/"
        print_success "ARM64 binary copied to app bundle"
    fi
    
    chmod +x "$app_dir/Contents/MacOS/$PROJECT_NAME"
    print_success "📱 macOS .app bundle created: $app_dir"
}

# Display build summary
display_summary() {
    print_header "📊 Build Summary"
    
    echo "Built artifacts in $DIST_DIR/:"
    echo ""
    
    # Platform binaries
    for platform in linux-x64 linux-arm64 osx-x64 osx-arm64 win-x64 win-arm64; do
        if [[ -d "$DIST_DIR/$platform" ]]; then
            local binary="$DIST_DIR/$platform/$PROJECT_NAME"
            if [[ $platform == win-* ]]; then
                binary="$binary.exe"
            fi
            
            if [[ -f "$binary" ]]; then
                local size=$(du -h "$binary" | cut -f1)
                echo "  ✅ $platform: $binary ($size)"
            else
                echo "  ❌ $platform: Binary not found"
            fi
        else
            echo "  ⏭️  $platform: Not built"
        fi
    done
    
    # WebAssembly
    if [[ -d "$DIST_DIR/web/wwwroot" ]]; then
        local web_size=$(du -sh "$DIST_DIR/web/wwwroot" | cut -f1)
        echo "  ✅ WebAssembly: $DIST_DIR/web/wwwroot/ ($web_size)"
    else
        echo "  ⏭️  WebAssembly: Not built"
    fi
    
    echo ""
    
    # Packages
    if [[ $PACKAGE == true ]]; then
        echo "Distribution packages:"
        
        if [[ -f "$DIST_DIR/anomali-import-tool_${VERSION}_amd64.deb" ]]; then
            local deb_size=$(du -h "$DIST_DIR/anomali-import-tool_${VERSION}_amd64.deb" | cut -f1)
            echo "  📦 Linux .deb: $DIST_DIR/anomali-import-tool_${VERSION}_amd64.deb ($deb_size)"
        fi
        
        if [[ -d "$DIST_DIR/Anomali Import Tool.app" ]]; then
            local app_size=$(du -sh "$DIST_DIR/Anomali Import Tool.app" | cut -f1)
            echo "  📱 macOS .app: $DIST_DIR/Anomali Import Tool.app ($app_size)"
        fi
        
        echo ""
    fi
    
    local total_size=$(du -sh "$DIST_DIR" | cut -f1)
    echo "Total distribution size: $total_size"
    echo ""
    
    print_success "🎉 Multi-platform build completed successfully!"
    echo ""
    echo "Next steps:"
    echo "  • Test binaries on target platforms"
    echo "  • Create additional packages (AppImage, DMG, MSI)"
    echo "  • Set up CI/CD for automated releases"
    echo "  • Configure code signing for distribution"
}

# Main execution
main() {
    check_prerequisites
    clean_builds
    setup_dist_dir
    
    if build_all_platforms; then
        if [[ $PACKAGE == true ]]; then
            print_header "📦 Creating Distribution Packages"
            create_deb_package
            create_macos_app
        fi
        
        display_summary
    else
        print_error "Build failed!"
        exit 1
    fi
}

# Execute main function
main "$@" 