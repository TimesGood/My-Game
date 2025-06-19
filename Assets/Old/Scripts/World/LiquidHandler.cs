using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


//Һ����������
public class LiquidHandler : Singleton<LiquidHandler> {
    public WorldGeneration world;
    public bool openFlow = false;
    public float[,] liquidVolume { get; set; }//��¼Һ����Ƭ���������
    public Dictionary<LiquidClass, HashSet<Vector3Int>> updates = new Dictionary<LiquidClass, HashSet<Vector3Int>>();//�洢Ҫ����Һ����������ڲ�ͬҺ�������ٶȲ�ͬ����Ҫ�Բ�ͬҺ�嵥������
    private Dictionary<LiquidClass, Coroutine> updateRoutines = new Dictionary<LiquidClass, Coroutine>();
    private Dictionary<LiquidClass, Coroutine> backUpdateRoutines = new Dictionary<LiquidClass, Coroutine>();

    public void Init() {
        liquidVolume = new float[world.worldWidth, world.worldHeight];
    }

    // Update is called once per frame
    void Update() {
        if (openFlow) UpdateLiquid();
    }


    private void UpdateLiquid() {
        foreach (var key in updates.Keys) {
            HashSet<Vector3Int> value;
            updates.TryGetValue(key, out value);

            //�ɼ������Һ�����
            Coroutine updateRoutine;
            updateRoutines.TryGetValue(key, out updateRoutine);
            if (updateRoutine == null)
                updateRoutines.Add(key, StartCoroutine(HandlerVisibleIn(key, value)));

            //���ɼ������Һ�����
            if (!backUpdateRoutines.ContainsKey(key)) {
                backUpdateRoutines[key] = StartCoroutine(HandlerVisibleOut(key, value));
            }

        }

    }

    private IEnumerator HandlerVisibleIn(LiquidClass liquid, HashSet<Vector3Int> updates) {

        //ֻ������������ӷ�Χ�ڵ�����Һ��
        Bounds bounds = ChunkHandler.Instance.GetCameraBounds();
        //���ӷ�Χ���Һ���������
        List<Vector3Int> keys = new List<Vector3Int>(updates);
        foreach (var item in keys) {
            if (!bounds.Contains(item)) continue;
            ProcessLiquidCell(liquid, item);
        }

        Render(bounds, updates);

        yield return new WaitForSeconds(liquid.flowSpeed);
        updateRoutines.Remove(liquid);
    }


    //������ӷ�Χ���Һ��
    private IEnumerator HandlerVisibleOut(LiquidClass liquid, HashSet<Vector3Int> updates) {

        //ֻ������������ӷ�Χ�ڵ�����Һ��
        Bounds bounds = ChunkHandler.Instance.GetCameraBounds();
        //���ӷ�Χ���Һ���������
        List<Vector3Int> keys = new List<Vector3Int>(updates);
        foreach (var item in keys) {
            if (!world.CheckWorldBound(item.x, item.y)) continue;
            if (bounds.Contains(item)) continue;//ֻ������Ļ���
            ProcessLiquidCell(liquid, item);

            //�����ȶ���ˮ�Ƴ���ǣ���ʡ����
            float curVolume = liquidVolume[item.x, item.y];
            if (curVolume == 1f) {
                bool top = world.tileDatas[(int)Layers.Liquid, item.x, item.y + 1] != null;
                bool bottom = world.tileDatas[(int)Layers.Liquid, item.x, item.y - 1] != null;
                bool left = world.tileDatas[(int)Layers.Liquid, item.x - 1, item.y] != null;
                bool right = world.tileDatas[(int)Layers.Liquid, item.x + 1, item.y] != null;
                if (top && bottom && left && right) updates.Remove(item);
            }
            if (curVolume == 0f) {
                updates.Remove(item);
                world.tileDatas[(int)Layers.Liquid, item.x, item.y] = null;
            }

        }
        yield return null;
        backUpdateRoutines.Remove(liquid);
    }

    //��Ⱦ����
    private void Render(Bounds bounds, HashSet<Vector3Int> updates) {

        ////��Ⱦ���ӷ�Χ��Һ����Ƭ
        for (int y = (int)bounds.min.y; y < bounds.max.y; y++) {
            for (int x = (int)bounds.min.x; x < bounds.max.x; x++) {
                Vector3Int pos = new Vector3Int(x, y);
                TileBase tile = world.tilemaps[(int)Layers.Liquid].GetTile(pos);
                //����˴�ˮԴ�ȶ���������Ƭ��Ϊ�գ��˴�����Ҫ��Ⱦ������
                if (!updates.Contains(pos) && tile != null) continue;

                LiquidClass liquidClass = (LiquidClass)world.tileDatas[(int)Layers.Liquid, x, y];
                if (liquidClass != null) {
                    float volume = liquidVolume[x, y];
                    world.tilemaps[(int)Layers.Liquid].SetTile(pos, liquidClass.GetTile(volume));
                } else {
                    world.tilemaps[(int)Layers.Liquid].SetTile(pos, null);
                    updates.Remove(pos);
                }

            }
        }
    }

