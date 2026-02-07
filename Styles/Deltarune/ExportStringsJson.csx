using System.Text.Json;
using System.Linq;
using UndertaleModLib.Models;
using UndertaleModLib;
using System.Collections.Generic;
using System.IO;

if (!Data.Code.Any(x => x.Name.Content == "gml_GlobalScript_msgnextloc"))
{
    // ch1
    File.Copy(Path.Combine(FilePath, "../lang/lang_en.json"), "../output.json", true);
    return;
}

var jData = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(FilePath, "../lang/lang_ja.json")));
var fData = new Dictionary<string, string>();

foreach (var code in Data.Code)
{
    if (code.ParentEntry is not null)
        continue;
    var lastExists = false;
    var entry = "";

    foreach (var instruction in code.Instructions)
    {
        if (!(UndertaleInstruction.GetInstructionType(instruction.Kind) == UndertaleInstruction.InstructionType.PushInstruction && instruction.Type1 == UndertaleInstruction.DataType.String))
            continue;
        var c = instruction.ValueString.Resource.Content.Replace("\r", "\\r").Replace("\n", "\\n");
        if (!lastExists)
        {
            if (!jData.ContainsKey(c))
                continue;
            lastExists = true;
            entry = c;
        }
        else
        {
            lastExists = false;
            fData[entry] = c;
        }
    }
}

File.WriteAllText("../output.json", JsonSerializer.Serialize(fData, new JsonSerializerOptions() { WriteIndented = true }));
