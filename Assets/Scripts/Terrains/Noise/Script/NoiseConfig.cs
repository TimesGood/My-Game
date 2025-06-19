using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEditor.Rendering.CameraUI;

public abstract class NoiseConfig : ScriptableObject
{
    [Header("���Ԥ��")]
    [SerializeField] protected Texture2D _noiseTexture; // ˽���ֶ�+���Է�װ
    public Texture2D noiseTexture => _noiseTexture;
    [Header("��������")]
    public int noiseWidth = 100;         // ������
    public int noiseHeight = 100;        // ����߶�
    public int seed = 1;                 // ����
    public bool openGPU = true;
    public bool isBinary = true;         // ��ֵ��

//#if UNITY_EDITOR
//    private void OnValidate() {
//        if (!EditorApplication.isPlaying) {
//            // �ӳٵ��ñ���Ƶ��ˢ��
//            EditorApplication.delayCall += GenerateNoiseWithSave;
//        }
//    }
//#endif

    //��ʼ����������
    public virtual void InitValidate(int width, int height, int seed) {
        noiseWidth = width;
        noiseHeight = height;
        this.seed = seed;
        GenerateBefore();
    }

    public Texture2D InitNoise() {
        // У��ߴ���Ч��
        if (noiseWidth < 1 || noiseHeight < 1) return null;
        GenerateBefore();
        if(openGPU)
            _noiseTexture = GenerateOnGPU();
        else
            _noiseTexture = GenerateNoise();

        _noiseTexture.Apply();

        
        

#if UNITY_EDITOR
        // �Զ����������ʲ�
        //if (!EditorApplication.isPlaying) SaveTextureAsset();

#endif
        return _noiseTexture;
    }

    //ִ������֮ǰ
    protected virtual void GenerateBefore() {
        // ����/��������
        if (_noiseTexture == null ||
           _noiseTexture.width != noiseWidth ||
           _noiseTexture.height != noiseHeight) {
            _noiseTexture = new Texture2D(noiseWidth, noiseHeight, TextureFormat.RGBA32, false) {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
            };
        }
    }

    //ִ������
    protected abstract Texture2D GenerateNoise();


    //GPU����
    protected virtual Texture2D GenerateOnGPU() {
        return null;
    }

    //תΪTexture2D����
    protected Texture2D ToTexture2D(RenderTexture rt) {
        TextureFormat format = SystemInfo.SupportsTextureFormat(TextureFormat.R8) ? TextureFormat.ARGB32 : TextureFormat.ARGB32;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        return tex;
    }

    //�������ɵ���ͼ
    public void DestroyNoiseTexture() {
        GameObject.DestroyImmediate(_noiseTexture, true);
    }


    //=================================================================================//
    //��ȡָ�����������ϵ���ɫ
    public Color GetPixel(int x, int y) {

        return _noiseTexture.GetPixel(x, y);
    }


    #region ����

#if UNITY_EDITOR
    private void SaveTextureAsset() {
        if (_noiseTexture == null) return;

        string path = AssetDatabase.GetAssetPath(this);

        // ���ScriptableObjectδ���棬�ȱ�����
        if (string.IsNullOrEmpty(path)) {
            string folderPath = "Assets/NoiseConfigs/";
            if (!AssetDatabase.IsValidFolder(folderPath)) {
                AssetDatabase.CreateFolder("Assets", "NoiseConfigs");
            }
            path = $"{folderPath}PerlinNoise_{Guid.NewGuid().ToString("N").Substring(0, 8)}.asset";
            AssetDatabase.CreateAsset(this, path);
        }
        // ��Texture��Ϊ����Դ���ӵ�ScriptableObject
        if (!AssetDatabase.IsSubAsset(_noiseTexture)) {
            // ɾ���ɵ�Texture����Դ��������ڣ�
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets) {
                if (asset is Texture2D && asset != this) {
                    DestroyImmediate(asset, true);
                }
            }

            AssetDatabase.AddObjectToAsset(_noiseTexture, this);
        }
        // �����Դ��Ҫ����
        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(_noiseTexture);
        AssetDatabase.SaveAssets();
    }

    // ������ӳٵ��÷���
    private void GenerateNoiseWithSave() {
        InitNoise();
        EditorApplication.delayCall -= GenerateNoiseWithSave;
    }
#endif

    #endregion
}
