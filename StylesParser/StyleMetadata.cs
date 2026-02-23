using System.Collections.Generic;

namespace DialogueHelper.StylesParser;

public class StyleMetadata
{
    public required string Name;
    public required string Author;
    public string? ScriptPath;
    public string? Class;
    public required List<char> NewLines;
    public required List<char> Ignore;
}
