# 🚀 Quick Start Guide

Get up and running with the Anomali Import Tool in just 5 minutes! This guide will walk you through your first document import.

## ⚡ Prerequisites (1 minute)

Before you begin, ensure you have:

- **Anomali ThreatStream Access**: Valid username and API key
- **Documents to Import**: PDF, Word, or Excel files ready
- **System Requirements**: Windows 10+, macOS 10.15+, or Linux with GUI support

## 📥 Step 1: Download & Launch (1 minute)

### Windows
```powershell
# Download the latest Windows installer
# Double-click AnomaliImportTool-Setup-1.0.0.exe
# Or run the portable .exe directly
```

### macOS
```bash
# Download the .dmg file
# Drag "Anomali Import Tool.app" to Applications
# Launch from Applications or Launchpad
```

### Linux
```bash
# Download and install .deb package
sudo dpkg -i anomali-import-tool_1.0.0_amd64.deb

# Or run the portable AppImage
chmod +x AnomaliImportTool-1.0.0-x86_64.AppImage
./AnomaliImportTool-1.0.0-x86_64.AppImage
```

### Web Browser
Visit the hosted web version at: `https://your-deployment-url.com`

## 🔧 Step 2: Initial Configuration (2 minutes)

### Configure Anomali Connection

1. **Launch the Application**
   - The welcome screen will appear on first launch

2. **Open Settings**
   - Click the **⚙️ Settings** button in the top toolbar
   - Or press `Ctrl+,` (Windows/Linux) or `Cmd+,` (macOS)

3. **API Configuration**
   ```
   Server URL: https://your-anomali-instance.com
   Username: your-username
   API Key: your-api-key-here
   API Version: v2 (or v3 if supported)
   ```

4. **Test Connection**
   - Click **"Test Connection"** button
   - ✅ Success: Green checkmark appears
   - ❌ Error: Review credentials and URL

5. **Save Settings**
   - Click **"Save"** to store configuration securely
   - Settings are encrypted and stored locally

### Optional: Quick Security Setup
```
🔒 Security Level: Standard (recommended for first use)
🔐 Session Timeout: 30 minutes
🛡️ Proxy Settings: (leave blank unless required)
```

## 📄 Step 3: Your First Import (2 minutes)

### Simple Mode (Recommended for Beginners)

1. **Select Files**
   - Click the **📁 Select Files** button
   - Choose 1-3 PDF, Word, or Excel files
   - Or drag & drop files into the main window

2. **Review Auto-Detection**
   - The tool automatically extracts:
     - ✅ Document content
     - ✅ Dates and usernames  
     - ✅ File metadata
   - Review the extracted information in the preview

3. **Configure Import Settings**
   ```
   Bulletin Name: [Auto-generated from filename]
   TLP Level: White (default)
   Status: Published
   Visibility: Private
   ```

4. **Import Documents**
   - Click the **🚀 Import** button
   - Watch the progress bar for real-time status
   - ✅ Success: Documents appear in your Anomali instance

### Advanced Mode (Power Users)

1. **Switch to Advanced Mode**
   - Toggle the **"Advanced Mode"** switch in the top-right

2. **Visual Document Management**
   - Documents appear as interactive cards
   - Drag to group related documents
   - Right-click for additional options

3. **Bulk Configuration**
   - Select multiple documents
   - Apply settings to all selected items
   - Use templates for consistent naming

## ✅ Verification

After import, verify your documents in Anomali ThreatStream:

1. **Login to ThreatStream**
   - Navigate to **Intelligence > Threat Bulletins**

2. **Find Your Import**
   - Search for the bulletin name or username
   - Verify content and attachments

3. **Review Audit Trail**
   - Check the application logs for import details
   - View **Settings > Logs** for troubleshooting

## 🎯 Next Steps

Now that you've completed your first import:

### Optimize Your Workflow
- 📖 Read the [**Complete Workflow Guide**](import-workflow.md)
- 🔧 Explore [**Advanced Features**](advanced/batch-processing.md)
- 🎨 Create [**Custom Templates**](advanced/custom-templates.md)

### Platform-Specific Features
- 🪟 [**Windows Power User Tips**](platforms/windows.md)
- 🍎 [**macOS Integration Features**](platforms/macos.md)
- 🐧 [**Linux Command Line Tools**](platforms/linux.md)

### Get Help
- ❓ Check the [**FAQ**](support/faq.md) for common questions
- 🔧 View [**Troubleshooting**](support/troubleshooting.md) for issues
- 💬 Join the [**Community Forum**](https://github.com/HiveHelixLabs/AnomaliImportTool/discussions)

## 🚨 Common First-Time Issues

### Connection Problems
```
❌ Error: "Connection failed"
✅ Solution: Verify server URL format (include https://)

❌ Error: "Authentication failed" 
✅ Solution: Double-check username and API key

❌ Error: "Timeout"
✅ Solution: Check network connection and proxy settings
```

### File Issues
```
❌ Error: "File not supported"
✅ Solution: Ensure files are PDF, DOCX, DOC, XLSX, or XLS

❌ Error: "File too large"
✅ Solution: Files must be under 50MB each

❌ Error: "Permission denied"
✅ Solution: Ensure files are not open in other applications
```

### Import Problems
```
❌ Error: "Import failed"
✅ Solution: Check ThreatStream permissions and quotas

❌ Error: "Duplicate bulletin"
✅ Solution: Use different names or update existing bulletins

❌ Error: "Invalid TLP"
✅ Solution: Verify TLP level is supported by your instance
```

## 📞 Need Help?

- 🆘 **Immediate Help**: Check [**Error Messages Guide**](support/error-messages.md)
- 💬 **Community**: [GitHub Discussions](https://github.com/HiveHelixLabs/AnomaliImportTool/discussions)
- 📧 **Support**: [support@hivehelixlabs.com](mailto:support@hivehelixlabs.com)

---

**🎉 Congratulations!** You've successfully completed your first import. The tool is now ready for your daily threat intelligence workflows.

*Continue with the [**Complete User Guide**](README.md) to unlock the full potential of the Anomali Import Tool.* 