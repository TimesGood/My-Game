using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ��ͼԪ����
[System.Serializable]
public class MapMetadata {
    public int seed; // ��ͼ����
    public Vector2Int mapSize;  // ��ͼ�ߴ�
    public int chunkSize = 32; // ����ߴ�
    public string savePath; // �洢·��
    public DateTime creationTime; //����ʱ��
}
