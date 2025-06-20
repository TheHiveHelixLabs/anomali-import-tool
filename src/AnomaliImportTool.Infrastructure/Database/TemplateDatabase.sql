-- ============================================================================
-- Anomali Import Tool - Template Database Schema
-- SQLite Database Schema for Import Template System
-- Supports versioning, categorization, field extraction, and template matching
-- ============================================================================

-- Enable foreign key constraints
PRAGMA foreign_keys = ON;

-- ============================================================================
-- Import Templates Table
-- Stores core template information with versioning and categorization
-- ============================================================================
CREATE TABLE IF NOT EXISTS import_templates (
    id TEXT PRIMARY KEY NOT NULL,
    name TEXT NOT NULL,
    description TEXT,
    version TEXT NOT NULL DEFAULT '1.0.0',
    category TEXT NOT NULL DEFAULT 'General',
    created_by TEXT,
    last_modified_by TEXT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_modified_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_active BOOLEAN NOT NULL DEFAULT 1,
    tags TEXT, -- JSON array of tags
    supported_formats TEXT, -- JSON array of supported file formats
    
    -- Template metadata
    confidence_threshold REAL NOT NULL DEFAULT 0.7,
    auto_apply BOOLEAN NOT NULL DEFAULT 0,
    allow_partial_matches BOOLEAN NOT NULL DEFAULT 1,
    template_priority INTEGER NOT NULL DEFAULT 0,
    
    -- Document matching criteria (JSON)
    document_matching_criteria TEXT,
    
    -- OCR settings (JSON)
    ocr_settings TEXT,
    
    -- Validation settings (JSON)
    template_validation TEXT,
    
    -- Usage statistics (JSON)
    usage_stats TEXT,
    
    -- Constraints
    CONSTRAINT chk_confidence_threshold CHECK (confidence_threshold >= 0.0 AND confidence_threshold <= 1.0),
    CONSTRAINT chk_template_priority CHECK (template_priority >= 0)
);

-- ============================================================================
-- Template Versions Table
-- Tracks template version history for rollback and auditing
-- ============================================================================
CREATE TABLE IF NOT EXISTS template_versions (
    id TEXT PRIMARY KEY NOT NULL,
    template_id TEXT NOT NULL,
    version_number TEXT NOT NULL,
    version_description TEXT,
    template_data TEXT NOT NULL, -- JSON snapshot of complete template
    created_by TEXT,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_current BOOLEAN NOT NULL DEFAULT 0,
    
    -- Foreign key constraints
    FOREIGN KEY (template_id) REFERENCES import_templates(id) ON DELETE CASCADE,
    
    -- Ensure only one current version per template
    UNIQUE(template_id, is_current) ON CONFLICT REPLACE
);

-- ============================================================================
-- Template Fields Table
-- Stores field definitions for each template
-- ============================================================================
CREATE TABLE IF NOT EXISTS template_fields (
    id TEXT PRIMARY KEY NOT NULL,
    template_id TEXT NOT NULL,
    name TEXT NOT NULL,
    display_name TEXT NOT NULL,
    description TEXT,
    field_type TEXT NOT NULL DEFAULT 'Text', -- Text, Username, TicketNumber, Date, etc.
    extraction_method TEXT NOT NULL DEFAULT 'Text', -- Text, Coordinates, OCR, Metadata, Hybrid
    is_required BOOLEAN NOT NULL DEFAULT 0,
    processing_order INTEGER NOT NULL DEFAULT 0,
    
    -- Text patterns and keywords (JSON arrays)
    text_patterns TEXT,
    keywords TEXT,
    
    -- Default values and formatting
    default_value TEXT,
    output_format TEXT,
    
    -- Field validation rules (JSON)
    validation_rules TEXT,
    
    -- Data transformation settings (JSON)
    data_transformation TEXT,
    
    -- Fallback options (JSON)
    fallback_options TEXT,
    
    -- Multi-value support
    supports_multiple_values BOOLEAN NOT NULL DEFAULT 0,
    value_separator TEXT DEFAULT ',',
    
    -- Confidence and matching
    confidence_threshold REAL NOT NULL DEFAULT 0.5,
    
    -- Timestamps
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign key constraints
    FOREIGN KEY (template_id) REFERENCES import_templates(id) ON DELETE CASCADE,
    
    -- Constraints
    CONSTRAINT chk_field_confidence CHECK (confidence_threshold >= 0.0 AND confidence_threshold <= 1.0),
    CONSTRAINT chk_processing_order CHECK (processing_order >= 0)
);

