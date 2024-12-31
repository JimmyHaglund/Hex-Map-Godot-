using Godot;

namespace JHM.MeshBasics;

public sealed partial class Texture2DArrayWizard : Node {
    [Export] public Texture2D[] Images { get; set; }

    public override void _Ready() {
        var textureArray = new Texture2DArray();
        var images = new Image[Images.Length];
        for (var n = 0; n < images.Length; n++) {
            images[n] = Images[n].GetImage();
        }
        textureArray.CreateFromImages(new(images));

        ResourceSaver.Save(textureArray, "res://misc/TerrainTexture2DArray.tres");
    }
}
