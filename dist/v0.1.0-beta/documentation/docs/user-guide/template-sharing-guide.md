# Template Sharing and Export/Import Guide

**Anomali Threat Bulletin Import Tool - Organizational Deployment**  
**Version**: 1.0  
**Last Updated**: June 25, 2025

---

## Table of Contents

1. [Overview](#overview)
2. [Template Export Procedures](#template-export-procedures)
3. [Template Import Procedures](#template-import-procedures)
4. [Organizational Deployment Strategies](#organizational-deployment-strategies)
5. [Template Distribution Methods](#template-distribution-methods)
6. [Version Control and Management](#version-control-and-management)
7. [Security and Access Control](#security-and-access-control)
8. [Troubleshooting](#troubleshooting)

---

## Overview

Template sharing enables organizations to standardize document processing across teams, departments, and locations. This guide covers best practices for exporting, importing, and deploying templates throughout your organization.

### Benefits of Template Sharing

- **Standardization**: Ensure consistent data extraction across the organization
- **Efficiency**: Eliminate duplicate template creation efforts
- **Quality Control**: Maintain centralized template standards and validation
- **Knowledge Transfer**: Share expertise between teams and departments
- **Compliance**: Ensure regulatory and organizational standards are met

### Supported Sharing Methods

- **File-based Export/Import**: JSON format for easy sharing
- **Network Deployment**: Centralized template repositories
- **Email Distribution**: Direct template sharing between users
- **Version Control Integration**: Git-based template management

---

## Template Export Procedures

### Single Template Export

#### Step-by-Step Process

1. **Select Template**
   - Navigate to Template Management
   - Select the template you want to export
   - Click on template name to view details

2. **Export Options**
   - Click **Export Template** button
   - Choose export format (JSON recommended)
   - Select export scope:
     - Template only
     - Template with sample data
     - Template with documentation

3. **Export Configuration**
   ```json
   {
     "exportOptions": {
       "includeMetadata": true,
       "includeSampleData": true,
       "includeUsageStatistics": false,
       "includeTestResults": true,
       "includeDocumentation": true
     }
   }
   ```

4. **Save Export File**
   - Choose destination folder
   - Name the file descriptively
   - Include version information in filename
   - Example: `SecurityIncidentTemplate_v2.1_20250625.json`

#### Export Verification

1. **Validate Export**
   - Check file size is reasonable (typically 1-50 KB)
   - Verify JSON structure is valid
   - Confirm all expected fields are present

2. **Test Import**
   - Import the exported template to a test environment
   - Verify all fields and rules are preserved
   - Test extraction functionality

### Bulk Template Export

#### Organization-Wide Export

1. **Select Multiple Templates**
   ```bash
   # Using PowerShell for bulk export
   Export-AnomaliTemplates -Category "Security" -OutputPath "C:\Templates\Security\"
   Export-AnomaliTemplates -All -OutputPath "C:\Templates\All\" -IncludeMetadata
   ```

2. **Category-Based Export**
   - Select templates by category (Security, Legal, Financial)
   - Export related templates together
   - Maintain dependency relationships

3. **Batch Export Configuration**
   ```json
   {
     "batchExportSettings": {
       "exportFormat": "JSON",
       "preserveHierarchy": true,
       "includeInheritanceRelationships": true,
       "validateBeforeExport": true,
       "compressionEnabled": true
     }
   }
   ```

#### Department-Specific Export

1. **Filter by Department**
   - Use template tags and categories
   - Filter by creator or owner
   - Include department-specific customizations

2. **Export Package Creation**
   - Create comprehensive template packages
   - Include setup instructions
   - Add configuration guidelines
   - Provide sample documents

---

## Template Import Procedures

### Single Template Import

#### Pre-Import Validation

1. **File Validation**
   - Verify JSON structure integrity
   - Check for required fields and properties
   - Validate field types and constraints
   - Confirm extraction rule syntax

2. **Compatibility Check**
   ```json
   {
     "compatibilityCheck": {
       "minVersion": "1.0",
       "maxVersion": "2.0",
       "requiredFeatures": ["OCR", "RegexExtraction"],
       "supportedFormats": ["pdf", "docx", "xlsx"]
     }
   }
   ```

#### Import Process

1. **Start Import Wizard**
   - Navigate to Template Management
   - Click **Import Template**
   - Select import method (File, URL, Clipboard)

2. **Select Import File**
   - Browse to template file location
   - Select JSON template file
   - Preview template information

3. **Configure Import Options**
   ```json
   {
     "importOptions": {
       "overwriteExisting": false,
       "createBackup": true,
       "validateAfterImport": true,
       "preserveIds": false,
       "updateDependencies": true
     }
   }
   ```

4. **Resolve Conflicts**
   - Handle duplicate template names
   - Merge field definitions if needed
   - Resolve category conflicts
   - Update version information

5. **Complete Import**
   - Review import summary
   - Test template functionality
   - Update template permissions
   - Notify relevant users

### Bulk Template Import

#### Department Deployment

1. **Prepare Import Package**
   ```powershell
   # PowerShell deployment script
   $templates = Get-ChildItem "\\server\templates\security\*.json"
   foreach ($template in $templates) {
       Import-AnomaliTemplate -Path $template.FullName -Validate -CreateBackup
   }
   ```

2. **Batch Import Configuration**
   - Set default permissions for imported templates
   - Configure category mappings
   - Establish naming conventions
   - Define validation rules

3. **Post-Import Validation**
   - Verify all templates imported successfully
   - Test critical templates
   - Update user documentation
   - Train users on new templates

---

## Organizational Deployment Strategies

### Centralized Template Repository

#### Network-Based Repository

1. **Setup Central Repository**
   ```
   \\orgserver\AnomaliTemplates\
   ├── Production\
   │   ├── Security\
   │   ├── Legal\
   │   ├── Financial\
   │   └── Operations\
   ├── Development\
   │   ├── Testing\
   │   └── Staging\
   └── Archive\
       ├── Deprecated\
       └── Versioned\
   ```

2. **Access Control**
   - Set read/write permissions by department
   - Implement approval workflows
   - Track template usage and modifications
   - Maintain audit logs

3. **Synchronization Process**
   ```json
   {
     "syncSettings": {
       "scheduleType": "Daily",
       "syncTime": "02:00",
       "conflictResolution": "ServerWins",
       "backupBeforeSync": true,
       "notifyOnChanges": true
     }
   }
   ```

### Distributed Deployment Model

#### Regional Deployment

1. **Regional Template Managers**
   - Designate template administrators per region
   - Implement regional customizations
   - Maintain central oversight
   - Coordinate cross-regional sharing

2. **Hub and Spoke Distribution**
   ```
   Corporate HQ (Master Templates)
   ├── Region A (Local Customizations)
   ├── Region B (Language Variants)
   ├── Region C (Regulatory Adaptations)
   └── Region D (Local Requirements)
   ```

### Version Control Integration

#### Git-Based Template Management

1. **Repository Structure**
   ```
   templates-repo/
   ├── src/
   │   ├── security/
   │   ├── legal/
   │   └── financial/
   ├── tests/
   │   ├── unit/
   │   └── integration/
   ├── docs/
   └── scripts/
       ├── deploy.ps1
       └── validate.py
   ```

2. **Workflow Process**
   - Development branches for template changes
   - Pull requests for template reviews
   - Automated testing and validation
   - Staged deployment to production

---

## Template Distribution Methods

### Email Distribution

#### Template Package Creation

1. **Email-Friendly Format**
   ```json
   {
     "templatePackage": {
       "metadata": {
         "name": "Q2 Security Templates",
         "version": "2.1",
         "created": "2025-06-25",
         "description": "Updated security templates for Q2"
       },
       "templates": [...],
       "documentation": "...",
       "installInstructions": "..."
     }
   }
   ```

2. **Distribution List Management**
   - Maintain department-specific lists
   - Include template administrators
   - Notify of updates and changes
   - Track delivery and installation

### Automated Distribution

#### Scheduled Deployment

1. **PowerShell Automation**
   ```powershell
   # Automated template deployment script
   param(
       [string]$SourcePath,
       [string[]]$TargetMachines,
       [string]$TemplateCategory
   )
   
   foreach ($machine in $TargetMachines) {
       $session = New-PSSession -ComputerName $machine
       Invoke-Command -Session $session -ScriptBlock {
           Import-AnomaliTemplates -Path $using:SourcePath -Category $using:TemplateCategory
       }
       Remove-PSSession $session
   }
   ```

2. **Group Policy Deployment**
   - Use Windows Group Policy for enterprise deployment
   - Schedule automatic template updates
   - Ensure consistent configuration across workstations

### Cloud-Based Distribution

#### SharePoint Integration

1. **SharePoint Template Library**
   - Create dedicated template libraries
   - Implement approval workflows
   - Enable version tracking
   - Provide search and discovery

2. **Cloud Synchronization**
   ```json
   {
     "cloudSync": {
       "provider": "SharePoint",
       "library": "AnomaliTemplates",
       "syncMode": "Bidirectional",
       "conflictResolution": "Manual",
       "offlineMode": true
     }
   }
   ```

---

## Version Control and Management

### Template Versioning

#### Version Number Scheme

1. **Semantic Versioning**
   - Major.Minor.Patch format (e.g., 2.1.3)
   - Major: Breaking changes or complete rewrites
   - Minor: New fields or extraction rules
   - Patch: Bug fixes and minor adjustments

2. **Version Metadata**
   ```json
   {
     "versionInfo": {
       "version": "2.1.3",
       "releaseDate": "2025-06-25",
       "author": "John Doe",
       "changes": [
         "Added IOC extraction field",
         "Fixed date pattern recognition",
         "Improved accuracy for PDF extraction"
       ],
       "compatibility": {
         "minSystemVersion": "1.0",
         "deprecatedFeatures": []
       }
     }
   }
   ```

### Change Management

#### Template Lifecycle

1. **Development Phase**
   - Create and test new templates
   - Validate against sample documents
   - Document configuration and usage

2. **Review and Approval**
   - Peer review process
   - Stakeholder approval
   - Security and compliance validation

3. **Deployment**
   - Staged rollout process
   - User training and documentation
   - Monitoring and feedback collection

4. **Maintenance**
   - Regular accuracy testing
   - Performance optimization
   - User feedback incorporation

#### Change Tracking

1. **Change Log Maintenance**
   ```json
   {
     "changeLog": [
       {
         "version": "2.1.3",
         "date": "2025-06-25",
         "author": "Jane Smith",
         "type": "Enhancement",
         "description": "Added support for multi-page extraction",
         "impactedFields": ["Summary", "Recommendations"],
         "testingNotes": "Tested with 50 sample documents"
       }
     ]
   }
   ```

2. **Rollback Procedures**
   - Maintain previous template versions
   - Document rollback triggers
   - Test rollback procedures
   - Communicate rollback decisions

---

## Security and Access Control

### Template Security

#### Access Control Lists

1. **Permission Levels**
   - **View**: Can see template details and use for extraction
   - **Edit**: Can modify template fields and rules
   - **Admin**: Can manage permissions and delete templates
   - **Deploy**: Can export and share templates

2. **Role-Based Access**
   ```json
   {
     "rolePermissions": {
       "SecurityAnalyst": ["View", "Use"],
       "TemplateManager": ["View", "Edit", "Deploy"],
       "SystemAdmin": ["View", "Edit", "Admin", "Deploy"],
       "DepartmentHead": ["View", "Edit", "Deploy"]
     }
   }
   ```

#### Template Encryption

1. **Sensitive Template Protection**
   - Encrypt templates containing sensitive patterns
   - Use organization-specific encryption keys
   - Implement secure key management

2. **Digital Signatures**
   - Sign templates to verify authenticity
   - Prevent unauthorized modifications
   - Validate template integrity during import

### Compliance and Auditing

#### Audit Trail

1. **Template Usage Tracking**
   ```json
   {
     "auditLog": [
       {
         "timestamp": "2025-06-25T10:30:00Z",
         "user": "john.doe@company.com",
         "action": "TemplateExported",
         "templateId": "security-incident-v2.1",
         "details": "Exported for regional deployment"
       }
     ]
   }
   ```

2. **Compliance Monitoring**
   - Track template modifications
   - Monitor usage patterns
   - Generate compliance reports
   - Maintain regulatory audit trails

---

## Troubleshooting

### Common Import Issues

#### Template Validation Errors

1. **Invalid JSON Format**
   ```bash
   Error: Invalid JSON structure
   Solution: Validate JSON syntax using online validators
   ```

2. **Missing Required Fields**
   ```bash
   Error: Template missing required field 'Name'
   Solution: Add missing fields or use template repair tool
   ```

3. **Version Compatibility**
   ```bash
   Error: Template requires features not available in current version
   Solution: Upgrade system or modify template compatibility
   ```

#### Import Conflicts

1. **Duplicate Template Names**
   - Choose to overwrite existing template
   - Rename imported template
   - Create new version of existing template

2. **Field Type Conflicts**
   - Review field definitions
   - Update field types to match expectations
   - Merge conflicting field configurations

### Performance Issues

#### Large Template Import

1. **Memory Issues**
   - Break large imports into smaller batches
   - Increase system memory allocation
   - Use streaming import for large files

2. **Network Timeouts**
   - Increase timeout settings
   - Use local file imports instead of network
   - Compress template files for faster transfer

### Recovery Procedures

#### Backup and Recovery

1. **Template Backup Strategy**
   ```powershell
   # Automated backup script
   $backupPath = "C:\AnomaliBackups\$(Get-Date -Format 'yyyyMMdd')"
   Export-AnomaliTemplates -All -OutputPath $backupPath -IncludeMetadata
   ```

2. **Recovery Process**
   - Identify last known good configuration
   - Restore from backup location
   - Verify template functionality
   - Update user documentation

---

## Best Practices Summary

### Export Best Practices

1. **Documentation**: Always include comprehensive documentation
2. **Testing**: Test exported templates before distribution
3. **Versioning**: Use clear version numbering and change logs
4. **Security**: Encrypt sensitive templates during transit

### Import Best Practices

1. **Validation**: Always validate templates before importing
2. **Backup**: Create backups before major imports
3. **Testing**: Test imported templates with sample documents
4. **Communication**: Notify users of new template availability

### Distribution Best Practices

1. **Standardization**: Maintain consistent template formats
2. **Training**: Provide training for new templates
3. **Monitoring**: Track template usage and accuracy
4. **Feedback**: Collect and incorporate user feedback

---

*This guide provides comprehensive procedures for template sharing and organizational deployment. Customize these processes based on your organization's specific requirements, security policies, and technical infrastructure.* 