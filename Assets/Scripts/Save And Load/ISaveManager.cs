using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISaveManager
{
    //��������
    void LoadData(MapData data);
    //��������
    void SaveData(ref MapData data);
}
