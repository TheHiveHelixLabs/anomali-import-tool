# 🔧 Troubleshooting Guide

This comprehensive troubleshooting guide helps you resolve common issues with the Anomali Import Tool. Issues are organized by category for quick navigation.

## 🚨 Critical Issues (Immediate Action Required)

### Application Won't Start

#### Symptom: Application fails to launch or crashes immediately
```
❌ Error: "Application failed to start correctly"
❌ Error: "Missing runtime dependencies"
❌ Error: "Access denied"
```

**Windows Solutions:**
```powershell
# Check if .NET 8 is installed (shouldn't be needed with self-contained builds)
dotnet --version

# Run as administrator if needed
Right-click → "Run as administrator"

# Check Windows Defender exclusions
Add-MpPreference -ExclusionPath "C:\Program Files\Anomali Import Tool"

# Verify file integrity
Get-FileHash "AnomaliImportTool.exe" -Algorithm SHA256
```

**macOS Solutions:**
```bash
# Check security settings
System Preferences → Security & Privacy → General
# Click "Open Anyway" if application is blocked

# Reset quarantine attributes
xattr -dr com.apple.quarantine "/Applications/Anomali Import Tool.app"

# Check permissions
ls -la "/Applications/Anomali Import Tool.app"
chmod +x "/Applications/Anomali Import Tool.app/Contents/MacOS/AnomaliImportTool"
```

**Linux Solutions:**
```bash
# Check executable permissions
chmod +x /usr/bin/anomali-import-tool

# Install missing dependencies
sudo apt-get install -f
sudo apt-get install libgtk-3-0 libglib2.0-0 libnss3

# Check system logs
journalctl -u anomali-import-tool
dmesg | grep -i anomali
```

### Database/Configuration Corruption

#### Symptom: Settings lost, application behaves unexpectedly
```
❌ Error: "Configuration file corrupted"
❌ Error: "Cannot load user settings"
❌ Behavior: Settings reset to defaults
```

**Solution:**
```bash
# Backup corrupted files
mkdir backup-$(date +%Y%m%d)
cp -r ~/.config/AnomaliImportTool backup-$(date +%Y%m%d)/

# Reset configuration (will lose custom settings)
rm -rf ~/.config/AnomaliImportTool
rm -rf ~/Library/Application\ Support/AnomaliImportTool  # macOS
rm -rf %APPDATA%\AnomaliImportTool  # Windows

# Restart application - it will create new configuration
```

## 🔌 Connection & API Issues

### Anomali ThreatStream Connection Problems

#### Symptom: Cannot connect to ThreatStream
```
❌ Error: "Connection failed"
❌ Error: "Authentication failed"
❌ Error: "SSL/TLS error"
❌ Error: "Timeout"
```

**Diagnosis Steps:**
```bash
# Test basic connectivity
ping your-anomali-instance.com
curl -I https://your-anomali-instance.com

# Test API endpoint
curl -H "Authorization: Bearer YOUR_API_KEY" \
     https://your-anomali-instance.com/api/v2/intelligence/

# Check DNS resolution
nslookup your-anomali-instance.com
dig your-anomali-instance.com
```

**Common Solutions:**

1. **Invalid API Credentials**
   ```
   ✅ Verify username is correct (case-sensitive)
   ✅ Regenerate API key in ThreatStream
   ✅ Check for special characters in credentials
   ✅ Ensure API key has proper permissions
   ```

2. **Network/Firewall Issues**
   ```
   ✅ Check corporate firewall settings
   ✅ Verify proxy configuration
   ✅ Test from different network
   ✅ Check port 443 (HTTPS) accessibility
   ```

3. **SSL Certificate Issues**
   ```
   ✅ Update system certificates
   ✅ Try disabling certificate validation (temporarily)
   ✅ Check for corporate MITM certificates
   ✅ Verify server certificate is valid
   ```

### Proxy Configuration Issues

#### Symptom: Connection works locally but fails through proxy
```
❌ Error: "Proxy authentication required"
❌ Error: "Connection refused"
❌ Behavior: Requests time out
```

