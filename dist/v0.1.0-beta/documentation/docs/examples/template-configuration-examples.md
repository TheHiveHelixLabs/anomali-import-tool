# Template Configuration Examples

**Anomali Threat Bulletin Import Tool - Document Type Examples**  
**Version**: 1.0  
**Last Updated**: June 25, 2025

---

## Table of Contents

1. [PDF Document Templates](#pdf-document-templates)
2. [Microsoft Word Templates](#microsoft-word-templates)
3. [Microsoft Excel Templates](#microsoft-excel-templates)
4. [Multi-Format Templates](#multi-format-templates)
5. [Industry-Specific Examples](#industry-specific-examples)

---

## PDF Document Templates

### Example 1: Security Incident Report (PDF)

**Use Case**: Extract key information from security incident reports in PDF format.

```json
{
  "templateName": "Security Incident Report PDF",
  "description": "Extracts incident details from standardized security reports",
  "category": "Security",
  "supportedFormats": ["pdf"],
  "fields": [
    {
      "name": "IncidentID",
      "displayName": "Incident ID",
      "fieldType": "Text",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "Incident ID:\\s*([A-Z0-9-]+)",
          "priority": 1
        }
      ],
      "extractionZones": [
        {
          "x": 100,
          "y": 80,
          "width": 200,
          "height": 25,
          "pageNumber": 1
        }
      ]
    },
    {
      "name": "ThreatLevel",
      "displayName": "Threat Level",
      "fieldType": "DropdownList",
      "isRequired": true,
      "options": ["LOW", "MEDIUM", "HIGH", "CRITICAL"],
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "Threat Level:\\s*(LOW|MEDIUM|HIGH|CRITICAL)",
          "priority": 1
        }
      ]
    },
    {
      "name": "IncidentDate",
      "displayName": "Incident Date",
      "fieldType": "Date",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "Date:\\s*(\\d{4}-\\d{2}-\\d{2})",
          "priority": 1
        },
        {
          "ruleType": "RegexPattern",
          "pattern": "(\\d{2}/\\d{2}/\\d{4})",
          "priority": 2
        }
      ]
    },
    {
      "name": "AffectedSystems",
      "displayName": "Affected Systems",
      "fieldType": "Text",
      "isRequired": false,
      "extractionZones": [
        {
          "x": 50,
          "y": 200,
          "width": 500,
          "height": 100,
          "pageNumber": 1
        }
      ]
    },
    {
      "name": "IOCIndicators",
      "displayName": "IOC Indicators",
      "fieldType": "Text",
      "isRequired": false,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "IOC:\\s*([a-fA-F0-9]{32,64})",
          "priority": 1
        }
      ]
    }
  ],
  "conditionalRules": [
    {
      "condition": "document.contains('MALWARE')",
      "action": "ExtractField",
      "targetFieldName": "MalwareFamily"
    }
  ]
}
```

### Example 2: Financial Audit Report (PDF)

**Use Case**: Extract financial data from audit reports with tabular data.

```json
{
  "templateName": "Financial Audit Report PDF",
  "description": "Extracts audit findings and financial metrics",
  "category": "Financial",
  "supportedFormats": ["pdf"],
  "fields": [
    {
      "name": "AuditPeriod",
      "displayName": "Audit Period",
      "fieldType": "Text",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "Audit Period:\\s*([^\\n]+)",
          "priority": 1
        }
      ]
    },
    {
      "name": "TotalRevenue",
      "displayName": "Total Revenue",
      "fieldType": "Number",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "Total Revenue:?\\s*\\$?([0-9,]+(?:\\.\\d{2})?)",
          "priority": 1
        }
      ],
      "validationRules": [
        {
          "ruleType": "Range",
          "minValue": 0,
          "maxValue": 999999999.99
        }
      ]
    },
    {
      "name": "AuditorName",
      "displayName": "Lead Auditor",
      "fieldType": "Text",
      "isRequired": true,
      "extractionZones": [
        {
          "x": 400,
          "y": 700,
          "width": 150,
          "height": 20,
          "pageNumber": 1
        }
      ]
    },
    {
      "name": "FindingsCount",
      "displayName": "Number of Findings",
      "fieldType": "Number",
      "isRequired": false,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "Total Findings:\\s*(\\d+)",
          "priority": 1
        }
      ]
    }
  ]
}
```

---

## Microsoft Word Templates

### Example 3: Contract Document (DOCX)

**Use Case**: Extract contract details from Word documents with structured headers.

```json
{
  "templateName": "Contract Document DOCX",
  "description": "Extracts key contract terms and parties",
  "category": "Legal",
  "supportedFormats": ["docx"],
  "fields": [
    {
      "name": "ContractTitle",
      "displayName": "Contract Title",
      "fieldType": "Text",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "StyleBasedExtraction",
          "stylePattern": "Heading 1",
          "priority": 1
        },
        {
          "ruleType": "RegexPattern",
          "pattern": "CONTRACT:\\s*([^\\n]+)",
          "priority": 2
        }
      ]
    },
    {
      "name": "PartyA",
      "displayName": "First Party",
      "fieldType": "Text",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "Party A[:\\s]*([^\\n,]+)",
          "priority": 1
        },
        {
          "ruleType": "RegexPattern",
          "pattern": "between\\s+([^\\n,]+)\\s+and",
          "priority": 2
        }
      ]
    },
    {
      "name": "PartyB",
      "displayName": "Second Party",
      "fieldType": "Text",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "Party B[:\\s]*([^\\n,]+)",
          "priority": 1
        },
        {
          "ruleType": "RegexPattern",
          "pattern": "and\\s+([^\\n,]+)",
          "priority": 2
        }
      ]
    },
    {
      "name": "ContractValue",
      "displayName": "Contract Value",
      "fieldType": "Number",
      "isRequired": false,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "Value:?\\s*\\$?([0-9,]+(?:\\.\\d{2})?)",
          "priority": 1
        },
        {
          "ruleType": "RegexPattern",
          "pattern": "amount of\\s*\\$?([0-9,]+(?:\\.\\d{2})?)",
          "priority": 2
        }
      ]
    },
    {
      "name": "EffectiveDate",
      "displayName": "Effective Date",
      "fieldType": "Date",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "Effective Date:\\s*(\\d{1,2}/\\d{1,2}/\\d{4})",
          "priority": 1
        },
        {
          "ruleType": "RegexPattern",
          "pattern": "effective\\s+(\\d{1,2}/\\d{1,2}/\\d{4})",
          "priority": 2
        }
      ]
    },
    {
      "name": "TerminationDate",
      "displayName": "Termination Date",
      "fieldType": "Date",
      "isRequired": false,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "Termination Date:\\s*(\\d{1,2}/\\d{1,2}/\\d{4})",
          "priority": 1
        }
      ]
    }
  ],
  "conditionalRules": [
    {
      "condition": "document.contains('NON-DISCLOSURE')",
      "action": "SetFieldValue",
      "targetFieldName": "ContractType",
      "value": "NDA"
    }
  ]
}
```

### Example 4: Technical Documentation (DOCX)

**Use Case**: Extract metadata and content from technical documentation.

```json
{
  "templateName": "Technical Documentation DOCX",
  "description": "Extracts technical document metadata and sections",
  "category": "Technical",
  "supportedFormats": ["docx"],
  "fields": [
    {
      "name": "DocumentTitle",
      "displayName": "Document Title",
      "fieldType": "Text",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "DocumentProperty",
          "propertyName": "Title",
          "priority": 1
        },
        {
          "ruleType": "StyleBasedExtraction",
          "stylePattern": "Title",
          "priority": 2
        }
      ]
    },
    {
      "name": "Author",
      "displayName": "Author",
      "fieldType": "Text",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "DocumentProperty",
          "propertyName": "Author",
          "priority": 1
        }
      ]
    },
    {
      "name": "Version",
      "displayName": "Document Version",
      "fieldType": "Text",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "Version:?\\s*([0-9.]+)",
          "priority": 1
        },
        {
          "ruleType": "RegexPattern",
          "pattern": "v([0-9.]+)",
          "priority": 2
        }
      ]
    },
    {
      "name": "LastModified",
      "displayName": "Last Modified",
      "fieldType": "Date",
      "isRequired": false,
      "extractionRules": [
        {
          "ruleType": "DocumentProperty",
          "propertyName": "LastModifiedTime",
          "priority": 1
        }
      ]
    },
    {
      "name": "PageCount",
      "displayName": "Page Count",
      "fieldType": "Number",
      "isRequired": false,
      "extractionRules": [
        {
          "ruleType": "DocumentProperty",
          "propertyName": "Pages",
          "priority": 1
        }
      ]
    }
  ]
}
```

---

## Microsoft Excel Templates

### Example 5: Financial Spreadsheet (XLSX)

**Use Case**: Extract financial data from Excel spreadsheets with specific cell references.

```json
{
  "templateName": "Financial Spreadsheet XLSX",
  "description": "Extracts financial metrics from Excel workbooks",
  "category": "Financial",
  "supportedFormats": ["xlsx"],
  "fields": [
    {
      "name": "ReportingPeriod",
      "displayName": "Reporting Period",
      "fieldType": "Text",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "CellReference",
          "cellAddress": "B2",
          "worksheetName": "Summary",
          "priority": 1
        }
      ]
    },
    {
      "name": "TotalRevenue",
      "displayName": "Total Revenue",
      "fieldType": "Number",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "CellReference",
          "cellAddress": "C5",
          "worksheetName": "Financial Summary",
          "priority": 1
        },
        {
          "ruleType": "NamedRange",
          "rangeName": "TotalRevenue",
          "priority": 2
        }
      ]
    },
    {
      "name": "NetIncome",
      "displayName": "Net Income",
      "fieldType": "Number",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "CellReference",
          "cellAddress": "C15",
          "worksheetName": "Financial Summary",
          "priority": 1
        }
      ]
    },
    {
      "name": "OperatingExpenses",
      "displayName": "Operating Expenses",
      "fieldType": "Number",
      "isRequired": false,
      "extractionRules": [
        {
          "ruleType": "CellSearch",
          "searchPattern": "Operating Expenses",
          "offsetX": 1,
          "offsetY": 0,
          "priority": 1
        }
      ]
    },
    {
      "name": "PreparedBy",
      "displayName": "Prepared By",
      "fieldType": "Text",
      "isRequired": false,
      "extractionRules": [
        {
          "ruleType": "CellReference",
          "cellAddress": "B25",
          "worksheetName": "Summary",
          "priority": 1
        }
      ]
    }
  ],
  "conditionalRules": [
    {
      "condition": "worksheet.exists('Quarterly')",
      "action": "ExtractField",
      "targetFieldName": "QuarterlyData"
    }
  ]
}
```

### Example 6: Inventory Report (XLSX)

**Use Case**: Extract inventory data from Excel reports with dynamic ranges.

```json
{
  "templateName": "Inventory Report XLSX",
  "description": "Extracts inventory metrics and product data",
  "category": "Operations",
  "supportedFormats": ["xlsx"],
  "fields": [
    {
      "name": "ReportDate",
      "displayName": "Report Date",
      "fieldType": "Date",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "CellReference",
          "cellAddress": "A1",
          "worksheetName": "Inventory",
          "priority": 1
        }
      ]
    },
    {
      "name": "TotalItems",
      "displayName": "Total Items",
      "fieldType": "Number",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "TableFunction",
          "functionType": "COUNT",
          "tableRange": "A5:F100",
          "columnName": "Product ID",
          "priority": 1
        }
      ]
    },
    {
      "name": "TotalValue",
      "displayName": "Total Inventory Value",
      "fieldType": "Number",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "TableFunction",
          "functionType": "SUM",
          "tableRange": "A5:F100",
          "columnName": "Total Value",
          "priority": 1
        }
      ]
    },
    {
      "name": "LowStockItems",
      "displayName": "Low Stock Items",
      "fieldType": "Number",
      "isRequired": false,
      "extractionRules": [
        {
          "ruleType": "ConditionalCount",
          "tableRange": "A5:F100",
          "columnName": "Quantity",
          "condition": "< 10",
          "priority": 1
        }
      ]
    }
  ]
}
```

---

## Multi-Format Templates

### Example 7: Universal Report Template

**Use Case**: Handle reports that might come in PDF, Word, or Excel formats.

```json
{
  "templateName": "Universal Report Template",
  "description": "Flexible template for reports in multiple formats",
  "category": "Universal",
  "supportedFormats": ["pdf", "docx", "xlsx"],
  "fields": [
    {
      "name": "ReportTitle",
      "displayName": "Report Title",
      "fieldType": "Text",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "(?:Title|Report):?\\s*([^\\n]+)",
          "priority": 1,
          "applicableFormats": ["pdf", "docx"]
        },
        {
          "ruleType": "CellReference",
          "cellAddress": "A1",
          "priority": 1,
          "applicableFormats": ["xlsx"]
        },
        {
          "ruleType": "StyleBasedExtraction",
          "stylePattern": "Title",
          "priority": 2,
          "applicableFormats": ["docx"]
        }
      ]
    },
    {
      "name": "ReportDate",
      "displayName": "Report Date",
      "fieldType": "Date",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "Date:?\\s*(\\d{1,2}/\\d{1,2}/\\d{4})",
          "priority": 1,
          "applicableFormats": ["pdf", "docx"]
        },
        {
          "ruleType": "CellReference",
          "cellAddress": "B2",
          "priority": 1,
          "applicableFormats": ["xlsx"]
        }
      ]
    },
    {
      "name": "Summary",
      "displayName": "Executive Summary",
      "fieldType": "Text",
      "isRequired": false,
      "extractionRules": [
        {
          "ruleType": "SectionExtraction",
          "sectionHeader": "Executive Summary",
          "priority": 1,
          "applicableFormats": ["pdf", "docx"]
        },
        {
          "ruleType": "CellReference",
          "cellAddress": "A10",
          "priority": 1,
          "applicableFormats": ["xlsx"]
        }
      ]
    }
  ]
}
```

---

## Industry-Specific Examples

### Example 8: Threat Intelligence Report

**Use Case**: Cybersecurity threat intelligence with IOC extraction.

```json
{
  "templateName": "Threat Intelligence Report",
  "description": "Extracts threat indicators and analysis data",
  "category": "Cybersecurity",
  "supportedFormats": ["pdf", "docx"],
  "fields": [
    {
      "name": "ThreatActor",
      "displayName": "Threat Actor",
      "fieldType": "Text",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "(?:Threat Actor|Actor|Group):?\\s*([^\\n]+)",
          "priority": 1
        }
      ]
    },
    {
      "name": "MD5Hashes",
      "displayName": "MD5 Hashes",
      "fieldType": "Text",
      "isRequired": false,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "([a-fA-F0-9]{32})",
          "priority": 1,
          "extractionMode": "All"
        }
      ]
    },
    {
      "name": "SHA256Hashes",
      "displayName": "SHA256 Hashes",
      "fieldType": "Text",
      "isRequired": false,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "([a-fA-F0-9]{64})",
          "priority": 1,
          "extractionMode": "All"
        }
      ]
    },
    {
      "name": "IPAddresses",
      "displayName": "IP Addresses",
      "fieldType": "Text",
      "isRequired": false,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "\\b(?:[0-9]{1,3}\\.){3}[0-9]{1,3}\\b",
          "priority": 1,
          "extractionMode": "All"
        }
      ]
    },
    {
      "name": "Domains",
      "displayName": "Malicious Domains",
      "fieldType": "Text",
      "isRequired": false,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "([a-zA-Z0-9.-]+\\.[a-zA-Z]{2,})",
          "priority": 1,
          "extractionMode": "All"
        }
      ]
    },
    {
      "name": "MITRE_TTPs",
      "displayName": "MITRE ATT&CK TTPs",
      "fieldType": "Text",
      "isRequired": false,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "(T\\d{4}(?:\\.\\d{3})?)",
          "priority": 1,
          "extractionMode": "All"
        }
      ]
    }
  ]
}
```

### Example 9: Legal Contract Analysis

**Use Case**: Extract key terms from legal contracts with clause identification.

```json
{
  "templateName": "Legal Contract Analysis",
  "description": "Comprehensive contract term extraction",
  "category": "Legal",
  "supportedFormats": ["pdf", "docx"],
  "fields": [
    {
      "name": "ContractType",
      "displayName": "Contract Type",
      "fieldType": "DropdownList",
      "isRequired": true,
      "options": ["Service Agreement", "NDA", "Employment", "Purchase Order", "Lease"],
      "extractionRules": [
        {
          "ruleType": "KeywordMapping",
          "keywordMap": {
            "non-disclosure": "NDA",
            "service agreement": "Service Agreement",
            "employment": "Employment",
            "purchase": "Purchase Order",
            "lease": "Lease"
          },
          "priority": 1
        }
      ]
    },
    {
      "name": "GoverningLaw",
      "displayName": "Governing Law",
      "fieldType": "Text",
      "isRequired": false,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "governed by.*?laws of\\s+([^\\n.,]+)",
          "priority": 1
        }
      ]
    },
    {
      "name": "LiabilityClause",
      "displayName": "Liability Limitation",
      "fieldType": "Text",
      "isRequired": false,
      "extractionRules": [
        {
          "ruleType": "SectionExtraction",
          "sectionHeader": "Limitation of Liability",
          "priority": 1
        }
      ]
    },
    {
      "name": "TerminationClause",
      "displayName": "Termination Terms",
      "fieldType": "Text",
      "isRequired": false,
      "extractionRules": [
        {
          "ruleType": "SectionExtraction",
          "sectionHeader": "Termination",
          "priority": 1
        }
      ]
    }
  ]
}
```

---

## Configuration Notes

### Performance Optimization

1. **Extraction Zone Priority**: Use coordinate zones for consistent layouts, patterns for variable formats
2. **Rule Ordering**: Place most specific rules first, general fallbacks last
3. **Format-Specific Rules**: Use `applicableFormats` to optimize processing per document type
4. **Conditional Logic**: Implement smart extraction based on document content

### Validation Best Practices

1. **Required Fields**: Mark truly essential fields to ensure data completeness
2. **Data Types**: Use appropriate field types for automatic validation
3. **Range Validation**: Set realistic min/max values for numeric fields
4. **Pattern Validation**: Use regex validation for structured data like emails, IDs

### Maintenance Guidelines

1. **Regular Testing**: Test templates with new document variations
2. **Version Control**: Maintain template versions and change logs
3. **User Feedback**: Collect accuracy feedback from end users
4. **Performance Monitoring**: Track extraction success rates and processing times

---

*These examples serve as starting points for your template configurations. Customize field names, extraction rules, and validation criteria based on your specific document formats and business requirements.* 