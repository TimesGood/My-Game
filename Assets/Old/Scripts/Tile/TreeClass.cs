using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

//��
[CreateAssetMenu(fileName = "TreeClass", menuName = "Tile/new TreeClass")]
public class TreeClass : TileClass
{
    public int maxHeight;//�������
    public int minHeight;//��С����
    public int treeWidth;//����
    public TileClass leaf;//����
    public float frequency = 0.04f;//���������ܶ�
    public float threshold = 0.6f;//��������ϡ�ж�
    public float prob = 0.7f;//���ɸ���
    public bool isSurface = false;//�����ڵر�
    public PerlinNoise noise;//��ͼ

    #region �༭��
    [Header("Grid Settings")]
    public int gridWidth = 15; // �����ȣ�������
    public int gridHeight = 15; // ����߶ȣ�������
    [Header("Tree Settings")]
    public Vector2Int originPoint = new Vector2Int(2, 2); // ԭ��λ�ã�����λ�ã�

    [HideInInspector]
    public bool[] clearMap; // �������ӳ��

    public void InitializeGrid() {
        clearMap = new bool[gridWidth * gridHeight];
        originPoint = new Vector2Int(gridWidth / 2, gridHeight / 2);
    }

    public bool ShouldClear(int x, int y) {
        int index = y * gridWidth + x;
        if (index >= 0 && index < clearMap.Length) {
            return clearMap[index];
        }
        return false;
    }

    #endregion

    //��������ʱ�������Լ���ʱ����Ҫ�����Χ��Ƭ

    public void PlanceSelf(int x, int y) {

        int h = Random.Range(minHeight, maxHeight);//����
        int maxBranches = Random.Range(3, 10);//���
        int bCounts = 0;//��込���
        //��������
        if (leaf == null) {
            //������Χ��Ƭ
            for (int gridX = 0; gridX < gridWidth; gridX++) {
                for (int gridY = 0; gridY < gridHeight; gridY++) {
                    if (ShouldClear(gridX, gridY)) {
                        //ת����ռ�
                        int worldX = gridX - originPoint.x + x;
                        int worldY = gridY - originPoint.y + y;
                        TileClass isAddon = WorldGeneration.Instance.GetTileData(Layers.Addons, worldX, worldY + 1);
                        if (isAddon != null) WorldGeneration.Instance.SetTileData(null, Layers.Addons, worldX, worldY + 1);
                        WorldGeneration.Instance.SetTileData(null, Layers.Ground, worldX, worldY);
                    }
                }
            }

            WorldGeneration.Instance.SetTileData(this, this.layer, x, y);
            return;
        }
        //�����
        for (int ny = y; ny < y + h; ny++) {
            WorldGeneration.Instance.SetTileData(this, this.layer, x, ny);
            //������׮
            if (ny == y) {
                //�����׮
                if (Random.Range(0, 100) < 30) {
                    if (x > 0 && WorldGeneration.Instance.GetTileData(Layers.Ground, x - 1, ny - 1) != null && WorldGeneration.Instance.GetTileData(Layers.Ground, x - 1, ny) == null) {
                        WorldGeneration.Instance.SetTileData(this, this.layer, x - 1, ny);
                    }
                }
                //�Ҳ���׮
                if (Random.Range(0, 100) < 30) {
                    if (WorldGeneration.Instance.GetTileData(Layers.Ground, x + 1, ny - 1) != null && WorldGeneration.Instance.GetTileData(Layers.Ground, x + 1, ny) == null) {
                        WorldGeneration.Instance.SetTileData(this, this.layer, x + 1, ny);
                    }
                }

            }
            //�������
            else if (ny >= y + 2 && ny <= y + h - 3) {
                if (bCounts < maxBranches && Random.Range(0, 100) < 40) {
                    if (x > 0 && WorldGeneration.Instance.GetTileData(Layers.Ground, x - 1, ny) == null && WorldGeneration.Instance.GetTileData(Layers.Addons, x - 1, ny - 1) != this) {
                        WorldGeneration.Instance.SetTileData(leaf, leaf.layer, x - 1, ny);
                        bCounts++;
                    }
                }
                if (bCounts < maxBranches && Random.Range(0, 100) < 40) {
                    if (WorldGeneration.Instance.GetTileData(Layers.Ground, x + 1, ny) == null && WorldGeneration.Instance.GetTileData(Layers.Addons, x + 1, ny - 1) != this) {
                        WorldGeneration.Instance.SetTileData(leaf, leaf.layer, x + 1, ny);
                        bCounts++;
                    }
                }
            }
        }
    }


}
