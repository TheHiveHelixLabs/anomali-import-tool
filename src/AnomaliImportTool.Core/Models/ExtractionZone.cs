using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AnomaliImportTool.Core.Models;

/// <summary>
/// Represents a coordinate-based extraction zone within a document
/// Supports visual selection, multi-page documents, and various coordinate systems
/// </summary>
public class ExtractionZone
{
    /// <summary>
    /// Unique identifier for the extraction zone
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Display name for the extraction zone
    /// </summary>
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this zone extracts
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// X coordinate of the top-left corner
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y coordinate of the top-left corner
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Width of the extraction zone
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Height of the extraction zone
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Page number (1-based) for multi-page documents
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Coordinate system used for this zone
    /// </summary>
    public CoordinateSystem CoordinateSystem { get; set; } = CoordinateSystem.Pixel;

    /// <summary>
    /// Zone type for different extraction behaviors
    /// </summary>
    public ExtractionZoneType ZoneType { get; set; } = ExtractionZoneType.Text;

    /// <summary>
    /// Priority of this zone (higher numbers processed first)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Whether this zone is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Color for visual selection display (hex format)
    /// </summary>
    [StringLength(7)]
    public string HighlightColor { get; set; } = "#FF0000";

    /// <summary>
    /// Opacity for visual overlay (0.0 to 1.0)
    /// </summary>
    public double Opacity { get; set; } = 0.3;

    /// <summary>
    /// Visual selection settings for UI support
    /// </summary>
    public VisualSelectionSettings VisualSettings { get; set; } = new();

    /// <summary>
    /// Zone-specific extraction settings
    /// </summary>
    public ZoneExtractionSettings ExtractionSettings { get; set; } = new();

    /// <summary>
    /// Additional metadata for the zone
    /// </summary>
    public Dictionary<string, object> ZoneMetadata { get; set; } = new();

    /// <summary>
    /// Position tolerance for matching (percentage of zone size)
    /// </summary>
    public double PositionTolerance { get; set; } = 0.1;

    /// <summary>
    /// Size tolerance for matching (percentage of zone size)
    /// </summary>
    public double SizeTolerance { get; set; } = 0.1;

    /// <summary>
    /// Creates a copy of the extraction zone
    /// </summary>
    /// <returns>New ExtractionZone instance</returns>
    public ExtractionZone CreateCopy()
    {
        return new ExtractionZone
        {
            Id = Guid.NewGuid(),
            Name = Name,
            Description = Description,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            PageNumber = PageNumber,
            CoordinateSystem = CoordinateSystem,
            ZoneType = ZoneType,
            Priority = Priority,
            IsActive = IsActive,
            HighlightColor = HighlightColor,
            Opacity = Opacity,
            VisualSettings = VisualSettings.CreateCopy(),
            ExtractionSettings = ExtractionSettings.CreateCopy(),
            ZoneMetadata = new Dictionary<string, object>(ZoneMetadata),
            PositionTolerance = PositionTolerance,
            SizeTolerance = SizeTolerance
        };
    }

    /// <summary>
    /// Validates the extraction zone configuration
    /// </summary>
    /// <returns>Validation result with any errors</returns>
    public TemplateValidationResult ValidateZone()
    {
        var result = new TemplateValidationResult { IsValid = true };

        // Validate coordinates
        if (Width <= 0)
        {
            result.IsValid = false;
            result.Errors.Add("Zone width must be greater than 0");
        }

        if (Height <= 0)
        {
            result.IsValid = false;
            result.Errors.Add("Zone height must be greater than 0");
        }

        if (PageNumber <= 0)
        {
            result.IsValid = false;
            result.Errors.Add("Page number must be greater than 0");
        }

        // Validate coordinate system specific constraints
        switch (CoordinateSystem)
        {
            case CoordinateSystem.Percentage:
                if (X < 0 || X > 100 || Y < 0 || Y > 100)
                {
                    result.IsValid = false;
                    result.Errors.Add("Percentage coordinates must be between 0 and 100");
                }
                if (X + Width > 100 || Y + Height > 100)
                {
                    result.IsValid = false;
                    result.Errors.Add("Zone extends beyond document boundaries (percentage)");
                }
                break;

            case CoordinateSystem.Pixel:
                if (X < 0 || Y < 0)
                {
                    result.IsValid = false;
                    result.Errors.Add("Pixel coordinates cannot be negative");
                }
                break;

            case CoordinateSystem.Points:
                if (X < 0 || Y < 0)
                {
                    result.IsValid = false;
                    result.Errors.Add("Point coordinates cannot be negative");
                }
                break;
        }

        // Validate color format
        if (!string.IsNullOrEmpty(HighlightColor) && !IsValidHexColor(HighlightColor))
        {
            result.IsValid = false;
            result.Errors.Add("Highlight color must be in hex format (#RRGGBB)");
        }

        // Validate opacity
        if (Opacity < 0.0 || Opacity > 1.0)
        {
            result.IsValid = false;
            result.Errors.Add("Opacity must be between 0.0 and 1.0");
        }

        return result;
    }

