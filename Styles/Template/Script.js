Console.WriteLine("initialized!");

async function Init() {
    Console.WriteLine("init called");
}

async function RegisterCustomSettings() {
    return [
        new CustomProperty("D", "bool", "boolean"),
        new CustomProperty("E", "int", "integer"),
        new CustomProperty("F", "string", "string"),
    ];
}

async function RegisterCustomProperties() {
    return [
        new CustomProperty("A", "bool", "boolean"),
        new CustomProperty("B", "int", "integer"),
        new CustomProperty("C", "string", "string"),
    ];
}

async function RegisterCustomTools() {
    return [
        new CustomTool("Custom Tool", async function (wh) {
            var w = new InfoWindow("This is a tool test message.");
            await wh.ShowDialog(w);
        }),
        new CustomTool("File Picker Example", async function (wh) {
            Console.WriteLine("1");
            var p = new FilePickerOpenOptions();
            Console.WriteLine("2");
            p.Title = "Select a file";
            {
                Console.WriteLine("3");
                var f = new FilePickerFileType("Example");
                Console.WriteLine("4");
                {
                    var a = new lib.System.Collections.Generic.List(lib.System.String);
                    a.Add("data.win");
                    f.Patterns = a;
                }
            }
            {
                Console.WriteLine("5");
                var f = new FilePickerFileType("All files");
                Console.WriteLine("6");
                {
                    var a = new lib.System.Collections.Generic.List(lib.System.String);
                    a.Add("*");
                    f.Patterns = a;
                }
            }
            Console.WriteLine("7");
            f = await wh.GetStorageProvider().OpenFilePickerAsync(f);
            Console.WriteLine("8");
            var w = new InfoWindow(`File(s) chosen: ${", ".concat(...f.Select(x => x.Name))}`);
            Console.WriteLine("9");
            await wh.ShowDialog(w);
            Console.WriteLine("10");
        }),
    ];
}
