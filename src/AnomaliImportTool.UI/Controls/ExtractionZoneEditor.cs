using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AnomaliImportTool.Core.Models;

namespace AnomaliImportTool.WPF.Controls;

/// <summary>
/// Placeholder control for editing extraction zones on top of a document preview.
/// </summary>
public class ExtractionZoneEditor : Canvas
{
    public static readonly DependencyProperty ZonesProperty = DependencyProperty.Register(
        nameof(Zones), typeof(ObservableCollection<ExtractionZone>), typeof(ExtractionZoneEditor),
        new FrameworkPropertyMetadata(new ObservableCollection<ExtractionZone>(), FrameworkPropertyMetadataOptions.AffectsRender));

    public ObservableCollection<ExtractionZone> Zones
    {
        get => (ObservableCollection<ExtractionZone>)GetValue(ZonesProperty);
        set => SetValue(ZonesProperty, value);
    }

    public ExtractionZoneEditor()
    {
        // Placeholder: will implement interactive editing in Task 3.2
        Background = Brushes.Transparent;
        IsHitTestVisible = false; // non-interactive for now
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (Zones == null) return;

        var pen = new Pen(Brushes.Red, 2);
        foreach (var zone in Zones)
        {
            var rect = new Rect(zone.X, zone.Y, zone.Width, zone.Height);
            dc.DrawRectangle(null, pen, rect);
        }
    }
} 