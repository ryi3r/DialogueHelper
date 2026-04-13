using System.Text.Json;
using System.Linq;
using UndertaleModLib.Models;
using UndertaleModLib;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text.Encodings.Web;

if (!Data.Code.Any(x => x.Name.Content == "gml_GlobalScript_msgnextloc"))
{
    // ch1
    File.Copy(Path.Combine(FilePath, "../lang/lang_en.json"), "../output.json", true);
    return;
}

var data = new Dictionary<string, string>();
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
            var key = _args[0].ValueString.Resource.Content;
            var value = _args[1].ValueString.Resource.Content;
            if (data.TryGetValue(key, out var r))
            {
                if (r != value)
                {
                    //ScriptMessage(string.Join("\n", code.Instructions[(i - 20)..(i + 1)].Select(x => $"{x}->{UndertaleInstruction.GetInstructionType(x.Kind)}")));
                    //ScriptMessage(string.Join("\n", _args.Select(x => $"{x}->{x.Kind},{x.Type1},{UndertaleInstruction.GetInstructionType(x.Kind)}")));
                    throw new($"on key {key} expected {value} but found {r}");
                }
            }
            else
                data.Add(key, value);
        }
    }
}

File.WriteAllText("../output.json", JsonSerializer.Serialize(data, new JsonSerializerOptions() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
