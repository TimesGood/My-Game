using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//µØÏÂÈºÂä
[CreateAssetMenu(fileName = "DeepBiome", menuName = "Biome/new DeepBiome")]
public class DeepBiome : BaseBiome {
    [field: SerializeField] public TileClass test { get; private set; }//
    public override IEnumerator GenerateBiome() {

        for (int x = 0; x < biomeSize.x; x++) {
            for (int y = 0; y < biomeSize.y; y++) {
                Vector2Int worldPos = LocalToWorldPos(x, y);
                if (isOutLine(x, y)) {
                    world.SetTileClass(test, test.layer, worldPos.x, worldPos.y);
                }
            
            }
        }

        yield return null;
    }
}
