using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

/// <summary>
/// 液体物理模拟，处理液体的流动、扩散和溢出
/// 借鉴 PixelAlchemy 的液体移动规则设计
/// </summary>
public class LiquidSimulation_Back {
    private readonly ChunkManager chunkManager;
    private readonly MaterialPhysicsConfig physicsConfig;
    private readonly System.Random random;

    // 流速控制：记录每个格子上次更新时间
    private readonly Dictionary<Vector2Int, float> lastUpdateTime = new Dictionary<Vector2Int, float>();

    // 全局速度倍率
    public float GlobalSpeedMultiplier { get; set; } = 1f;

    // 液体更新回调
    public System.Action<long, Vector2Int, float> OnUpdateVolume;

    /// <summary>
    /// 创建液体模拟实例
    /// </summary>
    /// <param name="chunkManager">区块管理器</param>
    /// <param name="physicsConfig">物理配置</param>
    /// <param name="seed">随机种子（0表示随机）</param>
    public LiquidSimulation_Back(ChunkManager chunkManager, MaterialPhysicsConfig physicsConfig, int seed = 0) {
        this.chunkManager = chunkManager;
        this.physicsConfig = physicsConfig;
        this.random = seed == 0 ? new System.Random() : new System.Random(seed);
    }

    /// <summary>
    /// 清除计时器缓存（世界重置时调用）
    /// </summary>
    public void ClearTimers() {
        lastUpdateTime.Clear();
    }

    /// <summary>
    /// 检查格子是否应该更新（基于流速控制）
    /// </summary>
    private bool ShouldUpdate(Vector2Int pos, float flowSpeed) {
        float currentTime = Time.time;
        // 应用全局速度倍率
        float effectiveSpeed = flowSpeed * Mathf.Max(0.1f, GlobalSpeedMultiplier);
        float updateInterval = 1f / Mathf.Max(0.1f, effectiveSpeed); // 防止除零

        if (lastUpdateTime.TryGetValue(pos, out float lastTime)) {
            if (currentTime - lastTime < updateInterval) {
                return false; // 还没到更新时间
            }
        }

        // 更新时间戳
        lastUpdateTime[pos] = currentTime;
        return true;
    }