-- ============================================================================
-- Extraction Zones Table
-- Stores coordinate-based extraction zones for template fields
-- ============================================================================
CREATE TABLE IF NOT EXISTS extraction_zones (
    id TEXT PRIMARY KEY NOT NULL,
    field_id TEXT NOT NULL,
    name TEXT NOT NULL,
    description TEXT,
    
    -- Coordinate information
    x REAL NOT NULL,
    y REAL NOT NULL,
    width REAL NOT NULL,
    height REAL NOT NULL,
    page_number INTEGER NOT NULL DEFAULT 1,
    coordinate_system TEXT NOT NULL DEFAULT 'Pixel', -- Pixel, Percentage, Points, Normalized
    
    -- Zone configuration
    zone_type TEXT NOT NULL DEFAULT 'Text', -- Text, OCR, Image, Table, Signature, Barcode
    priority INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT 1,
    
    -- Tolerance settings
    position_tolerance REAL NOT NULL DEFAULT 5.0,
    size_tolerance REAL NOT NULL DEFAULT 10.0,
    
    -- Visual selection settings (JSON)
    visual_selection_settings TEXT,
    
    -- Zone extraction settings (JSON)
    zone_extraction_settings TEXT,
    
    -- Timestamps
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign key constraints
    FOREIGN KEY (field_id) REFERENCES template_fields(id) ON DELETE CASCADE,
    
    -- Constraints
    CONSTRAINT chk_coordinates CHECK (x >= 0 AND y >= 0 AND width > 0 AND height > 0),
    CONSTRAINT chk_page_number CHECK (page_number > 0),
    CONSTRAINT chk_zone_priority CHECK (priority >= 0)
);

-- ============================================================================
-- Template Categories Table
-- Hierarchical category system for template organization
-- ============================================================================
CREATE TABLE IF NOT EXISTS template_categories (
    id TEXT PRIMARY KEY NOT NULL,
    name TEXT NOT NULL,
    description TEXT,
    parent_category_id TEXT,
    display_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT 1,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign key for hierarchical structure
    FOREIGN KEY (parent_category_id) REFERENCES template_categories(id) ON DELETE SET NULL,
    
    -- Unique constraint
    UNIQUE(name, parent_category_id)
);

-- ============================================================================
-- Template Inheritance Table
-- Manages parent-child relationships between templates for inheritance
-- ============================================================================
CREATE TABLE IF NOT EXISTS template_inheritance (
    id TEXT PRIMARY KEY NOT NULL,
    child_template_id TEXT NOT NULL,
    parent_template_id TEXT NOT NULL,
    
    -- Inheritance configuration
    inheritance_type TEXT NOT NULL DEFAULT 'Full', -- Full, FieldsOnly, SettingsOnly, Custom
    field_overrides TEXT, -- JSON object defining which fields to override
    settings_overrides TEXT, -- JSON object defining which settings to override
    
    -- Priority when multiple inheritance is allowed
    inheritance_priority INTEGER NOT NULL DEFAULT 0,
    
    -- Override behaviors
    allow_field_addition BOOLEAN NOT NULL DEFAULT 1,
    allow_field_removal BOOLEAN NOT NULL DEFAULT 0,
    allow_field_modification BOOLEAN NOT NULL DEFAULT 1,
    allow_settings_override BOOLEAN NOT NULL DEFAULT 1,
    
    -- Inheritance status
    is_active BOOLEAN NOT NULL DEFAULT 1,
    validation_status TEXT NOT NULL DEFAULT 'Valid', -- Valid, Invalid, Warning, Unknown
    validation_message TEXT,
    
    -- Timestamps
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign key constraints
    FOREIGN KEY (child_template_id) REFERENCES import_templates(id) ON DELETE CASCADE,
    FOREIGN KEY (parent_template_id) REFERENCES import_templates(id) ON DELETE CASCADE,
    
    -- Constraints
    CONSTRAINT chk_inheritance_priority CHECK (inheritance_priority >= 0),
    CONSTRAINT chk_no_self_inheritance CHECK (child_template_id != parent_template_id),
    
    -- Unique constraint to prevent duplicate inheritance relationships
    UNIQUE(child_template_id, parent_template_id)
);

