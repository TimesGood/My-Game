using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

//��Դ����
public class LightHandler : Singleton<LightHandler>
{
    public WorldGeneration world;
    public float[,] lightValues;//��¼������Ƭ��Ƭ����������
    public readonly float sunlight = 15f;//̫������
    public Texture2D lightTex;//���ղ���
    public Material lightMap;
    public bool updating;//�Ƿ����ڸ��¹�Դ
    public Queue<Vector2Int> updates = new Queue<Vector2Int>();//�洢Ҫ���µĹ�Դ������

    protected override void Awake() {
        base.Awake();
        lightValues = new float[world.worldWidth, world.worldHeight];

        lightTex = new Texture2D(world.worldWidth, world.worldHeight);
        lightTex.filterMode = FilterMode.Point;

        //�Ŵ󵽸��ǵ�ͼ��С
        transform.localScale = new Vector3(world.worldWidth, world.worldHeight, 1);
        //λ��
        transform.localPosition = new Vector3(world.worldWidth / 2f, world.worldHeight / 2f, 0);

    }
    private void Update() {
        if (!updating && updates.Count > 0) {
            updating = true;
            StartCoroutine(LightUpdate(updates.Dequeue()));
        }
    }

    public void InitLight()
    {
        StartCoroutine(UpdateScopeLight(0, world.worldWidth - 1, 0, world.worldHeight - 1));
    }

    IEnumerator LightUpdate(Vector2Int pos) {
        //���¸ù�Դһ����Χ�Ĺ���
        int px1 = Mathf.Clamp(pos.x - (int)sunlight, 0, world.worldWidth - 1);//����
        int px2 = Mathf.Clamp(pos.x + (int)sunlight, 0, world.worldWidth - 1);//����
        int py1 = Mathf.Clamp(pos.y - (int)sunlight, 0, world.worldHeight - 1);//����
        int py2 = Mathf.Clamp(pos.y + (int)sunlight, 0, world.worldHeight - 1);//����

        for (int x = px1; x <= px2; x++) {
            for (int y = py1; y <= py2; y++) {
                lightValues[x, y] = 0;
            }
        }

        UpdateScopeLight(px1, px2, py1, py2);
        yield return null;
        updating = false;
    }

    //����ָ����Χ�ڵĹ�Դ
    private IEnumerator UpdateScopeLight(int minX, int maxX, int minY, int maxY)
    {
        Debug.Log("��Ⱦ��1...");
        int processed = 0;
        //���½ǿ�ʼ��Ⱦ����
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                float lightValue = 0f;
                if (world.GetLightValue(x, y) != 0)
                {
                    lightValue = world.GetLightValue(x, y);
                }
                else if (world.GetTileClass(Layers.Background, x, y) == null && world.GetTileClass(Layers.Ground, x, y) == null)
                {
                    lightValue = sunlight;
                }
                else
                {
                    //��ȡ���ܹ���ֵ
                    int nx1 = Mathf.Clamp(x - 1, 0, world.worldWidth - 1);
                    int nx2 = Mathf.Clamp(x + 1, 0, world.worldWidth - 1);
                    int ny1 = Mathf.Clamp(y - 1, 0, world.worldHeight - 1);
                    int ny2 = Mathf.Clamp(y + 1, 0, world.worldHeight - 1);
                    //ȡ������һ��
                    lightValue = Mathf.Max(lightValues[x, ny1], lightValues[x, ny2], lightValues[nx1, y], lightValues[nx2, y]);

                    //�������
                    if (world.GetTileClass(Layers.Ground, x, y) == null)
                    {
                        lightValue -= 1f;
                    }
                    else
                    {
                        lightValue -= 2.5f;
                    }
                }
                lightValue = Mathf.Clamp(lightValue, 0, sunlight);
                lightValues[x, y] = lightValue;

                //ÿ֡������������ֹ����
                if (++processed % 10000 == 0) {
                    Debug.Log("1");
                    yield return null;
                }
            }
        }
        Debug.Log("��Ⱦ��2...");
        //���Ͻǿ�ʼ��Ⱦ����
        for (int x = maxX; x >= minX; x--)
        {
            for (int y = maxY; y >= minY; y--)
            {
                float lightValue = 0f;
                if (world.GetLightValue(x, y) != 0)
                {
                    lightValue = world.GetLightValue(x, y);
                }
                else if (world.GetTileClass(Layers.Background, x, y) == null && world.GetTileClass(Layers.Ground, x, y) == null)
                {
                    lightValue = sunlight;
                }
                else
                {
                    //��ȡ���ܹ���ֵ
                    int nx1 = Mathf.Clamp(x - 1, 0, world.worldWidth - 1);
                    int nx2 = Mathf.Clamp(x + 1, 0, world.worldWidth - 1);
                    int ny1 = Mathf.Clamp(y - 1, 0, world.worldHeight - 1);
                    int ny2 = Mathf.Clamp(y + 1, 0, world.worldHeight - 1);
                    //ȡ������һ��
                    lightValue = Mathf.Max(lightValues[x, ny1], lightValues[x, ny2], lightValues[nx1, y], lightValues[nx2, y]);

                    //�������
                    if (world.GetTileClass(Layers.Ground, x, y) == null)
                    {
                        lightValue -= 1f;
                    }
                    else
                    {
                        lightValue -= 2.5f;
                    }
                }
                lightValue = Mathf.Clamp(lightValue, 0, sunlight);
                lightValues[x, y] = lightValue;

                //ÿ֡������������ֹ����
                if (++processed % 10000 == 0) {
                    yield return null;
                }
            }
        }

        Debug.Log("Ӧ�ù���");

        //Ӧ������
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                lightTex.SetPixel(x, y, new Color(0, 0, 0, 1f - lightValues[x, y] / sunlight));
                //ÿ֡������������ֹ����
                if (++processed % 5000 == 0) {
                    yield return null;
                }
            }
        }
        
        lightTex.Apply();
        lightMap.SetTexture("_LightMap", lightTex);
    }


    //�����Ҫ���¹�Դ�ĵط�
    public void MarForUpdate(int x, int y)
    {
        updates.Enqueue(new Vector2Int(x, y));
    }

}
