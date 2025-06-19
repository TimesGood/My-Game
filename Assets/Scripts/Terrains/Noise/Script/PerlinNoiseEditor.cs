using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//�Զ���༭����չԤ��ͼ��
#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(NoiseConfig), true, isFallback = true)]
public class PerlinNoiseEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        NoiseConfig config = (NoiseConfig)target;

        //����Զ��尴ť���ֶ��������ͼ���Ż�����
        if (GUILayout.Button("Force Generate")) {
            config.InitNoise();
        }

        // ��ʾԤ��ͼ
        if (config.noiseTexture != null) {;
            float aspect = (float)config.noiseTexture.width / config.noiseTexture.height;
            Rect rect = GUILayoutUtility.GetAspectRect(aspect);
            EditorGUI.DrawPreviewTexture(rect, config.noiseTexture);
        }
    }
}
#endif