    /// <summary>
    /// Checks if a point is within this extraction zone
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <returns>True if point is within zone</returns>
    public bool ContainsPoint(double x, double y)
    {
        return x >= X && x <= X + Width && y >= Y && y <= Y + Height;
    }

    /// <summary>
    /// Checks if this zone overlaps with another zone
    /// </summary>
    /// <param name="other">Other extraction zone</param>
    /// <returns>True if zones overlap</returns>
    public bool OverlapsWith(ExtractionZone other)
    {
        if (PageNumber != other.PageNumber)
            return false;

        return !(X + Width < other.X || other.X + other.Width < X ||
                Y + Height < other.Y || other.Y + other.Height < Y);
    }

    /// <summary>
    /// Gets the area of this zone
    /// </summary>
    /// <returns>Zone area</returns>
    public double GetArea()
    {
        return Width * Height;
    }

    /// <summary>
    /// Creates a standard zone for text extraction
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="width">Width</param>
    /// <param name="height">Height</param>
    /// <param name="pageNumber">Page number</param>
    /// <returns>Configured text extraction zone</returns>
    public static ExtractionZone CreateTextZone(double x, double y, double width, double height, int pageNumber = 1)
    {
        return new ExtractionZone
        {
            Name = "Text Zone",
            Description = "Text extraction zone",
            X = x,
            Y = y,
            Width = width,
            Height = height,
            PageNumber = pageNumber,
            ZoneType = ExtractionZoneType.Text,
            HighlightColor = "#0066CC",
            ExtractionSettings = new ZoneExtractionSettings
            {
                TextExtractionMode = TextExtractionMode.PlainText,
                TrimWhitespace = true
            }
        };
    }

    /// <summary>
    /// Creates a zone optimized for OCR extraction
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    /// <param name="width">Width</param>
    /// <param name="height">Height</param>
    /// <param name="pageNumber">Page number</param>
    /// <returns>Configured OCR extraction zone</returns>
    public static ExtractionZone CreateOcrZone(double x, double y, double width, double height, int pageNumber = 1)
    {
        return new ExtractionZone
        {
            Name = "OCR Zone",
            Description = "OCR extraction zone for scanned content",
            X = x,
            Y = y,
            Width = width,
            Height = height,
            PageNumber = pageNumber,
            ZoneType = ExtractionZoneType.OCR,
            HighlightColor = "#FF6600",
            ExtractionSettings = new ZoneExtractionSettings
            {
                TextExtractionMode = TextExtractionMode.OCR,
                OcrLanguage = "eng",
                OcrConfidenceThreshold = 70
            }
        };
    }

    /// <summary>
    /// Validates hex color format
    /// </summary>
    private bool IsValidHexColor(string color)
    {
        if (string.IsNullOrEmpty(color) || color.Length != 7 || color[0] != '#')
            return false;

        for (int i = 1; i < 7; i++)
        {
            char c = color[i];
            if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                return false;
        }

        return true;
    }
}

/// <summary>
/// Coordinate system types for extraction zones
/// </summary>
public enum CoordinateSystem
{
    /// <summary>
    /// Pixel-based coordinates (absolute)
    /// </summary>
    Pixel,

    /// <summary>
    /// Percentage-based coordinates (0-100)
    /// </summary>
    Percentage,

    /// <summary>
    /// Point-based coordinates (72 points per inch)
    /// </summary>
    Points,

    /// <summary>
    /// Normalized coordinates (0.0-1.0)
    /// </summary>
    Normalized
}

/// <summary>
/// Types of extraction zones
/// </summary>
public enum ExtractionZoneType
{
    /// <summary>
    /// Standard text extraction zone
    /// </summary>
    Text,

    /// <summary>
    /// OCR-based extraction zone
    /// </summary>
    OCR,

    /// <summary>
    /// Image extraction zone
    /// </summary>
    Image,

    /// <summary>
    /// Table/structured data zone
    /// </summary>
    Table,