-- ============================================================================
-- Template Usage History Table
-- Tracks template usage for analytics and optimization
-- ============================================================================
CREATE TABLE IF NOT EXISTS template_usage_history (
    id TEXT PRIMARY KEY NOT NULL,
    template_id TEXT NOT NULL,
    document_name TEXT NOT NULL,
    document_path TEXT,
    document_type TEXT,
    
    -- Usage results
    extraction_successful BOOLEAN NOT NULL,
    fields_extracted INTEGER NOT NULL DEFAULT 0,
    fields_failed INTEGER NOT NULL DEFAULT 0,
    confidence_score REAL,
    extraction_time_ms INTEGER,
    
    -- Error information
    error_message TEXT,
    error_details TEXT, -- JSON with detailed error information
    
    -- Extracted data summary (JSON)
    extracted_data TEXT,
    
    -- User information
    used_by TEXT,
    used_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign key constraints
    FOREIGN KEY (template_id) REFERENCES import_templates(id) ON DELETE CASCADE,
    
    -- Constraints
    CONSTRAINT chk_confidence_score CHECK (confidence_score IS NULL OR (confidence_score >= 0.0 AND confidence_score <= 1.0)),
    CONSTRAINT chk_extraction_time CHECK (extraction_time_ms >= 0),
    CONSTRAINT chk_field_counts CHECK (fields_extracted >= 0 AND fields_failed >= 0)
);

-- ============================================================================
-- Template Performance Metrics Table
-- Aggregated performance data for template optimization
-- ============================================================================
CREATE TABLE IF NOT EXISTS template_performance_metrics (
    id TEXT PRIMARY KEY NOT NULL,
    template_id TEXT NOT NULL,
    
    -- Time period for metrics
    metric_date DATE NOT NULL,
    metric_period TEXT NOT NULL DEFAULT 'daily', -- daily, weekly, monthly
    
    -- Usage statistics
    total_uses INTEGER NOT NULL DEFAULT 0,
    successful_uses INTEGER NOT NULL DEFAULT 0,
    failed_uses INTEGER NOT NULL DEFAULT 0,
    success_rate REAL NOT NULL DEFAULT 0.0,
    
    -- Performance metrics
    avg_extraction_time_ms REAL NOT NULL DEFAULT 0.0,
    avg_confidence_score REAL NOT NULL DEFAULT 0.0,
    avg_fields_extracted REAL NOT NULL DEFAULT 0.0,
    
    -- Document type distribution (JSON)
    document_type_distribution TEXT,
    
    -- Common extraction errors (JSON)
    common_errors TEXT,
    
    -- Timestamps
    calculated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign key constraints
    FOREIGN KEY (template_id) REFERENCES import_templates(id) ON DELETE CASCADE,
    
    -- Unique constraint for metric periods
    UNIQUE(template_id, metric_date, metric_period),
    
    -- Constraints
    CONSTRAINT chk_usage_counts CHECK (total_uses >= 0 AND successful_uses >= 0 AND failed_uses >= 0),
    CONSTRAINT chk_success_rate CHECK (success_rate >= 0.0 AND success_rate <= 1.0),
    CONSTRAINT chk_avg_metrics CHECK (avg_extraction_time_ms >= 0.0 AND avg_confidence_score >= 0.0 AND avg_fields_extracted >= 0.0)
);

