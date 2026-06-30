using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Shape", menuName = "Generator Texture/new Shape")]
public class ShapePreview : TextureBase {
    public ShapeParams shapeParams;


    public override Texture2D Generator() {
        return ShapeSampler.GenerateTexture(width, height, shapeParams, seed);
    }
}
