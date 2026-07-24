using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class TilemapLayer : MonoBehaviour
{
    public LayerType layer;//Õº≤„¿‡–Õ
    public Tilemap _tilemap { get; private set; }
    protected ChunkManager chunkManager;

    protected void Awake() {
        _tilemap = GetComponent<Tilemap>();
        chunkManager = ChunkManager.Instance;
    }
}
