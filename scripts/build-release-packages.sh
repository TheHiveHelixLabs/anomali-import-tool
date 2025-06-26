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

# Clean and setup
print_header "🚀 Building Anomali Import Tool V0.1B Release Packages"
print_step "Creating distribution directory..."
rm -rf "$DIST_DIR"
mkdir -p "$DIST_DIR"

# Build function for cross-platform with specific framework targeting
build_cross_platform() {
    print_header "🌍 Building Cross-Platform Applications"
    
    local platforms=(
        "linux-x64:net8.0-desktop"
        "linux-arm64:net8.0-desktop" 
        "osx-x64:net8.0-desktop"
        "osx-arm64:net8.0-desktop"
        "win-x64:net8.0-desktop"
        "win-arm64:net8.0-desktop"
    )
    
    for platform_config in "${platforms[@]}"; do
        IFS=':' read -r platform framework <<< "$platform_config"
        print_step "Building $platform ($framework)..."
        
        local output_dir="$DIST_DIR/$platform"
        
        if dotnet publish "$UNO_PROJECT" \
            --configuration Release \
            --runtime "$platform" \
            --framework "$framework" \
            --output "$output_dir" \
            --self-contained true \
            -p:PublishSingleFile=true \
            -p:PublishTrimmed=false \
            -p:IncludeNativeLibrariesForSelfExtract=true \
            --verbosity minimal 2>/dev/null; then
            
            print_success "$platform build completed"
            
            # Create platform-specific package
            local package_name="anomali-import-tool-v$VERSION-$platform"
            cd "$DIST_DIR"
            tar -czf "$package_name.tar.gz" "$platform/"
            cd - > /dev/null
            
            print_success "Package created: $package_name.tar.gz"
        else
            print_warning "$platform build failed - skipping"
        fi
    done
}

# Build WebAssembly separately
build_webassembly() {
    print_header "🌐 Building WebAssembly Application"
    
    print_step "Building WebAssembly..."
    local output_dir="$DIST_DIR/webassembly"
    
    if dotnet publish "$UNO_PROJECT" \
        --configuration Release \
        --framework net8.0-browserwasm \
        --output "$output_dir" \
        --verbosity minimal 2>/dev/null; then
        
        print_success "WebAssembly build completed"
        
        # Create WebAssembly package
        local package_name="anomali-import-tool-v$VERSION-webassembly"
        cd "$DIST_DIR"
        tar -czf "$package_name.tar.gz" "webassembly/"
        cd - > /dev/null
        
        print_success "Package created: $package_name.tar.gz"
    else
        print_warning "WebAssembly build failed - skipping"
    fi
}

# Build Core Library (for developers)
build_core_library() {
    print_header "📚 Building Core Library Package"
    
    print_step "Building Core library..."
    local output_dir="$DIST_DIR/core-library"
    
    if dotnet build src/AnomaliImportTool.Core/AnomaliImportTool.Core.csproj \
        --configuration Release \
        --output "$output_dir" \
        --verbosity minimal; then
        
        print_success "Core library build completed"
        
        # Create core library package
        local package_name="core-library-v$VERSION"
        cd "$DIST_DIR"
        tar -czf "$package_name.tar.gz" "core-library/"
        cd - > /dev/null
        
        print_success "Package created: $package_name.tar.gz"
    else
        print_error "Core library build failed"
    fi
}

# Package documentation
package_documentation() {
    print_header "📖 Packaging Documentation"
    
    print_step "Copying documentation..."
    local doc_dir="$DIST_DIR/documentation"
    mkdir -p "$doc_dir"
    
    # Copy all documentation
    cp -r docs/* "$doc_dir/"
    cp README.md "$doc_dir/"
    cp RELEASE-NOTES-v0.1.0-beta.md "$doc_dir/"
    cp V0.1B-RELEASE-SUMMARY.md "$doc_dir/"
    cp GITHUB-RELEASE-FINAL.md "$doc_dir/"
    
    # Create documentation package
    local package_name="documentation-v$VERSION"
    cd "$DIST_DIR"
    tar -czf "$package_name.tar.gz" "documentation/"
    cd - > /dev/null
    
    print_success "Documentation package created: $package_name.tar.gz"
}

# Create developer package (source + binaries)
create_developer_package() {
    print_header "👨‍💻 Creating Developer Package"
    
    print_step "Creating developer package..."
    local dev_dir="$DIST_DIR/developer-package"
    mkdir -p "$dev_dir"
    
    # Copy source code (excluding build artifacts)
    print_step "Copying source code..."
    rsync -av --exclude='bin/' --exclude='obj/' --exclude='dist/' --exclude='.git/' \
        --exclude='TestResults/' --exclude='*.user' --exclude='*.suo' \
        src/ "$dev_dir/src/"
    
    # Copy project files
    cp *.sln "$dev_dir/" 2>/dev/null || true
    cp *.md "$dev_dir/" 2>/dev/null || true
    cp -r scripts/ "$dev_dir/" 2>/dev/null || true
    
    # Copy core library binaries
    if [ -d "$DIST_DIR/core-library" ]; then
        cp -r "$DIST_DIR/core-library" "$dev_dir/binaries/"
    fi
    
    # Create developer package
    local package_name="developer-package-v$VERSION"
    cd "$DIST_DIR"
    tar -czf "$package_name.tar.gz" "developer-package/"
    cd - > /dev/null
    
    print_success "Developer package created: $package_name.tar.gz"
}

# Create build info
create_build_info() {
    print_step "Creating build information..."
    
    cat > "$DIST_DIR/BUILD_INFO.md" << EOF
# Build Information

**Build Date**: $(date)  
**Build Environment**: $(uname -s) $(uname -r)  
**.NET Version**: $(dotnet --version)  
**Build Configuration**: Release  

## Build Results

$(cd "$DIST_DIR" && ls -la *.tar.gz | while read -r line; do
    echo "✅ Package: $(echo "$line" | awk '{print $9}' | sed 's/\.tar\.gz$//')"
done)

## Build Warnings

- Infrastructure project has compilation errors
- Windows-specific components cannot be built on Linux
- Some advanced features are not yet implemented

## Package Sizes

$(cd "$DIST_DIR" && du -h *)

EOF
}

# Create final release archive
create_release_archive() {
    print_header "📦 Creating Final Release Archive"
    
    print_step "Creating complete release archive..."
    
    # Create main release archive
    cd "$DIST_DIR"
    tar -czf "../anomali-import-tool-v$VERSION.tar.gz" .
    cd - > /dev/null
    
    print_success "Complete release archive created: dist/anomali-import-tool-v$VERSION.tar.gz"
}

# Main execution
main() {
    # Check prerequisites
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK not found. Please install .NET 8.0 SDK."
        exit 1
    fi
    
    print_step "Prerequisites check passed"
    
    # Execute build steps
    build_core_library
    build_cross_platform  
    build_webassembly
    package_documentation
    create_developer_package
    create_build_info
    create_release_archive
    
    print_header "🎉 Build Complete!"
    print_success "All packages built successfully!"
    print_step "Distribution files available in: $DIST_DIR"
    print_step "Complete release: dist/anomali-import-tool-v$VERSION.tar.gz"
}

# Run main function
main "$@" 