    /// <summary>
    /// Signature/handwriting zone
    /// </summary>
    Signature,

    /// <summary>
    /// Barcode/QR code zone
    /// </summary>
    Barcode
}

/// <summary>
/// Visual selection settings for UI support
/// </summary>
public class VisualSelectionSettings
{
    /// <summary>
    /// Show resize handles for zone editing
    /// </summary>
    public bool ShowResizeHandles { get; set; } = true;

    /// <summary>
    /// Allow zone dragging
    /// </summary>
    public bool AllowDragging { get; set; } = true;

    /// <summary>
    /// Show zone labels
    /// </summary>
    public bool ShowLabels { get; set; } = true;

    /// <summary>
    /// Label position relative to zone
    /// </summary>
    public LabelPosition LabelPosition { get; set; } = LabelPosition.TopLeft;

    /// <summary>
    /// Border style for zone display
    /// </summary>
    public BorderStyle BorderStyle { get; set; } = BorderStyle.Solid;

    /// <summary>
    /// Border thickness in pixels
    /// </summary>
    public int BorderThickness { get; set; } = 2;

    /// <summary>
    /// Snap to grid when moving/resizing
    /// </summary>
    public bool SnapToGrid { get; set; } = true;

    /// <summary>
    /// Grid size for snapping
    /// </summary>
    public int GridSize { get; set; } = 10;

    /// <summary>
    /// Creates a copy of the visual settings
    /// </summary>
    public VisualSelectionSettings CreateCopy()
    {
        return new VisualSelectionSettings
        {
            ShowResizeHandles = ShowResizeHandles,
            AllowDragging = AllowDragging,
            ShowLabels = ShowLabels,
            LabelPosition = LabelPosition,
            BorderStyle = BorderStyle,
            BorderThickness = BorderThickness,
            SnapToGrid = SnapToGrid,
            GridSize = GridSize
        };
    }
}

/// <summary>
/// Zone-specific extraction settings
/// </summary>
public class ZoneExtractionSettings
{
    /// <summary>
    /// Text extraction mode
    /// </summary>
    public TextExtractionMode TextExtractionMode { get; set; } = TextExtractionMode.PlainText;

    /// <summary>
    /// OCR language (if using OCR)
    /// </summary>
    public string OcrLanguage { get; set; } = "eng";

    /// <summary>
    /// OCR confidence threshold (0-100)
    /// </summary>
    public int OcrConfidenceThreshold { get; set; } = 70;

    /// <summary>
    /// Preserve text formatting
    /// </summary>
    public bool PreserveFormatting { get; set; } = false;

    /// <summary>
    /// Trim whitespace from extracted text
    /// </summary>
    public bool TrimWhitespace { get; set; } = true;

    /// <summary>
    /// Remove line breaks
    /// </summary>
    public bool RemoveLineBreaks { get; set; } = false;

    /// <summary>
    /// Minimum text length to extract
    /// </summary>
    public int MinimumTextLength { get; set; } = 1;

    /// <summary>
    /// Maximum text length to extract
    /// </summary>
    public int MaximumTextLength { get; set; } = 10000;

    /// <summary>
    /// Creates a copy of the extraction settings
    /// </summary>
    public ZoneExtractionSettings CreateCopy()
    {
        return new ZoneExtractionSettings
        {
            TextExtractionMode = TextExtractionMode,
            OcrLanguage = OcrLanguage,
            OcrConfidenceThreshold = OcrConfidenceThreshold,
            PreserveFormatting = PreserveFormatting,
            TrimWhitespace = TrimWhitespace,
            RemoveLineBreaks = RemoveLineBreaks,
            MinimumTextLength = MinimumTextLength,
            MaximumTextLength = MaximumTextLength
        };
    }
}

/// <summary>
/// Text extraction modes
/// </summary>
public enum TextExtractionMode
{
    /// <summary>
    /// Extract plain text without formatting
    /// </summary>
    PlainText,

    /// <summary>
    /// Extract text with formatting preserved
    /// </summary>
    FormattedText,

    /// <summary>
    /// Use OCR for text extraction
    /// </summary>
    OCR,

    /// <summary>
    /// Extract structured data (tables, lists)
    /// </summary>
    StructuredData
}

/// <summary>
/// Label positions for visual zones
/// </summary>
public enum LabelPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Center,
    Above,
    Below
}

/// <summary>
/// Border styles for visual zones
/// </summary>
public enum BorderStyle
{
    Solid,
    Dashed,
    Dotted,
    Double
} 