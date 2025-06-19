using System.Collections;
using System.Collections.Generic;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

public class Cell : MonoBehaviour
{
    public bool collapsed;
    public Tile[] tileOptions;
    public void CreateCell(bool collapseState, Tile[] tiles) {
        collapsed = collapseState;
        tileOptions = tiles; ;
    }

    public void RecreateCell(Tile[] tiles) {
        tileOptions = tiles;
    }
}