**Configuration Examples:**
```json
// HTTP Proxy
{
  "Proxy": {
    "Type": "Http",
    "Host": "proxy.company.com",
    "Port": 8080,
    "Username": "domain\\username",
    "Password": "password",
    "BypassLocalAddresses": true
  }
}

// SOCKS Proxy
{
  "Proxy": {
    "Type": "Socks5",
    "Host": "socks.company.com",
    "Port": 1080,
    "Authentication": false
  }
}

// Automatic Proxy
{
  "Proxy": {
    "Type": "SystemDefault",
    "UseSystemProxySettings": true
  }
}
```

**Troubleshooting Commands:**
```bash
# Test proxy connectivity
curl --proxy http://proxy.company.com:8080 https://httpbin.org/ip

# Check system proxy settings
# Windows
netsh winhttp show proxy

# macOS
scutil --proxy

# Linux
echo $http_proxy
echo $https_proxy
```

## 📄 Document Processing Issues

### File Format Problems

#### Symptom: Documents fail to process or import incorrectly
```
❌ Error: "Unsupported file format"
❌ Error: "File is corrupted"
❌ Error: "Cannot extract content"
❌ Behavior: Empty content extracted
```

**Supported Formats:**
```
✅ PDF: .pdf (including scanned PDFs with OCR)
✅ Word: .docx, .doc
✅ Excel: .xlsx, .xls
❌ Other: .txt, .rtf, .ppt, .pptx (not supported)
```

**Diagnosis Steps:**
```bash
# Check file integrity
file document.pdf
hexdump -C document.pdf | head

# Verify file size
ls -lh document.pdf
du -h document.pdf

# Test file accessibility
cat document.pdf > /dev/null
```

**Solutions:**

1. **Corrupted Files**
   ```
   ✅ Re-download or re-export the file
   ✅ Open in native application to verify
   ✅ Try different export formats
   ✅ Use file repair tools if available
   ```

2. **Protected Files**
   ```
   ✅ Remove password protection
   ✅ Check for DRM restrictions
   ✅ Ensure file isn't read-only
   ✅ Close file in other applications
   ```

3. **Large Files**
   ```
   ✅ Check file size limits (50MB max)
   ✅ Compress images in documents
   ✅ Split large documents
   ✅ Increase memory allocation
   ```

### OCR (Optical Character Recognition) Issues

#### Symptom: Scanned PDFs not processed correctly
```
❌ Error: "OCR failed"
❌ Error: "Text extraction failed"
❌ Behavior: No text extracted from scanned PDFs
```

**Troubleshooting:**
```bash
# Check OCR engine availability
which tesseract
tesseract --version

# Test OCR on sample image
tesseract sample.png output.txt
cat output.txt
```

**Solutions:**

1. **Poor Image Quality**
   ```
   ✅ Increase scan resolution (300+ DPI)
   ✅ Improve image contrast
   ✅ Ensure text is clearly readable
   ✅ Remove background noise/artifacts
   ```

2. **Language Settings**
   ```
   ✅ Configure correct language for OCR
   ✅ Install additional language packs
   ✅ Test with English documents first
   ✅ Check character encoding settings
   ```

3. **Performance Issues**
   ```
   ✅ Process smaller batches
   ✅ Increase processing timeout
   ✅ Monitor system memory usage
   ✅ Close other resource-intensive applications
   ```

## 🔄 Batch Processing Issues

### Memory and Performance Problems

#### Symptom: Application slows down or crashes during large batches
```
❌ Error: "Out of memory"
❌ Error: "Processing timeout"
❌ Behavior: Application becomes unresponsive
❌ Behavior: Processing takes too long
```

**Performance Monitoring:**
```bash
# Monitor memory usage
# Windows
Get-Process -Name "AnomaliImportTool" | Select-Object WorkingSet,PagedMemorySize

# macOS/Linux
ps aux | grep AnomaliImportTool
top -p $(pgrep AnomaliImportTool)
```

**Optimization Solutions:**

1. **Reduce Batch Size**
   ```json
   {
     "Processing": {
       "MaxBatchSize": 25,          // Reduce from 100
       "MaxConcurrentFiles": 3,     // Reduce from 5
       "ProcessingTimeout": "00:10:00"
     }
   }
   ```

