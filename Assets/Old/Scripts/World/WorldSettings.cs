using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CreateAssetMenu(fileName ="GameSettins", menuName = "MyGame/new GameSettings")]
public class WorldSettings : ScriptableObject
{
    [field:SerializeField] public int seed { get; private set; }//��������
    [field:SerializeField] public Vector2Int chunkSize { get; private set; }//�����С
    [field: SerializeField] public int chunkScale { get; private set; }//��������
    [HideInInspector] public Vector2Int worldSize { get; private set; }//ʵ�������С

    //��ͼ�߶ȣ�heightAddition + heightMulti * perlinnoise
    [field: SerializeField] public float heightAddition { get; private set; }//��׼�߶�
    [field: SerializeField] public float heightMulti { get; private set; }//�ɱ仯�߶�
    [field: SerializeField, Range(0, 1)]
    public float heightScale { get; private set; }//���θ߶Ȳ�����ֵ

    //��Ѩ��������
    [field: SerializeField, Range(0, 1)] public float caveThreshold { get; private set; }//��Ѩ��ֵ
    [field: SerializeField, Range(0, 1)] public float caveScale { get; private set; }//��Ѩ��Χ
    [field: SerializeField] public bool[,] cavePoints { get; private set; }//��Ƕ�Ѩ����λ��
    //ֲ������
    [field: SerializeField, Range(0, 1)] public float plantsThreshold { get; private set; }//ֲ����ֵ
    [field: SerializeField, Range(0, 1)] public float plantsFrequncy { get; private set; }//ֲ������Ƶ��
    //��ľ����
    [field: SerializeField, Range(0, 1)] public float treeThreshold { get; private set; }//��ľ��ֵ
    [field: SerializeField, Range(0, 1)] public float treeFrequncy { get; private set; }//��ľ����Ƶ��

    [field: SerializeField] public OreClass[] ores { get; private set; }//�����ɵĿ���
    [field: SerializeField] public Biome[] biomes { get; private set; }


    public void Init()
    {
        if (seed == 0) seed = Random.Range(-10000, 10000);
        Random.InitState(seed);
        worldSize = chunkSize * chunkScale;
        cavePoints = new bool[worldSize.x, worldSize.y];
    }

    //��Ƕ�Ѩ
    public void InitCaves()
    {
        for (int x = 0; x < worldSize.x; x++)
        {
            int height = GetHeight(x);
            for (int y = 0; y < height; y++)
            {
                float p = (float)y / height;//�߶�Խ�ߣ�ֵԽ��
                float v = Mathf.PerlinNoise((x + seed) * caveScale, (y + seed) * caveScale);
                v /= 0.5f + p;//�߶�Խ�ߣ�v��ֵ�ͻ�ԽС
                cavePoints[x, y] = v > caveThreshold;
            }
        }
    }

    public int GetHeight(int x)
    {
        return (int)(heightAddition + heightMulti * Mathf.PerlinNoise((x + seed) * heightScale, seed));
    }


}
