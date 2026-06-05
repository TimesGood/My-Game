using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace BuildingSystem.Modles {

    [Serializable]
    public class Buildable {
        [field: SerializeField]
        public Tilemap ParentTilemap { get; private set; }

        [field: SerializeField]
        public TileClass BuildableType { get; private set; }

        [field: SerializeField]
        public GameObject GameObject { get; private set; }

        [field: SerializeField]
        public Vector3Int Coordinates { get; private set; }

        public Buildable(TileClass type, Vector3Int coords, Tilemap tilemap, GameObject gameObject = null) {
            ParentTilemap = tilemap;
            BuildableType = type;
            GameObject = gameObject;
            Coordinates = coords;
        }

        public void Destroy() {
            if (GameObject != null) {
                Object.Destroy(GameObject);
            }
            ParentTilemap.SetTile(Coordinates, null);
        }
    }
}