2. **Memory Management**
   ```json
   {
     "Processing": {
       "EnableMemoryOptimization": true,
       "DisposeImmediately": true,
       "LargeFileThreshold": "10MB",
       "StreamLargeFiles": true
     }
   }
   ```

3. **System Resources**
   ```
   ✅ Close other applications
   ✅ Ensure sufficient disk space (2x file size)
   ✅ Use SSD for temporary files
   ✅ Increase virtual memory if needed
   ```

### File Locking Issues

#### Symptom: Files cannot be processed due to access conflicts
```
❌ Error: "File is in use by another process"
❌ Error: "Access denied"
❌ Error: "File locked"
```

**Solutions:**
```bash
# Windows - Find processes using file
handle.exe "path\to\file.pdf"
# Or use Resource Monitor

# macOS/Linux - Find processes using file
lsof /path/to/file.pdf
fuser /path/to/file.pdf

# Kill processes if necessary (be careful!)
kill -9 PID
```

**Prevention:**
```
✅ Close files in other applications before processing
✅ Process files from different directory
✅ Copy files to temporary location first
✅ Check for antivirus software interference
```

## 🖥️ Platform-Specific Issues

### Windows-Specific Problems

#### Windows Defender / Antivirus Issues
```
❌ Behavior: Application blocked or quarantined
❌ Error: "Virus detected" (false positive)
❌ Behavior: Slow performance during scanning
```

**Solutions:**
```powershell
# Add exclusions to Windows Defender
Add-MpPreference -ExclusionProcess "AnomaliImportTool.exe"
Add-MpPreference -ExclusionPath "C:\Program Files\Anomali Import Tool"

# Temporarily disable real-time protection (for testing)
Set-MpPreference -DisableRealtimeMonitoring $true
# Remember to re-enable: Set-MpPreference -DisableRealtimeMonitoring $false
```

#### Windows Registry Issues
```
❌ Error: "Registry access denied"
❌ Behavior: Settings not persisting
```

**Solutions:**
```powershell
# Run as administrator
# Or use portable mode (no registry required)
```

### macOS-Specific Problems

#### Gatekeeper and Notarization Issues
```
❌ Error: "App is damaged and can't be opened"
❌ Error: "Developer cannot be verified"
❌ Error: "Malicious software"
```

**Solutions:**
```bash
# Remove quarantine attribute
xattr -dr com.apple.quarantine "/Applications/Anomali Import Tool.app"

# Alternative: System Preferences approach
# System Preferences → Security & Privacy → General → "Open Anyway"
```

#### Permission Issues
```
❌ Error: "Operation not permitted"
❌ Error: "Full Disk Access required"
```

**Solutions:**
```
1. System Preferences → Security & Privacy → Privacy
2. Select "Full Disk Access" 
3. Click "+" and add Anomali Import Tool
4. Restart the application
```

### Linux-Specific Problems

#### Missing Dependencies
```
❌ Error: "libgtk-3.so.0: cannot open shared object file"
❌ Error: "GLIBC version not found"
```

**Solutions:**
```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install libgtk-3-0 libglib2.0-0 libnss3 libatk-bridge2.0-0 \
                     libdrm2 libxcomposite1 libxdamage1 libxrandr2 \
                     libgbm1 libatspi2.0-0

# CentOS/RHEL/Fedora
sudo yum install gtk3 glib2 nss atk at-spi2-atk
# Or: sudo dnf install gtk3 glib2 nss atk at-spi2-atk

# Check GLIBC version
ldd --version
```

#### Display/X11 Issues
```
❌ Error: "Cannot open display"
❌ Error: "No protocol specified"
```

**Solutions:**
```bash
# Set display variable
export DISPLAY=:0

# For remote systems
ssh -X user@remote-host
xhost +local:

# For Wayland users
export XDG_SESSION_TYPE=x11
export GDK_BACKEND=x11
```

## 🔍 Debugging and Diagnostics

### Enable Debug Logging

