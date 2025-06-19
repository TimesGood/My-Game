using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEditor.Rendering.CameraUI;

public abstract class NoiseConfig : ScriptableObject
{
    [Header("输出预览")]
    [SerializeField] protected Texture2D _noiseTexture; // 私有字段+属性封装
    public Texture2D noiseTexture => _noiseTexture;
    [Header("基础属性")]
    public int noiseWidth = 100;         // 纹理宽度
    public int noiseHeight = 100;        // 纹理高度
    public int seed = 1;                 // 种子
    public bool openGPU = true;
    public bool isBinary = true;         // 二值化

//#if UNITY_EDITOR
//    private void OnValidate() {
//        if (!EditorApplication.isPlaying) {
//            // 延迟调用避免频繁刷新
//            EditorApplication.delayCall += GenerateNoiseWithSave;
//        }
//    }
//#endif

    //初始化基础属性
    public virtual void InitValidate(int width, int height, int seed) {
        noiseWidth = width;
        noiseHeight = height;
        this.seed = seed;
        GenerateBefore();
    }

    public Texture2D InitNoise() {
        // 校验尺寸有效性
        if (noiseWidth < 1 || noiseHeight < 1) return null;
        GenerateBefore();
        if(openGPU)
            _noiseTexture = GenerateOnGPU();
        else
            _noiseTexture = GenerateNoise();

        _noiseTexture.Apply();

        
        

#if UNITY_EDITOR
        // 自动保存纹理资产
        //if (!EditorApplication.isPlaying) SaveTextureAsset();

#endif
        return _noiseTexture;
    }

    //执行生成之前
    protected virtual void GenerateBefore() {
        // 创建/重置纹理
        if (_noiseTexture == null ||
           _noiseTexture.width != noiseWidth ||
           _noiseTexture.height != noiseHeight) {
            _noiseTexture = new Texture2D(noiseWidth, noiseHeight, TextureFormat.RGBA32, false) {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
            };
        }
    }

    //执行生成
    protected abstract Texture2D GenerateNoise();


    //GPU生成
    protected virtual Texture2D GenerateOnGPU() {
        return null;
    }

    //转为Texture2D材质
    protected Texture2D ToTexture2D(RenderTexture rt) {
        TextureFormat format = SystemInfo.SupportsTextureFormat(TextureFormat.R8) ? TextureFormat.ARGB32 : TextureFormat.ARGB32;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        return tex;
    }

    //销毁生成的噪图
    public void DestroyNoiseTexture() {
        GameObject.DestroyImmediate(_noiseTexture, true);
    }


    //=================================================================================//
    //获取指定纹理坐标上的颜色
    public Color GetPixel(int x, int y) {

        return _noiseTexture.GetPixel(x, y);
    }


    #region 保存

#if UNITY_EDITOR
    private void SaveTextureAsset() {
        if (_noiseTexture == null) return;

        string path = AssetDatabase.GetAssetPath(this);

        // 如果ScriptableObject未保存，先保存它
        if (string.IsNullOrEmpty(path)) {
            string folderPath = "Assets/NoiseConfigs/";
            if (!AssetDatabase.IsValidFolder(folderPath)) {
                AssetDatabase.CreateFolder("Assets", "NoiseConfigs");
            }
            path = $"{folderPath}PerlinNoise_{Guid.NewGuid().ToString("N").Substring(0, 8)}.asset";
            AssetDatabase.CreateAsset(this, path);
        }
        // 将Texture作为子资源附加到ScriptableObject
        if (!AssetDatabase.IsSubAsset(_noiseTexture)) {
            // 删除旧的Texture子资源（如果存在）
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets) {
                if (asset is Texture2D && asset != this) {
                    DestroyImmediate(asset, true);
                }
            }

            AssetDatabase.AddObjectToAsset(_noiseTexture, this);
        }
        // 标记资源需要保存
        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(_noiseTexture);
        AssetDatabase.SaveAssets();
    }

    // 分离的延迟调用方法
    private void GenerateNoiseWithSave() {
        InitNoise();
        EditorApplication.delayCall -= GenerateNoiseWithSave;
    }
#endif

    #endregion
}
