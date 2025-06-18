#!/bin/bash

# Architecture Fitness Tests Runner
# This script runs architecture fitness tests and validates Clean Architecture compliance

set -e

echo "🏗️  Running Architecture Fitness Tests..."
echo "========================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TEST_PROJECT="$PROJECT_ROOT/tests/AnomaliImportTool.Tests.Architecture"
RESULTS_DIR="$PROJECT_ROOT/TestResults"
REPORT_FILE="$RESULTS_DIR/architecture-fitness-report.xml"

echo -e "${BLUE}Project Root:${NC} $PROJECT_ROOT"
echo -e "${BLUE}Test Project:${NC} $TEST_PROJECT"
echo ""

# Create results directory
mkdir -p "$RESULTS_DIR"

# Function to print section headers
print_section() {
    echo ""
    echo -e "${BLUE}==== $1 ====${NC}"
}

# Function to print success
print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

# Function to print warning
print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

# Function to print error
print_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Check if .NET is installed
print_section "Environment Check"
if ! command -v dotnet &> /dev/null; then
    print_error ".NET SDK is not installed or not in PATH"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
print_success ".NET SDK version: $DOTNET_VERSION"

# Check if test project exists
if [ ! -f "$TEST_PROJECT/AnomaliImportTool.Tests.Architecture.csproj" ]; then
    print_error "Architecture test project not found at $TEST_PROJECT"
    exit 1
fi

print_success "Architecture test project found"

# Restore dependencies
print_section "Restoring Dependencies"
cd "$PROJECT_ROOT"
if dotnet restore; then
    print_success "Dependencies restored successfully"
else
    print_error "Failed to restore dependencies"
    exit 1
fi

# Build the solution
print_section "Building Solution"
if dotnet build --no-restore; then
    print_success "Solution built successfully"
else
    print_error "Build failed"
    exit 1
fi

# Run architecture fitness tests
print_section "Running Architecture Fitness Tests"
cd "$TEST_PROJECT"

echo "Running tests with detailed output..."
TEST_RESULT=0

# Run tests and capture output
if dotnet test --no-build --verbosity normal --logger "trx;LogFileName=architecture-fitness-report.trx" --results-directory "$RESULTS_DIR"; then
    print_success "All architecture fitness tests passed!"
    TEST_RESULT=0
else
    print_warning "Some architecture fitness tests failed"
    TEST_RESULT=1
fi

# Generate summary report
print_section "Test Results Summary"

# Parse TRX file if it exists
TRX_FILE="$RESULTS_DIR/architecture-fitness-report.trx"
if [ -f "$TRX_FILE" ]; then
    # Extract test counts (simplified parsing)
    echo "Detailed results saved to: $TRX_FILE"
else
    print_warning "Test results file not found"
fi

# Display architecture health score
print_section "Architecture Health Score"
echo "🎯 Target: 100% compliance (all tests passing)"

if [ $TEST_RESULT -eq 0 ]; then
    echo -e "${GREEN}🏆 Current Score: 100% - Excellent architecture compliance!${NC}"
    echo ""
    echo "✅ Clean Architecture principles maintained"
    echo "✅ Naming conventions followed"
    echo "✅ Design patterns correctly implemented"
    echo "✅ Security best practices enforced"
    echo "✅ Performance considerations validated"
else
    echo -e "${YELLOW}📊 Current Score: <100% - Architecture violations detected${NC}"
    echo ""
    echo "Please review the test output above and address the following:"
    echo "• Fix failing architecture fitness tests"
    echo "• Ensure Clean Architecture dependency rules are followed"
    echo "• Verify naming conventions compliance"
    echo "• Check design pattern implementations"
    echo "• Review security and performance constraints"
fi

# Recommendations
print_section "Recommendations"
if [ $TEST_RESULT -eq 0 ]; then
    echo "🎉 Great job! Your architecture is compliant with all fitness functions."
    echo ""
    echo "To maintain this quality:"
    echo "• Run these tests in your CI/CD pipeline"
    echo "• Set up pre-commit hooks to catch violations early"
    echo "• Review architecture decisions during code reviews"
    echo "• Keep fitness tests updated as architecture evolves"
else
    echo "🔧 To improve architecture compliance:"
    echo ""
    echo "1. Address failing tests one by one"
    echo "2. Refactor code to follow Clean Architecture principles"
    echo "3. Ensure proper separation of concerns"
    echo "4. Use dependency injection correctly"
    echo "5. Make value objects immutable"
    echo "6. Keep domain layer pure (no infrastructure dependencies)"
    echo ""
    echo "📚 Resources:"
    echo "• Clean Architecture: https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html"
    echo "• Architecture documentation: $PROJECT_ROOT/docs/architecture/"
fi

# CI/CD Integration instructions
print_section "CI/CD Integration"
echo "To integrate these tests into your CI/CD pipeline:"
echo ""
echo "GitHub Actions:"
echo "```yaml"
echo "- name: Run Architecture Fitness Tests"
echo "  run: ./scripts/run-architecture-tests.sh"
echo "```"
echo ""
echo "Azure DevOps:"
echo "```yaml"
echo "- script: ./scripts/run-architecture-tests.sh"
echo "  displayName: 'Architecture Fitness Tests'"
echo "```"

# Final result
echo ""
echo "========================================"
if [ $TEST_RESULT -eq 0 ]; then
    print_success "Architecture fitness tests completed successfully!"
    echo -e "${GREEN}🏗️  Architecture is healthy and compliant!${NC}"
else
    print_error "Architecture fitness tests failed!"
    echo -e "${RED}🏗️  Architecture needs attention!${NC}"
    echo ""
    echo "Please fix the failing tests before proceeding."
fi

echo ""
echo "Report location: $RESULTS_DIR"
echo "For detailed analysis, see: docs/architecture/ArchitectureFitnessTestResults.md"

exit $TEST_RESULT 