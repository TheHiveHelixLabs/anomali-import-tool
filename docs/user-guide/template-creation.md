# Import Template Creation Guide

> 🚧 **Coming Soon**: This feature is currently in development. This guide describes the planned functionality for the Import Template System.

The Import Template System allows you to create custom templates for extracting specific metadata from different document types. Templates ensure consistent and accurate data extraction across your organization.

## 📋 **What are Import Templates?**

Import Templates are reusable configurations that define:
- **Where** to extract data from documents (coordinate-based zones)
- **What** type of data to extract (usernames, ticket numbers, dates, etc.)
- **How** to validate and transform the extracted data
- **Which** documents the template applies to

### Benefits
- **Consistency**: Standardized extraction across all team members
- **Accuracy**: Reduced manual errors in metadata extraction
- **Efficiency**: Automated processing of similar document types
- **Scalability**: Share templates across the organization

## 🎯 **Template Creation Workflow**

### Step 1: Template Creation Interface
1. **Open Template Creator**: Click "Create Template" in the 7-step wizard
2. **Load Sample Document**: Upload a representative document for template creation
3. **Name Your Template**: Provide descriptive name and category

### Step 2: Visual Zone Selection
1. **Document Preview**: View your document in the visual editor
2. **Zone Creation**: Click and drag to create extraction zones
3. **Zone Configuration**: Set field type, validation rules, and extraction patterns

### Step 3: Field Configuration
Configure each extraction zone with:
- **Field Type**: Username, Ticket Number, Date, Approval Status, Custom
- **Extraction Method**: Text pattern, OCR, coordinate-based
- **Validation Rules**: Format requirements, required/optional
- **Transformation**: Data formatting and normalization

### Step 4: Template Testing
1. **Live Preview**: See extraction results in real-time
2. **Test Documents**: Validate against multiple sample documents
3. **Confidence Scoring**: Review extraction accuracy indicators
4. **Refinement**: Adjust zones and patterns based on results

### Step 5: Template Finalization
1. **Template Metadata**: Description, category, version info
2. **Sharing Settings**: Private, team, or organization-wide
3. **Save & Export**: Store template and create shareable file

## 🔧 **Template Components**

### Extraction Zones
**Coordinate-Based Regions** - Specific areas of documents for data extraction:
```
Zone Properties:
- X, Y coordinates (top-left corner)
- Width and height dimensions
- Page number (for multi-page documents)
- OCR settings (for scanned documents)
```

### Field Types
**Supported Data Types** for automatic recognition:

| Field Type | Description | Pattern Examples |
|------------|-------------|------------------|
| **Username** | User/author identification | `john.doe`, `J.Smith`, `admin@company.com` |
| **Ticket Number** | ServiceNow/JIRA tickets | `INC0001234`, `TASK-5678`, `REQ00009876` |
| **Date** | Various date formats | `2024-01-15`, `Jan 15, 2024`, `15/01/2024` |
| **Approval Status** | Document approval state | `Approved`, `Pending`, `Rejected` |
| **Risk Level** | Security risk classification | `High`, `Medium`, `Low`, `Critical` |
| **Custom Field** | Organization-specific data | Regex patterns, keyword lists |

### Validation Rules
**Data Quality Assurance** - Ensure extracted data meets requirements:
- **Required Fields**: Mark essential fields that must be extracted
- **Format Validation**: Regex patterns for specific formats
- **Value Lists**: Predefined acceptable values
- **Range Validation**: Numeric and date range constraints

## 🎨 **Visual Template Editor**

### Interface Components
- **Document Canvas**: Interactive preview with zoom and pan
- **Zone Palette**: Drag-and-drop field types onto document
- **Property Panel**: Configure selected zone properties
- **Preview Panel**: Real-time extraction results
- **Template Tree**: Hierarchical view of all zones and fields

### Zone Creation Methods
1. **Click & Drag**: Create rectangular zones by dragging
2. **Snap to Text**: Automatically detect text boundaries
3. **Multi-Select**: Select multiple text elements as single zone
4. **Table Detection**: Automatic table cell recognition
5. **OCR Regions**: Define areas for optical character recognition

### Advanced Features
- **Conditional Extraction**: Extract data based on document content
- **Multi-Page Handling**: Templates spanning multiple pages
- **Template Inheritance**: Base templates with customizable overrides
- **Batch Operations**: Apply templates to multiple documents

## 📋 **Template Types**

### Standard Templates
**Common document types** with pre-configured fields:

