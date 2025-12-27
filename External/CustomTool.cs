using System;
using System.Threading.Tasks;
using Microsoft.ClearScript;

namespace DialogueHelper.External;

public class CustomTool(string name, ScriptObject func)
{
    public string Name = name;
    public ScriptObject Func = func;
}
