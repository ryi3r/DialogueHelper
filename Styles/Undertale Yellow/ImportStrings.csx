using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using static Microsoft.VisualBasic.FileIO.TextFieldParser;
using System.Windows.Forms;
using static System.Windows.Forms.FileDialog;
using UndertaleModLib.Util;
using System.Buffers;
using static System.Uri;
using System.Collections.Generic;

EnsureDataLoaded();

foreach (UndertaleCode c in Data.Code)
    NukeProfileGML(c.Name.Content);

int lastString = Data.Strings.Count - 1;

using (OpenFileDialog openFileDialog = new OpenFileDialog())
{
    openFileDialog.InitialDirectory = "c:\\";
    openFileDialog.Filter = "txt files (*.txt)|*.txt";
    openFileDialog.FilterIndex = 1;
    openFileDialog.RestoreDirectory = true;

    if (openFileDialog.ShowDialog() == DialogResult.OK)
    {
        var filePath = openFileDialog.FileName;
        var fileStream = openFileDialog.OpenFile();

        using (StreamReader reader = new StreamReader(fileStream))
        {
            int line_no = 1;
            string line = "";
            var data = new Dictionary<string, List<string>>();
            var last_entry = "";
            while ((line = reader.ReadLine()) != null)
            {
                var entries = line.Split(";");
                var i = 0;
                int header = -1;
                var cid = 0;
                var og_content = "";
                var set_og = true;
                var content = "";
                foreach (var entry in entries)
                {
                    if (i == 0)
                    {
                        header = Int32.Parse(entry);
                    }
                    else
                    {
                        // Data
                        //ScriptMessage(entry);
                        var sec = entry.Split(":");
                        if (sec.Length < 2)
                            continue;
                        var name = sec[0];
                        var value = Uri.UnescapeDataString(sec[1]);
                        switch (header)
                        {
                            case 0: // New Entry
                            //ScriptMessage(name);
                            if (name == "ID")
                            {
                                data.Add(value, new());
                                last_entry = value;
                            }
                            break;
                            case 1: // Add String
                            if (name == "ID")
                            {
                                cid = Int32.Parse(value);
                            }
                            else if (name == "OriginalContent")
                            {
                                og_content = value;
                            }
                            else if (name == "Content")
                            {
                                content = value;
                                set_og = false;
                            }
                            break;
                        }
                    }
                    i += 1;
                }
                if (header == 1)
                {
                    //ScriptMessage(last_entry);
                    data[last_entry].Add(set_og ? og_content : content);
                }
                //str.Content = line;
            }
            foreach (var element in data)
            {
                var key = element.Key;
                var value = element.Value;
                foreach (var code in Data.Code)
                {
                    if (code == null)
                        continue;
                    if (code.Name.Content == key)
                    {
                        foreach (var instruction in code.Instructions)
                        {
                            if (UndertaleInstruction.GetInstructionType(instruction.Kind) == UndertaleInstruction.InstructionType.PushInstruction
                                && instruction.Type1 == UndertaleInstruction.DataType.String)
                            {
                                if (value.Count == 0)
                                    break;
                                var current = value[0];
                                value.RemoveAt(0);
                                // TODO: Check for already existing strings
                                UndertaleString String = new();
                                String.Content = current;
                                Data.Strings.Add(String);
                                UndertaleResourceById<UndertaleString, UndertaleChunkSTRG> Resource = new();
                                Resource.CachedId = lastString ++;
                                Resource.Resource = String;
                                instruction.Value = Resource;
                            }
                        }
                    }
                }
            }

            ReapplyProfileCode();
        }
    }else{
        ScriptError("Please load in a file.");
    }
}