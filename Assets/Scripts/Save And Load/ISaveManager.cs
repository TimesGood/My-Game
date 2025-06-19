using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISaveManager
{
    //加载数据
    void LoadData(MapData data);
    //保存数据
    void SaveData(ref MapData data);
}
