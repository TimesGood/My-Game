using UnityEngine;

/// <summary>
/// 噪声预览UI编辑
/// </summary>
#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(TextureBase), true, isFallback = true)]
public class TextureEditor : Editor {

    private Texture2D previewTexture;
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        TextureBase noise = (TextureBase) target;

        //添加自定义按钮，手动点击生成图像，优化性能
        if (GUILayout.Button("Force Generate")) {
            previewTexture = noise.Generator();
            Repaint();
        }

        if (previewTexture != null) {
            // 计算宽高比，避免除零
            float aspect = (noise.width > 0 && noise.height > 0) ? (float)noise.width / noise.height : 1f;
            Rect rect = GUILayoutUtility.GetAspectRect(aspect);
            EditorGUI.DrawPreviewTexture(rect, previewTexture);
        }
    }
}
#endif