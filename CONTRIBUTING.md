# Contributing to Anomali Threat Bulletin Import Tool

Thank you for your interest in contributing to the Anomali Threat Bulletin Import Tool! This document provides guidelines and information for contributors.

## 🤝 Code of Conduct

This project adheres to a Code of Conduct that all contributors are expected to follow. Please read [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) before contributing.

## 🚀 Getting Started

### Prerequisites
- Windows 10/11 (64-bit)
- .NET 6.0 SDK or later
- Visual Studio 2022 or VS Code
- Git

### Development Setup
```bash
# Fork the repository on GitHub
# Clone your fork
git clone https://github.com/YOUR-USERNAME/anomali-import-tool.git
cd anomali-import-tool

# Add upstream remote
git remote add upstream https://github.com/original-owner/anomali-import-tool.git

# Install dependencies
dotnet restore

# Build the project
dotnet build

# Run tests
dotnet test

# Run the application
dotnet run --project src/AnomaliImportTool
```

## 📋 How to Contribute

### 1. Find an Issue
- Check [existing issues](../../issues)
- Look for issues labeled `good first issue` or `help wanted`
- Create a new issue if you find a bug or have a feature request

### 2. Create a Branch
```bash
git checkout -b feature/your-feature-name
# or
git checkout -b bugfix/issue-number-description
```

