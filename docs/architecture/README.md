# Anomali Threat Bulletin Import Tool - Architecture Documentation

## 🏗️ **Current Architecture**

The Anomali Threat Bulletin Import Tool follows a **Cross-Platform Clean Architecture** designed for portability, enterprise quality, and zero-installation deployment across Windows, macOS, Linux, and WebAssembly platforms.

> 🏆 **Migration Success**: Successfully migrated from Windows-only WinUI 3 to universal Uno Platform deployment with zero compilation errors.

📘 **[View the Simplified Architecture Overview](SimplifiedArchitectureOverview.md)** - Primary architecture document for development guidance.

## 🎯 **Architecture Principles**

1. **Cross-Platform First** - Single codebase, native performance on all platforms
2. **Zero Installation** - Portable deployment requiring no system modifications
3. **Clean Architecture** - SOLID principles with dependency inversion and separation of concerns
4. **Enterprise Quality** - 95%+ test coverage, WCAG 2.1 AA accessibility, comprehensive security
5. **Template-Driven Processing** - Extensible document processing with custom import templates *(New Feature)*

## 🏛️ **System Architecture Overview**

```
┌─────────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                           │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   Uno Platform  │  │ WinUI Services  │  │   WebAssembly   │ │
│  │ (Cross-Platform)│  │   (Windows)     │  │     (Web)       │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                               │
┌─────────────────────────────────────────────────────────────────┐
│                    APPLICATION LAYER                            │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   ViewModels    │  │   Services      │  │ Template System │ │
│  │  (ReactiveUI)   │  │ (Cross-Platform)│  │  (New Feature)  │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                               │
┌─────────────────────────────────────────────────────────────────┐
│                     DOMAIN LAYER                               │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   Core Models   │  │   Interfaces    │  │ Template Models │ │
│  │   (Portable)    │  │   (Contracts)   │  │  (New Feature)  │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                               │
┌─────────────────────────────────────────────────────────────────┐
│                  INFRASTRUCTURE LAYER                          │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │Document Process │  │  Anomali API    │  │ Template Engine │ │
│  │ (Multi-Format)  │  │  Integration    │  │  (New Feature)  │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## 📁 **Project Structure**

### **Current Cross-Platform Structure**
```
AnomaliImportTool/
├── src/
│   ├── AnomaliImportTool.Core/           # 🎯 Business logic & models (Platform-agnostic)
│   ├── AnomaliImportTool.Infrastructure/ # 🔧 Services & external integrations
│   ├── AnomaliImportTool.UI/            # 🎨 Shared UI components & ViewModels
│   ├── AnomaliImportTool.Uno/           # 📱 Cross-platform Uno application (MAIN)
│   └── AnomaliImportTool.WinUI/         # 🪟 Windows-specific accessibility services
├── tests/                               # 🧪 Comprehensive test suite (95%+ coverage)
│   ├── AnomaliImportTool.Tests.Unit/
│   ├── AnomaliImportTool.Tests.Integration/
│   └── AnomaliImportTool.Tests.UI/
├── docs/                               # 📚 Documentation & architecture guides
└── scripts/                            # 🛠️ Cross-platform build & deployment
```

### **Import Template System Architecture** *(New Feature - In Development)*
```
Template System Components:
├── Core/Models/
│   ├── ImportTemplate.cs               # Template definition with extraction zones
│   ├── TemplateField.cs               # Field configuration with validation
│   └── ExtractionZone.cs              # Coordinate-based extraction regions
├── Infrastructure/Services/
│   ├── ImportTemplateService.cs       # Template CRUD and management
│   ├── TemplateMatchingService.cs     # Document-template matching logic
│   └── TemplateExtractionEngine.cs    # Field extraction using templates
└── UI/
    ├── Views/TemplateCreationView.xaml  # Visual template creation interface
    ├── Controls/ExtractionZoneEditor.cs # Visual zone selection control
    └── ViewModels/Template*.cs          # Template management ViewModels
