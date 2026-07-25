using UnityEngine;

/// <summary>
/// 示例配置，展示如何设置材料物理属性
/// 这个类可以作为创建 MaterialPhysicsConfig 资源的参考
/// </summary>
public class ExampleConfig : MonoBehaviour {
    /// <summary>
    /// 创建示例配置数据
    /// 在编辑器中右键点击 MaterialPhysicsConfig 资源，选择 "Create Example Data" 可以快速创建配置
    /// </summary>
    [ContextMenu("Create Example Data")]
    public void CreateExampleData() {
        MaterialPhysicsConfig config = ScriptableObject.CreateInstance<MaterialPhysicsConfig>();

        // 示例：配置油（密度最低，浮在水面上）
        var oilEntry = new MaterialPhysicsConfig.MaterialDefinitionEntry {
            materialName = "Oil",
            blockId = 1000, // 假设油的 blockId
            definition = new SimulationMaterialDefinition {
                movementMode = MaterialMovementMode.Liquid,
                density = 10, // 密度很低
                moveProbability = 1f,
                lateralProbability = 0.8f,
                horizontalSearchDistance = 6,
                canBeDisplaced = true,
                isFlammable = true,
                flammability = 0.8f,
                ignitionTemperature = 80f,
                minVolume = 0.005f,
                maxVolume = 1f,
                flowSpeed = 8f,
                horizontalSpreadDistance = 6
            }
        };

        // 示例：配置水（中等密度，浮在油下面，岩浆上面）
        var waterEntry = new MaterialPhysicsConfig.MaterialDefinitionEntry {
            materialName = "Water",
            blockId = 1001, // 假设水的 blockId
            definition = new SimulationMaterialDefinition {
                movementMode = MaterialMovementMode.Liquid,
                density = 30, // 中等密度
                moveProbability = 1f,
                lateralProbability = 0.8f,
                horizontalSearchDistance = 6,
                canBeDisplaced = true,
                isFlammable = false,
                flammability = 0f,
                ignitionTemperature = 100f,
                minVolume = 0.005f,
                maxVolume = 1f,
                flowSpeed = 10f, // 水流速度：每秒更新10次
                horizontalSpreadDistance = 6
            }
        };

        // 示例：配置毒液（密度比水高，沉在水下面）
        var poisonEntry = new MaterialPhysicsConfig.MaterialDefinitionEntry {
            materialName = "Poison",
            blockId = 1005, // 假设毒液的 blockId
            definition = new SimulationMaterialDefinition {
                movementMode = MaterialMovementMode.Liquid,
                density = 50, // 密度比水高
                moveProbability = 1f,
                lateralProbability = 0.8f,
                horizontalSearchDistance = 5,
                canBeDisplaced = true,
                isFlammable = false,
                flammability = 0f,
                ignitionTemperature = 100f,
                minVolume = 0.005f,
                maxVolume = 1f,
                flowSpeed = 9f,
                horizontalSpreadDistance = 5
            }
        };

        // 示例：配置沙子
        var sandEntry = new MaterialPhysicsConfig.MaterialDefinitionEntry {
            materialName = "Sand",
            blockId = 1002, // 假设沙子的 blockId
            definition = new SimulationMaterialDefinition {
                movementMode = MaterialMovementMode.Powder,
                density = 70,
                moveProbability = 1f,
                lateralProbability = 0f,
                horizontalSearchDistance = 1,
                canBeDisplaced = false,
                isFlammable = false,
                flammability = 0f,
                ignitionTemperature = 100f,
                minVolume = 0.005f,
                maxVolume = 1f
            }
        };

        // 示例：配置砾石
        var gravelEntry = new MaterialPhysicsConfig.MaterialDefinitionEntry {
            materialName = "Gravel",
            blockId = 1003, // 假设砾石的 blockId
            definition = new SimulationMaterialDefinition {
                movementMode = MaterialMovementMode.Powder,
                density = 80,
                moveProbability = 0.9f,
                lateralProbability = 0f,
                horizontalSearchDistance = 1,
                canBeDisplaced = false,
                isFlammable = false,
                flammability = 0f,
                ignitionTemperature = 100f,
                minVolume = 0.005f,
                maxVolume = 1f
            }
        };

        // 示例：配置岩浆（密度最高，沉在最下面）
        var lavaEntry = new MaterialPhysicsConfig.MaterialDefinitionEntry {
            materialName = "Lava",
            blockId = 1004, // 假设岩浆的 blockId
            definition = new SimulationMaterialDefinition {
                movementMode = MaterialMovementMode.Liquid,
                density = 88, // 密度最高
                moveProbability = 0.72f,
                lateralProbability = 0.45f,
                horizontalSearchDistance = 3,
                canBeDisplaced = true,
                isFlammable = false,
                flammability = 0f,
                ignitionTemperature = 160f,
                minVolume = 0.01f,
                maxVolume = 1f,
                flowSpeed = 3f, // 岩浆流速较慢：每秒更新3次
                horizontalSpreadDistance = 3
            }
        };

        config.materialEntries = new MaterialPhysicsConfig.MaterialDefinitionEntry[] {
            oilEntry,
            waterEntry,
            poisonEntry,
            sandEntry,
            gravelEntry,
            lavaEntry
        };

        // 在编辑器中保存为资源
        #if UNITY_EDITOR
        string path = "Assets/Data/Physics/MaterialPhysicsConfig.asset";
        UnityEditor.AssetDatabase.CreateAsset(config, path);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.EditorUtility.FocusProjectWindow();
        UnityEditor.Selection.activeObject = config;
        Debug.Log($"[ExampleConfig] 示例配置已创建: {path}");
        #endif
    }
}
