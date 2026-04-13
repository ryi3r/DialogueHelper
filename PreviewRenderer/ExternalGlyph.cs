using SixLabors.ImageSharp;

namespace DialogueHelper.PreviewRenderer;

public struct ExternalGlyph()
{
    public (double X, double Y) Scale = (1, 1);
    public Color Color = new() { R = 0xff, G = 0xff, B = 0xff, A = 0xff };
    public (double X, double Y) Position = (0, 0);
    public (double X, double Y) Size = (0, 0);
}
