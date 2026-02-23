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

var bJson = File.ReadAllText("../input.json");
var jData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(bJson)!;
if (jData.ContainsKey("date"))
    jData.Remove("date");

var refs = new Dictionary<UndertaleString, int>();
foreach (var code in Data.Code)
{
    if (code.ParentEntry is not null)
        continue;
    foreach (var inst in code.Instructions)
    {
        if (!(UndertaleInstruction.GetInstructionType(inst.Kind) == UndertaleInstruction.InstructionType.PushInstruction && inst.Type1 == UndertaleInstruction.DataType.String))
            continue;
        refs.TryAdd(inst.ValueString.Resource, 0);
        refs[inst.ValueString.Resource]++;
    }
}

foreach (var code in Data.Code)
{
    if (code.ParentEntry is not null)
        continue;
    var lastExists = false;
    var entry = "";

    foreach (var inst in code.Instructions)
    {
        if (!(UndertaleInstruction.GetInstructionType(inst.Kind) == UndertaleInstruction.InstructionType.PushInstruction && inst.Type1 == UndertaleInstruction.DataType.String))
            continue;
        if (!lastExists)
        {
            var c = inst.ValueString.Resource.Content;
            if (!jData.ContainsKey(c))
                continue;
            lastExists = true;
            entry = c;
        }
        else
        {
            lastExists = false;
            var res = inst.ValueString.Resource;
            refs.TryAdd(res, 0);
            if (refs[res] <= 1)
                inst.ValueString.Resource.Content = jData[entry].GetString(); // safe to update
            else
                inst.ValueString = new(Data.Strings.MakeString(jData[entry].GetString(), createNew: true));
        }
    }
}