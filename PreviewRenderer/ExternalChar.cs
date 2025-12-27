
namespace DialogueHelper.PreviewRenderer;

public partial class ExternalChar
{
    public required string Char;
    public required int Index;
    public required string String;

    public (double X, double Y) StartPosition = (0, 0);
    public (double X, double Y) PositionOffset = (0, 0);

    public required bool IsIgnore;
    public required bool IsNewline;
}
