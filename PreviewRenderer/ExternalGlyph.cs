using SixLabors.ImageSharp;

namespace DialogueHelper.PreviewRenderer;

public class ExternalGlyph
{
    public (double X, double Y) Scale = (1.0, 1.0);
    public Color Color = new() { R = 0xff, G = 0xff, B = 0xff, A = 0xff };
    public double Alpha = 1.0;
    public (double X, double Y) Position = (0.0f, 0.0f);
    public (double X, double Y) Size = (0.0, 0.0);
}
