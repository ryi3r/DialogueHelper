using SixLabors.ImageSharp;

namespace DialogueHelper.PreviewRenderer;

public partial class ExternalGlyph
{
    public (double X, double Y) Scale = (1, 1);
    public Color Color = Color.White;
    public double Alpha = 1.0;
    public (double X, double Y) Position = (0, 0);
    public (double X, double Y) Size = (0, 0);
}