    /// <summary>
    /// 处理单个液体格子的物理计算
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <param name="grid">模拟网格</param>
    /// <returns>是否发生了变化</returns>
    public bool StepCell(int x, int y, SimulationGrid grid) {
        if (!grid.InBounds(x, y)) return false;

        TileData tileData = chunkManager.GetTileData(x, y);
        if (!tileData.HasLiquid) return false;

        long liquidId = tileData.liquidId;
        float curVolume = tileData.liquidVolume;

        // 获取材料定义
        var materialDef = physicsConfig.GetDefinition(liquidId);
        if (materialDef == null || !materialDef.IsLiquid) return false;

        // 流速控制：检查是否到达更新时间
        Vector2Int pos = new Vector2Int(x, y);
        if (!ShouldUpdate(pos, materialDef.flowSpeed)) {
            // 此帧不到更新时间，放到下一帧
            grid.MarkChanged(x, y);
            return false;
        }

        // 检查最小体积阈值
        if (curVolume < materialDef.minVolume) {
            // 体积太小，移除液体
            UpdateVolume(liquidId, pos, 0f);
            return true;
        }

        // 检查是否有固体方块阻挡
        TileClass foregroundTile = chunkManager.GetTileClass(LayerType.Foreground, x, y);
        if (foregroundTile != null) {
            // 被固体阻挡，移除液体
            UpdateVolume(liquidId, pos, 0f);
            return true;
        }

        

        // 1. 尝试向下流动（优先级最高）
        if (TryFlowDown(x, y, ref curVolume, liquidId, materialDef)) {
            return true;
        }

        // 2. 尝试横向扩散（液体特性）
        if (TryDiffusion(x, y, ref curVolume, liquidId, materialDef)) {
            return true;
        }

        // 3. 尝试向上溢出（体积>1时）
        if (TryOverflow(x, y, curVolume, liquidId, materialDef)) {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 尝试向下流动
    /// </summary>
    private bool TryFlowDown(int x, int y, ref float curVolume, long liquidId, SimulationMaterialDefinition materialDef) {
        var pos = new Vector2Int(x, y);
        if (y <= 0) return false;

        Vector2Int downPos = pos + Vector2Int.down;

        // 检查下方是否可流动（有固体阻挡）
        if (chunkManager.GetTileClass(LayerType.Foreground, downPos.x, downPos.y) != null) return false;

        // 获取下方液体信息
        float downVolume = chunkManager.GetLiquidVolume(downPos);
        LiquidClass downLiquid = chunkManager.GetTileClass(LayerType.Liquid, downPos.x, downPos.y) as LiquidClass;

        // 如果下方是同种液体且已满，不流动
        if (downVolume >= materialDef.maxVolume && (downLiquid == null || downLiquid.blockId == liquidId)) return false;

        // 如果下方是不同类型的液体
        if (downLiquid != null && downLiquid.blockId != liquidId) {
            var downMaterialDef = physicsConfig.GetDefinition(downLiquid.blockId);
            if (downMaterialDef == null) return false;

            // 密度比较：只有当前液体密度大于下方液体密度时，才能下沉
            if (materialDef.density <= downMaterialDef.density) {
                return false; // 密度不够，不能下沉
            }

            UpdateVolume(liquidId, downPos, curVolume);// 交换下方液体
            UpdateVolume(downLiquid.blockId, pos, downVolume); // 交换当前液体
            return true;
        }

        // 下方是空的或同种液体，执行普通流动
        downVolume += curVolume;
        UpdateVolume(liquidId, downPos, downVolume);

        // 清空当前位置
        curVolume = 0;
        UpdateVolume(liquidId, pos, curVolume);

        return true;
    }

    /// <summary>
    /// 尝试横向扩散
    /// </summary>
    private bool TryDiffusion(int x, int y, ref float curVolume, long liquidId, SimulationMaterialDefinition materialDef) {
        var pos = new Vector2Int(x, y);
        List<Vector2Int> flowDirs = new List<Vector2Int>();
        
        // 检测可用流动方向
        Vector2Int leftDir = new Vector2Int(x - 1, y);
        Vector2Int rightDir = new Vector2Int(x + 1, y);
        if (CheckFlowDirection(rightDir, curVolume, liquidId, materialDef)) flowDirs.Add(rightDir);
        if (CheckFlowDirection(leftDir, curVolume, liquidId, materialDef)) flowDirs.Add(leftDir);
        if (flowDirs.Count == 0) return false;

        // 计算每个方向的分配量
        float avg = curVolume;
        foreach (var item in flowDirs) {
            if (liquidId != chunkManager.GetLiquidId(item)) continue;
            avg += chunkManager.GetLiquidVolume(item);
        }
        avg /= (flowDirs.Count + 1);

        // 更新当前位置
        curVolume = avg;
        
        UpdateVolume(liquidId, pos, curVolume);

        // 更新目标位置
        foreach (var dir in flowDirs) {
            LiquidClass targetLiquid = chunkManager.GetTileClass(LayerType.Liquid, dir.x, dir.y) as LiquidClass;

            // 如果目标是不同液体，需要根据密度处理
            if (targetLiquid != null && targetLiquid.blockId != liquidId) {
                var targetMaterialDef = physicsConfig.GetDefinition(targetLiquid.blockId);
                if (targetMaterialDef != null) {

                    float targetVolume = chunkManager.GetLiquidVolume(dir);
                    LiquidClass upClass = chunkManager.GetTileClass(LayerType.Liquid, dir + Vector2Int.up) as LiquidClass;
                    if (upClass != null && upClass.blockId == targetLiquid.blockId) {
                        targetVolume += chunkManager.GetLiquidVolume(dir + Vector2Int.up);
                    }
                    // 找到最上方的液体
                    UpdateVolume(targetLiquid.blockId, dir + Vector2Int.up, targetVolume);
                    UpdateVolume(liquidId, dir, avg);

                    continue;
                }
            }

            // 同种液体或空位置，直接更新
            UpdateVolume(liquidId, dir, avg);
        }

        return true;
    }

    /// <summary>
    /// 检查是否可以向指定方向流动
    /// </summary>
    private bool CheckFlowDirection(Vector2Int dir, float curVolume, long liquidId, SimulationMaterialDefinition materialDef) {
        int x = dir.x;
        int y = dir.y;
        if (!chunkManager.CheckWorldBound(x, y)) return false;

        // 检查是否有固体阻挡
        if (chunkManager.GetTileClass(LayerType.Foreground, x, y) != null) return false;

        // 获取目标位置液体信息
        TileClass targetLiquid = chunkManager.GetTileClass(LayerType.Liquid, x, y);
        float targetVolume = chunkManager.GetLiquidVolume(dir);

        // 如果目标是不同类型的液体
        if (targetLiquid != null && targetLiquid.blockId != liquidId) {
            var targetMaterialDef = physicsConfig.GetDefinition(targetLiquid.blockId);
            if (targetMaterialDef == null) return false;
            if (materialDef.density > targetMaterialDef.density) return true;
            else return false;
        }

        // 如果目标是空的或同种液体，检查体积差
        if (curVolume > targetVolume && curVolume - targetVolume > materialDef.minVolume) {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 尝试向指定方向扩散
    /// </summary>
    private bool TrySpreadInDirection(int x, int y, float curVolume, long liquidId, SimulationMaterialDefinition materialDef, int direction) {
        int maxDistance = materialDef.horizontalSearchDistance;

        for (int distance = 1; distance <= maxDistance; distance++) {
            int targetX = x + direction * distance;
            if (!chunkManager.CheckWorldBound(targetX, y)) break;

            // 检查是否有固体阻挡
            TileClass targetForeground = chunkManager.GetTileClass(LayerType.Foreground, targetX, y);
            if (targetForeground != null) break;

            // 获取目标位置信息
            TileData targetTile = chunkManager.GetTileData(targetX, y);
            float targetVolume = targetTile.liquidVolume;
            long targetLiquidId = targetTile.liquidId;

            // 如果是不同类型的液体，检查是否可以混合
            if (targetLiquidId != 0 && targetLiquidId != liquidId) {
                var targetMaterialDef = physicsConfig.GetDefinition(targetLiquidId);
                if (targetMaterialDef != null && !materialDef.canBeDisplaced) {
                    break;
                }
            }

            // 检查是否可以流动（目标体积小于当前体积）
            if (targetVolume < curVolume && curVolume - targetVolume > 0.0001f) {
                // 计算平均体积
                float totalVolume = curVolume + targetVolume;
                float avgVolume = totalVolume / 2f;

                // 更新当前位置
                UpdateVolume(liquidId, new Vector2Int(x, y), avgVolume);
                // 更新目标位置
                UpdateVolume(liquidId, new Vector2Int(targetX, y), avgVolume);

                return true;
            }

            // 如果目标位置有不同液体且不可位移，停止搜索
            if (targetLiquidId != 0 && targetLiquidId != liquidId) {
                break;
            }
        }

        return false;
    }

    /// <summary>
    /// 尝试向上溢出
    /// </summary>
    private bool TryOverflow(int x, int y, float curVolume, long liquidId, SimulationMaterialDefinition materialDef) {
        //if (curVolume <= materialDef.maxVolume) return false;
        //if (y >= chunkManager.Height - 1) return false;

        //Vector2Int upPos = new Vector2Int(x, y + 1);
        //if (!chunkManager.CheckWorldBound(upPos.x, upPos.y)) return false;

        //// 检查上方是否有固体阻挡
        //TileClass upForeground = chunkManager.GetTileClass(LayerType.Foreground, upPos.x, upPos.y);
        //if (upForeground != null) return false;

        //// 获取上方液体信息
        //TileData upTile = chunkManager.GetTileData(upPos);
        //float upVolume = upTile.liquidVolume;
        //long upLiquidId = upTile.liquidId;

        //// 如果上方是不同类型的液体，检查是否可以混合
        //if (upLiquidId != 0 && upLiquidId != liquidId) {
            //var upMaterialDef = physicsConfig.GetDefinition(upLiquidId);
            //if (upMaterialDef != null && !materialDef.canBeDisplaced) {
                //return false;
            //}
        //}

        //// 计算溢出量
        //float overflowAmount = curVolume - materialDef.maxVolume;

        //// 更新当前位置（保持最大体积）
        //UpdateVolume(liquidId, new Vector2Int(x, y), materialDef.maxVolume);
        //// 更新上方位置（添加溢出量）
        //UpdateVolume(liquidId, upPos, upVolume + overflowAmount);

        //return true;

        if (curVolume <= materialDef.maxVolume) return false;
        var pos = new Vector2Int(x, y);
        //液体溢出
        Vector2Int upPos = pos + Vector2Int.up;
        LiquidClass targetLiquid = chunkManager.GetTileClass(LayerType.Liquid, upPos.x, upPos.y) as LiquidClass;

        // 如果溢出目标不是相同液体
        if (targetLiquid != null && targetLiquid.blockId != liquidId) {
            return false;
        }
        //float upVolume = liquidHandler.liquidVolume[upPos.x, upPos.y];
        float upVolume = chunkManager.GetLiquidVolume(upPos);
        upVolume += curVolume - materialDef.maxVolume;
        UpdateVolume(liquidId, upPos, upVolume);

        curVolume = materialDef.maxVolume;
        UpdateVolume(liquidId, pos, curVolume);
        return true;
    }

    /// <summary>
    /// 更新液体体积
    /// </summary>
    private void UpdateVolume(long liquidId, Vector2Int pos, float volume) {
        OnUpdateVolume?.Invoke(liquidId, pos, volume);
    }
}
