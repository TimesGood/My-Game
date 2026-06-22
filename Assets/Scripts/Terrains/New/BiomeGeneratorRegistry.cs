// =============================================
//  BiomeGeneratorRegistry.cs — 生成器注册表
//  按 Id 建立 Dictionary，供 MapGenerator 查表
// =============================================
using System.Collections.Generic;

public class BiomeGeneratorRegistry {
    private readonly Dictionary<string, BiomeGeneratorBase> _map = new();

    public BiomeGeneratorRegistry(BiomeGeneratorBase[] generators) {
        foreach (var g in generators)
            _map.TryAdd(g.Id, g);
    }

    public BiomeGeneratorBase Get(string id) {
        if (_map.TryGetValue(id, out var gen)) return gen;
        throw new System.Exception($"[MapGen] 未找到 GeneratorId={id} 的生成器");
    }

    public bool Has(string id) => _map.ContainsKey(id);
}