### 3. Make Changes
- Follow our [coding standards](#coding-standards)
- Write tests for new functionality
- Update documentation as needed
- Ensure all tests pass

### 4. Commit Changes
```bash
git add .
git commit -m "feat: add new feature description"
```

Follow [Conventional Commits](https://www.conventionalcommits.org/) format:
- `feat:` new features
- `fix:` bug fixes
- `docs:` documentation changes
- `style:` formatting changes
- `refactor:` code refactoring
- `test:` adding tests
- `chore:` maintenance tasks

### 5. Push and Create Pull Request
```bash
git push origin your-branch-name
```

Create a pull request from your fork to the main repository.

## 🏗️ Project Structure

```
anomali-import-tool/
├── src/
│   ├── AnomaliImportTool/          # Main application
│   ├── AnomaliImportTool.Core/     # Core business logic
│   ├── AnomaliImportTool.Infrastructure/ # External services
│   └── AnomaliImportTool.Tests/    # Unit tests
├── docs/                           # Documentation
├── scripts/                        # Build and deployment scripts
├── tests/                          # Integration and E2E tests
├── .github/                        # GitHub workflows
├── LICENSE                         # MIT License
├── README.md                       # Project overview
└── CONTRIBUTING.md                 # This file
```

## 📏 Coding Standards

### C# Guidelines
- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
- Use meaningful names for variables, methods, and classes
- Maximum method length: 20 lines
- Maximum class length: 200 lines
- Cyclomatic complexity < 10

### Code Quality Requirements
- **Test Coverage**: Minimum 95% code coverage
- **Static Analysis**: Zero critical issues in SonarQube
- **Technical Debt**: Keep below 5% ratio
- **Documentation**: 80% API documentation coverage

### Architecture Principles
- Follow SOLID principles
- Implement Clean Architecture patterns
- Use dependency injection
- Implement proper error handling
- Follow Domain-Driven Design (DDD)

### Example Code Style
```csharp
namespace AnomaliImportTool.Core.Services
{
    public class ThreatBulletinService : IThreatBulletinService
    {
        private readonly ILogger<ThreatBulletinService> _logger;
        private readonly IThreatStreamApiClient _apiClient;

        public ThreatBulletinService(
            ILogger<ThreatBulletinService> logger,
            IThreatStreamApiClient apiClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public async Task<Result<ThreatBulletin>> CreateBulletinAsync(
            CreateBulletinRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating threat bulletin: {Name}", request.Name);
                
                var bulletin = await _apiClient.CreateBulletinAsync(request, cancellationToken);
                
                _logger.LogInformation("Successfully created bulletin with ID: {Id}", bulletin.Id);
                return Result.Success(bulletin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create threat bulletin: {Name}", request.Name);
                return Result.Failure<ThreatBulletin>($"Failed to create bulletin: {ex.Message}");
            }
        }
    }
}
```

## 🧪 Testing Guidelines

### Test Categories
1. **Unit Tests**: Test individual components in isolation
2. **Integration Tests**: Test component interactions
3. **End-to-End Tests**: Test complete user workflows
4. **Performance Tests**: Validate performance requirements

### Test Structure
```csharp
[TestClass]
public class ThreatBulletinServiceTests
{
    private Mock<ILogger<ThreatBulletinService>> _mockLogger;
    private Mock<IThreatStreamApiClient> _mockApiClient;
    private ThreatBulletinService _service;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<ThreatBulletinService>>();
        _mockApiClient = new Mock<IThreatStreamApiClient>();
        _service = new ThreatBulletinService(_mockLogger.Object, _mockApiClient.Object);
    }

    [TestMethod]
    public async Task CreateBulletinAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateBulletinRequest { Name = "Test Bulletin" };
        var expectedBulletin = new ThreatBulletin { Id = 1, Name = "Test Bulletin" };
        
        _mockApiClient
            .Setup(x => x.CreateBulletinAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBulletin);

        // Act
        var result = await _service.CreateBulletinAsync(request);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(expectedBulletin.Id, result.Value.Id);
        _mockApiClient.Verify(x => x.CreateBulletinAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

## 📚 Documentation

### Required Documentation
- **Code Comments**: Document complex logic and public APIs
- **XML Documentation**: All public methods and classes
- **README Updates**: Update feature lists and usage examples
- **Architecture Decisions**: Document significant design decisions

### Documentation Format
```csharp
/// <summary>
/// Creates a new threat bulletin in ThreatStream.
/// </summary>
/// <param name="request">The bulletin creation request containing name, description, and metadata.</param>
/// <param name="cancellationToken">Cancellation token for the operation.</param>
/// <returns>A result containing the created bulletin or error information.</returns>
/// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
/// <exception cref="ThreatStreamApiException">Thrown when API call fails.</exception>
public async Task<Result<ThreatBulletin>> CreateBulletinAsync(
    CreateBulletinRequest request,
    CancellationToken cancellationToken = default)
```

## 🔍 Pull Request Guidelines

### PR Checklist
- [ ] Code follows project style guidelines
- [ ] Tests added/updated for new functionality
- [ ] All tests pass locally
- [ ] Documentation updated
- [ ] PR description clearly explains changes
- [ ] Linked to relevant issues
- [ ] No merge conflicts

### PR Template
```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing completed

## Screenshots (if applicable)

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex code
- [ ] Documentation updated
- [ ] Tests added and passing
```

## 🐛 Bug Reports

### Bug Report Template
```markdown
**Bug Description**
Clear description of the bug

**Steps to Reproduce**
1. Go to '...'
2. Click on '....'
3. Scroll down to '....'
4. See error

**Expected Behavior**
What should happen

**Actual Behavior**
What actually happens

**Environment**
- OS: [e.g., Windows 11]
- .NET Version: [e.g., 6.0.1]
- Application Version: [e.g., 1.0.0]

**Additional Context**
Screenshots, logs, or other relevant information
```

## 💡 Feature Requests

### Feature Request Template
```markdown
**Feature Description**
Clear description of the proposed feature

**Problem Statement**
What problem does this solve?

**Proposed Solution**
How should this be implemented?

**Alternatives Considered**
Other approaches you've considered

**Additional Context**
Mockups, examples, or references
```

## 🏷️ Issue Labels

- `bug`: Something isn't working
- `enhancement`: New feature or request
- `documentation`: Improvements or additions to documentation
- `good first issue`: Good for newcomers
- `help wanted`: Extra attention is needed
- `question`: Further information is requested
- `wontfix`: This will not be worked on

## 🎯 Development Workflow

### Branch Strategy
- `main`: Production-ready code
- `develop`: Integration branch for features
- `feature/*`: New features
- `bugfix/*`: Bug fixes
- `hotfix/*`: Critical fixes for production

### Release Process
1. Feature development in feature branches
2. Merge to develop for integration testing
3. Create release branch from develop
4. Final testing and bug fixes
5. Merge to main and tag release
6. Deploy to production

## 📞 Getting Help

- **GitHub Discussions**: [Project Discussions](../../discussions)
- **Issues**: [GitHub Issues](../../issues)
- **Documentation**: [Project Docs](docs/)

## 🙏 Recognition

Contributors are recognized in:
- [CONTRIBUTORS.md](CONTRIBUTORS.md)
- Release notes
- Project documentation

Thank you for contributing to the Anomali Threat Bulletin Import Tool! 