using Godot;

namespace JHM;

public sealed partial class FileNameTextEdit : TextEdit {
    [Export] private string _allowedCharactersRegex = "^[a-zA-Z0-9_\\-\\. ]*$"; // Adjust the regex to allow your desired characters.

    public override void _Ready() {
        TextChanged += () => this.ValidateFileTextInput();
    }
}