-- ============================================================================
-- Template Sharing Table
-- Manages template sharing and export/import operations
-- ============================================================================
CREATE TABLE IF NOT EXISTS template_sharing (
    id TEXT PRIMARY KEY NOT NULL,
    template_id TEXT NOT NULL,
    
    -- Sharing information
    shared_by TEXT NOT NULL,
    share_type TEXT NOT NULL DEFAULT 'export', -- export, import, public_share
    
    -- Export/Import data
    export_format TEXT NOT NULL DEFAULT 'json',
    exported_data TEXT, -- Complete template data in JSON format
    export_metadata TEXT, -- JSON with export metadata
    
    -- Sharing settings
    is_public BOOLEAN NOT NULL DEFAULT 0,
    share_token TEXT UNIQUE,
    expires_at DATETIME,
    
    -- Version information
    template_version TEXT,
    export_version TEXT NOT NULL DEFAULT '1.0',
    
    -- Timestamps
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    accessed_at DATETIME,
    access_count INTEGER NOT NULL DEFAULT 0,
    
    -- Foreign key constraints
    FOREIGN KEY (template_id) REFERENCES import_templates(id) ON DELETE CASCADE,
    
    -- Constraints
    CONSTRAINT chk_access_count CHECK (access_count >= 0)
);

-- ============================================================================
-- Document Template Matches Table
-- Caches template matching results for performance
-- ============================================================================
CREATE TABLE IF NOT EXISTS document_template_matches (
    id TEXT PRIMARY KEY NOT NULL,
    document_fingerprint TEXT NOT NULL, -- SHA256 hash of document content
    template_id TEXT NOT NULL,
    
    -- Matching results
    confidence_score REAL NOT NULL,
    match_factors TEXT, -- JSON with detailed matching factors
    
    -- Performance data
    matching_time_ms INTEGER NOT NULL,
    matched_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Cache expiration
    expires_at DATETIME,
    is_valid BOOLEAN NOT NULL DEFAULT 1,
    
    -- Foreign key constraints
    FOREIGN KEY (template_id) REFERENCES import_templates(id) ON DELETE CASCADE,
    
    -- Constraints
    CONSTRAINT chk_match_confidence CHECK (confidence_score >= 0.0 AND confidence_score <= 1.0),
    CONSTRAINT chk_matching_time CHECK (matching_time_ms >= 0),
    
    -- Unique constraint for caching
    UNIQUE(document_fingerprint, template_id)
);

-- ============================================================================
-- Indexes for Performance Optimization
-- ============================================================================

-- Import Templates Indexes
CREATE INDEX IF NOT EXISTS idx_import_templates_category ON import_templates(category);
CREATE INDEX IF NOT EXISTS idx_import_templates_active ON import_templates(is_active);
CREATE INDEX IF NOT EXISTS idx_import_templates_created_at ON import_templates(created_at);
CREATE INDEX IF NOT EXISTS idx_import_templates_name ON import_templates(name);

-- Template Versions Indexes
CREATE INDEX IF NOT EXISTS idx_template_versions_template_id ON template_versions(template_id);
CREATE INDEX IF NOT EXISTS idx_template_versions_current ON template_versions(template_id, is_current);

-- Template Fields Indexes
CREATE INDEX IF NOT EXISTS idx_template_fields_template_id ON template_fields(template_id);
CREATE INDEX IF NOT EXISTS idx_template_fields_processing_order ON template_fields(template_id, processing_order);
CREATE INDEX IF NOT EXISTS idx_template_fields_required ON template_fields(template_id, is_required);

-- Extraction Zones Indexes
CREATE INDEX IF NOT EXISTS idx_extraction_zones_field_id ON extraction_zones(field_id);
CREATE INDEX IF NOT EXISTS idx_extraction_zones_page ON extraction_zones(page_number);
CREATE INDEX IF NOT EXISTS idx_extraction_zones_priority ON extraction_zones(field_id, priority);

-- Template Categories Indexes
CREATE INDEX IF NOT EXISTS idx_template_categories_parent ON template_categories(parent_category_id);
CREATE INDEX IF NOT EXISTS idx_template_categories_order ON template_categories(display_order);

