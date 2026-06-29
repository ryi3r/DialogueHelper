using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace DialogueHelper.PreviewRenderer;

public class TintedImage : Control
{
    public static readonly StyledProperty<SKBitmap?> SourceProperty = AvaloniaProperty.Register<TintedImage, SKBitmap?>(nameof(Source));
    public static readonly StyledProperty<PixelRect?> SourceRectProperty = AvaloniaProperty.Register<TintedImage, PixelRect?>(nameof(SourceRect));
    public static readonly StyledProperty<Color> TintProperty = AvaloniaProperty.Register<TintedImage, Color>(nameof(Tint), new() { R = 255, G = 255, B = 255 });
    public static readonly StyledProperty<bool> TintEnabledProperty = AvaloniaProperty.Register<TintedImage, bool>(nameof(TintEnabled));

    public SKBitmap? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public PixelRect? SourceRect
    {
        get => GetValue(SourceRectProperty);
        set => SetValue(SourceRectProperty, value);
    }

    public Color Tint
    {
        get => GetValue(TintProperty);
        set => SetValue(TintProperty, value);
    }

    public bool TintEnabled
    {
        get => GetValue(TintEnabledProperty);
        set => SetValue(TintEnabledProperty, value);
    }

    public double ScaleX { get; set; } = 1;
    public double ScaleY { get; set; } = 1;

    static TintedImage() => AffectsRender<TintedImage>(SourceProperty, SourceRectProperty, TintProperty, TintEnabledProperty);

    protected override Size MeasureOverride(Size availableSize) => new(SourceRect?.Width ?? Source?.Width ?? 0 * ScaleX, SourceRect?.Height ?? Source?.Height ?? 0 * ScaleY);

    public override void Render(DrawingContext context)
    {
        if (Source is null)
            return;
        context.Custom(new TintedDrawOp(new(Bounds.Size), Source, SourceRect ?? new(0, 0, Source.Width, Source.Height), Tint, TintEnabled));
    }

    sealed class TintedDrawOp(Rect bounds, SKBitmap src, PixelRect srcRect, Color tint, bool tintEnabled) : ICustomDrawOperation
    {
        readonly SKBitmap _src = src;
        readonly PixelRect _srcRect = srcRect;
        readonly Color _tint = tint;
        readonly bool _tintEnabled = tintEnabled;

        public Rect Bounds { get; } = bounds;
        public bool HitTest(Point p) => Bounds.Contains(p);
        public bool Equals(ICustomDrawOperation? other) => false;
        public void Dispose() { }

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>()!;
            using var lease = leaseFeature.Lease();
            using var paint = new SKPaint { IsAntialias = false };
            if (_tintEnabled)
            {
                paint.ColorFilter = SKColorFilter.CreateColorMatrix([
                    _tint.R / 255f,              0,              0,              0, 0,
                                 0, _tint.G / 255f,              0,              0, 0,
                                 0,              0, _tint.B / 255f,              0, 0,
                                 0,              0,              0, _tint.A / 255f, 0,
                ]);
            }
            lease.SkCanvas.DrawBitmap(_src, new SKRect(_srcRect.X, _srcRect.Y, _srcRect.Right, _srcRect.Bottom), Bounds.ToSKRect(), paint);
        }
    }
}