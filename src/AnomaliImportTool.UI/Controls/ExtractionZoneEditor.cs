using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AnomaliImportTool.Core.Models;
using System.Windows.Input;
using System;
using System.Linq;

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

    private ExtractionZone? _activeZone;
    private Point _startPoint;
    private bool _isDrawing;
    private bool _isMoving;

    public ExtractionZone? SelectedZone
    {
        get => _activeZone;
        private set
        {
            _activeZone = value;
            InvalidateVisual();
        }
    }

    public ExtractionZoneEditor()
    {
        Background = Brushes.Transparent;
        Cursor = Cursors.Cross;
        Focusable = true;

        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        KeyDown += OnKeyDown;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
        var pos = e.GetPosition(this);
        var hitZone = HitTestZone(pos);
        if (hitZone != null)
        {
            // Select and start moving
            SelectedZone = hitZone;
            _isMoving = true;
            _startPoint = pos;
        }
        else
        {
            // Start drawing new zone
            _isDrawing = true;
            _startPoint = pos;
            var zone = new ExtractionZone
            {
                Id = Guid.NewGuid(),
                Name = $"Zone{Zones.Count + 1}",
                PageNumber = 1,
                X = pos.X,
                Y = pos.Y,
                Width = 0,
                Height = 0
            };
            Zones.Add(zone);
            SelectedZone = zone;
        }
        CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDrawing && !_isMoving) return;
        var pos = e.GetPosition(this);
        if (SelectedZone == null) return;

        if (_isDrawing)
        {
            SelectedZone.Width = Math.Abs(pos.X - _startPoint.X);
            SelectedZone.Height = Math.Abs(pos.Y - _startPoint.Y);
            SelectedZone.X = Math.Min(pos.X, _startPoint.X);
            SelectedZone.Y = Math.Min(pos.Y, _startPoint.Y);
        }
        else if (_isMoving)
        {
            var dx = pos.X - _startPoint.X;
            var dy = pos.Y - _startPoint.Y;
            SelectedZone.X += dx;
            SelectedZone.Y += dy;
            _startPoint = pos;
        }
        InvalidateVisual();
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDrawing || _isMoving)
        {
            _isDrawing = _isMoving = false;
            ReleaseMouseCapture();
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && SelectedZone != null)
        {
            Zones.Remove(SelectedZone);
            SelectedZone = null;
            InvalidateVisual();
        }
    }

    private ExtractionZone? HitTestZone(Point pos)
    {
        return Zones.FirstOrDefault(z => pos.X >= z.X && pos.X <= z.X + z.Width && pos.Y >= z.Y && pos.Y <= z.Y + z.Height);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (Zones == null) return;

        foreach (var zone in Zones)
        {
            var rect = new Rect(zone.X, zone.Y, zone.Width, zone.Height);
            var pen = zone == SelectedZone ? new Pen(Brushes.LimeGreen, 3) : new Pen(Brushes.Red, 2);
            dc.DrawRectangle(null, pen, rect);
        }
    }
} 