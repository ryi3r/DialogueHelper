using System.Collections.Generic;
using System.Threading.Tasks;
using DialogueHelper.External;
using DialogueHelper.PreviewRenderer;

public class StyleScript
{
    public virtual async Task Init(CustomToolWindowHandler wh) { }

    public virtual async Task<IEnumerable<ICustomProperty>> RegisterCustomSettings() => [];

    public virtual async Task<IEnumerable<ICustomProperty>> RegisterCustomProperties() => [];

    public virtual async Task<IEnumerable<CustomTool>> RegisterCustomTools() => [];

    // if true then it finishes drawing early.
    public virtual async Task<bool> PrepareDraw(CustomToolWindowHandler wh, ExternalData data) => false;

    // if true then it finishes drawing early.
    public virtual async Task<bool> DrawGlyph(CustomToolWindowHandler wh, ExternalData data) => false;
}