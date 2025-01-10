using Godot;
namespace JHM;

public static class SceneInstantiator {
    private static Node? _instance;

    public static void SetDefaultParent(Node root) { 
        _instance = root;
    }

    public static TResult? InstantiateAndAddRoot<TResult>(string scenePath, string? name = null) where TResult : Node {
        if (!ValidateInstance()) return null;

        var packedScene = LoadScene(scenePath);
        if (packedScene is null) return null;

        return InstantiateAndAddRoot<TResult>(_instance!, packedScene, name);
    }

    public static TResult? InstantiateAndAddRoot<TResult>(PackedScene target, string? name = null) where TResult : Node {
        if (!ValidateInstance()) return null;

        return InstantiateAndAddRoot<TResult>(_instance!, target, name);
    }

    /// <summary>
    /// Instantiates the target scene and adds it as a child to the root window of the parent.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="parent">A node contained in the root window where the scene should be contained.</param>
    /// <param name="target"></param>
    /// <param name="name">Optional name for the new scene instance.</param>
    /// <returns></returns>
    public static TResult? InstantiateAndAddRoot<TResult>(Node parent, PackedScene target, string? name = null) where TResult : Node {
        var result = target.Instantiate<TResult>();
        parent.GetTree().Root.AddChild(result);
        if (name is not null) result.Name = name;
        return result;
    }

    public static T InstantiateChild<T>(this Node parent, PackedScene scene, string? name = null) where T : Node {
        T result = scene.Instantiate<T>();
        parent.AddChild(result);
        if (name is not null) {
            result.Name = name;
        }
        return result;
    }

    public static T InstantiateOrphan<T>(this Node _, PackedScene scene, string? name = null) where T : Node {
        T result = scene.Instantiate<T>();
        if (name is not null) {
            result.Name = name;
        }
        return result;
    }

    public static T InstantiateOrphan<T>(PackedScene scene, string? name = null) where T : Node {
        T result = scene.Instantiate<T>();
        if (name is not null) {
            result.Name = name;
        }
        return result;
    }

    private static PackedScene? LoadScene(string scenePath) {
        var result = GD.Load<PackedScene>(scenePath);
        if (result is null) {
            GD.Print($"No scene found at path: {scenePath}");
            return null;
        }
        return result;
    }

    private static bool ValidateInstance() {
        var result = _instance is not null;
        if (!result) {
            GD.PrintErr("SceneInstantiator must be assigned a parent via the SceneInstantiator.SetDefaultParent method.");
        }
        return result;
    }
}
