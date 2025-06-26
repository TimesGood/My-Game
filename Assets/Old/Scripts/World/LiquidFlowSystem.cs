//using Unity.Collections;
//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Mathematics;
//using UnityEngine;
//using UnityEngine.Tilemaps;
//using static UnityEngine.EventSystems.EventTrigger;

//// ����ECS���
//public struct LiquidVolume : IComponentData {
//    public float Value;
//}

//public struct LiquidTypeData : IComponentData {
//    public int ClassID;      // Һ������ID
//    public float FlowSpeed;  // �����ٶ�
//    public float MinVolume;  // ��С�ɼ����
//    public float Density;    // �ܶȣ����ڻ�ϣ�
//}

//public struct GridPosition : IComponentData {
//    public int2 Value;
//}

//public struct DirtyFlag : IComponentData, IEnableableComponent { }

//// 2. ����ECSϵͳ
//[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
//public partial class LiquidFlowSystem : SystemBase {
//    private EntityQuery _dirtyLiquidQuery;
//    private WorldGeneration _world;
//    private Tilemap _liquidTilemap;
//    private EndSimulationEntityCommandBufferSystem _ecbSystem;
//    private NativeParallelHashMap<int2, Entity> _gridEntityMap;

//    // ���ڴ洢������Ϣ��NativeArray
//    private NativeArray<bool> _groundMap;

//    protected override void OnCreate() {
//        base.OnCreate();
//        _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

//        // ��������DirtyFlag��Һ��ʵ��
//        _dirtyLiquidQuery = GetEntityQuery(
//            ComponentType.ReadWrite<LiquidVolume>(),
//            ComponentType.ReadOnly<LiquidTypeData>(),
//            ComponentType.ReadOnly<GridPosition>(),
//            ComponentType.ReadOnly<DirtyFlag>()
//        );
//    }

//    protected override void OnStartRunning() {
//        base.OnStartRunning();
//        _world = WorldGeneration.Instance;
//        _liquidTilemap = _world.tilemaps[(int)Layers.Liquid];

//        // ��ʼ������ʵ��ӳ��
//        _gridEntityMap = new NativeParallelHashMap<int2, Entity>(
//            _world.worldWidth * _world.worldHeight,
//            Allocator.Persistent
//        );

//        // ��ʼ��������Ϣ
//        InitializeGroundMap();

//        // ��ʼ������ʵ��
//        InitializeExistingEntities();
//    }

//    protected override void OnStopRunning() {
//        base.OnStopRunning();
//        if (_gridEntityMap.IsCreated) _gridEntityMap.Dispose();
//        if (_groundMap.IsCreated) _groundMap.Dispose();
//    }

//    private void InitializeGroundMap() {
//        int size = _world.worldWidth * _world.worldHeight;
//        _groundMap = new NativeArray<bool>(size, Allocator.Persistent);

//        for (int y = 0; y < _world.worldHeight; y++) {
//            for (int x = 0; x < _world.worldWidth; x++) {
//                int index = y * _world.worldWidth + x;
//                _groundMap[index] = _world.GetTileClass(Layers.Ground, x, y) != null;
//            }
//        }
//    }

//    private void InitializeExistingEntities() {
//        EntityManager entityManager = EntityManager;

//        Entities.ForEach((Entity entity, in GridPosition gridPos) => {
//            _gridEntityMap.TryAdd(gridPos.Value, entity);
//        }).Run();
//    }

//    protected override void OnUpdate() {
//        if (!LiquidHandler.Instance.openFlow) return;

//        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
//        var gridMap = _gridEntityMap;
//        var groundMap = _groundMap;
//        int worldWidth = _world.worldWidth;
//        // ����Һ������
//        Entities
//            .WithName("ProcessLiquidFlow")
//            .WithAll<DirtyFlag>()
//            .ForEach((Entity entity, int entityInQueryIndex,
//                ref LiquidVolume volume, in LiquidTypeData liquidType, in GridPosition gridPos) => {
//                    int2 pos = gridPos.Value;
//                    int index = pos.y * worldWidth + pos.x;

