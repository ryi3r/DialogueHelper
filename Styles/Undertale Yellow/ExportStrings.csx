using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;
using System.Windows.Forms;
using static System.Windows.Forms.FileDialog;
using static System.Uri;

EnsureDataLoaded();

using (SaveFileDialog saveFileDialog = new SaveFileDialog())
{
    saveFileDialog.InitialDirectory = "c:\\";
    saveFileDialog.Filter = "txt files (*.txt)|*.txt";
    saveFileDialog.FilterIndex = 1;
    saveFileDialog.RestoreDirectory = true;

    if (saveFileDialog.ShowDialog() == DialogResult.OK)
    {
        var fileStream = saveFileDialog.OpenFile();

        using (StreamWriter writer = new StreamWriter(fileStream))
        {
            int id = 0;
            foreach (var code in Data.Code)
            {
                if (code == null)
                    continue;
                
                var wrote_header = false;

                foreach (var instruction in code.Instructions)
                {
                    if (UndertaleInstruction.GetInstructionType(instruction.Kind) == UndertaleInstruction.InstructionType.PushInstruction
                        && instruction.Type1 == UndertaleInstruction.DataType.String)
                    {
                        if (!wrote_header) {
                            wrote_header = true;
                            writer.WriteLine("0;ID:" + Uri.EscapeDataString(code.Name.Content));
                        }
                        var str = ((UndertaleString)((UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>)instruction.Value).Resource).Content;
                        writer.WriteLine("1;ID:" + id.ToString() + ";OriginalContent:" + Uri.EscapeDataString(str));
                    }
                }
                id += 1;
            }
            writer.Write("8;"); // File end.
        }
    }
}