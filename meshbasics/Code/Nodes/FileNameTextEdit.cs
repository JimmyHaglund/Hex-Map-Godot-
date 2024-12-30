using System.Text.RegularExpressions;
using Godot;

namespace JHM.MeshBasics;
public sealed partial class FileNameTextEdit : TextEdit {
    [Export] private string _allowedCharactersRegex = "^[a-zA-Z0-9_\\-\\. ]*$"; // Adjust the regex to allow your desired characters.

    public override void _Ready() {
        TextChanged += ValidateText;
    }

    private void ValidateText() {
        var text = Text;
        if (Regex.IsMatch(text, _allowedCharactersRegex)) return;

        Text = Regex.Replace(text, "[^a-zA-Z0-9_\\-\\. ]", "");
        // Reset the caret position to match user's editing flow.
        var line = GetLineCount() - 1;
        SetCaretLine(line);
        SetCaretColumn(GetLine(line).Length);
    }
}
