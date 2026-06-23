using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// BiomeDefinition 自定义 Inspector：
/// Features 列表支持通过下拉菜单添加任意 BiomeFeature 子类型。
/// </summary>
[CustomEditor(typeof(BiomeDefinition))]
public class BiomeDefinitionEditor : Editor
{
    private ReorderableList _featuresList;
    private static List<System.Type> _featureTypes;

    private void OnEnable()
    {
        // 缓存所有 Feature 子类型
        if (_featureTypes == null)
        {
            _featureTypes = new List<System.Type>();
            var baseType = typeof(BiomeFeature);
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsSubclassOf(baseType) && !type.IsAbstract)
                        _featureTypes.Add(type);
                }
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 绘制默认 Inspector（基本信息）
        DrawDefaultInspectorExcept("features");

        // ── Features 自定义列表 ──
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Feature 列表（按顺序执行）", EditorStyles.boldLabel);

        var featuresProp = serializedObject.FindProperty("_features");
        if (featuresProp == null)
        {
            EditorGUILayout.HelpBox("找不到 _features 字段", MessageType.Error);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        // 绘制已添加的 Feature
        for (int i = 0; i < featuresProp.arraySize; i++)
        {
            var elem = featuresProp.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            // Feature 类型标签
            string typeName = "null";
            if (string.IsNullOrEmpty(elem.managedReferenceFullTypename))
            {
                // 空引用 → 显示下拉让用户选类型
                EditorGUILayout.LabelField($"⛔ 空引用", EditorStyles.miniLabel);
            }
            else
            {
                string shortName = elem.managedReferenceFullTypename;
                int lastDot = shortName.LastIndexOf('.') + 1;
                typeName = shortName.Substring(lastDot, shortName.Length - lastDot);
            }

            elem.isExpanded = EditorGUILayout.Foldout(elem.isExpanded, $"[{i}] {typeName}", true);
            EditorGUILayout.EndHorizontal();

            if (elem.isExpanded && elem.managedReferenceFullTypename != null)
            {
                EditorGUI.indentLevel++;
                // 递归绘制该 Feature 的所有字段
                var child = elem.Copy();
                var end = child.GetEndProperty();
                bool enter = true;
                while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
                {
                    enter = false;
                    EditorGUILayout.PropertyField(child, true);
                }
                EditorGUI.indentLevel--;
            }

            // 删除按钮
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("删除", GUILayout.Width(50)))
            {
                featuresProp.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                return; // 数组已变，直接返回避免迭代异常
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // ── 添加 Feature 下拉按钮 ──
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+ 添加 Feature", GUILayout.Width(120), GUILayout.Height(24)))
        {
            ShowAddFeatureMenu(featuresProp);
        }
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }

    private void ShowAddFeatureMenu(SerializedProperty _featuresProp)
    {
        var menu = new GenericMenu();
        foreach (var type in _featureTypes)
        {
            // 使用 [CreateAssetMenu] 的 menuName 作为显示名（如果有）
            string displayName = type.Name;
            var attrs = type.GetCustomAttributes(typeof(CreateAssetMenuAttribute), false);
            if (attrs.Length > 0) displayName = ((CreateAssetMenuAttribute)attrs[0]).menuName;

            var capturedType = type;
            menu.AddItem(new GUIContent(displayName), false, () =>
            {
                // 在数组末尾添加新元素
                int idx = _featuresProp.arraySize;
                _featuresProp.InsertArrayElementAtIndex(idx);

                var elem = _featuresProp.GetArrayElementAtIndex(idx);
                // 创建该类型的实例
                var instance = System.Activator.CreateInstance(capturedType);
                elem.managedReferenceValue = instance;

                serializedObject.ApplyModifiedProperties();
            });
        }
        menu.ShowAsContext();
    }

    /// <summary>绘制默认属性，跳过指定字段</summary>
    private void DrawDefaultInspectorExcept(string _skipField)
    {
        var prop = serializedObject.GetIterator();
        bool enter = true;
        while (prop.NextVisible(enter))
        {
            enter = false;
            if (prop.name == _skipField) continue;
            EditorGUILayout.PropertyField(prop, true);
        }
    }
}
