using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

//物品
//物品类型枚举
public enum ItemType {
    Buildable,//建造物
    Material,//材料
    Equipment//装备
}
[CreateAssetMenu(fileName = "New Item Data", menuName = "Data/Item")]//在Unity编辑器中创建菜单
public class ItemData : ScriptableObject {
    public string itemId;
    public ItemType itemType;
    public string itemName;//物品名
    public Sprite icon;//物品图标

    [Range(0, 100)]
    public float dropChance;//掉落几率

    protected StringBuilder sb = new StringBuilder();

    private void OnValidate() {
        //生成物品ID（#if#endif插值语法，表示如果运行环境为编辑器时，执行代码块）
#if UNITY_EDITOR
        string path = AssetDatabase.GetAssetPath(this);
        itemId = AssetDatabase.AssetPathToGUID(path);
#endif
    }

    public virtual string GetDescription() {
        return sb.ToString();
    }
}
