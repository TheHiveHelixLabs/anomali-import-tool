# Import Template System User Guide

**Anomali Threat Bulletin Import Tool - Template Creation & Management**  
**Version**: 1.0  
**Last Updated**: June 25, 2025

---

## Table of Contents

1. [Overview](#overview)
2. [Getting Started](#getting-started)
3. [Creating Your First Template](#creating-your-first-template)
4. [Advanced Template Features](#advanced-template-features)
5. [Template Management](#template-management)
6. [Best Practices](#best-practices)
7. [Troubleshooting](#troubleshooting)

---

## Overview

The Import Template System is a powerful feature that automates document field extraction by creating reusable templates. These templates can intelligently identify and extract specific data fields from your documents, dramatically reducing manual data entry and improving accuracy.

### Key Benefits

- **Automated Data Extraction**: Eliminate manual copy-paste operations
- **Consistency**: Ensure standardized data extraction across your organization
- **Efficiency**: Process multiple documents with a single template
- **Accuracy**: Reduce human errors in data entry
- **Scalability**: Share templates across teams and departments

### Supported Document Types

- **PDF Documents**: Text-based and scanned PDFs with OCR support
- **Microsoft Word**: .docx files with rich content extraction
- **Microsoft Excel**: .xlsx files with cell-level data extraction

---

## Getting Started

### Accessing the Template System

1. Launch the Anomali Import Tool
2. Navigate to **Template Management** from the main menu
3. Click **Create New Template** to begin

### System Requirements

- Minimum 4GB RAM for optimal performance
- 100MB free disk space for template storage
- Network connectivity for template sharing (optional)

---

## Creating Your First Template

### Step 1: Template Basic Information

1. **Template Name**: Choose a descriptive name (e.g., "Security Incident Report")
2. **Description**: Provide context about the template's purpose
3. **Category**: Select or create a category for organization
4. **Supported Formats**: Choose document types this template supports

```
Example:
Name: "Threat Intelligence Report Template"
Description: "Extracts key threat indicators from security reports"
Category: "Security"
Formats: PDF, DOCX
```

### Step 2: Define Extraction Fields

Each template needs at least one field to extract data. Common field types include:

#### Text Fields
- **Purpose**: Extract text content like names, titles, descriptions
- **Configuration**: Set field name, display label, and whether it's required
- **Example**: Report Title, Analyst Name, Executive Summary

#### Number Fields
- **Purpose**: Extract numeric values with validation
- **Configuration**: Set minimum/maximum values, decimal places
- **Example**: Severity Score (1-10), Budget Amount, Count of Indicators

#### Date Fields
- **Purpose**: Extract dates in various formats
- **Configuration**: Set date format patterns and validation rules
- **Example**: Report Date, Incident Date, Expiration Date

#### Email Fields
- **Purpose**: Extract and validate email addresses
- **Configuration**: Automatic email format validation
- **Example**: Contact Email, Reporter Email, Escalation Contact

#### Dropdown Fields
- **Purpose**: Extract values from predefined options
- **Configuration**: Set available options and default values
- **Example**: Threat Level (Low/Medium/High/Critical), Status, Priority

### Step 3: Configure Extraction Rules

For each field, you can define how the system should find and extract data:

#### Method 1: Visual Zone Selection
1. Upload a sample document
2. Click and drag to select the area containing your field
3. The system automatically creates coordinate-based extraction zones
4. Best for consistent document layouts

#### Method 2: Pattern Matching
1. Define regex patterns for flexible text extraction
2. Use keyword searches for simple text matching
3. Best for documents with varying layouts

```
Examples:
- Report Title: Pattern "Title:\s*(.+)"
- Email Address: Pattern "[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}"
- Date: Pattern "\d{4}-\d{2}-\d{2}"
```

### Step 4: Test Your Template

1. **Load Test Document**: Upload a sample document
2. **Run Extraction**: Test the template against your sample
3. **Review Results**: Verify extracted data accuracy
4. **Refine Rules**: Adjust extraction patterns as needed
5. **Validate**: Ensure all required fields are extracted correctly

### Step 5: Save and Deploy

1. **Save Template**: Store your template in the system
2. **Set Permissions**: Configure who can use the template
3. **Share Template**: Export for use by other team members
4. **Monitor Usage**: Track template performance and accuracy

---

## Advanced Template Features

### Template Inheritance

Create parent templates with common fields and child templates that extend them:

1. **Create Parent Template**: Define common fields used across document types
2. **Create Child Template**: Inherit parent fields and add specific ones
3. **Benefits**: Maintain consistency while allowing specialization

```
Example Hierarchy:
Parent: "Base Security Report"
├── Child: "Incident Response Report"
├── Child: "Threat Intelligence Report"
└── Child: "Vulnerability Assessment Report"
```

### Conditional Extraction Rules

Set up rules that extract fields based on document content:

1. **Condition**: Define when the rule should apply
2. **Action**: Specify what to do when condition is met
3. **Target**: Choose which field to affect

```
Examples:
- If document contains "CONFIDENTIAL" → Extract Classification field
- If document type is "Invoice" → Extract Invoice Number
- If threat level is "HIGH" → Extract additional IOC fields
```

### Multi-Page Document Handling

Configure extraction for documents spanning multiple pages:

1. **Page-Specific Zones**: Define different extraction areas per page
2. **Cross-Page Fields**: Extract content that spans multiple pages
3. **Dynamic Pages**: Handle documents with variable page counts

### OCR Configuration

Optimize text recognition for scanned documents:

1. **Language Settings**: Configure OCR for specific languages
2. **Image Quality**: Adjust for different scan qualities
3. **Text Preprocessing**: Enable image enhancement for better recognition

---

## Template Management

### Organizing Templates

#### Categories and Tags
- Use categories for high-level organization (Security, Legal, Financial)
- Add tags for detailed classification (urgent, quarterly, external)
- Create naming conventions for easy identification

#### Version Control
- Templates automatically track version history
- Compare different versions to see changes
- Rollback to previous versions if needed
- Track who made changes and when

### Sharing Templates

#### Export Templates
1. Select templates to export
2. Choose export format (JSON)
3. Save to file or shared location
4. Include documentation for recipients

#### Import Templates
1. Navigate to Template Import
2. Select JSON file or paste content
3. Review template details
4. Confirm import and resolve conflicts

#### Team Collaboration
- Share templates via network drives
- Email template files to team members
- Use version control systems for large organizations
- Document template changes and usage guidelines

### Template Performance Monitoring

#### Usage Statistics
- Track how often templates are used
- Monitor extraction success rates
- Identify templates needing optimization
- Analyze processing times and accuracy

#### Performance Optimization
- Review extraction rules effectiveness
- Update patterns based on new document formats
- Optimize coordinate zones for better accuracy
- Remove unused or redundant fields

---

## Best Practices

### Template Design

1. **Start Simple**: Begin with essential fields and expand gradually
2. **Test Thoroughly**: Use multiple sample documents during development
3. **Document Everything**: Maintain clear descriptions and usage notes
4. **Regular Updates**: Keep templates current with document format changes

### Field Configuration

1. **Meaningful Names**: Use clear, descriptive field names
2. **Validation Rules**: Implement appropriate data validation
3. **Default Values**: Set sensible defaults where applicable
4. **Required Fields**: Mark truly essential fields as required

### Extraction Rules

1. **Specific Patterns**: Use precise regex patterns to avoid false matches
2. **Fallback Methods**: Provide multiple extraction methods for reliability
3. **Zone Accuracy**: Ensure extraction zones are precisely positioned
4. **Performance**: Balance accuracy with processing speed

### Maintenance

1. **Regular Testing**: Periodically test templates with new documents
2. **User Feedback**: Collect feedback from template users
3. **Performance Review**: Monitor and optimize template performance
4. **Documentation Updates**: Keep user guides and examples current

---

## Troubleshooting

### Common Issues

#### Template Not Extracting Data
1. **Check Document Format**: Ensure document type is supported
2. **Verify Zones**: Confirm extraction zones are properly positioned
3. **Test Patterns**: Validate regex patterns with sample text
4. **Review Logs**: Check system logs for error messages

#### Low Extraction Accuracy
1. **Refine Patterns**: Improve regex patterns for better matching
2. **Adjust Zones**: Fine-tune coordinate-based extraction areas
3. **Add Alternatives**: Provide multiple extraction methods
4. **Update Training**: Use recent documents for pattern development

#### Performance Issues
1. **Simplify Rules**: Reduce complex extraction patterns
2. **Optimize Zones**: Use smaller, more targeted extraction areas
3. **Remove Unused Fields**: Delete fields that aren't needed
4. **Update Hardware**: Ensure system meets performance requirements

### Getting Help

#### Documentation Resources
- Template Configuration Examples
- API Documentation
- Troubleshooting Guide
- Best Practices Guide

#### Support Channels
- System Administrator
- User Community Forums
- Technical Support Team
- Training Resources

#### Self-Service Tools
- Template Validator
- Performance Analyzer
- Pattern Tester
- Sample Document Library

---

## Quick Reference

### Template Creation Checklist

- [ ] Choose descriptive template name
- [ ] Set appropriate category and tags
- [ ] Define required and optional fields
- [ ] Configure extraction rules (zones or patterns)
- [ ] Test with multiple sample documents
- [ ] Validate all extraction results
- [ ] Document template purpose and usage
- [ ] Set appropriate permissions
- [ ] Save and deploy template

### Keyboard Shortcuts

- **Ctrl+N**: Create new template
- **Ctrl+S**: Save template
- **Ctrl+T**: Test template
- **F5**: Refresh template list
- **Ctrl+E**: Export template
- **Ctrl+I**: Import template

### File Locations

- **Templates**: `%AppData%/AnomaliImportTool/Templates/`
- **Logs**: `%AppData%/AnomaliImportTool/Logs/`
- **Exports**: `%UserProfile%/Downloads/`
- **Samples**: `%ProgramFiles%/AnomaliImportTool/Samples/`

---

*For additional support and advanced configuration options, please refer to the complete documentation suite or contact your system administrator.* 