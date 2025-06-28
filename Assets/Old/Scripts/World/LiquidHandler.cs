using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using static ChunkHandler;
using static UnityEditor.PlayerSettings;


//Һ����������
public class LiquidHandler : Singleton<LiquidHandler> {

    public WorldGeneration world;
    public LiquidClass[] liquids;//ע����Ҫ�����Һ��
    public bool openFlow = false;
    public float[,] liquidVolume { get; set; }//��¼Һ����Ƭ���������
    public Dictionary<LiquidClass, Dictionary<Vector2Int, int>> updates = new Dictionary<LiquidClass, Dictionary<Vector2Int, int>>();//�洢Ҫ����Һ����������ڲ�ͬҺ�������ٶȲ�ͬ����Ҫ�Բ�ͬҺ�嵥������
    private Dictionary<LiquidClass, Coroutine> updateRoutines = new Dictionary<LiquidClass, Coroutine>();
    private Dictionary<LiquidClass, Coroutine> backUpdateRoutines = new Dictionary<LiquidClass, Coroutine>();

    // ����ȶ���ˮԴ�Ƴ�����
    private float lastCheckUpdateTime;
    private const float checkUpdateInterval = 1f; // ���¼��

    protected override void Awake() {
        base.Awake();
        //��ʼ��Һ��洢
        liquidVolume = new float[world.worldWidth, world.worldHeight];
        //��ʼ��ע��Һ���ֵ�
        foreach (var liquid in liquids) {
            updates.Add(liquid, new Dictionary<Vector2Int, int>());
        }

    }

    // Update is called once per frame
    void Update() {
        UpdateLiquid();
    }

    private void LateUpdate() {
        Bounds bounds = ChunkHandler.Instance.GetCameraBounds();
        foreach (var kvp in updates) {
            Render(bounds, kvp.Value);
        }
    }

    private void UpdateLiquid() {
        // ÿ֡��ദ��2��Һ��
        if (!openFlow) return;

        // ÿ֡��ദ��2��Һ��
        int processed = 0;
        foreach (var kvp in updates) {
            if (updateRoutines.ContainsKey(kvp.Key) || processed >= 2)
                continue;

            processed++;
            updateRoutines[kvp.Key] = StartCoroutine(
                HandleLiquidUpdate(kvp.Key, kvp.Value)
            );
            ScanClearSteadyLiquid(kvp.Value);
        }


    }
    private IEnumerator HandleLiquidUpdate(LiquidClass liquid, Dictionary<Vector2Int, int> updates) {
        Bounds bounds = ChunkHandler.Instance.GetCameraBounds();
        List<Vector2Int> keys = new List<Vector2Int>(updates.Keys);
        int processed = 0;

        // ������Ļ������
        foreach (var item in keys) {
            if (!world.CheckWorldBound(item.x, item.y) || bounds.Contains((Vector3Int)item)) continue;
            float curVolume = liquidVolume[item.x, item.y];
            float oldVolume = curVolume;
            ProcessLiquidCell(liquid, item, ref curVolume);
            if (!updates.ContainsKey(item)) continue;
            if (curVolume == oldVolume) {
                updates[item] += 1;
            } else {
                updates[item] = 0;
            }

            if (++processed % 1000 == 0) yield return null;
        }

        // �ȴ��������
        yield return new WaitForSeconds(liquid.flowSpeed);


        processed = 0;
        keys = new List<Vector2Int>(updates.Keys); // ��ȡ���¼���
        //�����ټ��㣬ʹ�����ڵ�Һ��������Ȼ
        keys.Sort((a, b) => { return a.y.CompareTo(b.y); });

        // ������Ļ������
        foreach (var item in keys) {
            if (!bounds.Contains(((Vector3Int)item))) continue;
            float curVolume = liquidVolume[item.x, item.y];
            float oldVolume = curVolume;
            ProcessLiquidCell(liquid, item, ref curVolume);
            //��¼���Һ������ޱ仯��������+1���������
            if (!updates.ContainsKey(item)) continue;
            if (curVolume == oldVolume) {
                updates[item] += 1;
            } else {
                updates[item] = 0;
            }

            if (++processed % 1000 == 0) yield return null;
        }

        updateRoutines.Remove(liquid);
    }

