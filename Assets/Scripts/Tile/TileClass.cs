using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEditor.Progress;

//基础瓦片
[CreateAssetMenu(fileName = "TileClass", menuName = "Tile/new TileClass")]
public class TileClass : ScriptableObject
{
    public CustomTile tile;//tile
    public Layers layer;//方块所属图层
    public long blockId;//方块Id
    public bool isIlluminated;//是否自发光
    public float lightLevel;//发光强度
    public Color lightColor;//发光颜色

    
    private void OnValidate() {
        //TileBase与TileClass的Id必须对应
        if (tile != null && tile.blockId != blockId) {
            tile.blockId = blockId;
        }
    }


    //创建时执行
    protected virtual void OnEnable() {
#if UNITY_EDITOR
        if (blockId == 0) {
            RegenerateID();
        }
#endif
    }

#if UNITY_EDITOR
    [ContextMenu("Regenerate ID")]
    private void RegenerateID() {
        string path = AssetDatabase.GetAssetPath(this);
        string guid = AssetDatabase.AssetPathToGUID(path);
        //string guid = System.Guid.NewGuid().ToString();
        blockId = CalculateStableHash(guid);
        if(tile != null)
            tile.blockId = blockId;
        EditorUtility.SetDirty(this);
    }
#endif

    // 稳定的 64 位哈希算法
    private static long CalculateStableHash(string input) {
        unchecked {
            long hash = 0;
            foreach (char c in input) {
                hash = (hash * 31) + c;
                hash = hash ^ (hash >> 32);
            }
            return hash;
        }
    }
}
