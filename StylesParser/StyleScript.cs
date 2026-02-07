using System.Collections.Generic;
using System.Threading.Tasks;
using DialogueHelper.External;
using DialogueHelper.PreviewRenderer;

public class StyleScript
{
    public virtual async Task Init(CustomToolWindowHandler wh) { }

    public virtual async Task<IEnumerable<CustomProperty>> RegisterCustomSettings() => [];

    public virtual async Task<IEnumerable<CustomProperty>> RegisterCustomProperties() => [];

    public virtual async Task<IEnumerable<CustomTool>> RegisterCustomTools() => [];

    public virtual async Task PrepareDraw(CustomToolWindowHandler wh, ExternalData data) { }

    public virtual async Task DrawGlyph(CustomToolWindowHandler wh, ExternalData data) { }
}