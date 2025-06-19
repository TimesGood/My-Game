using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


//生成规则
public abstract class GenerateRule
{
    public Texture2D spreadTexture;
    public float frequency;
    public float threshold;
    public float multiply = 40f;
    public int addition = 25;
    public TileBase tile;
    


}