#### Security Exception Template
- **Username**: Document author
- **Ticket Number**: Associated ServiceNow ticket
- **Approval Date**: When exception was approved
- **Expiration Date**: When exception expires
- **Risk Level**: Security risk assessment
- **Justification**: Business justification text

#### Threat Intelligence Report Template  
- **Report Date**: When report was created
- **Threat Actor**: Associated threat group
- **Campaign**: Threat campaign name
- **Confidence**: Intelligence confidence level
- **IOCs**: Indicators of compromise
- **Analyst**: Report author

#### Incident Response Template
- **Incident ID**: Unique incident identifier
- **Severity**: Incident severity level
- **Response Team**: Assigned team members
- **Timeline**: Key incident timestamps
- **Impact**: Business impact assessment

### Custom Templates
**Organization-specific** templates for unique document types:
- **Regulatory Compliance**: Audit reports, compliance assessments
- **Vendor Security**: Third-party security evaluations
- **Risk Assessments**: Security risk analysis documents
- **Policy Documents**: Security policies and procedures

## 🔄 **Template Workflow Integration**

### Automatic Template Matching
1. **Document Analysis**: System analyzes uploaded documents
2. **Template Suggestion**: Recommends best-matching templates
3. **Confidence Scoring**: Shows match probability percentage
4. **Manual Override**: User can select different template

### Template Selection Process
1. **Smart Suggestions**: Top 3 recommended templates
2. **Category Browsing**: Browse by document type
3. **Search Functionality**: Find templates by name or description
4. **Recent Templates**: Quick access to frequently used templates

### Extraction Process
1. **Template Application**: Selected template applied to document
2. **Field Extraction**: Data extracted from defined zones
3. **Validation Check**: Extracted data validated against rules
4. **Confidence Indicators**: Visual feedback on extraction quality
5. **Manual Review**: User can review and correct extractions

## 🚀 **Best Practices**

### Template Design
- **Start Simple**: Begin with basic fields and add complexity gradually
- **Use Representative Samples**: Create templates with typical documents
- **Test Thoroughly**: Validate templates with multiple sample documents
- **Document Variations**: Account for different layouts and formats

### Field Configuration
- **Clear Naming**: Use descriptive field names and descriptions
- **Appropriate Validation**: Balance strictness with flexibility
- **Fallback Patterns**: Provide alternative extraction methods
- **Error Handling**: Plan for extraction failures

### Organization & Sharing
- **Consistent Naming**: Use standard naming conventions
- **Categorization**: Organize templates by document type or purpose
- **Version Control**: Maintain template versions and change logs
- **Documentation**: Include usage instructions and examples

### Performance Optimization
- **Zone Efficiency**: Use minimal zones for better performance
- **OCR Usage**: Only use OCR when necessary (scanned documents)
- **Batch Processing**: Optimize templates for bulk operations
- **Caching**: Leverage template caching for repeated operations

## 🔧 **Template Management**

### Template Library
- **Personal Templates**: Private templates for individual use
- **Team Templates**: Shared within team or department
- **Organization Templates**: Company-wide standardized templates
- **Public Templates**: Community-shared templates

### Import/Export
- **JSON Format**: Portable template format for sharing
- **Bulk Operations**: Import/export multiple templates
- **Version Migration**: Update templates to new format versions
- **Cross-Platform**: Templates work across all supported platforms

### Maintenance
- **Performance Monitoring**: Track template extraction accuracy
- **Usage Analytics**: Identify most/least used templates
- **Quality Metrics**: Monitor extraction success rates
- **Regular Updates**: Keep templates current with document changes

## 🆘 **Troubleshooting**

### Common Issues
**Template Not Matching Documents**
- Verify document format matches template expectations
- Check if document layout has changed
- Update template zones if document structure changed

**Low Extraction Confidence**
- Review and adjust extraction zones
- Improve OCR settings for scanned documents
- Add alternative extraction patterns

**Missing Required Fields**
- Check if required zones are properly configured
- Verify field validation rules aren't too strict
- Consider making non-critical fields optional

### Getting Help
- **Template Testing**: Use built-in testing tools
- **Community Templates**: Browse shared template library
- **Support Documentation**: Check troubleshooting guides
- **Technical Support**: Contact support for complex issues

---

**Status**: 🚧 **In Development** - This feature is planned for implementation  
**Expected Release**: Q1 2025  
**Current Alternative**: Use manual metadata extraction in existing workflow 