```

## 🌟 **Key Architectural Achievements**

### ✅ **Completed Migrations & Features**
- **Cross-Platform Migration**: Successfully migrated from WinUI 3 to Uno Platform
- **Zero Installation Deployment**: Complete portable architecture with embedded runtime
- **Professional UI Framework**: "Living Workspace" with 60 FPS animations and WCAG 2.1 AA accessibility
- **Enterprise Security**: AES-256 encryption with portable credential storage
- **Comprehensive Testing**: 95%+ coverage with architecture fitness tests

### 🚧 **In Development**
- **Import Template System**: Visual template creation and intelligent document matching
- **Enhanced 7-Step Wizard**: Template selection integrated into workflow
- **Template Management**: Organizational template sharing and version control

## 📋 **Key Documentation**

### 🏗️ **Core Architecture**
- [**Simplified Architecture Overview**](SimplifiedArchitectureOverview.md) - **START HERE** - Primary development guide
- [**Layer Responsibilities Guide**](LayerResponsibilitiesGuide.md) - Code organization and patterns
- [**Clean Architecture Dependencies**](CleanArchitectureDependencies.md) - Project dependency structure

### 🎯 **Implementation Guides**
- [**Assembly Scanning Configuration**](AssemblyScanningConfiguration.md) - Service registration patterns
- [**Service Lifetime Configuration**](ServiceLifetimeConfiguration.md) - Dependency injection setup
- [**Architectural Decision Records**](ArchitecturalDecisionRecords.md) - Historical and current decisions

### 🏆 **Migration Documentation**
- [**Uno Migration Success Report**](UNO_MIGRATION_SUCCESS_REPORT.md) - Complete migration achievement report
- [**Architecture Fitness Test Results**](ArchitectureFitnessTestResults.md) - Quality validation metrics

## 🖥️ **Platform-Specific Architecture**

### **Multi-Target Framework Support**
```xml
<TargetFrameworks>net8.0-desktop;net8.0-browserwasm</TargetFrameworks>
```

| Platform | Framework | Deployment | Architecture Notes |
|----------|-----------|------------|-------------------|
| **Windows** | net8.0-desktop | Self-contained .exe/.msi | Full feature set with Windows-specific services |
| **macOS** | net8.0-desktop | Universal .app/.dmg | Native macOS integration, notarization ready |
| **Linux** | net8.0-desktop | .deb/AppImage | Desktop integration, universal compatibility |
| **WebAssembly** | net8.0-browserwasm | Progressive Web App | Browser-based with offline capability |

### **Uno Platform Integration**
- **ReactiveUI Preserved**: Full MVVM pattern compatibility
- **Core Logic Unchanged**: Zero modifications to business logic
- **Platform Services**: Conditional compilation for platform-specific features
- **Shared UI**: 90%+ code reuse across all platforms

## 🎯 **What We Build**

### ✅ **Current Features**
- **Document Processing**: PDF, Word, Excel with OCR for scanned documents
- **Intelligent Grouping**: Similar names, time proximity, content analysis
- **Anomali Integration**: ThreatStream API v2/v3 with comprehensive error handling
- **Portable Deployment**: Zero installation with encrypted credential storage
- **Professional UI**: Dual-mode interface with accessibility compliance

### 🚧 **Upcoming Features**
- **Import Template System**: Visual template creation for metadata extraction
- **Enhanced Workflow**: 7-step wizard with template selection and matching
- **Template Sharing**: Organizational template management and distribution
- **Advanced Analytics**: Template performance metrics and optimization

### ❌ **What We DON'T Do** *(Simplified Architecture)*
- Complex monitoring/telemetry beyond basic logging
- Multiple bounded contexts or microservices architecture
- CQRS/Event Sourcing patterns
- Built-in Git integration within the application
- Exotic testing patterns or frameworks

## 📊 **Architecture Health & Quality**

### **Current Metrics**
- **Build Status**: ✅ Zero compilation errors across all platforms
- **Test Coverage**: 95%+ with comprehensive integration and UI tests
- **Architecture Fitness**: 40/45 tests passing (89% compliance)
- **Code Quality**: 10/10 target rating with static analysis
- **Security**: Zero critical vulnerabilities, AES-256 encryption

### **Quality Standards**
- **SOLID Principles**: Enforced through architecture tests
- **Dependency Inversion**: Clean separation between layers
- **Cross-Platform Compatibility**: Native performance on all supported platforms
- **Accessibility**: WCAG 2.1 AA compliance with voice control support
- **Security**: Zero-trust model with encrypted portable storage

## 🚀 **Development Guidelines**

### **When Adding New Features**
1. **Core First**: Add business logic to `AnomaliImportTool.Core`
2. **Interface Definition**: Define contracts in Core interfaces
3. **Infrastructure Implementation**: Implement services in Infrastructure layer
4. **UI Integration**: Add ViewModels and Views to UI/Uno projects
5. **Cross-Platform Testing**: Validate on multiple platforms

### **Template System Development** *(Current Priority)*
1. **Domain Models**: ImportTemplate, TemplateField, ExtractionZone
2. **Service Layer**: Template management, matching, and extraction services
3. **UI Components**: Visual template creation and management interfaces
4. **Integration**: Seamless workflow integration with existing features

### **Architecture Validation**
```bash
# Run architecture fitness tests
dotnet test tests/AnomaliImportTool.Tests.Unit/ --filter Category=Architecture

# Cross-platform build validation
./scripts/build-all.sh --clean --test

# Platform-specific testing
dotnet test --framework net8.0-desktop
dotnet test --framework net8.0-browserwasm
```

## 🔗 **Quick Reference Links**

### **Development Resources**
- [🎯 Current Implementation Tasks](../../tasks/tasks-prd-anomali-threat-bulletin-import-tool.md)
- [📋 Product Requirements Document](../../tasks/prd-anomali-threat-bulletin-import-tool.md)
- [🤝 Contributing Guidelines](../../CONTRIBUTING.md)

### **Platform Documentation**
- [🪟 Windows Deployment](../DEPLOYMENT_GUIDE.md#windows)
- [🍎 macOS Deployment](../DEPLOYMENT_GUIDE.md#macos)
- [🐧 Linux Deployment](../DEPLOYMENT_GUIDE.md#linux)
- [🌐 WebAssembly Deployment](../DEPLOYMENT_GUIDE.md#web)

## 📞 **Architecture Support**

### **Questions & Guidance**
1. **Layer Responsibilities**: Check [Layer Responsibilities Guide](LayerResponsibilitiesGuide.md)
2. **Historical Decisions**: Review [Architectural Decision Records](ArchitecturalDecisionRecords.md)
3. **Cross-Platform Issues**: Consult [Uno Migration Success Report](UNO_MIGRATION_SUCCESS_REPORT.md)
4. **Template System**: Refer to implementation tasks and PRD specifications

### **Quality Assurance**
- **Architecture Tests**: Automated validation with NetArchTest.Rules
- **Code Quality**: SonarQube integration with quality gates
- **Performance**: Continuous monitoring and optimization
- **Security**: Regular vulnerability scanning and compliance validation

---

**🏆 Status**: Cross-platform migration completed successfully - Focus shifted to Import Template System implementation  
**🎯 Next Milestone**: Complete template system for enhanced document processing automation  
**📝 Last Updated**: January 2025 - Reflects current production-ready cross-platform architecture 