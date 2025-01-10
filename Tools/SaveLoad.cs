using System;
using System.IO;
using Godot;

namespace JHM;
public static class BinarySaveLoad {
    public static void Save(string filePath, Action<BinaryWriter> writeAction) {
        using var fileStream = File.Open(filePath, FileMode.Create);
        using var writer = new BinaryWriter(fileStream);
        writeAction(writer);
    }

    public static void Load(string filePath, Action<BinaryReader> readAction) {
        if (!File.Exists(filePath)) {
            GD.PrintErr($"File does not exist at path: {filePath}");
            return;
        }
        using var fileStream = File.OpenRead(filePath);
        using var reader = new BinaryReader(fileStream);
        readAction(reader);
    }

    public static void Delete(string filePath) {
        if (string.IsNullOrEmpty(filePath)) {
            return;
        }
        if (!File.Exists(filePath)) return;
        File.Delete(filePath);
    }
}