    //�ڶ���������֧����Ļǰ����Ļ��ͬ��Һ�崦���ٶȡ����ϣ���ӿ�Һ������Ļ��Ĵ����ٶȣ�ʹ�ø÷���
    //private void UpdateLiquid() {
    //    Bounds bounds = ChunkHandler.Instance.GetCameraBounds();
    //    foreach (var kvp in updates) {
    //        List<Vector2Int> outUpdates = new List<Vector2Int>();
    //        List<Vector2Int> inUpdates = new List<Vector2Int>();
    //        foreach (var inKvp in kvp.Value) {

    //            if (bounds.Contains((Vector3Int)inKvp.Key)) {
    //                inUpdates.Add(inKvp.Key);
    //            } else {
    //                outUpdates.Add(inKvp.Key);
    //            }
    //        }
    //        //�ɼ������Һ�����
    //        Coroutine updateRoutine;
    //        updateRoutines.TryGetValue(kvp.Key, out updateRoutine);
    //        if (updateRoutine == null)
    //            updateRoutines[kvp.Key] = StartCoroutine(HandlerVisibleIn(bounds, kvp.Key, inUpdates, kvp.Value));

    //        //���ɼ������Һ�����
    //        Coroutine backUpdateRoutine;
    //        backUpdateRoutines.TryGetValue(kvp.Key, out backUpdateRoutine);
    //        if (backUpdateRoutine == null) {
    //            backUpdateRoutines[kvp.Key] = StartCoroutine(HandlerVisibleOut(bounds, kvp.Key, outUpdates, kvp.Value));

    //        }

    //        ScanClearSteadyLiquid(kvp.Value);
    //    }

    //}

    //private IEnumerator HandlerVisibleIn(Bounds bounds, LiquidClass liquid, List<Vector2Int> inUpdate, Dictionary<Vector2Int, int> updates) {
    //    yield return new WaitForSeconds(liquid.flowSpeed);
    //    
    //    int processed = 0;
    //    //�����ڼ��㣬����ˮ����Ȼһ��
    //    inUpdate.Sort((a, b) => {
    //        return a.y.CompareTo(b.y);
    //    });

    //    foreach (var item in inUpdate) {
    //        float curVolume = liquidVolume[item.x, item.y];
    //        float oldVolume = curVolume;
    //        ProcessLiquidCell(liquid, item, ref curVolume);
    //        if (!updates.ContainsKey(item)) continue;
    //        if (curVolume == oldVolume) {
    //            updates[item] += 1;
    //        } else {
    //            updates[item] = 0;
    //        }
    //        // ÿ֡����1000����Ƭ��ֹ����
    //        if (++processed % 1000 == 0) {
    //            yield return null;
    //        }
    //    }
    //    updateRoutines.Remove(liquid);
    //}


    ////������ӷ�Χ���Һ��
    //private IEnumerator HandlerVisibleOut(Bounds bounds, LiquidClass liquid, List<Vector2Int> outUpdates, Dictionary<Vector2Int, int> updates) {


    //    int processed = 0;
    //    //���ӷ�Χ���Һ���������
    //    foreach (var item in outUpdates) {
    //        if (!world.CheckWorldBound(item.x, item.y)) continue;
    //        if (bounds.Contains((Vector3Int)item)) continue;//ֻ������Ļ���
    //        float curVolume = liquidVolume[item.x, item.y];
    //        float oldVolume = curVolume;
    //        ProcessLiquidCell(liquid, item, ref curVolume);
    //        if (!updates.ContainsKey(item)) continue;
    //        if (curVolume == oldVolume) {
    //            updates[item] += 1;
    //        } else {
    //            updates[item] = 0;
    //        }

    //        // ÿ֡����1000����Ƭ��ֹ����
    //        if (++processed % 1000 == 0) {
    //            yield return null;
    //        }

    //    }

    //    backUpdateRoutines.Remove(liquid);
    //}

