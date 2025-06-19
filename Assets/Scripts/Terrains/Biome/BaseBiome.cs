using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

//Ⱥ�����
public abstract class BaseBiome : ScriptableObject
{
    protected WorldGeneration world => WorldGeneration.Instance;
    public int biomeWidth;//Ⱥ���ֻ�����Ⱥ�����ɵ�����ߣ�ʵ������������Ϊ׼��
    public int biomeHeight;//Ⱥ���
    private Vector2Int worldPosition;//Ⱥ������λ��
    protected Vector2Int generatePos;//����������ʼ��
    private Vector2Int curBiomePos = new Vector2Int(0, 0);//Ⱥ�䱾��λ��

    //Ⱥ����ͼ����
    [field: SerializeField] public ShapeGenerator outLine { get; private set; }//Ⱥ������

    //��ʼ��Ⱥ��
    public virtual void InitBiome(Vector2Int worldPosition, int seed) {
        this.worldPosition = worldPosition;
        generatePos = new Vector2Int(worldPosition.x - biomeWidth / 2, worldPosition.y - biomeHeight / 2);

        InitNoise(seed);
    }

    //��ʼ����ͼ����
    public virtual void InitNoise(int seed) {
        //������ͼ����
        outLine.InitValidate(biomeWidth, biomeHeight, seed);
        outLine.InitNoise();

    }

    //��������λ�û�ȡ��Ӧ��ͼλ��
    public int GetLocalPositionX(int x) {
        int biomeX = x - (worldPosition.x - biomeWidth / 2);
        return biomeX;

    }
    //��������λ�û�ȡ��Ӧ��ͼλ��
    public int GetLocalPositionY(int y) {
        int biomeY = y - (worldPosition.y - biomeHeight / 2);
        return biomeY;

    }

    //��������λ�û�ȡ��Ӧ��ͼλ��
    public Vector2Int GetLocalPosition(int x, int y) {
        curBiomePos.x = GetLocalPositionX(x);
        curBiomePos.y = GetLocalPositionY(y);
        return curBiomePos;

    }


    public abstract IEnumerator GenerateBiome();

}
