using System.Text;
using System.Text.Json;
using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;
using System.Windows.Forms;
using UndertaleModLib.Models;
using static System.Windows.Forms.FileDialog;
using UndertaleModLib;
using UndertaleModLib.Decompiler;

var bjson = File.ReadAllText("../input.json");
var jData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(bjson);
if (jData.ContainsKey("date"))
    jData.Remove("date");

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
        if (!lastExists)
        {
            var c = ((UndertaleString)(instruction.ValueString).Resource).Content;
            if (!jData.ContainsKey(c))
                continue;
            lastExists = true;
            entry = c;
        }
        else
        {
            lastExists = false;
            ((UndertaleString)(instruction.ValueString).Resource).Content = jData[entry].GetString();
        }
    }
}
