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

    public virtual CustomTool? OnFileSave => null;

    /// <summary>
    /// if it returns true then it finishes drawing early.
    /// </summary>
    public virtual Task<bool> PrepareDraw(CustomToolWindowHandler wh, ExternalData data) => Task.FromResult(false);

    /// <summary>
    /// if it returns true then it finishes drawing early.
    /// </summary>
    public virtual Task<bool> DrawGlyph(CustomToolWindowHandler wh, ExternalData data) => Task.FromResult(false);
}