//                    // ���̫С���Ƴ�
//                    if (volume.Value < liquidType.MinVolume) {
//                        volume.Value = 0;
//                        ecb.RemoveComponent<LiquidVolume>(entityInQueryIndex, entity);
//                        ecb.DestroyEntity(entityInQueryIndex, entity);
//                        gridMap.Remove(pos);
//                        return;
//                    }

//                    // ������������
//                    if (!TryFlowDown(ref volume, in liquidType, in gridPos, gridMap, groundMap, worldWidth, ecb, entityInQueryIndex)) {
//                        // ��ɢ����
//                        DistributePressure(ref volume, in liquidType, in gridPos, gridMap, groundMap, worldWidth, ecb, entityInQueryIndex);
//                    }

//                    // Һ���������
//                    HandleOverflow(ref volume, in liquidType, in gridPos, gridMap, groundMap, worldWidth, ecb, entityInQueryIndex);

//                    // �����ǣ������ȶ��������±�ǣ�
//                    ecb.SetComponentEnabled<DirtyFlag>(entityInQueryIndex, entity, false);

//                }).ScheduleParallel();

//        // �ȶ�״̬���
//        Entities
//            .WithName("CheckStableState")
//            .WithAll<LiquidVolume>()
//            .ForEach((Entity entity, int entityInQueryIndex,
//                in LiquidVolume volume, in GridPosition gridPos) => {
//                    if (IsStable(volume.Value, gridPos.Value, groundMap, worldWidth)) {
//                        ecb.SetComponentEnabled<DirtyFlag>(entityInQueryIndex, entity, false);
//                    }
//                }).ScheduleParallel();

//        // ��Ⱦϵͳ
//        Entities
//            .WithName("RenderLiquidTiles")
//            .WithChangeFilter<LiquidVolume>()
//            .WithoutBurst() // ��Ҫ�����йܶ���
//            .ForEach((in LiquidVolume volume, in LiquidTypeData liquidType, in GridPosition gridPos) => {
//                Vector3Int tilePos = new Vector3Int(gridPos.Value.x, gridPos.Value.y, 0);
//                LiquidClass liquidClass = LiquidTypeRegistry.Instance.GetLiquidClass(liquidType.ClassID);

//                if (liquidClass == null || volume.Value <= 0) {
//                    _liquidTilemap.SetTile(tilePos, null);
//                } else {
//                    TileBase newTile = liquidClass.GetTile(volume.Value);
//                    TileBase currentTile = _liquidTilemap.GetTile(tilePos);

//                    if (newTile != currentTile) {
//                        _liquidTilemap.SetTile(tilePos, newTile);
//                    }
//                }
//            }).Run(); // ���������߳�����

//        _ecbSystem.AddJobHandleForProducer(Dependency);
//    }

//    private bool TryFlowDown(ref LiquidVolume volume, in LiquidTypeData liquidType, in GridPosition gridPos,
//        NativeParallelHashMap<int2, Entity> gridMap, NativeArray<bool> groundMap, int worldWidth,
//        EntityCommandBuffer.ParallelWriter ecb, int sortKey) {
//        int2 pos = gridPos.Value;
//        int2 downPos = new int2(pos.x, pos.y - 1);

//        // �߽���
//        if (downPos.y < 0 || downPos.x < 0 || downPos.x >= worldWidth)
//            return false;

//        // ����·��Ƿ��е���
//        int downIndex = downPos.y * worldWidth + downPos.x;
//        if (downIndex >= 0 && downIndex < groundMap.Length && groundMap[downIndex])
//            return false;

//        // ��ȡ�·�Һ��ʵ��
//        if (!gridMap.TryGetValue(downPos, out Entity downEntity)) {
//            // ������Һ��ʵ��
//            downEntity = ecb.CreateEntity(sortKey);
//            ecb.AddComponent(sortKey, downEntity, new LiquidVolume { Value = 0 });
//            ecb.AddComponent(sortKey, downEntity, new LiquidTypeData {
//                ClassID = liquidType.ClassID,
//                FlowSpeed = liquidType.FlowSpeed,
//                MinVolume = liquidType.MinVolume
//            });
//            ecb.AddComponent(sortKey, downEntity, new GridPosition { Value = downPos });
//            ecb.AddComponent<DirtyFlag>(sortKey, downEntity);
//            gridMap.Add(downPos, downEntity);
//        }

