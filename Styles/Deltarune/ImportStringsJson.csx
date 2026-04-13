using System.Text.Json;
using System.Linq;
using UndertaleModLib.Models;
using UndertaleModLib;
using System.IO;
using System.Collections.Generic;

if (!Data.Code.Any(x => x.Name.Content == "gml_GlobalScript_msgnextloc"))
{
    // ch1
    File.Copy("../input.json", Path.Combine(FilePath, "../lang/lang_en.json"), true);
    return;
}

var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText("../input.json"))!;
data.Remove("date");

var refs = new Dictionary<UndertaleString, int>();
foreach (var code in Data.Code)
{
    if (code.ParentEntry is not null)
        continue;
    foreach (var inst in code.Instructions)
    {
        if (UndertaleInstruction.GetInstructionType(inst.Kind) == UndertaleInstruction.InstructionType.PushInstruction && inst.Type1 == UndertaleInstruction.DataType.String)
        {
            refs.TryAdd(inst.ValueString.Resource, 0);
            refs[inst.ValueString.Resource]++;
        }
    }
}

var locFuncs = new HashSet<string>() { "gml_Script_msgsetloc", "gml_Script_msgnextloc", "gml_Script_stringsetloc", "gml_Script_msgsetsubloc", "gml_Script_msgnextsubloc", "gml_Script_stringsetsubloc", "gml_Script_c_msgsetloc", "gml_Script_c_msgnextloc", "gml_Script_c_msgnextloc", "gml_Script_c_msgsetsubloc", "gml_Script_c_msgnextsubloc" };

foreach (var code in Data.Code)
{
    if (code.ParentEntry is not null)
        continue;
    for (var i = 0; i < code.Instructions.Count; i++)
    {
        var inst = code.Instructions[i];
        if (UndertaleInstruction.GetInstructionType(inst.Kind) == UndertaleInstruction.InstructionType.CallInstruction && locFuncs.TryGetValue(inst.ValueFunction.Name.Content, out var argPos))
        {
            var _args = new List<UndertaleInstruction>();
            var j = i;
            for (var k = 0; k < 2; k++)
            {
                while (j >= 0)
                {
                    j--;
                    var cur = code.Instructions[j];
                    if (UndertaleInstruction.GetInstructionType(cur.Kind) == UndertaleInstruction.InstructionType.PushInstruction && cur.Type1 == UndertaleInstruction.DataType.String)
                    {
                        _args.Insert(0, cur);
                        break;
                    }
                }
            }
            if (data.TryGetValue(_args[0].ValueString.Resource.Content, out var elm))
            {
                var res = _args[1].ValueString.Resource;
                refs.TryAdd(res, 0);
                var s = elm.GetString();
                if (refs[res] <= 1)
                    inst.ValueString.Resource.Content = s; // safe to update
                else
                    inst.ValueString = new(Data.Strings.MakeString(s, createNew: true));
            }
        }
    }
}
