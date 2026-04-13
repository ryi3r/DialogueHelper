using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace DialogueHelper.Views;

public partial class TextBoxWindow : Window
{
    public FilePickerFileType[] FilePickerFormat = [];

    public TextBoxWindow() => InitializeComponent();

    public TextBoxWindow(string header, string content, FilePickerFileType[] filePickerFormat, bool editable = false) : this()
    {
        InnerText.Text = header;
        TextBox.Text = content;
        TextBox.IsReadOnly = !editable;
        FilePickerFormat = filePickerFormat;
    }
    
    void OkButton_OnClick(object? sender, RoutedEventArgs e) => Close(true);

    async void SaveTextButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var data = await StorageProvider.SaveFilePickerAsync(new()
        {
            Title = "Select a file to save to",
            FileTypeChoices = FilePickerFormat,
        });
        if (data is null)
            return;
        var stream = await data.OpenWriteAsync();
        await stream.WriteAsync(Encoding.UTF8.GetBytes(TextBox.Text ?? ""));
        await stream.DisposeAsync();
    }

    async void CopyClipboardTextButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Clipboard is null)
            return;
        var data = new DataTransfer();
        data.Add(DataTransferItem.CreateText(TextBox.Text ?? ""));
        await Clipboard.SetDataAsync(data);
    }
}