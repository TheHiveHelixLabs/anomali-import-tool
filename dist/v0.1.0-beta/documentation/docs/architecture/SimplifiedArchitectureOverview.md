# Simplified Architecture Overview - Anomali Import Tool

## Executive Summary

The Anomali Import Tool is a standalone, portable Windows application designed with a focused purpose: **read documents from local storage and import them into Anomali ThreatStream via API**. This document outlines the simplified architecture that prioritizes simplicity, maintainability, and core functionality.

## Core Principles

1. **Single Responsibility**: Import documents to Anomali - nothing more, nothing less
2. **Portability First**: Zero installation, runs from any location (USB, network share, local)
3. **Minimal Dependencies**: Self-contained executable with embedded .NET runtime
4. **Simple Architecture**: Just enough structure for maintainability without over-engineering

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    User Interface (WPF)                  │
│  - File Selection    - API Configuration   - Progress   │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────┐
│                    Core Application                      │
│  ┌─────────────────┐  ┌──────────────┐  ┌───────────┐ │
│  │ Document Reader │  │ API Client   │  │ Security  │ │
│  │ - PDF           │  │ - Auth       │  │ - Encrypt │ │
│  │ - Word          │  │ - Upload     │  │ - Storage │ │
│  │ - Excel         │  │ - Retry      │  │           │ │
│  └─────────────────┘  └──────────────┘  └───────────┘ │
└─────────────────────────────────────────────────────────┘
```

## Project Structure

```
AnomaliImportTool/
├── src/
│   ├── AnomaliImportTool.Core/          # Core business logic
│   │   ├── DocumentProcessing/          # File reading and extraction
│   │   ├── ApiClient/                   # Anomali API integration
│   │   ├── Security/                    # Credential management
│   │   ├── Models/                      # Data models
│   │   └── Interfaces/                  # Core abstractions
│   │
│   ├── AnomaliImportTool.UI/            # WPF presentation layer
│   │   ├── Views/                       # XAML views
│   │   ├── ViewModels/                  # MVVM view models
│   │   ├── Controls/                    # Custom controls
│   │   └── Resources/                   # Styles, icons, themes
│   │
│   └── AnomaliImportTool.Infrastructure/# External implementations
│       ├── FileSystem/                  # File I/O operations
│       ├── Logging/                     # Simple file logging
│       └── Configuration/               # Settings management
│
├── tests/
│   ├── AnomaliImportTool.Tests.Unit/    # Unit tests
│   └── AnomaliImportTool.Tests.Integration/ # API integration tests
│
├── docs/                                # Documentation
├── scripts/                             # Build and deployment scripts
└── AnomaliImportTool.sln               # Solution file
```

## Core Components

### 1. Document Processing
- **Purpose**: Extract text and metadata from documents
- **Supported Formats**: PDF, Word (.docx/.doc), Excel (.xlsx/.xls)
- **Features**: 
  - Text extraction
  - Basic OCR for scanned PDFs
  - Metadata extraction (author, dates, etc.)
  - File grouping by similarity

### 2. API Client
- **Purpose**: Interface with Anomali ThreatStream API
- **Features**:
  - Authentication (Username + API Key)
  - Create threat bulletins
  - Upload attachments
  - Simple retry logic for transient failures
  - Basic error handling

### 3. Security Module
- **Purpose**: Protect sensitive data
- **Features**:
  - API credential encryption (Windows DPAPI)
  - Secure credential storage (portable)
  - Input validation
  - HTTPS enforcement

### 4. User Interface
- **Purpose**: Simple, intuitive user interaction
- **Technology**: WPF with MVVM pattern
- **Features**:
  - Drag-and-drop file selection
  - API configuration dialog
  - Progress tracking
  - Error display
  - Basic theme support (light/dark)

## Data Flow

1. **User Input** → Select files/folder via UI
2. **File Processing** → Extract content and metadata
3. **Preview** → Show extracted data to user
4. **Confirmation** → User approves import
5. **API Upload** → Send to Anomali ThreatStream
6. **Result** → Display success/failure

## Removed Complexity

The following have been intentionally excluded to maintain simplicity:

- ❌ Git integration (handled by developer's IDE/workflow)
- ❌ Complex monitoring and telemetry
- ❌ CQRS, Event Sourcing, DDD patterns
- ❌ Multiple bounded contexts
- ❌ Advanced testing patterns (mutation, chaos, property-based)
- ❌ Built-in video tutorials and help systems
- ❌ Scheduled operations and watch folders
- ❌ Complex logging with correlation IDs
- ❌ Real-time dashboards

## Security Considerations

- API credentials encrypted using Windows DPAPI
- All settings stored locally with the application
- No registry modifications
- No network communication except to Anomali API
- Input validation on all user inputs
- Secure HTTPS communication only

## Deployment

- Single self-contained executable
- Includes .NET runtime (no installation required)
- Configuration files stored alongside executable
- Can run from:
  - USB drives
  - Network shares
  - Local directories
  - Read-only media (with temp directory for config)

## Dependencies

### Core Dependencies Only:
- .NET 6+ (self-contained)
- WPF for UI
- PDFSharp or iTextSharp (PDF processing)
- OpenXML SDK (Word/Excel processing)
- Newtonsoft.Json (API communication)
- Basic logging (Serilog with file sink only)

## Testing Strategy

### Focused Testing Approach:
1. **Unit Tests**: Core business logic (file processing, API client)
2. **Integration Tests**: API communication with mock server
3. **UI Tests**: Basic smoke tests for critical paths

### Testing Goals:
- 80% code coverage for core components
- All happy paths tested
- Common error scenarios covered
- No exotic testing patterns

## Maintenance and Support

- Simple architecture = easy maintenance
- Clear separation of concerns
- Minimal external dependencies
- Straightforward debugging
- Standard .NET development practices

## Future Considerations

If needed in the future, the architecture allows for:
- Additional file format support
- Alternative API endpoints
- Enhanced error recovery
- Performance optimizations

However, any additions should maintain the tool's focused purpose and simplicity.

## Conclusion

This simplified architecture provides a solid foundation for a tool that does one thing well: import documents into Anomali ThreatStream. By removing unnecessary complexity and focusing on core functionality, we've created a maintainable, portable solution that meets the actual business needs without over-engineering. 