**Configuration:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "AnomaliImportTool": "Trace"
    },
    "Console": {
      "IncludeScopes": true,
      "LogToStdErr": false
    },
    "File": {
      "Path": "logs/debug-.log",
      "RollingInterval": "Hour",
      "LogLevel": "Trace"
    }
  }
}
```

**Command Line:**
```bash
# Enable verbose logging
anomali-import-tool --verbose --log-level trace

# Save logs to file
anomali-import-tool --log-file debug.log 2>&1 | tee console.log
```

### Collect Diagnostic Information

**System Information Script:**
```bash
#!/bin/bash
echo "=== System Information ===" > diagnostic.txt
uname -a >> diagnostic.txt
echo "" >> diagnostic.txt

echo "=== Application Version ===" >> diagnostic.txt
anomali-import-tool --version >> diagnostic.txt
echo "" >> diagnostic.txt

echo "=== Network Connectivity ===" >> diagnostic.txt
ping -c 3 your-anomali-instance.com >> diagnostic.txt
echo "" >> diagnostic.txt

echo "=== Recent Logs ===" >> diagnostic.txt
tail -100 ~/.config/AnomaliImportTool/logs/latest.log >> diagnostic.txt
```

**Windows PowerShell:**
```powershell
# Create diagnostic report
$output = "diagnostic.txt"
"=== System Information ===" | Out-File $output
Get-ComputerInfo | Out-File $output -Append
""  | Out-File $output -Append

"=== Application Version ===" | Out-File $output -Append
& "anomali-import-tool" --version | Out-File $output -Append
""  | Out-File $output -Append

"=== Recent Logs ===" | Out-File $output -Append
Get-Content "$env:APPDATA\AnomaliImportTool\logs\latest.log" -Tail 100 | Out-File $output -Append
```

### Performance Profiling

**Monitor Resource Usage:**
```bash
# Linux
while true; do
  ps aux | grep AnomaliImportTool | grep -v grep
  sleep 5
done

# Monitor network activity
netstat -an | grep :443
ss -tuln | grep :443
```

**Memory Leak Detection:**
```csharp
// Enable in configuration for development
{
  "Diagnostics": {
    "EnableMemoryProfiling": true,
    "GCSettings": {
      "Server": false,
      "Concurrent": true
    }
  }
}
```

## 📞 Getting Additional Help

### Before Contacting Support

**Prepare the following information:**

1. **System Information**
   - Operating system and version
   - Application version
   - Hardware specifications (RAM, CPU)

2. **Error Details**
   - Exact error message
   - Steps to reproduce
   - When the error first occurred

3. **Log Files**
   - Application logs (last 24 hours)
   - System logs (if relevant)
   - Configuration files (remove sensitive data)

4. **Environment**
   - Network configuration
   - Proxy settings
   - Antivirus software
   - Other security tools

### Support Channels

#### Community Support (Free)
- **GitHub Discussions**: [Community Forum](https://github.com/HiveHelixLabs/AnomaliImportTool/discussions)
- **GitHub Issues**: [Bug Reports](https://github.com/HiveHelixLabs/AnomaliImportTool/issues)
- **Documentation**: Check all documentation first

#### Professional Support
- **Email**: [support@hivehelixlabs.com](mailto:support@hivehelixlabs.com)
- **Enterprise Support**: [enterprise@hivehelixlabs.com](mailto:enterprise@hivehelixlabs.com)

#### Emergency Support (Critical Issues)
- **24/7 Hotline**: Available for enterprise customers
- **Escalation Process**: Contact enterprise support for escalation procedures

### What to Include in Support Requests

```
Subject: [Anomali Import Tool] Brief description of issue

Environment:
- OS: Ubuntu 22.04 LTS
- Version: Anomali Import Tool v1.0.0
- Hardware: 16GB RAM, Intel i7

Issue Description:
- What you were trying to do
- What happened instead
- Error messages (exact text)
- When it started happening

Steps to Reproduce:
1. First step
2. Second step
3. Error occurs

Troubleshooting Attempted:
- List what you've already tried
- Reference this troubleshooting guide sections

Logs and Files:
- Attach relevant log files
- Include configuration (remove sensitive data)
- Screenshots if applicable
```

---

**Remember**: Most issues can be resolved using this troubleshooting guide. If you need to contact support, following these steps first will help ensure a faster resolution! 