-- Template Inheritance Indexes
CREATE INDEX IF NOT EXISTS idx_template_inheritance_child ON template_inheritance(child_template_id);
CREATE INDEX IF NOT EXISTS idx_template_inheritance_parent ON template_inheritance(parent_template_id);
CREATE INDEX IF NOT EXISTS idx_template_inheritance_active ON template_inheritance(is_active);
CREATE INDEX IF NOT EXISTS idx_template_inheritance_priority ON template_inheritance(child_template_id, inheritance_priority);

-- Usage History Indexes
CREATE INDEX IF NOT EXISTS idx_template_usage_template_id ON template_usage_history(template_id);
CREATE INDEX IF NOT EXISTS idx_template_usage_date ON template_usage_history(used_at);
CREATE INDEX IF NOT EXISTS idx_template_usage_success ON template_usage_history(template_id, extraction_successful);

-- Performance Metrics Indexes
CREATE INDEX IF NOT EXISTS idx_template_performance_template_id ON template_performance_metrics(template_id);
CREATE INDEX IF NOT EXISTS idx_template_performance_date ON template_performance_metrics(metric_date);

-- Sharing Indexes
CREATE INDEX IF NOT EXISTS idx_template_sharing_template_id ON template_sharing(template_id);
CREATE INDEX IF NOT EXISTS idx_template_sharing_token ON template_sharing(share_token);
CREATE INDEX IF NOT EXISTS idx_template_sharing_public ON template_sharing(is_public);

-- Document Matches Indexes
CREATE INDEX IF NOT EXISTS idx_document_matches_fingerprint ON document_template_matches(document_fingerprint);
CREATE INDEX IF NOT EXISTS idx_document_matches_template_id ON document_template_matches(template_id);
CREATE INDEX IF NOT EXISTS idx_document_matches_confidence ON document_template_matches(confidence_score DESC);
CREATE INDEX IF NOT EXISTS idx_document_matches_expiry ON document_template_matches(expires_at);

-- ============================================================================
-- Views for Common Queries
-- ============================================================================

-- Active Templates with Usage Statistics
CREATE VIEW IF NOT EXISTS v_active_templates_with_stats AS
SELECT 
    t.id,
    t.name,
    t.description,
    t.version,
    t.category,
    t.created_by,
    t.created_at,
    t.last_modified_at,
    t.confidence_threshold,
    COUNT(f.id) as field_count,
    COUNT(DISTINCT uz.id) as total_zones,
    COALESCE(pm.success_rate, 0.0) as success_rate,
    COALESCE(pm.total_uses, 0) as total_uses,
    COALESCE(pm.avg_extraction_time_ms, 0.0) as avg_extraction_time_ms
FROM import_templates t
LEFT JOIN template_fields f ON t.id = f.template_id
LEFT JOIN extraction_zones uz ON f.id = uz.field_id
LEFT JOIN (
    SELECT 
        template_id,
        AVG(success_rate) as success_rate,
        SUM(total_uses) as total_uses,
        AVG(avg_extraction_time_ms) as avg_extraction_time_ms
    FROM template_performance_metrics 
    WHERE metric_date >= date('now', '-30 days')
    GROUP BY template_id
) pm ON t.id = pm.template_id
WHERE t.is_active = 1
GROUP BY t.id, t.name, t.description, t.version, t.category, t.created_by, t.created_at, t.last_modified_at, t.confidence_threshold, pm.success_rate, pm.total_uses, pm.avg_extraction_time_ms;

-- Template Categories with Template Counts
CREATE VIEW IF NOT EXISTS v_template_categories_with_counts AS
SELECT 
    c.id,
    c.name,
    c.description,
    c.parent_category_id,
    c.display_order,
    COUNT(t.id) as template_count,
    COUNT(CASE WHEN t.is_active = 1 THEN 1 END) as active_template_count
FROM template_categories c
LEFT JOIN import_templates t ON c.name = t.category
WHERE c.is_active = 1
GROUP BY c.id, c.name, c.description, c.parent_category_id, c.display_order
ORDER BY c.display_order, c.name;

