using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEditor.Progress;

//基础瓦片
[CreateAssetMenu(fileName = "TileClass", menuName = "Tile/new TileClass")]
public class TileClass : ScriptableObject
{
    public TileBase tile;//tile
    public Layers layer;//方块所属图层
    public int blockId;//方块Id
    public bool isIlluminated;//是否自发光
    public float lightLevel;//发光强度
    public Color lightColor;//发光颜色


    //生成瓦片Id
    private void OnValidate() {
#if UNITY_EDITOR
        //string path = AssetDatabase.GetAssetPath(this);
        //blockId = AssetDatabase.AssetPathToGUID(path);
        if (tile != null && tile is CustomTile) ((CustomTile)tile).blockId = blockId;
#endif
    }
}
