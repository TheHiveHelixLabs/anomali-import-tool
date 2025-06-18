# Architecture Documentation

This directory contains comprehensive documentation about the Clean Architecture implementation of the Anomali Threat Bulletin Import Tool.

## Documentation Index

### 📋 Overview Documents
- **[Clean Architecture Overview](./CleanArchitectureOverview.md)** - Comprehensive guide to the Clean Architecture implementation, principles, and benefits
- **[Layer Responsibilities Guide](./LayerResponsibilitiesGuide.md)** - Practical guide for developers on what belongs in each layer with examples and anti-patterns

### 🏗️ Technical Documentation
- **[Clean Architecture Dependencies](./CleanArchitectureDependencies.md)** - Visual diagram and explanation of project dependencies
- **[Assembly Scanning Configuration](./AssemblyScanningConfiguration.md)** - Automatic service registration and dependency injection setup

### 📊 Quality Assurance
- **[Architecture Fitness Test Results](./ArchitectureFitnessTestResults.md)** - Automated architecture validation results and health metrics

### 🎯 Decision Records
- **[Architectural Decision Records (ADRs)](./ArchitecturalDecisionRecords.md)** - Documented decisions, rationale, and trade-offs for key architectural choices

## Quick Start

If you're new to this codebase, start with these documents in order:

1. **[Clean Architecture Overview](./CleanArchitectureOverview.md)** - Understand the overall architecture
2. **[Layer Responsibilities Guide](./LayerResponsibilitiesGuide.md)** - Learn what goes where
3. **[Clean Architecture Dependencies](./CleanArchitectureDependencies.md)** - See the project structure
4. **[Architectural Decision Records](./ArchitecturalDecisionRecords.md)** - Understand why decisions were made

## Architecture Health

Current architecture health metrics:

- **Architecture Fitness Tests**: 89% passing (40/45 tests)
- **Dependency Rule Compliance**: 100% (all Clean Architecture rules enforced)
- **Code Quality**: 10/10 rating target
- **Documentation Coverage**: 100% (all layers and decisions documented)

### Known Issues
- 5 failing fitness tests related to value object immutability and domain layer file system dependencies
- Remediation plan available in [Architecture Fitness Test Results](./ArchitectureFitnessTestResults.md)

## Contributing

When making architectural changes:

1. Update relevant documentation
2. Run architecture fitness tests: `dotnet test tests/AnomaliImportTool.Tests.Architecture/`
3. Create or update ADRs for significant decisions
4. Ensure compliance with layer responsibilities

## Tools and Validation

- **Architecture Tests**: Automated validation using NetArchTest.Rules
- **Static Analysis**: SonarQube integration (planned)
- **Code Quality Gates**: Cyclomatic complexity <10, technical debt <5%
- **Documentation**: Living documentation that evolves with the codebase

## Support

For questions about the architecture:

1. Check the [Layer Responsibilities Guide](./LayerResponsibilitiesGuide.md) for practical guidance
2. Review [ADRs](./ArchitecturalDecisionRecords.md) for decision context
3. Run architecture tests to validate compliance
4. Consult the [Clean Architecture Overview](./CleanArchitectureOverview.md) for principles

---

*This documentation is maintained as living documentation and should be updated with any architectural changes.* 