-- Recent Template Usage Summary
CREATE VIEW IF NOT EXISTS v_recent_template_usage AS
SELECT 
    t.id as template_id,
    t.name as template_name,
    t.category,
    COUNT(th.id) as usage_count,
    COUNT(CASE WHEN th.extraction_successful = 1 THEN 1 END) as successful_count,
    COUNT(CASE WHEN th.extraction_successful = 0 THEN 1 END) as failed_count,
    ROUND(AVG(CASE WHEN th.extraction_successful = 1 THEN 100.0 ELSE 0.0 END), 2) as success_rate,
    AVG(th.extraction_time_ms) as avg_extraction_time,
    MAX(th.used_at) as last_used
FROM import_templates t
LEFT JOIN template_usage_history th ON t.id = th.template_id
WHERE th.used_at >= datetime('now', '-7 days') OR th.used_at IS NULL
GROUP BY t.id, t.name, t.category
ORDER BY usage_count DESC, success_rate DESC;

-- ============================================================================
-- Triggers for Automatic Maintenance
-- ============================================================================

-- Update last_modified_at on template changes
CREATE TRIGGER IF NOT EXISTS trg_import_templates_update_timestamp
    AFTER UPDATE ON import_templates
    FOR EACH ROW
    WHEN OLD.last_modified_at = NEW.last_modified_at
BEGIN
    UPDATE import_templates SET last_modified_at = CURRENT_TIMESTAMP WHERE id = NEW.id;
END;

-- Update field timestamps
CREATE TRIGGER IF NOT EXISTS trg_template_fields_update_timestamp
    AFTER UPDATE ON template_fields
    FOR EACH ROW
    WHEN OLD.updated_at = NEW.updated_at
BEGIN
    UPDATE template_fields SET updated_at = CURRENT_TIMESTAMP WHERE id = NEW.id;
END;

-- Update zone timestamps
CREATE TRIGGER IF NOT EXISTS trg_extraction_zones_update_timestamp
    AFTER UPDATE ON extraction_zones
    FOR EACH ROW
    WHEN OLD.updated_at = NEW.updated_at
BEGIN
    UPDATE extraction_zones SET updated_at = CURRENT_TIMESTAMP WHERE id = NEW.id;
END;

-- Cleanup expired document matches
CREATE TRIGGER IF NOT EXISTS trg_cleanup_expired_matches
    AFTER INSERT ON document_template_matches
    FOR EACH ROW
BEGIN
    DELETE FROM document_template_matches 
    WHERE expires_at < CURRENT_TIMESTAMP AND expires_at IS NOT NULL;
END;

-- ============================================================================
-- Initial Data Setup
-- ============================================================================

-- Insert default categories
INSERT OR IGNORE INTO template_categories (id, name, description, display_order) VALUES
('cat_general', 'General', 'General purpose templates', 1),
('cat_security', 'Security', 'Security-related templates', 2),
('cat_reports', 'Reports', 'Report processing templates', 3),
('cat_exceptions', 'Exceptions', 'Exception request templates', 4),
('cat_apt', 'APT Analysis', 'Advanced Persistent Threat analysis templates', 5),
('cat_incidents', 'Incidents', 'Incident response templates', 6);

-- Insert subcategories
INSERT OR IGNORE INTO template_categories (id, name, description, parent_category_id, display_order) VALUES
('cat_sec_exceptions', 'Security Exceptions', 'Security exception requests', 'cat_security', 1),
('cat_sec_policies', 'Security Policies', 'Security policy documents', 'cat_security', 2),
('cat_rep_threat', 'Threat Reports', 'Threat intelligence reports', 'cat_reports', 1),
('cat_rep_vulnerability', 'Vulnerability Reports', 'Vulnerability assessment reports', 'cat_reports', 2);

-- ============================================================================
-- Schema Version Information
-- ============================================================================
CREATE TABLE IF NOT EXISTS schema_version (
    version TEXT PRIMARY KEY,
    applied_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    description TEXT
);

INSERT OR REPLACE INTO schema_version (version, description) VALUES 
('1.0.0', 'Initial template database schema with full feature support');

-- ============================================================================
-- End of Schema
-- ============================================================================ 