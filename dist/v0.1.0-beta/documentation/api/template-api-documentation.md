# Template System API Documentation

**Anomali Threat Bulletin Import Tool - Integration & Extension API**  
**Version**: 1.0  
**Last Updated**: June 25, 2025

---

## Table of Contents

1. [API Overview](#api-overview)
2. [Authentication](#authentication)
3. [Template Management API](#template-management-api)
4. [Document Processing API](#document-processing-api)
5. [Template Matching API](#template-matching-api)
6. [Field Extraction API](#field-extraction-api)
7. [Plugin System](#plugin-system)
8. [Webhook Integration](#webhook-integration)
9. [SDK Documentation](#sdk-documentation)
10. [Error Handling](#error-handling)

---

## API Overview

### Base URL
```
https://api.anomali.local/template-system/v1
```

### Response Format
All API responses follow this standard format:
```json
{
  "success": true,
  "data": {...},
  "error": null,
  "timestamp": "2025-06-25T10:30:00Z",
  "requestId": "req_12345678-1234-1234-1234-123456789abc"
}
```

### Rate Limiting
- **Standard Operations**: 1000 requests per hour
- **Bulk Operations**: 100 requests per hour
- **Template Processing**: 500 documents per hour

### Supported Formats
- **Request**: JSON, multipart/form-data (for file uploads)
- **Response**: JSON
- **Document Formats**: PDF, DOCX, XLSX

---

## Authentication

### API Key Authentication
Include your API key in the header:
```http
Authorization: Bearer YOUR_API_KEY
Content-Type: application/json
```

### Service Account Authentication
For server-to-server integration:
```http
Authorization: ServiceAccount YOUR_SERVICE_ACCOUNT_KEY
Content-Type: application/json
```

### OAuth 2.0 Integration
For third-party applications:
```http
Authorization: Bearer YOUR_OAUTH_TOKEN
Content-Type: application/json
```

---

## Template Management API

### List Templates

**GET** `/templates`

List all available templates with optional filtering.

#### Parameters
```json
{
  "category": "Security",
  "tags": ["incident", "response"],
  "includeInactive": false,
  "page": 1,
  "limit": 50,
  "sortBy": "name",
  "sortOrder": "asc"
}
```

#### Response
```json
{
  "success": true,
  "data": {
    "templates": [
      {
        "id": "template_12345",
        "name": "Security Incident Report",
        "description": "Extract security incident details",
        "category": "Security",
        "tags": ["incident", "security", "response"],
        "version": "2.1.3",
        "isActive": true,
        "supportedFormats": ["pdf", "docx"],
        "createdAt": "2025-01-15T10:00:00Z",
        "lastModified": "2025-06-20T15:30:00Z",
        "createdBy": "user@company.com",
        "usageCount": 342,
        "averageAccuracy": 0.94
      }
    ],
    "pagination": {
      "page": 1,
      "limit": 50,
      "total": 127,
      "pages": 3
    }
  }
}
```

### Get Template Details

**GET** `/templates/{templateId}`

Retrieve detailed information about a specific template.

#### Response
```json
{
  "success": true,
  "data": {
    "id": "template_12345",
    "name": "Security Incident Report",
    "description": "Extract security incident details from reports",
    "category": "Security",
    "tags": ["incident", "security"],
    "version": "2.1.3",
    "isActive": true,
    "supportedFormats": ["pdf", "docx"],
    "fields": [
      {
        "id": "field_001",
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
        "validationRules": [
          {
            "ruleType": "Pattern",
            "pattern": "^[A-Z]{2,4}-\\d{4}-\\d+$",
            "errorMessage": "Invalid incident ID format"
          }
        ]
      }
    ],
    "documentMatchingCriteria": {
      "requiredKeywords": ["incident", "security"],
      "minimumConfidenceThreshold": 0.7,
      "autoApplicationThreshold": 0.9
    },
    "metadata": {
      "createdAt": "2025-01-15T10:00:00Z",
      "lastModified": "2025-06-20T15:30:00Z",
      "createdBy": "user@company.com",
      "usageStatistics": {
        "totalUsage": 342,
        "successfulExtractions": 325,
        "averageProcessingTime": 2.3,
        "averageAccuracy": 0.94
      }
    }
  }
}
```

### Create Template

**POST** `/templates`

Create a new template.

#### Request Body
```json
{
  "name": "New Security Template",
  "description": "Template for security reports",
  "category": "Security",
  "tags": ["security", "incident"],
  "supportedFormats": ["pdf", "docx"],
  "fields": [
    {
      "name": "ReportTitle",
      "displayName": "Report Title",
      "fieldType": "Text",
      "isRequired": true,
      "extractionRules": [
        {
          "ruleType": "RegexPattern",
          "pattern": "Title:\\s*(.+)",
          "priority": 1
        }
      ]
    }
  ],
  "documentMatchingCriteria": {
    "requiredKeywords": ["security", "report"],
    "minimumConfidenceThreshold": 0.6
  }
}
```

#### Response
```json
{
  "success": true,
  "data": {
    "id": "template_67890",
    "name": "New Security Template",
    "version": "1.0.0",
    "createdAt": "2025-06-25T10:30:00Z"
  }
}
```

### Update Template

**PUT** `/templates/{templateId}`

Update an existing template.

#### Request Body
```json
{
  "name": "Updated Security Template",
  "description": "Updated template description",
  "fields": [
    {
      "id": "field_001",
      "name": "UpdatedField",
      "displayName": "Updated Field",
      "isRequired": false
    }
  ],
  "versionNotes": "Added new field for enhanced extraction"
}
```

### Delete Template

**DELETE** `/templates/{templateId}`

Soft delete a template (marks as inactive).

#### Response
```json
{
  "success": true,
  "data": {
    "message": "Template marked as inactive",
    "templateId": "template_12345",
    "inactivatedAt": "2025-06-25T10:30:00Z"
  }
}
```

### Export Template

**GET** `/templates/{templateId}/export`

Export template in JSON format.

#### Parameters
- `includeMetadata`: Include usage statistics and metadata
- `includeDocumentation`: Include field descriptions and examples

#### Response
```json
{
  "success": true,
  "data": {
    "exportFormat": "json",
    "exportedAt": "2025-06-25T10:30:00Z",
    "template": {
      // Complete template configuration
    }
  }
}
```

### Import Template

**POST** `/templates/import`

Import template from JSON configuration.

#### Request Body
```json
{
  "templateData": {
    // Template configuration JSON
  },
  "importOptions": {
    "overwriteExisting": false,
    "validateBeforeImport": true,
    "createBackup": true
  }
}
```

---

## Document Processing API

### Process Document

**POST** `/documents/process`

Process a document using automatic template matching.

#### Request (Multipart Form)
```
POST /documents/process
Content-Type: multipart/form-data

file: [document.pdf]
options: {
  "autoSelectTemplate": true,
  "minimumConfidence": 0.7,
  "includeExtractedContent": true,
  "returnConfidenceScores": true
}
```

#### Response
```json
{
  "success": true,
  "data": {
    "documentId": "doc_12345",
    "processedAt": "2025-06-25T10:30:00Z",
    "template": {
      "id": "template_12345",
      "name": "Security Incident Report",
      "version": "2.1.3",
      "confidenceScore": 0.89
    },
    "extractedFields": [
      {
        "fieldName": "IncidentID",
        "value": "SEC-2025-001234",
        "confidence": 0.95,
        "extractionMethod": "regex",
        "location": {
          "page": 1,
          "coordinates": {"x": 100, "y": 80, "width": 150, "height": 20}
        }
      },
      {
        "fieldName": "ThreatLevel",
        "value": "HIGH",
        "confidence": 0.87,
        "extractionMethod": "pattern_match"
      }
    ],
    "processingMetrics": {
      "totalFields": 12,
      "extractedFields": 10,
      "completenessScore": 0.83,
      "processingTime": 2.4,
      "qualityScore": 0.91
    },
    "warnings": [
      {
        "field": "ContactEmail",
        "message": "Email format validation failed",
        "severity": "low"
      }
    ]
  }
}
```

### Process with Specific Template

**POST** `/documents/process/{templateId}`

Process a document using a specific template.

#### Request
```
POST /documents/process/template_12345
Content-Type: multipart/form-data

file: [document.pdf]
options: {
  "includeValidationErrors": true,
  "autoCorrectData": true,
  "returnRawExtraction": false
}
```

### Get Document Processing Status

**GET** `/documents/{documentId}/status`

Check the processing status of a document.

#### Response
```json
{
  "success": true,
  "data": {
    "documentId": "doc_12345",
    "status": "completed",
    "progress": 100,
    "startedAt": "2025-06-25T10:30:00Z",
    "completedAt": "2025-06-25T10:30:15Z",
    "processingTime": 15.2,
    "errors": [],
    "warnings": []
  }
}
```

---

## Template Matching API

### Find Matching Templates

**POST** `/templates/match`

Find templates that match a given document.

#### Request
```
POST /templates/match
Content-Type: multipart/form-data

file: [document.pdf]
options: {
  "minimumConfidence": 0.5,
  "maxResults": 10,
  "includeReasons": true,
  "categories": ["Security", "Legal"]
}
```

#### Response
```json
{
  "success": true,
  "data": {
    "documentFingerprint": {
      "format": "pdf",
      "pageCount": 3,
      "wordCount": 456,
      "language": "en",
      "keywords": ["security", "incident", "analysis"]
    },
    "matches": [
      {
        "template": {
          "id": "template_12345",
          "name": "Security Incident Report",
          "category": "Security"
        },
        "confidenceScore": 0.89,
        "matchingReasons": [
          "Keywords match: security, incident",
          "Document format: PDF",
          "Expected page count: 2-5 pages"
        ],
        "confidenceBreakdown": {
          "keywordMatch": 0.92,
          "formatMatch": 1.0,
          "structureMatch": 0.85,
          "contentMatch": 0.87
        }
      }
    ],
    "processingTime": 1.2
  }
}
```

### Analyze Template Performance

**GET** `/templates/{templateId}/performance`

Get performance analytics for a template.

#### Parameters
- `startDate`: Start date for analytics (ISO format)
- `endDate`: End date for analytics
- `includeDetails`: Include detailed breakdown

#### Response
```json
{
  "success": true,
  "data": {
    "templateId": "template_12345",
    "analysisperiod": {
      "startDate": "2025-05-01T00:00:00Z",
      "endDate": "2025-06-25T23:59:59Z"
    },
    "metrics": {
      "totalUsages": 156,
      "successfulExtractions": 148,
      "successRate": 0.95,
      "averageProcessingTime": 2.3,
      "averageAccuracy": 0.92,
      "averageCompleteness": 0.89
    },
    "fieldPerformance": [
      {
        "fieldName": "IncidentID",
        "extractionRate": 0.99,
        "accuracy": 0.97,
        "averageConfidence": 0.94
      }
    ],
    "trends": {
      "accuracyTrend": "improving",
      "usageTrend": "stable",
      "performanceTrend": "stable"
    },
    "recommendations": [
      "Consider updating regex pattern for ContactEmail field",
      "Template performing well overall"
    ]
  }
}
```

---

## Field Extraction API

### Extract Specific Field

**POST** `/extraction/field`

Extract a specific field from a document without using a template.

#### Request
```json
{
  "document": "base64_encoded_document",
  "extractionRule": {
    "fieldName": "InvoiceNumber",
    "ruleType": "RegexPattern",
    "pattern": "Invoice #:\\s*([A-Z0-9-]+)",
    "zones": [
      {"x": 100, "y": 50, "width": 200, "height": 30, "page": 1}
    ]
  },
  "options": {
    "enableOCR": true,
    "returnConfidence": true
  }
}
```

#### Response
```json
{
  "success": true,
  "data": {
    "fieldName": "InvoiceNumber",
    "value": "INV-2025-001234",
    "confidence": 0.94,
    "extractionMethod": "regex",
    "location": {
      "page": 1,
      "coordinates": {"x": 120, "y": 55, "width": 150, "height": 20}
    },
    "alternatives": [
      {"value": "INV-2025-01234", "confidence": 0.76}
    ]
  }
}
```

### Batch Field Extraction

**POST** `/extraction/batch`

Extract multiple fields from one or more documents.

#### Request
```json
{
  "documents": [
    {
      "id": "doc1",
      "content": "base64_encoded_content",
      "format": "pdf"
    }
  ],
  "extractionRules": [
    {
      "fieldName": "Title",
      "ruleType": "RegexPattern",
      "pattern": "Title:\\s*(.+)"
    },
    {
      "fieldName": "Date",
      "ruleType": "RegexPattern",
      "pattern": "Date:\\s*(\\d{4}-\\d{2}-\\d{2})"
    }
  ],
  "options": {
    "parallelProcessing": true,
    "failOnError": false
  }
}
```

---

## Plugin System

### Plugin Interface

Create custom document processors by implementing the IDocumentProcessor interface:

```csharp
public interface IDocumentProcessor
{
    string Name { get; }
    string Version { get; }
    string[] SupportedFormats { get; }
    
    Task<Document> ProcessDocumentAsync(string filePath, CancellationToken cancellationToken);
    Task<bool> CanProcessAsync(string filePath);
    Task<ProcessingCapabilities> GetCapabilitiesAsync();
}
```

### Custom Extraction Plugin

```csharp
[PluginExport("CustomExtractor", "1.0")]
public class CustomExtractionPlugin : IExtractionPlugin
{
    public async Task<ExtractionResult> ExtractAsync(
        Document document, 
        ExtractionRule rule, 
        ExtractionContext context)
    {
        // Custom extraction logic
        return new ExtractionResult
        {
            Value = extractedValue,
            Confidence = confidenceScore,
            Method = "custom_algorithm"
        };
    }
}
```

### Plugin Registration

**POST** `/plugins/register`

Register a new plugin with the system.

#### Request
```json
{
  "pluginName": "CustomExtractionPlugin",
  "version": "1.0.0",
  "description": "Custom extraction algorithm for specialized documents",
  "supportedFormats": ["pdf"],
  "entryPoint": "CustomExtractionPlugin.dll",
  "configuration": {
    "algorithmType": "advanced",
    "enableDebugging": false
  }
}
```

### Plugin Management

**GET** `/plugins`
List all registered plugins.

**GET** `/plugins/{pluginId}`
Get plugin details.

**PUT** `/plugins/{pluginId}/enable`
Enable a plugin.

**PUT** `/plugins/{pluginId}/disable`
Disable a plugin.

---

## Webhook Integration

### Webhook Configuration

**POST** `/webhooks`

Configure webhooks for template system events.

#### Request
```json
{
  "name": "Document Processing Webhook",
  "url": "https://your-server.com/webhook/endpoint",
  "events": [
    "document.processed",
    "template.created",
    "extraction.completed",
    "extraction.failed"
  ],
  "secret": "your_webhook_secret",
  "retryPolicy": {
    "maxRetries": 3,
    "retryDelay": 5000
  },
  "headers": {
    "X-Custom-Header": "custom-value"
  }
}
```

### Webhook Events

#### Document Processed
```json
{
  "event": "document.processed",
  "timestamp": "2025-06-25T10:30:00Z",
  "data": {
    "documentId": "doc_12345",
    "templateId": "template_12345",
    "extractedFields": {...},
    "processingTime": 2.4,
    "success": true
  }
}
```

#### Template Created
```json
{
  "event": "template.created",
  "timestamp": "2025-06-25T10:30:00Z",
  "data": {
    "templateId": "template_67890",
    "name": "New Security Template",
    "createdBy": "user@company.com",
    "version": "1.0.0"
  }
}
```

#### Extraction Failed
```json
{
  "event": "extraction.failed",
  "timestamp": "2025-06-25T10:30:00Z",
  "data": {
    "documentId": "doc_12345",
    "templateId": "template_12345",
    "error": {
      "code": "EXTRACTION_TIMEOUT",
      "message": "Document processing timed out",
      "details": {...}
    }
  }
}
```

---

## SDK Documentation

### .NET SDK

#### Installation
```bash
Install-Package AnomaliImportTool.SDK
```

#### Basic Usage
```csharp
using AnomaliImportTool.SDK;

var client = new TemplateApiClient("YOUR_API_KEY");

// Process document
var result = await client.ProcessDocumentAsync("document.pdf");

// Get templates
var templates = await client.GetTemplatesAsync(new TemplateFilter
{
    Category = "Security",
    IncludeInactive = false
});

// Create template
var template = new Template
{
    Name = "New Template",
    Category = "Security",
    Fields = new List<TemplateField> { ... }
};
var created = await client.CreateTemplateAsync(template);
```

### Python SDK

#### Installation
```bash
pip install anomali-import-tool-sdk
```

#### Basic Usage
```python
from anomali_import_tool import TemplateClient

client = TemplateClient(api_key="YOUR_API_KEY")

# Process document
result = client.process_document("document.pdf")

# Get templates
templates = client.get_templates(category="Security")

# Create template
template = {
    "name": "New Template",
    "category": "Security",
    "fields": [...]
}
created = client.create_template(template)
```

### JavaScript SDK

#### Installation
```bash
npm install @anomali/import-tool-sdk
```

#### Basic Usage
```javascript
import { TemplateClient } from '@anomali/import-tool-sdk';

const client = new TemplateClient({ apiKey: 'YOUR_API_KEY' });

// Process document
const result = await client.processDocument('document.pdf');

// Get templates
const templates = await client.getTemplates({ category: 'Security' });

// Create template
const template = {
  name: 'New Template',
  category: 'Security',
  fields: [...]
};
const created = await client.createTemplate(template);
```

---

## Error Handling

### Error Response Format

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid template configuration",
    "details": {
      "field": "extractionRules",
      "reason": "Regex pattern is invalid"
    },
    "suggestions": [
      "Check regex pattern syntax",
      "Refer to pattern documentation"
    ]
  },
  "timestamp": "2025-06-25T10:30:00Z",
  "requestId": "req_12345678-1234-1234-1234-123456789abc"
}
```

### Error Codes

| Code | Description | HTTP Status |
|------|-------------|-------------|
| `INVALID_API_KEY` | API key is invalid or expired | 401 |
| `INSUFFICIENT_PERMISSIONS` | User lacks required permissions | 403 |
| `TEMPLATE_NOT_FOUND` | Template ID does not exist | 404 |
| `VALIDATION_ERROR` | Request validation failed | 400 |
| `PROCESSING_ERROR` | Document processing failed | 422 |
| `RATE_LIMIT_EXCEEDED` | API rate limit exceeded | 429 |
| `INTERNAL_ERROR` | Unexpected server error | 500 |
| `SERVICE_UNAVAILABLE` | Service temporarily unavailable | 503 |

### Retry Strategies

#### Exponential Backoff
```javascript
async function retryWithBackoff(operation, maxRetries = 3) {
  for (let attempt = 1; attempt <= maxRetries; attempt++) {
    try {
      return await operation();
    } catch (error) {
      if (attempt === maxRetries) throw error;
      
      const delay = Math.pow(2, attempt) * 1000; // Exponential backoff
      await new Promise(resolve => setTimeout(resolve, delay));
    }
  }
}
```

#### Circuit Breaker Pattern
```csharp
public class CircuitBreakerClient
{
    private readonly ICircuitBreaker _circuitBreaker;
    
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        return await _circuitBreaker.ExecuteAsync(operation);
    }
}
```

---

## Rate Limiting

### Headers
All responses include rate limiting headers:
```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 995
X-RateLimit-Reset: 1640995200
```

### Rate Limit Handling
```javascript
const response = await fetch('/api/templates', {
  headers: { 'Authorization': 'Bearer YOUR_TOKEN' }
});

if (response.status === 429) {
  const resetTime = response.headers.get('X-RateLimit-Reset');
  const delay = (resetTime * 1000) - Date.now();
  
  console.log(`Rate limited. Retry after ${delay}ms`);
  await new Promise(resolve => setTimeout(resolve, delay));
}
```

---

## API Versioning

### Version Strategy
- **Current Version**: v1
- **Deprecation Policy**: 12 months notice
- **Backward Compatibility**: Maintained within major versions

### Version Header
```http
Accept: application/vnd.anomali.v1+json
```

### Migration Guide
When upgrading API versions:
1. Review changelog for breaking changes
2. Update SDK to compatible version
3. Test in staging environment
4. Gradual rollout to production

---

*This API documentation provides comprehensive integration guidance for the Template System. For additional support, consult the SDK documentation or contact the development team.* 