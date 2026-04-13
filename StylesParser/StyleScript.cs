using System.Collections.Generic;
using System.Threading.Tasks;
using DialogueHelper.External;
using DialogueHelper.PreviewRenderer;

public class StyleScript
{
    public virtual Task Init(CustomToolWindowHandler wh) => Task.CompletedTask;

    public virtual Task<IEnumerable<ICustomProperty>> RegisterCustomSettings() => Task.FromResult<IEnumerable<ICustomProperty>>([]);

    public virtual Task<IEnumerable<ICustomProperty>> RegisterCustomProperties() => Task.FromResult<IEnumerable<ICustomProperty>>([]);

    public virtual Task<IEnumerable<CustomTool>> RegisterCustomTools() => Task.FromResult<IEnumerable<CustomTool>>([]);

    // if true then it finishes drawing early.
    public virtual Task<bool> PrepareDraw(CustomToolWindowHandler wh, ExternalData data) => Task.FromResult(false);

    // if true then it finishes drawing early.
    public virtual Task<bool> DrawGlyph(CustomToolWindowHandler wh, ExternalData data) => Task.FromResult(false);
}