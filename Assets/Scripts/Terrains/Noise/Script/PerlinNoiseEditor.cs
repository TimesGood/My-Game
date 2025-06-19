using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//自定义编辑器拓展预览图像
#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(NoiseConfig), true, isFallback = true)]
public class PerlinNoiseEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        NoiseConfig config = (NoiseConfig)target;

        //添加自定义按钮，手动点击生成图像，优化性能
        if (GUILayout.Button("Force Generate")) {
            config.InitNoise();
        }

        // 显示预览图
        if (config.noiseTexture != null) {;
            float aspect = (float)config.noiseTexture.width / config.noiseTexture.height;
            Rect rect = GUILayoutUtility.GetAspectRect(aspect);
            EditorGUI.DrawPreviewTexture(rect, config.noiseTexture);
        }
    }
}
#endif
