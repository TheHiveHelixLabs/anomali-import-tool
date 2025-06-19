# Anomali Import Tool - Portable Build Script
# Creates a self-contained, single-file executable for portable deployment

param(
    [string]$Version = "1.0.0",
    [string]$OutputPath = ".\Release",
    [switch]$Clean = $false
)

Write-Host "Anomali Import Tool - Portable Build Script" -ForegroundColor Green
Write-Host "Version: $Version" -ForegroundColor Yellow
Write-Host ""

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Join-Path $ScriptDir ".."
$UIProject = Join-Path $ProjectRoot "src\AnomaliImportTool.UI\AnomaliImportTool.UI.csproj"
$PublishProfile = Join-Path $ProjectRoot "src\AnomaliImportTool.UI\Properties\PublishProfiles\SelfContained.pubxml"

# Clean previous builds if requested
if ($Clean) {
    Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
    $CleanPaths = @(
        (Join-Path $ProjectRoot "src\AnomaliImportTool.Core\bin"),
        (Join-Path $ProjectRoot "src\AnomaliImportTool.Core\obj"),
        (Join-Path $ProjectRoot "src\AnomaliImportTool.Infrastructure\bin"),
        (Join-Path $ProjectRoot "src\AnomaliImportTool.Infrastructure\obj"),
        (Join-Path $ProjectRoot "src\AnomaliImportTool.UI\bin"),
        (Join-Path $ProjectRoot "src\AnomaliImportTool.UI\obj"),
        $OutputPath
    )
    
    foreach ($path in $CleanPaths) {
        if (Test-Path $path) {
            Remove-Item -Path $path -Recurse -Force
            Write-Host "  Cleaned: $path"
        }
    }
}

# Create output directory
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath | Out-Null
}

# Restore packages
Write-Host ""
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore $UIProject
if ($LASTEXITCODE -ne 0) {
    Write-Host "Package restore failed!" -ForegroundColor Red
    exit 1
}

# Build the solution
Write-Host ""
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build $UIProject --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Run tests
Write-Host ""
Write-Host "Running tests..." -ForegroundColor Yellow
$TestProjects = @(
    (Join-Path $ProjectRoot "tests\AnomaliImportTool.Tests.Unit\AnomaliImportTool.Tests.Unit.csproj"),
    (Join-Path $ProjectRoot "tests\AnomaliImportTool.Tests.Integration\AnomaliImportTool.Tests.Integration.csproj")
)

foreach ($testProject in $TestProjects) {
    if (Test-Path $testProject) {
        Write-Host "  Testing: $(Split-Path -Leaf $testProject)"
        dotnet test $testProject --configuration Release --no-build --verbosity quiet
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Tests failed!" -ForegroundColor Red
            exit 1
        }
    }
}

# Publish as self-contained single file
Write-Host ""
Write-Host "Publishing self-contained executable..." -ForegroundColor Yellow
dotnet publish $UIProject `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $OutputPath `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:PublishTrimmed=true `
    -p:TrimMode=partial `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -p:Version=$Version `
    -p:FileVersion=$Version `
    -p:AssemblyVersion=$Version

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed!" -ForegroundColor Red
    exit 1
}

# Copy additional files
Write-Host ""
Write-Host "Copying additional files..." -ForegroundColor Yellow

# Create config directory
$ConfigDir = Join-Path $OutputPath "config"
if (!(Test-Path $ConfigDir)) {
    New-Item -ItemType Directory -Path $ConfigDir | Out-Null
}

# Create logs directory
$LogsDir = Join-Path $OutputPath "logs"
if (!(Test-Path $LogsDir)) {
    New-Item -ItemType Directory -Path $LogsDir | Out-Null
}

# Copy documentation
$DocsToInclude = @(
    (Join-Path $ProjectRoot "README.md"),
    (Join-Path $ProjectRoot "LICENSE"),
    (Join-Path $ProjectRoot "docs\quick-start.md")
)

foreach ($doc in $DocsToInclude) {
    if (Test-Path $doc) {
        Copy-Item $doc $OutputPath
        Write-Host "  Copied: $(Split-Path -Leaf $doc)"
    }
}

# Create deployment info file
$DeploymentInfo = @{
    Version = $Version
    BuildDate = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    RuntimeIdentifier = "win-x64"
    TargetFramework = "net8.0-windows"
    DeploymentType = "self-contained-single-file"
}

$DeploymentInfo | ConvertTo-Json | Out-File (Join-Path $OutputPath "deployment-info.json")

# Calculate file size
$ExePath = Join-Path $OutputPath "AnomaliImportTool.exe"
if (Test-Path $ExePath) {
    $FileSize = (Get-Item $ExePath).Length / 1MB
    Write-Host ""
    Write-Host "Build completed successfully!" -ForegroundColor Green
    Write-Host "Executable: $ExePath" -ForegroundColor Cyan
    Write-Host "File size: $([math]::Round($FileSize, 2)) MB" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "The application can be run from any location without installation." -ForegroundColor Yellow
    Write-Host "All configuration and logs will be stored relative to the executable." -ForegroundColor Yellow
} else {
    Write-Host "Build completed but executable not found!" -ForegroundColor Red
    exit 1
}

# Create a simple batch file to run the tool
$BatchContent = @"
@echo off
echo Starting Anomali Import Tool...
start "" "%~dp0AnomaliImportTool.exe"
"@

$BatchContent | Out-File (Join-Path $OutputPath "Run-AnomaliImportTool.bat") -Encoding ASCII

Write-Host ""
Write-Host "Deployment package created in: $OutputPath" -ForegroundColor Green 