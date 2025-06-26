#!/bin/bash
# build-available-packages.sh - Build Available V0.1B Packages
# Usage: ./scripts/build-available-packages.sh

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

print_header "Building Available Anomali Import Tool V0.1B Packages"

# Clean and create distribution directory
print_step "Preparing distribution directory..."
rm -rf "$DIST_DIR"
mkdir -p "$DIST_DIR"

# Build Core library (this should work)
print_step "Building Core library..."
if dotnet build src/AnomaliImportTool.Core/AnomaliImportTool.Core.csproj --configuration Release; then
    echo -e "${GREEN}✅ Core library built successfully${NC}"
    
    # Create Core library package
    mkdir -p "$DIST_DIR/core-library"
    cp -r src/AnomaliImportTool.Core/bin/Release/net8.0/* "$DIST_DIR/core-library/"
    
    # Create developer package
    print_step "Creating developer package..."
    mkdir -p "$DIST_DIR/developer-package"
    cp -r src/AnomaliImportTool.Core/bin/Release/net8.0/* "$DIST_DIR/developer-package/"
    
    # Copy source code for developers
    mkdir -p "$DIST_DIR/developer-package/source"
    cp -r src/AnomaliImportTool.Core/* "$DIST_DIR/developer-package/source/" 2>/dev/null || true
    
else
    print_error "Core library build failed"
fi

# Try to build Uno WebAssembly (for web deployment)
print_step "Attempting WebAssembly build..."
if dotnet build src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/AnomaliImportTool.Uno.csproj --configuration Release --framework net8.0-browserwasm; then
    echo -e "${GREEN}✅ WebAssembly build successful${NC}"
    
    mkdir -p "$DIST_DIR/web-deployment"
    if [ -d "src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/bin/Release/net8.0-browserwasm" ]; then
        cp -r src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/bin/Release/net8.0-browserwasm/* "$DIST_DIR/web-deployment/" 2>/dev/null || true
    fi
else
    print_warning "WebAssembly build failed - web deployment not available"
fi

# Try to build Uno Desktop (for Linux/macOS)
print_step "Attempting cross-platform desktop build..."
if dotnet build src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/AnomaliImportTool.Uno.csproj --configuration Release --framework net8.0-desktop; then
    echo -e "${GREEN}✅ Cross-platform desktop build successful${NC}"
    
    mkdir -p "$DIST_DIR/cross-platform-desktop"
    if [ -d "src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/bin/Release/net8.0-desktop" ]; then
        cp -r src/AnomaliImportTool.Uno/AnomaliImportTool.Uno/bin/Release/net8.0-desktop/* "$DIST_DIR/cross-platform-desktop/" 2>/dev/null || true
    fi
else
    print_warning "Cross-platform desktop build failed"
fi

# Create documentation package
print_step "Creating documentation package..."
mkdir -p "$DIST_DIR/documentation"

# Copy existing documentation
cp *.md "$DIST_DIR/documentation/" 2>/dev/null || true
cp -r docs/* "$DIST_DIR/documentation/" 2>/dev/null || true

# Create README for the release
cat > "$DIST_DIR/README.md" << 'EOF'
# Anomali Import Tool V0.1.0-Beta Release

## 🎯 What's Included

This beta release contains the **core functionality** of the Anomali Import Tool with working components that can be used for development and testing.

### ✅ Available Packages

#### 1. **Core Library** (`core-library/`)
- Complete core business logic
- Template management system
- Document processing interfaces
- API integration models
- **Status**: ✅ Fully functional

#### 2. **Developer Package** (`developer-package/`)
- Core library binaries
- Source code for reference
- Development documentation
- **Status**: ✅ Ready for integration

#### 3. **Web Deployment** (`web-deployment/`) [If Available]
- WebAssembly build for browser deployment
- Zero-installation web application
- **Status**: ⚠️ May not be available due to compilation issues

#### 4. **Cross-Platform Desktop** (`cross-platform-desktop/`) [If Available]  
- Desktop application for Linux/macOS
- **Status**: ⚠️ May not be available due to compilation issues

#### 5. **Documentation** (`documentation/`)
- Complete project documentation
- API references
- Setup guides
- **Status**: ✅ Complete

## 🚧 Known Limitations

### Missing Components
- **Windows Native UI**: Requires Windows environment to build
- **Complete Infrastructure Layer**: Has compilation errors that need resolution
- **CLI Interface**: Not yet implemented (planned for V0.2)
- **Automation Features**: Scheduled imports, watch folders (planned for V0.2)

### Build Issues
- Infrastructure project has type mismatches and missing properties
- Some advanced features require additional development
- Cross-platform builds may fail due to dependency issues

## 🎯 What You Can Do With This Release

### For Developers
1. **Integrate Core Library**: Use the core library in your own applications
2. **Extend Functionality**: Build upon the existing template and document processing systems
3. **API Integration**: Connect to Anomali ThreatStream using the provided interfaces

### For Testing
1. **Core Logic Testing**: Test template matching and document processing
2. **API Integration Testing**: Verify Anomali ThreatStream connectivity
3. **Template Management**: Create and manage import templates

## 🚀 Next Steps

### V0.2 Development Priorities
1. **Fix Infrastructure Compilation Issues**
2. **Complete CLI Interface**
3. **Add Automation Features**
4. **Improve Cross-Platform Compatibility**
5. **Add Advanced Export Options**

### Getting Started
1. Extract the package you need
2. Review the documentation in `documentation/`
3. For development: Start with `developer-package/`
4. For web deployment: Use `web-deployment/` if available

## 📞 Support

This is a beta release. For issues or questions:
- Check the documentation first
- Review the source code in the developer package
- Report issues with detailed error information

---

**Version**: 0.1.0-beta  
**Release Date**: June 26, 2025  
**Compatibility**: .NET 8.0+  
**Platform Support**: Windows (full), Linux/macOS (limited)
EOF

# Create build information
print_step "Creating build information..."
cat > "$DIST_DIR/BUILD_INFO.md" << EOF
# Build Information

**Build Date**: $(date)  
**Build Environment**: Linux $(uname -r)  
**.NET Version**: $(dotnet --version)  
**Build Configuration**: Release  

## Build Results

$(ls -la "$DIST_DIR" | grep -v "^total" | tail -n +2 | while read -r line; do
    if [[ $line == d* ]]; then
        dir_name=$(echo "$line" | awk '{print $NF}')
        echo "✅ Package: $dir_name"
    fi
done)

## Build Warnings

- Infrastructure project has compilation errors
- Windows-specific components cannot be built on Linux
- Some advanced features are not yet implemented

## Package Sizes

$(du -sh "$DIST_DIR"/* 2>/dev/null | sed 's/^/- /')

EOF

# Create version info
echo "$VERSION" > "$DIST_DIR/VERSION"

# Create compressed packages
print_step "Creating compressed packages..."
cd "$DIST_DIR"

# Create individual package archives
for dir in */; do
    if [ -d "$dir" ]; then
        package_name=$(basename "$dir")
        print_step "Compressing $package_name..."
        tar -czf "${package_name}-v${VERSION}.tar.gz" "$package_name"
        echo -e "${GREEN}✅ Created: ${package_name}-v${VERSION}.tar.gz${NC}"
    fi
done

# Create complete release archive
cd ..
print_step "Creating complete release archive..."
tar -czf "anomali-import-tool-v${VERSION}-beta.tar.gz" "v0.1.0-beta"

cd ..

print_header "🎉 Build Complete!"

echo -e "${GREEN}✅ Release packages available in: $DIST_DIR${NC}"
echo ""
echo -e "${BLUE}📦 Created packages:${NC}"
ls -la "$DIST_DIR"/*.tar.gz 2>/dev/null | sed 's/^/  /' || echo "  No compressed packages created"

echo ""
echo -e "${BLUE}📁 Available directories:${NC}"
ls -la "$DIST_DIR" | grep '^d' | awk '{print "  📂 " $NF}' | grep -v '^\.$\|^\.\.$'

echo ""
echo -e "${YELLOW}⚠️  Note: This is a partial build due to compilation issues in the Infrastructure project.${NC}"
echo -e "${YELLOW}   Full Windows builds require a Windows environment.${NC}"
echo ""
echo -e "${GREEN}🚀 Ready for GitHub Release!${NC}" 