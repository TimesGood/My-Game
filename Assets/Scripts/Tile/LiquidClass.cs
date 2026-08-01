using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;
using static UnityEditor.Progress;

//液体类瓦片
[CreateAssetMenu(fileName = "LiquidClass", menuName = "Tile/new LiquidClass")]
public class LiquidClass : TileClass {
    private ChunkManager chunk => ChunkManager.Instance;
    private LiquidHandler liquidHandler;
    [field: SerializeField] public TileBase[] tiles { get; private set; } // 不同水位瓦片
    [field: SerializeField] public float flowSpeed { get; private set; }//�����ٶ�
    [field: SerializeField] public float minVolume { get; private set; } = 0.005f;//��Сˮλ

    [field: SerializeField] public TileClass medium;//�벻ͬҺ������֮�����ɵ����

    //根据水位获取对应瓦片
    public TileBase GetTileToVolume(float volume) {
        // 空体积不渲染
        if (volume <= 0f) return null;

        if (volume >= 1) {
            return tiles[tiles.Length - 1];
        } else {
            int liquidIndex = Mathf.FloorToInt(volume * (tiles.Length - 1));
            liquidIndex = liquidIndex >= 0 ? liquidIndex : 0;
            // 薄液不再返回 null，使用最薄瓦片，避免小体积液体不可见/跳变
            return tiles[liquidIndex];
        }
    }

    public IEnumerator CalculatePhysicsDelay(Vector2Int pos) {
        yield return new WaitForSeconds(1f / flowSpeed);
        CalculatePhysics(pos);
        
    }

    public bool CalculatePhysics(Vector2Int pos) {
        liquidHandler = LiquidHandler.Instance;

        int x = pos.x;
        int y = pos.y;
        //float curVolume = liquidHandler.liquidVolume[pos.x, pos.y];
        float curVolume = liquidHandler.GetVolume(pos);
        

        //���̫Сʱ����������Ƭ
        if (curVolume < minVolume) {
            liquidHandler.UpdateVolume(this, pos, 0);
            return false;
        }
        //Һ���ڵ�����Ƭ�У�����
        if (chunk.GetTileClass(LayerType.Foreground, x, y) != null) {
            liquidHandler.UpdateVolume(this, pos, 0);
            return false;
        }
        // ������������
        if (TryFlowDown(pos, ref curVolume)) return true;
        
        // ��ɢ����
        if(TryDiffusion(pos, ref curVolume)) return true;

        //Һ�����
        if (TryOverflow(pos, curVolume)) return true;
        return false;
    }

    // �������������������Ƿ�ɹ�������
    private bool TryFlowDown(Vector2Int pos, ref float curVolume) {
        int x = pos.x;
        int y = pos.y;
        if (y <= 0) return false;

        Vector2Int downPos = pos + Vector2Int.down;
        // ����·��Ƿ������
        if (chunk.GetTileClass(LayerType.Foreground, downPos.x, downPos.y) != null) return false;
        //Һ������
        //float downVolume = liquidHandler.liquidVolume[downPos.x, downPos.y];
        float downVolume = liquidHandler.GetVolume(downPos);
        LiquidClass downLiquid = chunk.GetTileClass(LayerType.Liquid, downPos.x, downPos.y) as LiquidClass;

        if (downVolume >= 1f && (downLiquid == null || downLiquid == this)) return false;

        
        if (downLiquid == null || !downLiquid.TouchLiquid(pos, downPos)) {
            downVolume += curVolume;
            liquidHandler.UpdateVolume(this, downPos, downVolume);

        }

        curVolume = 0;
        liquidHandler.UpdateVolume(this, pos, curVolume);

        //������Χ���ȶ�״̬Һ�壬���¼���������Һ��Һ��
        //liquidHandler.MarkForUpdate(this, pos + Vector2Int.up);
        //liquidHandler.MarkForUpdate(this, pos + Vector2Int.left);
        //liquidHandler.MarkForUpdate(this, pos + Vector2Int.right);
        return true;
    }


