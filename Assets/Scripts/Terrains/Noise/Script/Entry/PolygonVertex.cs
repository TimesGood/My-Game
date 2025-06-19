using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[System.Runtime.InteropServices.StructLayout(
    System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct PolygonVertex
{
    public Vector2 position;
    public Vector2 noiseOffset;
}
