# Clean Architecture Dependency Structure

## Overview
This document outlines the dependency structure of the Anomali Import Tool following Clean Architecture principles.

## Dependency Rules
1. **Domain Layer** - No dependencies (pure business logic)
2. **Application Layer** - Only depends on Domain
3. **Infrastructure Layer** - Depends on Application and Domain
4. **Presentation Layer** - Depends on all layers (composition root)

## Project Dependencies

### ✅ Core.Domain (0 dependencies)
- **Purpose**: Domain entities, value objects, enums, shared kernel
- **Dependencies**: None (pure domain logic)
- **Status**: ✅ Compliant

### ✅ Core.Application (1 dependency)
- **Purpose**: Use cases, interfaces, application services
- **Dependencies**: 
  - Core.Domain
- **Status**: ✅ Compliant

### ✅ Infrastructure Projects (2 dependencies each)
All infrastructure projects follow the same pattern:
- **AnomaliImportTool.Infrastructure**
- **AnomaliImportTool.Security**
- **AnomaliImportTool.DocumentProcessing**
- **AnomaliImportTool.Api**
- **AnomaliImportTool.Git**

**Dependencies for each**:
- Core.Application (for interfaces)
- Core.Domain (for entities and value objects)
- **Status**: ✅ Compliant

### ✅ WPF Presentation Layer (7 dependencies)
- **Purpose**: User interface, composition root, dependency injection
- **Dependencies**:
  - Core.Application
  - Core.Domain
  - Infrastructure
  - Security
  - DocumentProcessing
  - Api
  - Git
- **Status**: ✅ Compliant

## Test Project Dependencies

### Unit Tests
- **Purpose**: Test core business logic
- **Dependencies**: Core.Domain, Core.Application, Infrastructure

### Integration Tests
- **Purpose**: Test infrastructure integrations
- **Dependencies**: Core.Domain, Core.Application, Infrastructure, Api, Security

### Performance Tests
- **Purpose**: Test processing performance
- **Dependencies**: Core.Domain, Core.Application, DocumentProcessing

### Security Tests
- **Purpose**: Test security components
- **Dependencies**: Core.Domain, Core.Application, Security

### End-to-End Tests
- **Purpose**: UI automation testing
- **Dependencies**: None (uses external automation tools)

## Dependency Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    WPF Presentation Layer                   │
│  (Composition Root - References All Infrastructure)         │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────┐
│                 Infrastructure Layer                        │
│  ┌─────────────┐ ┌──────────────┐ ┌─────┐ ┌─────────────┐  │
│  │Infrastructure│ │DocumentProc. │ │ Api │ │ Security    │  │
│  └─────────────┘ └──────────────┘ └─────┘ └─────────────┘  │
│                           │                                 │
└───────────────────────────┼─────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   Application Layer                         │
│            (Interfaces & Use Cases)                         │
└─────────────────┬───────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────────┐
│                    Domain Layer                             │
│  (Entities, Value Objects, Domain Events, Shared Kernel)   │
│                   NO DEPENDENCIES                           │
└─────────────────────────────────────────────────────────────┘
```

## Benefits of This Structure

1. **Testability**: Easy to mock dependencies and test in isolation
2. **Flexibility**: Infrastructure can be swapped without affecting business logic
3. **Maintainability**: Clear separation of concerns
4. **Scalability**: New features can be added without breaking existing code
5. **Independence**: Business logic is independent of frameworks and external concerns

## Verification

All projects build successfully and follow Clean Architecture dependency rules:
- ✅ Domain has no dependencies
- ✅ Application only depends on Domain
- ✅ Infrastructure depends on Application and Domain
- ✅ Presentation depends on all layers (composition root)
- ✅ Tests have appropriate dependencies for their scope 