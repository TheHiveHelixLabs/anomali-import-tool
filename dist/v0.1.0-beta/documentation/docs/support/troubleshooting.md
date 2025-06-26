# Template Troubleshooting Guide

**Anomali Threat Bulletin Import Tool - Template Issues Resolution**  
**Version**: 1.0  
**Last Updated**: June 25, 2025

---

## Table of Contents

1. [Quick Diagnostics](#quick-diagnostics)
2. [Template Matching Issues](#template-matching-issues)
3. [Field Extraction Problems](#field-extraction-problems)
4. [Performance Issues](#performance-issues)
5. [Document Format Issues](#document-format-issues)
6. [System Errors](#system-errors)
7. [Advanced Debugging](#advanced-debugging)
8. [Getting Additional Help](#getting-additional-help)

---

## Quick Diagnostics

### Initial Troubleshooting Checklist

Before diving into specific issues, run through this quick checklist:

- [ ] **Document Format**: Is the document in a supported format (PDF, DOCX, XLSX)?
- [ ] **File Size**: Is the document under 50MB?
- [ ] **Template Status**: Is the template active and properly configured?
- [ ] **System Resources**: Is there sufficient memory and disk space?
- [ ] **Recent Changes**: Were any recent changes made to the template or system?

### System Health Check

```powershell
# Run system diagnostic
Get-AnomaliSystemHealth -IncludeTemplates -IncludePerformance
```

**Expected Output:**
```
System Status: Healthy
Template Database: Connected
OCR Engine: Running
Memory Usage: 65% (OK)
Disk Space: 15GB Available (OK)
```

### Log File Locations

- **Application Logs**: `%AppData%\AnomaliImportTool\Logs\Application.log`
- **Template Logs**: `%AppData%\AnomaliImportTool\Logs\Templates.log`
- **Error Logs**: `%AppData%\AnomaliImportTool\Logs\Errors.log`
- **Performance Logs**: `%AppData%\AnomaliImportTool\Logs\Performance.log`

---

## Template Matching Issues

### Problem: Template Not Found/Selected

#### Symptoms
- No templates appear in the template selection list
- System says "No matching templates found"
- Templates exist but don't match documents

#### Diagnosis Steps

1. **Check Template Status**
   ```json
   GET /api/templates/status
   {
     "templateId": "your-template-id",
     "isActive": true,
     "supportedFormats": ["pdf", "docx"],
     "lastModified": "2025-06-25T10:30:00Z"
   }
   ```

2. **Verify Document Format**
   - Ensure document extension matches supported formats
   - Check for corrupted file headers
   - Validate file can be opened in native application

3. **Review Matching Criteria**
   ```json
   {
     "documentMatchingCriteria": {
       "requiredKeywords": ["security", "incident"],
       "minimumConfidenceThreshold": 0.5,
       "autoApplicationThreshold": 0.8
     }
   }
   ```

#### Solutions

**Solution 1: Lower Confidence Threshold**
```json
{
  "minimumConfidenceThreshold": 0.3,
  "autoApplicationThreshold": 0.6
}
```

**Solution 2: Add Alternative Keywords**
```json
{
  "requiredKeywords": ["security", "incident", "threat", "analysis", "report"],
  "optionalKeywords": ["IOC", "malware", "vulnerability"]
}
```

**Solution 3: Enable Format Auto-Detection**
```json
{
  "formatDetection": {
    "enableAutoDetection": true,
    "fallbackToGeneric": true,
    "ignoreExtension": false
  }
}
```

### Problem: Wrong Template Selected

#### Symptoms
- System selects inappropriate template
- Extraction results don't match document content
- Low confidence scores but template still applied

#### Diagnosis Steps

1. **Review Template Hierarchy**
   - Check for template inheritance conflicts
   - Verify parent-child relationships
   - Look for overlapping keywords

2. **Analyze Confidence Scores**
   ```bash
   Get-TemplateMatchingDetails -DocumentPath "document.pdf" -ShowAllMatches
   ```

3. **Check Keyword Conflicts**
   ```json
   {
     "conflictAnalysis": {
       "template1": {
         "keywords": ["financial", "report", "quarterly"],
         "confidence": 0.75
       },
       "template2": {
         "keywords": ["security", "report", "analysis"],
         "confidence": 0.70
       }
     }
   }
   ```

#### Solutions

**Solution 1: Refine Keywords**
- Make keywords more specific
- Add negative keywords to exclude inappropriate matches
- Use weighted keyword scoring

**Solution 2: Adjust Confidence Thresholds**
```json
{
  "advancedMatching": {
    "minimumGapBetweenMatches": 0.2,
    "requireUnambiguousMatch": true,
    "enableManualOverride": true
  }
}
```

**Solution 3: Implement Template Filters**
```json
{
  "templateFilters": {
    "byCategory": ["Security"],
    "byTags": ["incident-response"],
    "excludeCategories": ["Financial", "Legal"]
  }
}
```

---

## Field Extraction Problems

### Problem: Fields Not Extracting

#### Symptoms
- Fields appear blank in extraction results
- Error messages about missing data
- Partial extractions with some fields working

#### Diagnosis Steps

1. **Test Extraction Rules**
   ```regex
   # Test regex pattern
   Pattern: "Incident ID:\s*([A-Z0-9-]+)"
   Test String: "Incident ID: SEC-2025-001"
   Expected Match: "SEC-2025-001"
   ```

2. **Check Extraction Zones**
   ```json
   {
     "extractionZone": {
       "x": 100,
       "y": 80,
       "width": 200,
       "height": 25,
       "pageNumber": 1,
       "documentFormat": "pdf"
     }
   }
   ```

3. **Validate Document Structure**
   - Check if document layout has changed
   - Verify text is selectable (not an image)
   - Confirm OCR quality for scanned documents

#### Solutions

**Solution 1: Update Regex Patterns**
```regex
# More flexible patterns
Original: "Incident ID:\s*([A-Z0-9-]+)"
Updated: "(?:Incident\s*ID|ID):?\s*([A-Z0-9-]+)"

# Multiple pattern fallbacks
Primary: "Date:\s*(\d{4}-\d{2}-\d{2})"
Secondary: "Date:\s*(\d{1,2}/\d{1,2}/\d{4})"
Tertiary: "(\d{1,2}-\d{1,2}-\d{4})"
```

**Solution 2: Adjust Extraction Zones**
```json
{
  "multiZoneExtraction": [
    {
      "zone": {"x": 100, "y": 80, "width": 200, "height": 25},
      "priority": 1
    },
    {
      "zone": {"x": 90, "y": 75, "width": 220, "height": 30},
      "priority": 2
    }
  ]
}
```

**Solution 3: Enable OCR Enhancement**
```json
{
  "ocrSettings": {
    "enablePreprocessing": true,
    "imageEnhancement": true,
    "language": "eng",
    "dpi": 300,
    "noiseReduction": true
  }
}
```

### Problem: Incorrect Data Extracted

#### Symptoms
- Wrong values in extracted fields
- Data from wrong sections of document
- Formatted incorrectly (dates, numbers)

#### Diagnosis Steps

1. **Pattern Testing**
   ```javascript
   // Test extraction pattern
   var pattern = /Amount:\s*\$?([0-9,]+(?:\.\d{2})?)/;
   var testText = "Total Amount: $1,234.56";
   var match = pattern.exec(testText);
   console.log(match[1]); // Should output: 1,234.56
   ```

2. **Zone Verification**
   - Use visual zone selector to verify coordinates
   - Check if zone overlaps with unwanted content
   - Validate zone size and position

3. **Data Type Validation**
   ```json
   {
     "fieldValidation": {
       "fieldType": "Number",
       "dataFormat": "Currency",
       "minValue": 0,
       "maxValue": 9999999.99,
       "expectedPattern": "\\$?[0-9,]+(?:\\.[0-9]{2})?"
     }
   }
   ```

#### Solutions

**Solution 1: Improve Pattern Specificity**
```regex
# More specific patterns
Generic: "(\d+)"
Specific: "(?:Invoice\s*#|Invoice\s*Number):?\s*([0-9-]+)"

# Context-aware extraction
"(?:Total|Final|Net)\s*Amount:?\s*\$?([0-9,]+(?:\.\d{2})?)"
```

**Solution 2: Add Data Validation**
```json
{
  "postExtractionValidation": {
    "emailField": {
      "pattern": "^[\\w\\.-]+@[\\w\\.-]+\\.[a-zA-Z]{2,}$",
      "errorMessage": "Invalid email format"
    },
    "dateField": {
      "format": "YYYY-MM-DD",
      "minimumDate": "2020-01-01",
      "maximumDate": "2030-12-31"
    }
  }
}
```

**Solution 3: Implement Multi-Method Extraction**
```json
{
  "extractionMethods": [
    {
      "method": "regex",
      "pattern": "Amount:\\s*\\$?([0-9,]+(?:\\.[0-9]{2})?)",
      "priority": 1
    },
    {
      "method": "zone",
      "coordinates": {"x": 200, "y": 150, "width": 100, "height": 20},
      "priority": 2
    },
    {
      "method": "keyword_search",
      "keywords": ["$", "USD", "total"],
      "proximity": 50,
      "priority": 3
    }
  ]
}
```

---

## Performance Issues

### Problem: Slow Template Processing

#### Symptoms
- Template matching takes more than 30 seconds
- System becomes unresponsive during processing
- High CPU or memory usage

#### Diagnosis Steps

1. **Performance Profiling**
   ```powershell
   Measure-TemplatePerformance -TemplatePath "template.json" -DocumentPath "document.pdf"
   ```

2. **Resource Monitoring**
   ```bash
   # Check system resources
   Get-Process "AnomaliImportTool" | Select CPU, Memory, HandleCount
   ```

3. **Template Complexity Analysis**
   ```json
   {
     "complexityMetrics": {
       "fieldCount": 25,
       "regexPatternCount": 40,
       "extractionZoneCount": 15,
       "complexityScore": 8.5
     }
   }
   ```

#### Solutions

**Solution 1: Optimize Regex Patterns**
```regex
# Slow pattern
.*(?:keyword1|keyword2|keyword3).*

# Optimized pattern
\b(?:keyword1|keyword2|keyword3)\b[^\n]*
```

**Solution 2: Reduce Extraction Zones**
```json
{
  "optimizationStrategy": {
    "combineOverlappingZones": true,
    "removeUnusedZones": true,
    "minimizeZoneSize": true,
    "useTargetedExtraction": true
  }
}
```

**Solution 3: Enable Caching**
```json
{
  "performanceSettings": {
    "enableTemplateCache": true,
    "enableDocumentCache": true,
    "cacheTimeout": 3600,
    "maxCacheSize": "500MB"
  }
}
```

### Problem: Memory Issues

#### Symptoms
- Out of memory errors
- Application crashes during processing
- System becomes sluggish

#### Solutions

**Solution 1: Batch Processing**
```json
{
  "batchSettings": {
    "maxDocumentsPerBatch": 10,
    "memoryThreshold": "1GB",
    "enableGarbageCollection": true,
    "processingMode": "sequential"
  }
}
```

**Solution 2: Memory Optimization**
```json
{
  "memorySettings": {
    "maxHeapSize": "2GB",
    "garbageCollectionMode": "aggressive",
    "imageCompressionLevel": 75,
    "enableStreamProcessing": true
  }
}
```

---

## Document Format Issues

### Problem: PDF Text Not Extracting

#### Symptoms
- Blank extraction results from PDF
- OCR not working on scanned PDFs
- Garbled text extraction

#### Diagnosis Steps

1. **PDF Analysis**
   ```powershell
   Test-PdfContent -Path "document.pdf" -ShowTextLayers -ShowImages
   ```

2. **Text Layer Detection**
   ```json
   {
     "pdfAnalysis": {
       "hasTextLayer": false,
       "isScanned": true,
       "imageQuality": "medium",
       "pageCount": 5,
       "ocrRequired": true
     }
   }
   ```

#### Solutions

**Solution 1: Enable OCR**
```json
{
  "ocrSettings": {
    "enabled": true,
    "engine": "Tesseract",
    "language": ["eng", "spa"],
    "dpi": 300,
    "imagePreprocessing": {
      "deskew": true,
      "denoise": true,
      "sharpen": true
    }
  }
}
```

**Solution 2: Image Enhancement**
```json
{
  "imageProcessing": {
    "contrastEnhancement": 1.2,
    "brightnessAdjustment": 1.1,
    "gammaCorrection": 1.0,
    "noiseReduction": "medium"
  }
}
```

### Problem: Word Document Issues

#### Symptoms
- Missing content from DOCX files
- Incorrect table extraction
- Style information lost

#### Solutions

**Solution 1: Style-Based Extraction**
```json
{
  "wordProcessing": {
    "preserveStyles": true,
    "extractTables": true,
    "includeHeaders": true,
    "processTrackChanges": false
  }
}
```

**Solution 2: Table Handling**
```json
{
  "tableExtraction": {
    "preserveStructure": true,
    "extractHeaders": true,
    "handleMergedCells": true,
    "outputFormat": "structured"
  }
}
```

---

## System Errors

### Common Error Messages

#### Error: "Template validation failed"

**Cause**: Template structure or field definitions are invalid

**Solution**:
```json
{
  "templateValidation": {
    "validateOnLoad": true,
    "repairCorruption": true,
    "backupBeforeRepair": true
  }
}
```

#### Error: "Insufficient memory to process document"

**Cause**: Document too large or system memory limited

**Solution**:
- Reduce document size by splitting into sections
- Increase system memory allocation
- Enable streaming processing mode

#### Error: "Template database connection failed"

**Cause**: Database connectivity issues

**Solution**:
```powershell
# Reset database connection
Reset-AnomaliDatabase -Repair -Optimize
```

---

## Advanced Debugging

### Debug Mode

Enable detailed logging for troubleshooting:

```json
{
  "debugSettings": {
    "enableDebugMode": true,
    "logLevel": "Verbose",
    "includeStackTrace": true,
    "saveIntermediateResults": true,
    "debugOutputPath": "C:\\Debug\\AnomaliImportTool\\"
  }
}
```

### Template Testing Tools

#### Pattern Tester
```javascript
// Test regex patterns
function testPattern(pattern, text) {
    const regex = new RegExp(pattern);
    const match = regex.exec(text);
    return {
        matched: match !== null,
        result: match ? match[1] : null,
        fullMatch: match ? match[0] : null
    };
}
```

#### Zone Validator
```json
{
  "zoneValidation": {
    "checkOverlaps": true,
    "validateCoordinates": true,
    "testExtraction": true,
    "generateReport": true
  }
}
```

### Performance Analysis

```powershell
# Comprehensive performance analysis
Start-PerformanceAnalysis -Template "template.json" -Documents @("doc1.pdf", "doc2.docx") -DetailedReport
```

---

## Getting Additional Help

### Self-Service Resources

1. **Knowledge Base**: Access built-in help system
2. **Video Tutorials**: Step-by-step video guides
3. **Sample Templates**: Download working template examples
4. **Community Forums**: Connect with other users

### Support Escalation

#### Level 1: Template Issues
- Review documentation and examples
- Check configuration settings
- Test with sample documents

#### Level 2: System Issues
- Check system requirements
- Review error logs
- Contact technical support

#### Level 3: Advanced Problems
- Provide detailed error logs
- Share problematic templates
- Work with engineering team

### Creating Support Tickets

Include the following information:
```
Template Name: [Template Name]
Document Type: [PDF/DOCX/XLSX]
Error Message: [Exact error text]
Steps to Reproduce: [Detailed steps]
Expected Behavior: [What should happen]
Actual Behavior: [What actually happens]
System Information: [OS, Memory, Version]
Log Files: [Attach relevant logs]
```

---

## Quick Reference

### Common Solutions

| Problem | Quick Fix |
|---------|-----------|
| No templates found | Lower confidence threshold |
| Blank extraction | Check OCR settings |
| Wrong data extracted | Update regex patterns |
| Slow performance | Optimize extraction zones |
| Memory errors | Enable batch processing |
| PDF not readable | Enable OCR preprocessing |

### Essential Commands

```powershell
# Test template
Test-AnomaliTemplate -Path "template.json" -Document "test.pdf"

# Clear cache
Clear-AnomaliCache -All

# Reset configuration
Reset-AnomaliConfiguration -Default

# Export logs
Export-AnomaliLogs -Path "C:\Logs\" -DateRange "Last7Days"
```

---

*This troubleshooting guide covers the most common issues encountered when working with templates. For additional support, consult the complete documentation or contact technical support.* 