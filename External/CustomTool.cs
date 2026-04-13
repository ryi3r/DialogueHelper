using System;
using System.Threading.Tasks;

namespace DialogueHelper.External;

public class CustomTool(string name, Func<CustomToolWindowHandler, Task> func, string? hotKey = null)
{
    public string Name = name;
    public Func<CustomToolWindowHandler, Task> Func = func;
    public string? HotKey = hotKey;
}
