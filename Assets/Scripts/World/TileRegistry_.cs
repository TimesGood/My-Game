using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ÍßÆ¬×¢²á±í
/// </summary>
public static class TileRegistry_ {
    private static Dictionary<long, TileClass> tileDictionary = new Dictionary<long, TileClass>();
    private static Dictionary<TileClass, long> reverseLookup = new Dictionary<TileClass, long>();

    public static long RegisterTile(TileClass tile) {
        if (tile == null) return 0;

        if (reverseLookup.TryGetValue(tile, out long id))
            return id;

        tileDictionary.Add(tile.blockId, tile);
        reverseLookup.Add(tile, tile.blockId);
        return tile.blockId;
    }

    public static TileClass GetTile(long id) {
        if (id == 0) return null;
        return tileDictionary.TryGetValue(id, out var tile) ? tile : null;
    }

    public static void ClearRegistry() {
        tileDictionary.Clear();
        reverseLookup.Clear();
    }
}
