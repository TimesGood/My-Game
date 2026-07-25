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

        // 示例：配置水
        var waterEntry = new MaterialPhysicsConfig.MaterialDefinitionEntry {
            materialName = "Water",
            blockId = 1001, // 假设水的 blockId
            definition = new SimulationMaterialDefinition {
                movementMode = MaterialMovementMode.Liquid,
                density = 30,
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

        // 示例：配置岩浆
        var lavaEntry = new MaterialPhysicsConfig.MaterialDefinitionEntry {
            materialName = "Lava",
            blockId = 1004, // 假设岩浆的 blockId
            definition = new SimulationMaterialDefinition {
                movementMode = MaterialMovementMode.Liquid,
                density = 88,
                moveProbability = 0.72f,
                lateralProbability = 0.45f,
                horizontalSearchDistance = 3,
                canBeDisplaced = false,
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
            waterEntry,
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