    // ��ɢ����
    private bool TryDiffusion(Vector2Int pos, ref float curVolume) {
        int x = pos.x;
        int y = pos.y;
        List<Vector2Int> flowDirs = new List<Vector2Int>();
        // ��������������
        Vector2Int leftDir = pos + Vector2Int.left;
        Vector2Int rightDir = pos + Vector2Int.right;
        if (CheckFlowDirection(leftDir, curVolume)) flowDirs.Add(leftDir);
        if (CheckFlowDirection(rightDir, curVolume)) flowDirs.Add(rightDir);
        if (flowDirs.Count == 0) return false;
        // ����ÿ������ķ�����
        float avg = curVolume;
        foreach (var item in flowDirs) {
            //avg += liquidHandler.liquidVolume[item.x, item.y];
            avg += liquidHandler.GetVolume(item);
        }
        avg /= (flowDirs.Count + 1);

        //avg = Mathf.Round(avg * 10000f) / 10000f;
        curVolume = avg;
        liquidHandler.UpdateVolume(this, pos, curVolume);
        foreach (var dir in flowDirs) {
            LiquidClass targetLiquid = chunk.GetTileClass(LayerType.Liquid, dir.x, dir.y) as LiquidClass;
            if (targetLiquid != null && targetLiquid.TouchLiquid(pos, dir)) continue;
            
            liquidHandler.UpdateVolume(this, dir, avg);
        }

        //������Χ���ȶ�״̬Һ�壬���¼���������Һ��Һ��
        //liquidHandler.MarkForUpdate(this, pos + Vector2Int.up);
        //liquidHandler.MarkForUpdate(this, pos + Vector2Int.left);
        //liquidHandler.MarkForUpdate(this, pos + Vector2Int.right);
        //liquidHandler.MarkRoundForUpdate(this, pos);
        return true;
    }

    //�������
    private bool TryOverflow(Vector2Int pos, float curVolume) {
        if (curVolume <= 1f) return false;
        //Һ�����
        Vector2Int upPos = pos + Vector2Int.up;
        LiquidClass targetLiquid = chunk.GetTileClass(LayerType.Liquid, upPos.x, upPos.y) as LiquidClass;
        if (targetLiquid != null && targetLiquid != this) {
            return false;
        }
        //float upVolume = liquidHandler.liquidVolume[upPos.x, upPos.y];
        float upVolume = liquidHandler.GetVolume(upPos);
        upVolume += curVolume - 1f;
        liquidHandler.UpdateVolume(this, upPos, upVolume);

        curVolume = 1f;
        liquidHandler.UpdateVolume(this, pos, curVolume);
        return true;

    }
    private bool CheckFlowDirection(Vector2Int dir, float curVolume) {
        int x = dir.x;
        int y = dir.y;
        if (!chunk.CheckWorldBound(x, y)) return false;

        bool flag = false;
        //���Һ�岻��ͬ��������
        TileClass targetLiquid = chunk.GetTileClass(LayerType.Liquid, x, y);
        //float targetVolume = liquidHandler.liquidVolume[x, y];
        float targetVolume = liquidHandler.GetVolume(dir);
        if (targetLiquid != null && targetLiquid != this) flag = true;

        if (chunk.GetTileClass(LayerType.Foreground, x, y) == null && curVolume > targetVolume && curVolume - targetVolume > 0.0001f) flag = true;
        return flag;
    }
    //���ˮƽ���������Ƿ������
    private void CheckFlowDirection(int x, int y, float curVolume, ref List<Vector2Int> dirs) {
        if (!chunk.CheckWorldBound(x, y)) return;
        TileClass targetLiquid = chunk.GetTileClass(LayerType.Liquid, x, y);

        //float targetVolume = liquidHandler.liquidVolume[x, y];
        float targetVolume = liquidHandler.GetVolume(new Vector2Int(x, y));
        if (chunk.GetTileClass(LayerType.Foreground, x, y) != null || (targetVolume >= curVolume && (targetLiquid == null || targetLiquid == this))) return;
        //����Һ���������޼�������ɢ������ˮ�����һֱ�ڼ���
        if (curVolume - targetVolume < 0.0001f) return;
        dirs.Add(new Vector2Int(x, y));
    }


    // ������Һ��Ӵ�ʱ����һЩ�¼����������������ʣ�
    // origin���Ӵ��ߣ� target�����Ӵ���
    private bool TouchLiquid(Vector2Int origin, Vector2Int target) {
        LiquidClass originLiquid = chunk.GetTileClass(LayerType.Liquid, origin.x, origin.y) as LiquidClass;
        //����Ӵ�Ŀ�겻����ͬҺ�壬���д���
        if (originLiquid != this) {

            liquidHandler.UpdateVolume(this, target, 0);
            ChunkManager.Instance.SetBlockId(LayerType.Foreground, target, medium.blockId);
            //world.SetTileClass(liquidHandler.test, Layers.Ground, target.x, target.y);
            //world.tilemaps[(int)Layers.Ground].SetTile((Vector3Int)target, liquidHandler.test.tile);
            return true;
        }
        return false;
    }
}
