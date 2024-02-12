using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;
using System.Linq;
using System.Windows.Forms;

int progress = 0;
string fntFolder = Path.Combine(Path.GetDirectoryName(ScriptPath), @"Fonts\");
TextureWorker worker = new TextureWorker();
Directory.CreateDirectory(fntFolder);
List<string> input = new List<string>();

if (ShowInputDialog() == System.Windows.Forms.DialogResult.Cancel)
	return;

string[] arrayString = input.ToArray();

await DumpFonts();
worker.Cleanup();
HideProgressBar();
ScriptMessage("Finished!");

void UpdateProgress()
{
    UpdateProgressBar(null, "Fonts", progress++, Data.Fonts.Count);
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

async Task DumpFonts()
{
    await Task.Run(() => Parallel.ForEach(Data.Fonts, DumpFont));
}

void DumpFont(UndertaleFont font)
{
    worker.ExportAsPNG(font.Texture, fntFolder + font.Name.Content + ".png");
    using(StreamWriter writer = new StreamWriter(fntFolder + "glyphs_" + font.Name.Content + ".txt"))
    {
        writer.WriteLine(font.DisplayName + ";" + font.EmSize + ";" + font.Bold + ";" + font.Italic + ";" + font.Charset + ";" + font.AntiAliasing + ";" + font.ScaleX + ";" + font.ScaleY + ";" + font.Ascender + ";" + font.AscenderOffset);

        foreach(var g in font.Glyphs)
        {
            writer.WriteLine(g.Character + ";" + g.SourceX + ";" + g.SourceY + ";" + g.SourceWidth + ";" + g.SourceHeight + ";" + g.Shift + ";" + g.Offset);
        }
    }
}

private DialogResult ShowInputDialog()
{
    System.Drawing.Size size = new System.Drawing.Size(400, 400);
    Form inputBox = new Form();

    inputBox.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
    inputBox.ClientSize = size;
    inputBox.Text = "Fonts exporter";

    System.Windows.Forms.CheckedListBox fonts_list = new CheckedListBox();
    foreach(var x in Data.Fonts)
    {
        fonts_list.Items.Add(x.Name.ToString().Replace("\"", ""));
    }
    for (int i = 0; i < fonts_list.Items.Count; i++)
    {
        fonts_list.SetItemChecked(i, true);
    }

    fonts_list.Size = new System.Drawing.Size(size.Width - 10, size.Height - 50);
    fonts_list.Location = new System.Drawing.Point(5, 5);
    inputBox.Controls.Add(fonts_list);
    
    Button okButton = new Button();
    okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
    okButton.Name = "okButton";
    okButton.Size = new System.Drawing.Size(75, 23);
    okButton.Text = "&OK";
    okButton.Location = new System.Drawing.Point(size.Width - 80 - 80, size.Height - 39);
    inputBox.Controls.Add(okButton);

    Button cancelButton = new Button();
    cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
    cancelButton.Name = "cancelButton";
    cancelButton.Size = new System.Drawing.Size(75, 23);
    cancelButton.Text = "&Cancel";
    cancelButton.Location = new System.Drawing.Point(size.Width - 80, size.Height - 39);
    inputBox.Controls.Add(cancelButton);

    inputBox.AcceptButton = okButton;
    inputBox.CancelButton = cancelButton; 

    DialogResult result = inputBox.ShowDialog();
    
    foreach(var item in fonts_list.CheckedItems)
    {
        input.Add(item.ToString());
    }

    return result;
}