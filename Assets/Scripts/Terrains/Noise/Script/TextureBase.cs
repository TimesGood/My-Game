using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Cinemachine.NoiseSettings;

public abstract class TextureBase : ScriptableObject {
    public int width;
    public int height;
    public int seed;

    public abstract Texture2D Generator();
}
