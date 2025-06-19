using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//����Ⱥ��
[System.Serializable]
public class Biome_New
{
    public Color biomeColor;

    //����
    public float terrainFreq = 0.05f;//����Ƶ�ʣ�ɽ��Ƶ�ʣ�ֵԽ��Խ�ܼ���
    public float heightMulti = 40f;//����߶�
    public int heightAddition = 25;//̧��һ��
    public float dirtFreq = 0.2f;
    public float dirtMulti = 20f;
    public int dirtAddition = 10;//����̧��

    //��Ƭ�ϼ�
    public TileAtlas tileAtlas;

    //��Ѩ
    public Texture2D caveNoiseTexture;//��Ѩ����Ԥ��
    public float caveFreq = 0.05f;//����Ƶ�ʣ�ֵԽ��Խ�ܼ���
    public float caveThreshold = 0.25f;//��Ѩ��ֵ
    public bool isGenerateCaves = false;//�Ƿ����ɶ�Ѩ

    //��ľ
    public float treeFreq;//��ľ����Ƶ��
    public float treeThreshold;//��ľ��ֵ
    public int minTreeHeight;//��С��
    public int maxTreeHeight;//�����

    //ֲ��
    public float plantsFreq;
    public float plantsThreshold;

    //��ʯ
    public OreClass_New[] ores;
}
