using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEditor.Progress;

//������Ƭ
[CreateAssetMenu(fileName = "TileClass", menuName = "Tile/new TileClass")]
public class TileClass : ScriptableObject
{
    public TileBase tile;//tile
    public Layers layer;//��������ͼ��
    public int blockId;//����Id
    public bool isIlluminated;//�Ƿ��Է���
    public float lightLevel;//����ǿ��
    public Color lightColor;//������ɫ


    //������ƬId
    private void OnValidate() {
#if UNITY_EDITOR
        //string path = AssetDatabase.GetAssetPath(this);
        //blockId = AssetDatabase.AssetPathToGUID(path);
        if (tile != null && tile is CustomTile) ((CustomTile)tile).blockId = blockId;
#endif
    }
}
