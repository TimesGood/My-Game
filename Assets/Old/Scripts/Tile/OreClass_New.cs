using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//������Ƭ�������ã�Ԥ������
[System.Serializable]
public class OreClass_New
{
    public Texture2D spreadTexture;
    public TileClass oreClass;
    [field: SerializeField, Range(0, 1)] public float oreRarity { get; private set; }//ϡ�ж�
    [field: SerializeField, Range(0, 1)] public float oreRadius { get; private set; }//���ɴ�С
    [field: SerializeField] public float minY { get; private set; }//��С��������
    [field: SerializeField] public float maxY { get; private set; }//�����������
    [field: SerializeField] public float offset { get; private set; }//ƫ�ƣ���ͬƫ��ʹ�������ɵķ���ŵ���ͬ�ط���
}