    // ���Ĵ����߼�
    private void ProcessLiquidCell(LiquidClass liquid, Vector3Int pos) {
        int x = pos.x;
        int y = pos.y;

        float curVolume = liquidVolume[x, y];
        //���̫Сʱ����������Ƭ
        if (curVolume < liquid.minVolume) {
            liquidVolume[x, y] = 0;
            world.tileDatas[(int)Layers.Liquid, x, y] = null;
            MarkForUpdate(liquid, x, y);
            return;
        }

        // ������������
        if (!TryFlowDown(pos, liquid, ref curVolume)) {
            // ��ɢ����
            DistributePressure(pos, liquid, curVolume);
        }

        //Һ�����
        curVolume = liquidVolume[x, y];
        float topVolume = liquidVolume[x, y + 1];
        if (curVolume > 1f) {
            liquidVolume[x, y] = 1f;
            liquidVolume[x, y + 1] = topVolume + curVolume - 1f;
            MarkForUpdate(liquid, x, y + 1);
            world.tileDatas[(int)Layers.Liquid, x, y + 1] = liquid;
        }
    }

    // �������������������Ƿ�ɹ�������
    private bool TryFlowDown(Vector3Int pos, LiquidClass liquid, ref float curVolume) {
        int x = pos.x;
        int y = pos.y;
        if (y <= 0) return false;


        // ����·��Ƿ������
        if (world.GetTileData(Layers.Ground, x, y - 1) != null) return false;
        float downVolume = liquidVolume[x, y - 1];
        if (downVolume >= 1f) return false;
        downVolume += curVolume;
        liquidVolume[x, y] = 0;
        liquidVolume[x, y - 1] = downVolume;
        MarkForUpdate(liquid, x, y + 1);
        MarkForUpdate(liquid, pos);
        MarkForUpdate(liquid, x, y - 1);
        world.tileDatas[(int)Layers.Liquid, x, y] = null;
        world.tileDatas[(int)Layers.Liquid, x, y - 1] = liquid;
        return true;
    }


    // ��ɢ����
    private void DistributePressure(Vector3Int pos, LiquidClass liquid, float curVolume) {
        int x = pos.x;
        int y = pos.y;
        List<Vector3Int> flowDirs = new List<Vector3Int>();

        // ��������������
        CheckFlowDirection(x - 1, y, curVolume, ref flowDirs); // ��
        CheckFlowDirection(x + 1, y, curVolume, ref flowDirs); // ��
        if (flowDirs.Count == 0) return;
        // ����ÿ������ķ�����
        float avg = curVolume;
        foreach (var item in flowDirs) {
            avg += liquidVolume[item.x, item.y];
        }
        avg /= (flowDirs.Count + 1);

        liquidVolume[x, y] = avg;
        MarkForUpdate(liquid, pos);
        foreach (var dir in flowDirs) {
            liquidVolume[dir.x, dir.y] = avg;
            MarkForUpdate(liquid, dir);
            world.tileDatas[(int)Layers.Liquid, dir.x, dir.y] = liquid;
        }
    }

    // ������������Ƿ���Ч
    private void CheckFlowDirection(int x, int y, float curVolume, ref List<Vector3Int> dirs) {
        if (!world.CheckWorldBound(x, y)) return;
        if (world.GetTileData(Layers.Ground, x, y) != null || liquidVolume[x, y] >= curVolume) return;
        dirs.Add(new Vector3Int(x, y));
    }

    public void MarkForUpdate(LiquidClass liquid, int x, int y) {
        Vector3Int pos = new Vector3Int(x, y);
        if (liquid == null) Debug.Log(1);
        MarkForUpdate(liquid, pos);
    }

    // �����Ҫ���µ�����
    public void MarkForUpdate(LiquidClass liquid, Vector3Int pos) {
        HashSet<Vector3Int> target = null;
        if (!updates.ContainsKey(liquid)) {
            target = new HashSet<Vector3Int>();
            updates.Add(liquid, target);
        } else {
            updates.TryGetValue(liquid, out target);
        }

        if (!target.Contains(pos)) {
            target.Add(pos);
        }
    }
}
