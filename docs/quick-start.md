# Quick Start Guide

Get up and running with the Anomali Threat Bulletin Import Tool in just 5 minutes!

## 📋 Prerequisites

Before you begin, ensure you have:
- Windows 10/11 (64-bit)
- .NET 6.0 or later installed
- Access to an Anomali ThreatStream instance
- Valid ThreatStream API credentials (Username + API Key)

## 🚀 Step 1: Download and Install

### Option A: Download Release (Recommended)
1. Go to the [Releases page](../../releases)
2. Download the latest `AnomaliImportTool-v1.0.0.zip`
3. Extract to your desired location (e.g., `C:\Tools\AnomaliImportTool`)
4. Run `AnomaliImportTool.exe`

### Option B: Build from Source
```bash
git clone https://github.com/yourusername/anomali-import-tool.git
cd anomali-import-tool
dotnet build --configuration Release
dotnet run --project src/AnomaliImportTool
```

## ⚙️ Step 2: Initial Configuration

1. **Launch the Application**
   - Double-click `AnomaliImportTool.exe`
   - The application will open with a welcome screen

2. **Configure ThreatStream Connection**
   - Click **Settings** → **ThreatStream Configuration**
   - Enter your ThreatStream details:
     ```
     Server URL: https://your-threatstream-instance.com
     Username: your-username
     API Key: your-api-key
     ```
   - Click **Test Connection** to verify
   - Click **Save** when the test succeeds

3. **Basic Settings**
   - Go to **Settings** → **General**
   - Set your preferred language and theme
   - Configure default file locations

## 📄 Step 3: Import Your First Documents

1. **Select Files**
   - Click **Import** → **Select Files**
   - Choose your Word, Excel, or PDF files
   - Or drag and drop files directly into the application

2. **Review Grouping**
   - The tool will automatically group related files
   - Review the suggestions and adjust if needed
   - Click **Next** to proceed

3. **Configure Threat Bulletin**
   - Set bulletin name: `Threat Report - {date}`
   - Choose classification: `Internal Use`
   - Set confidence level: `Medium (50)`
   - Add tags: `malware`, `phishing`, etc.

4. **Preview and Import**
   - Review the import summary
   - Click **Import** to create the threat bulletin
   - Monitor progress in real-time

## ✅ Step 4: Verify Success

1. **Check Import Status**
   - View the import results in the **History** tab
   - Verify all files were processed successfully

2. **Verify in ThreatStream**
   - Log into your ThreatStream instance
   - Navigate to **Threat Bulletins**
   - Confirm your bulletin was created with attachments

## 🎯 Next Steps

Now that you're up and running, explore these advanced features:

### Customize Naming Templates
- Go to **Settings** → **Naming Templates**
- Create custom naming schemes with placeholders
- Example: `{category} - {date} - {username}`

### Set Up OCR Processing
- Go to **Settings** → **Document Processing**
- Enable OCR for scanned PDFs
- Configure language packs if needed

### Configure Automated Workflows
- Set up watched folders for automatic processing
- Configure email notifications for import results
- Set up scheduled imports

## 🔧 Common Configuration Examples

### Example 1: Basic Security Team Setup
```
Naming Template: Security Alert - {date} - {source}
Default Classification: TLP:AMBER
Auto-assign Tags: security, alert
Confidence Level: High (75)
```

### Example 2: Incident Response Team
```
Naming Template: IR-{ticket-number} - {date}
Default Classification: TLP:RED
Auto-assign Tags: incident, response
Confidence Level: Very High (90)
```

### Example 3: Threat Intelligence Team
```
Naming Template: TI-{category} - {date} - {analyst}
Default Classification: TLP:GREEN
Auto-assign Tags: threat-intel, analysis
Confidence Level: Medium (60)
```

## 📞 Need Help?

If you encounter any issues:

1. **Check the Troubleshooting Guide**: [docs/troubleshooting.md](troubleshooting.md)
2. **Review the FAQ**: [docs/faq.md](faq.md)
3. **Search existing issues**: [GitHub Issues](../../issues)
4. **Create a new issue**: [New Issue](../../issues/new)

## 🎉 Congratulations!

You've successfully set up the Anomali Threat Bulletin Import Tool! You can now:

- ✅ Import multiple document types
- ✅ Create organized threat bulletins
- ✅ Leverage intelligent file grouping
- ✅ Maintain audit trails
- ✅ Scale your threat intelligence workflow

For more advanced features and detailed configuration options, see the complete [User Guide](user-guide.md).

---

**Total Setup Time**: ~5 minutes  
**Next**: [User Guide](user-guide.md) | [Configuration Guide](configuration.md) 