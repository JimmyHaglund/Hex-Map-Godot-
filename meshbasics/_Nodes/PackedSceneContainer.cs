using Godot;

namespace JHM.MeshBasics;
public sealed partial class PackedSceneContainer : Node {
    [Export] public PackedScene[] Scenes { get; set; }
}

public static class PackedSceneContainerCollectionExtensions { 

    public static PackedScene[][] To2DArray(this PackedSceneContainer[] packedScenes) { 
        var result = new PackedScene[packedScenes.Length][];
        for(var n = 0; n < packedScenes.Length; n++) { 
            result[n] = packedScenes[n].Scenes;
        }

        return result;
    }
}
