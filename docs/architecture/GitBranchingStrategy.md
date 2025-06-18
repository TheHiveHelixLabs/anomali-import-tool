# Git Branching Strategy

## Overview

This project follows a **GitHub Flow** branching strategy optimized for continuous integration and deployment, with clear separation between development work and production-ready releases.

## Branch Structure

### 🚀 `main` Branch
- **Purpose**: Production-ready code with compiled releases
- **Protection**: Protected branch with required reviews
- **Content**: Only stable, tested, and release-ready code
- **Releases**: All GitHub releases are created from this branch
- **Compiled Binaries**: Contains downloadable executables for end users

### 🔧 `develop` Branch  
- **Purpose**: Integration branch for ongoing development
- **Content**: Latest development features and ongoing work
- **Testing**: Continuous integration and automated testing
- **Merge Source**: Feature branches merge here first
- **Quality Gate**: Must pass all tests before merging to main

### 🌟 Feature Branches
- **Naming Convention**: `feature/task-number-description`
- **Examples**: 
  - `feature/1.6.3-setup-moq-framework`
  - `feature/2.1.1-implement-credential-storage`
- **Lifecycle**: Created from `develop`, merged back to `develop`
- **Duration**: Short-lived (1-7 days typically)

## Workflow Process

### 1. Development Workflow
```bash
# Start new feature
git checkout develop
git pull origin develop
git checkout -b feature/1.6.3-setup-moq-framework

# Work on feature
git add .
git commit -m "feat(testing): implement Moq setup for unit tests"

# Push feature branch
git push -u origin feature/1.6.3-setup-moq-framework

# Create Pull Request to develop
# After review and approval, merge to develop
```

### 2. Release Workflow
```bash
# When develop is ready for release
git checkout main
git pull origin main
git merge develop

# Create release tag
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin main --tags

# Trigger GitHub Actions for binary compilation
```

### 3. Hotfix Workflow
```bash
# For critical production fixes
git checkout main
git checkout -b hotfix/critical-security-fix

# Fix the issue
git commit -m "fix(security): resolve critical vulnerability"

# Merge to both main and develop
git checkout main
git merge hotfix/critical-security-fix
git checkout develop
git merge hotfix/critical-security-fix
```

## Branch Protection Rules

### `main` Branch
- ✅ Require pull request reviews (2 reviewers)
- ✅ Require status checks to pass
- ✅ Require branches to be up to date
- ✅ Restrict pushes to administrators only
- ✅ Require signed commits

### `develop` Branch
- ✅ Require pull request reviews (1 reviewer)
- ✅ Require status checks to pass
- ✅ Allow force pushes for administrators
- ✅ Delete head branches after merge

## Commit Message Standards

Following **Conventional Commits** format:

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Types
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

### Examples
```bash
feat(api): add ThreatStream authentication service
fix(security): resolve credential storage vulnerability
docs(architecture): update Clean Architecture documentation
test(unit): add comprehensive Moq testing scenarios
```

## Release Management

### Version Numbering
Following **Semantic Versioning (SemVer)**:
- `MAJOR.MINOR.PATCH` (e.g., 1.2.3)
- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

### Release Process
1. **Development Phase**: Work in `develop` branch
2. **Feature Freeze**: Stop adding new features
3. **Testing Phase**: Comprehensive testing on `develop`
4. **Release Candidate**: Merge `develop` to `main`
5. **Final Testing**: Production-like testing on `main`
6. **Release**: Create GitHub release with compiled binaries
7. **Post-Release**: Merge any hotfixes back to `develop`

### GitHub Releases
- **Pre-release**: Tagged from `develop` for testing
- **Stable Release**: Tagged from `main` with compiled binaries
- **Assets**: Include portable executable, documentation, and checksums

## Continuous Integration

### GitHub Actions Triggers
- **Pull Requests**: Run tests and quality checks
- **Push to develop**: Run full test suite and build
- **Push to main**: Run tests, build, and create release artifacts
- **Release Tags**: Compile binaries and publish GitHub release

### Quality Gates
- ✅ All unit tests pass (95%+ coverage)
- ✅ Integration tests pass
- ✅ Security scans pass
- ✅ Code quality metrics meet standards
- ✅ Architecture fitness tests pass

## Best Practices

### For Developers
1. **Always branch from `develop`** for new features
2. **Keep feature branches small** and focused
3. **Write descriptive commit messages** following conventional format
4. **Test locally** before pushing
5. **Update documentation** with code changes
6. **Delete feature branches** after merging

### For Reviewers
1. **Check code quality** and adherence to standards
2. **Verify tests** are comprehensive and meaningful
3. **Ensure documentation** is updated
4. **Test functionality** locally when possible
5. **Approve only** when confident in changes

### For Releases
1. **Test thoroughly** on `develop` before merging to `main`
2. **Update version numbers** appropriately
3. **Generate release notes** from commit history
4. **Verify binary compilation** works correctly
5. **Test portable deployment** scenarios

## Emergency Procedures

### Rollback Process
```bash
# If main branch has issues
git checkout main
git revert <problematic-commit-hash>
git push origin main

# Or reset to previous good state
git reset --hard <last-good-commit>
git push --force-with-lease origin main
```

### Branch Recovery
```bash
# If develop gets corrupted
git checkout main
git checkout -b develop-recovery
git push -u origin develop-recovery

# After verification, replace develop
git branch -D develop
git checkout -b develop
git push -u origin develop --force
```

## Monitoring and Metrics

### Branch Health Metrics
- **Merge Frequency**: How often features are integrated
- **Branch Lifetime**: Average time from creation to merge
- **Conflict Rate**: Percentage of merges with conflicts
- **Test Success Rate**: Percentage of passing CI builds

### Release Metrics
- **Release Frequency**: How often we ship to production
- **Lead Time**: Time from feature start to production
- **Deployment Success Rate**: Percentage of successful releases
- **Rollback Rate**: Percentage of releases requiring rollback

This branching strategy ensures stable releases while maintaining development velocity and code quality standards required for the 10/10 rating across all categories. 