using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//��Ƭ�ϼ�
[CreateAssetMenu(fileName = "TileAtlas", menuName = "Tile/new TileAtlas")]
public class TileAtlas : ScriptableObject
{
    //���λ���
    [field: SerializeField] public TileClass grassBlock { get; private set; }//���
    [field: SerializeField] public TileClass dirtBlock { get; private set; }//����
    [field: SerializeField] public TileClass dirtWall { get; private set; }//����ǽ��
    [field: SerializeField] public TileClass stoneBlock { get; private set; }//�Ҳ�
    [field: SerializeField] public TileClass stoneWall { get; private set; }//�Ҳ�ǽ��
    [field: SerializeField] public TileClass plants { get; private set; }//ֲ��
    [field: SerializeField] public TileClass tree { get; private set; }//��ľ
    [field: SerializeField] public TileClass leaf { get; private set; }//���

    //��ʯ
    [field: SerializeField] public OreClass coal { get; private set; }
    [field: SerializeField] public OreClass iron { get; private set; }
    [field: SerializeField] public OreClass gold { get; private set; }
    [field: SerializeField] public OreClass diamond { get; private set; }
}
