using Godot;
using System.Text.RegularExpressions;

namespace JHM;

public static class TextEditExtensions {
    public static void ValidateFileTextInput(this TextEdit textEdit) {
        const string _allowedCharactersRegex = "^[a-zA-Z0-9_\\-\\. ]*$";
        var text = textEdit.Text;
        if (Regex.IsMatch(text, _allowedCharactersRegex)) return;

        textEdit.Text = Regex.Replace(text, "[^a-zA-Z0-9_\\-\\. ]", "");
        // Reset the caret position to match user's editing flow.
        var line = textEdit.GetLineCount() - 1;
        textEdit.SetCaretLine(line);
        textEdit.SetCaretColumn(textEdit.GetLine(line).Length);
    }
}