//        // ��ȡ�·�Һ�����
//        LiquidVolume downVolume = GetComponent<LiquidVolume>(downEntity);

//        // ����Ƿ�����
//        if (downVolume.Value >= 1f)
//            return false;

//        // ��������
//        float flowAmount = math.min(volume.Value, 1f - downVolume.Value);
//        volume.Value -= flowAmount;
//        downVolume.Value += flowAmount;

//        // �������
//        ecb.SetComponent(sortKey, downEntity, downVolume);
//        ecb.SetComponent(sortKey, gridMap.Entity, volume);

//        // ��Ǹ���
//        ecb.SetComponentEnabled<DirtyFlag>(sortKey, downEntity, true);
//        MarkNeighborsDirty(downPos, ecb, sortKey, gridMap);

//        return true;
//    }

//    private void DistributePressure(ref LiquidVolume volume, in LiquidTypeData liquidType, in GridPosition gridPos,
//        NativeParallelHashMap<int2, Entity> gridMap, EntityCommandBuffer.ParallelWriter ecb, int sortKey) {
//        int2 pos = gridPos.Value;
//        NativeList<int2> flowDirs = new NativeList<int2>(Allocator.Temp);

//        // �����������
//        CheckFlowDirection(new int2(pos.x - 1, pos.y), liquidType.ClassID, volume.Value, gridMap, ref flowDirs);
//        CheckFlowDirection(new int2(pos.x + 1, pos.y), liquidType.ClassID, volume.Value, gridMap, ref flowDirs);

//        if (flowDirs.Length == 0) return;

//        // ����ƽ�����
//        float total = volume.Value;
//        foreach (int2 dir in flowDirs) {
//            if (gridMap.TryGetValue(dir, out Entity dirEntity)) {
//                total += GetComponent<LiquidVolume>(dirEntity).Value;
//            }
//        }

//        float avg = total / (flowDirs.Length + 1);

//        // ���õ�ǰ���
//        volume.Value = avg;
//        ecb.SetComponent(sortKey, gridPos.Entity, volume);

//        // ������Χ���
//        foreach (int2 dir in flowDirs) {
//            if (gridMap.TryGetValue(dir, out Entity dirEntity)) {
//                ecb.SetComponent(sortKey, dirEntity, new LiquidVolume { Value = avg });
//                ecb.SetComponentEnabled<DirtyFlag>(sortKey, dirEntity, true);
//            } else {
//                // ������ʵ��
//                Entity newEntity = ecb.CreateEntity(sortKey);
//                ecb.AddComponent(sortKey, newEntity, new LiquidVolume { Value = avg });
//                ecb.AddComponent(sortKey, newEntity, new LiquidTypeData {
//                    ClassID = liquidType.ClassID,
//                    FlowSpeed = liquidType.FlowSpeed,
//                    MinVolume = liquidType.MinVolume
//                });
//                ecb.AddComponent(sortKey, newEntity, new GridPosition { Value = dir });
//                ecb.AddComponent<DirtyFlag>(sortKey, newEntity);
//                gridMap.Add(dir, newEntity);
//            }
//        }

//        // ����ھӸ���
//        MarkNeighborsDirty(pos, ecb, sortKey, gridMap);
//        flowDirs.Dispose();
//    }

//    private void CheckFlowDirection(int2 pos, int liquidClassID, float curVolume, NativeParallelHashMap<int2, Entity> gridMap, ref NativeList<int2> dirs) {
//        if (!_world.CheckWorldBound(pos.x, pos.y)) return;

//        // ������
//        if (_world.GetTileClass(Layers.Ground, pos.x, pos.y) != null) return;

//        // ���Һ������
//        if (gridMap.TryGetValue(pos, out Entity entity)) {
//            LiquidTypeData type = GetComponent<LiquidTypeData>(entity);
//            if (type.ClassID != liquidClassID) return;

