using Godot;

namespace JHM.MeshBasics;
internal static class NodeExtensions {
    public static T InstantiateChild<T>(this Node parent, PackedScene scene, string name = null) where T : Node {
        T result = scene.Instantiate<T>();
        parent.AddChild(result);
        if (name is not null) {
            result.Name = name;
        }
        return result;
    }

    public static T InstantiateOrphan<T>(this Node parent, PackedScene scene, string name = null) where T : Node {
        T result = scene.Instantiate<T>();
        if (name is not null) {
            result.Name = name;
        }
        return result;
    }
}