    //ɨ�������ȶ�״̬Һ��
    private void ScanClearSteadyLiquid(Dictionary<Vector2Int, int> updates) {
        // ʹ�ù̶�������£�����ÿ֡�����
        if (Time.time - lastCheckUpdateTime > checkUpdateInterval) {
            //��飬���Һ�岻���������һ���������ж���Һ�崦���ȶ�״̬
            foreach (var key in updates.ToList()) {
                updates.TryGetValue(key.Key, out int num);
                if (num > 5) {
                    updates.Remove(key.Key);
                }
            }
            Debug.Log(updates.Count);
            lastCheckUpdateTime = Time.time;
        }

    }

    //��Ⱦ����
    private void Render(Bounds bounds, Dictionary<Vector2Int, int> updates) {
        List<Vector2Int> toRemove = new List<Vector2Int>();
        Tilemap liquidMap = world.tilemaps[(int)Layers.Liquid];

        //��Ⱦ���ӷ�Χ��Һ����Ƭ
        for (int y = (int)bounds.min.y; y < bounds.max.y; y++) {
            for (int x = (int)bounds.min.x; x < bounds.max.x; x++) {
                Vector3Int worldPos = new Vector3Int(x, y);

                LiquidClass liquidClass = (LiquidClass)world.GetTileClass(Layers.Liquid, x, y);
                TileBase oldTile = world.tilemaps[(int)Layers.Liquid].GetTile(worldPos);
                if (liquidClass != null) {

                    float volume = liquidVolume[x, y];
                    //��ʱ�����ڴ���Һ���ڿ��е��¸���Һ���޷���Һ���˶���������Ⱦʱ���һ���Ƿ����쳣����Һ�壬���¼����Һ��
                    Vector3Int downPos = worldPos + Vector3Int.down;
                    TileClass downGroundClass = world.GetTileClass(Layers.Ground, downPos.x, downPos.y);
                    TileClass downLiquidClass = world.GetTileClass(Layers.Liquid, downPos.x, downPos.y);
                    if (!updates.ContainsKey((Vector2Int)worldPos) && downGroundClass == null && downLiquidClass == null)
                        MarkForUpdate(liquidClass, (Vector2Int)worldPos);

                    //�����Ⱦǰ�¾�Һ����Ƭһ�£�����Ҫ�ٴ���Ⱦ����
                    TileBase newTile = liquidClass.GetTileToVolume(volume);
                    if (newTile == oldTile) continue;
                    world.tilemaps[(int)Layers.Liquid].SetTile(worldPos, newTile);
                } else {
                    if (oldTile == null) continue;
                    world.tilemaps[(int)Layers.Liquid].SetTile(worldPos, null);
                }
            }
        }
    }

    // ���Ĵ����߼�
    private void ProcessLiquidCell(LiquidClass liquid, Vector2Int pos, ref float curVolume) {
        int x = pos.x;
        int y = pos.y;

        //���̫Сʱ����������Ƭ
        if (curVolume < liquid.minVolume) {
            UpdateVolume(null, pos, 0);
            MarkForUpdate(liquid, pos);
            return;
        }
        //Һ���ڵ�����Ƭ�У�����
        if (world.GetTileClass(Layers.Ground, x, y) != null) {
            UpdateVolume(null, pos, 0);
            MarkForUpdate(liquid, pos);
            return;
        }
        // ������������
        if (TryFlowDown(liquid, pos, ref curVolume)) return;
        // ��ɢ����
        if (HandlerDiffusion(liquid, pos, ref curVolume)) return;


        //Һ�����
        if (HandlerOverflow(liquid, pos, curVolume)) curVolume = 1f; ;


    }

    // �������������������Ƿ�ɹ�������
    private bool TryFlowDown(LiquidClass liquid, Vector2Int pos, ref float curVolume) {
        int x = pos.x;
        int y = pos.y;
        if (y <= 0) return false;

        Vector2Int downPos = pos + Vector2Int.down;
        // ����·��Ƿ������
        if (world.GetTileClass(Layers.Ground, downPos.x, downPos.y) != null) return false;
        //Һ������
        float downVolume = liquidVolume[downPos.x, downPos.y];
        if (downVolume >= 1f) return false;
        //Һ�岻һ��
        LiquidClass downLiquid = world.GetTileClass(Layers.Liquid, downPos.x, downPos.y) as LiquidClass;
        if (downLiquid != null && downLiquid != liquid)
            return false;
        downVolume += curVolume;
        curVolume = 0;

        UpdateVolume(liquid, pos, curVolume);
        MarkForUpdate(liquid, pos);

        UpdateVolume(liquid, downPos, downVolume);
        MarkForUpdate(liquid, downPos);

        //������Χ���ȶ�״̬Һ�壬���¼���������Һ��Һ��
        MarkForUpdate(liquid, pos + Vector2Int.up);
        //MarkForUpdate(liquid, pos + Vector2Int.left);
        //MarkForUpdate(liquid, pos + Vector2Int.right);
        return true;
    }