//            LiquidVolume volume = GetComponent<LiquidVolume>(entity);
//            if (volume.Value >= curVolume) return;
//        }

//        dirs.Add(pos);
//    }

//    private void HandleOverflow(ref LiquidVolume volume, in LiquidTypeData liquidType, in GridPosition gridPos,
//        NativeParallelHashMap<int2, Entity> gridMap, EntityCommandBuffer.ParallelWriter ecb, int sortKey) {
//        int2 pos = gridPos.Value;
//        int2 topPos = new int2(pos.x, pos.y + 1);

//        if (volume.Value <= 1f) return;
//        if (!_world.CheckWorldBound(topPos.x, topPos.y)) return;
//        if (_world.GetTileClass(Layers.Ground, topPos.x, topPos.y) != null) return;

//        float overflow = volume.Value - 1f;
//        volume.Value = 1f;
//        ecb.SetComponent(sortKey, gridPos.Entity, volume);

//        // ��ȡ�Ϸ�ʵ��
//        if (!gridMap.TryGetValue(topPos, out Entity topEntity)) {
//            topEntity = ecb.CreateEntity(sortKey);
//            ecb.AddComponent(sortKey, topEntity, new LiquidVolume { Value = overflow });
//            ecb.AddComponent(sortKey, topEntity, new LiquidTypeData {
//                ClassID = liquidType.ClassID,
//                FlowSpeed = liquidType.FlowSpeed,
//                MinVolume = liquidType.MinVolume
//            });
//            ecb.AddComponent(sortKey, topEntity, new GridPosition { Value = topPos });
//            ecb.AddComponent<DirtyFlag>(sortKey, topEntity);
//            gridMap.Add(topPos, topEntity);
//        } else {
//            LiquidVolume topVolume = GetComponent<LiquidVolume>(topEntity);
//            topVolume.Value += overflow;
//            ecb.SetComponent(sortKey, topEntity, topVolume);
//            ecb.SetComponentEnabled<DirtyFlag>(sortKey, topEntity, true);
//        }

//        MarkNeighborsDirty(topPos, ecb, sortKey, gridMap);
//    }

//    private void MarkNeighborsDirty(int2 pos, EntityCommandBuffer.ParallelWriter ecb, int sortKey,
//        NativeParallelHashMap<int2, Entity> gridMap) {
//        MarkDirty(new int2(pos.x, pos.y + 1), ecb, sortKey, gridMap); // ��
//        MarkDirty(new int2(pos.x, pos.y - 1), ecb, sortKey, gridMap); // ��
//        MarkDirty(new int2(pos.x - 1, pos.y), ecb, sortKey, gridMap); // ��
//        MarkDirty(new int2(pos.x + 1, pos.y), ecb, sortKey, gridMap); // ��
//    }

//    private void MarkDirty(int2 pos, EntityCommandBuffer.ParallelWriter ecb, int sortKey,
//        NativeParallelHashMap<int2, Entity> gridMap) {
//        if (gridMap.TryGetValue(pos, out Entity entity)) {
//            ecb.SetComponentEnabled<DirtyFlag>(sortKey, entity, true);
//        }
//    }

//    private bool IsStable(float volume, int2 pos, WorldGeneration world) {
//        if (volume < 1f) return false;

//        // ������ܵ��ȶ���
//        bool top = world.GetTileClass(Layers.Liquid, pos.x, pos.y + 1) != null ||
//                   world.GetTileClass(Layers.Ground, pos.x, pos.y + 1) != null;

//        bool bottom = (world.GetTileClass(Layers.Liquid, pos.x, pos.y - 1) != null) ||
//                      world.GetTileClass(Layers.Ground, pos.x, pos.y - 1) != null;

//        bool left = (world.GetTileClass(Layers.Liquid, pos.x - 1, pos.y) != null) ||
//                    world.GetTileClass(Layers.Ground, pos.x - 1, pos.y) != null;

//        bool right = (world.GetTileClass(Layers.Liquid, pos.x + 1, pos.y) != null) ||
//                     world.GetTileClass(Layers.Ground, pos.x + 1, pos.y) != null;

//        return top && bottom && left && right;
//    }
//}

