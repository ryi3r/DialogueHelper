using System;
using System.Threading.Tasks;

namespace DialogueHelper.External;

public class CustomTool(string name, Func<CustomToolWindowHandler, Task> func)
{
    public string Name = name;
    public Func<CustomToolWindowHandler, Task> Func = func;
}