    // ��ɢ����
    private bool HandlerDiffusion(LiquidClass liquid, Vector2Int pos, ref float curVolume) {
        int x = pos.x;
        int y = pos.y;
        List<Vector2Int> flowDirs = new List<Vector2Int>();

        // ��������������
        CheckFlowDirection(x - 1, y, liquid, curVolume, ref flowDirs); // ��
        CheckFlowDirection(x + 1, y, liquid, curVolume, ref flowDirs); // ��
        if (flowDirs.Count == 0) return false;
        // ����ÿ������ķ�����
        float avg = curVolume;
        foreach (var item in flowDirs) {
            avg += liquidVolume[item.x, item.y];
        }
        avg /= (flowDirs.Count + 1);

        //avg = Mathf.Round(avg * 10000f) / 10000f;
        curVolume = avg;
        UpdateVolume(liquid, pos, curVolume);
        MarkForUpdate(liquid, pos);
        if(HandlerOverflow(liquid, pos, curVolume)) curVolume = 1f;
        foreach (var dir in flowDirs) {
            UpdateVolume(liquid, dir, avg);
            MarkForUpdate(liquid, dir);
            HandlerOverflow(liquid, dir, avg);
        }

        //������Χ���ȶ�״̬Һ�壬���¼���������Һ��Һ��
        MarkForUpdate(liquid, pos + Vector2Int.up);
        MarkForUpdate(liquid, pos + Vector2Int.left);
        MarkForUpdate(liquid, pos + Vector2Int.right);
        return true;
    }

    //�������
    private bool HandlerOverflow(LiquidClass liquid, Vector2Int pos, float curVolume) {
        if (curVolume <= 1f) return false;
        //Һ�����
        Vector2Int upPos = pos + Vector2Int.up;
        float upVolume = liquidVolume[upPos.x, upPos.y];
        upVolume += curVolume - 1f;
        UpdateVolume(liquid, upPos, upVolume);
        MarkForUpdate(liquid, upPos);

        curVolume = 1f;
        UpdateVolume(liquid, pos, curVolume);
        MarkForUpdate(liquid, pos);
        return true;
        
    }

    // ������������Ƿ���Ч
    private void CheckFlowDirection(int x, int y, LiquidClass liquid, float curVolume, ref List<Vector2Int> dirs) {
        if (!world.CheckWorldBound(x, y)) return;
        // ��CheckFlowDirection����ӣ�
        TileClass targetLiquid = world.GetTileClass(Layers.Liquid, x, y);
        if (targetLiquid != null && targetLiquid != liquid)
            return;
        float targetVolume = liquidVolume[x, y];
        if (world.GetTileClass(Layers.Ground, x, y) != null || targetVolume >= curVolume) return;
        //����Һ���������޼�������ɢ������ˮ�����һֱ�ڼ���
        if (curVolume - targetVolume < 0.0001f) return;
        dirs.Add(new Vector2Int(x, y));
    }


    //����Һ�����
    private void UpdateVolume(LiquidClass liquid, Vector2Int pos, float volume) {
        liquidVolume[pos.x, pos.y] = volume;
        world.SetTileClass(liquid, Layers.Liquid, pos.x, pos.y);
    }

    //���Ҫ���������
    public void MarkForUpdate(LiquidClass liquid, Vector2Int pos) {
        if (!world.CheckWorldBound(pos.x, pos.y)) return;

        if (!updates.TryGetValue(liquid, out var set)) {
            throw new Exception("Һ��" + liquid.name + "δ����ע�ᣡ");
        }

        if (!set.ContainsKey(pos)) {
            set.Add(pos, 0);
        }
    }
}
