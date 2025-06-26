using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEditor.Progress;

//������Ƭ
[CreateAssetMenu(fileName = "TileClass", menuName = "Tile/new TileClass")]
public class TileClass : ScriptableObject
{
    public CustomTile tile;//tile
    public Layers layer;//��������ͼ��
    public long blockId;//����Id
    public bool isIlluminated;//�Ƿ��Է���
    public float lightLevel;//����ǿ��
    public Color lightColor;//������ɫ

    
    private void OnValidate() {
        //TileBase��TileClass��Id�����Ӧ
        if (tile != null && tile.blockId != blockId) {
            tile.blockId = blockId;
        }
    }


    //����ʱִ��
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

    // �ȶ��� 64 λ��ϣ�㷨
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
