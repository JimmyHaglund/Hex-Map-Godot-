using Godot;

namespace JHM;

public partial class SceneInstantiator : Node {
    private static SceneInstantiator _instance;

    public override void _Ready() {
        _instance = _instance ?? this;
    }

    public static TResult Instantiate<TResult>(string scenePath, string name = null) where TResult : Node {
        var packedScene = _instance.LoadScene(scenePath);
        return _instance.InstantiateAndAddRoot<TResult>(packedScene, name);
    }

    public static Node Instantiate(string scenePath, string name = null) {
        var packedScene = _instance.LoadScene(scenePath);
        return _instance.InstantiateAndAddRoot<Node>(packedScene, name);
    }

    private PackedScene LoadScene(string scenePath) {
        var result = GD.Load<PackedScene>(scenePath);
        if (result is null) {
            GD.Print($"Missing scene: {scenePath}");
            return null;
        }
        return result;
    }

    public static TResult Instantiate<TResult>(PackedScene target, string name = null) where TResult : Node {
        return _instance.InstantiateAndAddRoot<TResult>(target, name);
    }

    private TResult InstantiateAndAddRoot<TResult>(PackedScene target, string name = null) where TResult : Node {
        var result = target.Instantiate<TResult>();
        GetTree().Root.AddChild(result);
        if (name is not null) result.Name = name;
        return result;
    }
}
