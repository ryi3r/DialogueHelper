using System.Text;
using System.Text.Json;
using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;
using UndertaleModLib.Models;
using UndertaleModLib;
using UndertaleModLib.Decompiler;

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
        var c = ((UndertaleString)(instruction.ValueString).Resource).Content.Replace("\r", "\\r").Replace("\n", "\\n");
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
