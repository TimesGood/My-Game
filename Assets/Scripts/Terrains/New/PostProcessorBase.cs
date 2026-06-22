// =============================================
//  PostProcessorBase.cs — 后处理器抽象基类
// =============================================
using UnityEngine.Tilemaps;
using UnityEngine;

public abstract class PostProcessorBase : ScriptableObject {
    public int Order = 0; // 执行顺序

    public abstract void Process(System.Random rng);
}
