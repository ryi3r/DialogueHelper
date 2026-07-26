using System;
using System.Threading.Tasks;

namespace DialogueHelper.External;

public class CustomTool(string name, string? tooltip, Func<CustomToolWindowHandler, Task> func, string? hotKey = null)
{
    public string Name = name;
    public string? Tooltip = tooltip;
    public Func<CustomToolWindowHandler, Task> Func = func;
    public string? HotKey = hotKey;
}
